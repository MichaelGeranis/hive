import { useState } from 'react'
import {
  Crown,
  ExternalLink,
  Forward,
  Plus,
  Save,
  Trash2,
  UserPlus,
  X
} from 'lucide-react'
import { quarterlyPlanningApi } from '../services/api'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useConfirm } from '../contexts/ConfirmContext'
import {
  DECISION_STATUSES,
  DEPENDENCY_STATUS_LABELS,
  STATUS_FLOW,
  STATUS_LABELS,
  STATUS_STYLES,
  WORK_TYPE_LABELS,
  formatEffort,
  isLane,
  canBeSized,
  describeSize,
  priorityLabel,
  sizeOptions,
  toUpdateDto
} from '../utils/planningDisplay'
import { ExternalDependencyStatus, InitiativeKind, InitiativeRole, InitiativeStatus, WorkType } from '../types/quarterlyPlanning'
import type { Initiative, PlanningBoard } from '../types/quarterlyPlanning'
import type { RunChange } from '../utils/planningDisplay'

interface OptionDrawerProps {
  board: PlanningBoard
  initiative: Initiative
  run: RunChange
  onClose: () => void
}

/**
 * Everything about one option: its solution design (size, products), the decision,
 * who leads it, and what it needs from other teams.
 */
export default function OptionDrawer({ board, initiative, run, onClose }: OptionDrawerProps) {
  const confirm = useConfirm()
  // While a confirm dialog is open, Escape answers it and leaves the drawer open
  const [asking, setAsking] = useState(false)
  useEscapeKey(onClose, !asking)
  const lane = isLane(initiative)

  return (
    <div className="fixed inset-0 z-40 flex justify-end bg-black/20" onClick={onClose}>
      <div
        className="w-full max-w-2xl h-full bg-white shadow-2xl overflow-y-auto"
        role="dialog"
        aria-label={initiative.name}
        onClick={e => e.stopPropagation()}
      >
        <div className="sticky top-0 bg-white border-b border-slate-200 px-6 py-4 flex items-start justify-between z-10">
          <div className="flex items-center gap-3">
            <span className="px-2 py-1 rounded-sm text-white text-xs font-semibold" style={{ backgroundColor: initiative.color }}>
              {priorityLabel(initiative)}
            </span>
            <div>
              <h2 className="text-lg font-semibold text-slate-900">{initiative.name}</h2>
              <p className="text-xs text-slate-500">
                {lane ? 'Standing lane' : initiative.kind === InitiativeKind.Leftover ? 'Leftover from last quarter' : 'Option'} ·{' '}
                {WORK_TYPE_LABELS[initiative.workType]}
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-1 rounded-sm hover:bg-slate-100" aria-label="Close">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-6 py-4 space-y-6">
          {!lane && <ForecastStrip initiative={initiative} board={board} />}
          {!lane && <DecisionSection initiative={initiative} run={run} />}
          {/* Re-keyed so a status change (which can clear the size) resets the form */}
          <DetailsSection key={`${initiative.status}-${initiative.tshirtSize}`} board={board} initiative={initiative} run={run} />
          {!lane && <PeopleSection board={board} initiative={initiative} run={run} />}
          {!lane && <ExternalDependenciesSection board={board} initiative={initiative} run={run} />}

          <div className="flex flex-wrap gap-2 pt-4 border-t border-slate-100">
            {!lane && (
              <button
                onClick={() => run(() => quarterlyPlanningApi.carryOver(initiative.id), `${initiative.name} was carried over to next quarter`)}
                className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
                title="Copy to next quarter as a leftover, with its open dependencies"
              >
                <Forward className="w-4 h-4" /> Carry over to next quarter
              </button>
            )}
            <button
              onClick={async () => {
                setAsking(true)
                const confirmed = await confirm({
                  title: `Delete ${initiative.name}?`,
                  message: 'Its allocations and dependencies go too.',
                  confirmLabel: 'Delete'
                })
                setAsking(false)
                if (!confirmed) return
                onClose()
                run(() => quarterlyPlanningApi.deleteInitiative(initiative.id))
              }}
              className="flex items-center gap-2 px-3 py-2 text-sm text-red-600 border border-red-200 rounded-lg hover:bg-red-50 ml-auto"
            >
              <Trash2 className="w-4 h-4" /> Delete
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

function Section({ title, hint, children }: { title: string; hint?: string; children: React.ReactNode }) {
  return (
    <section>
      <h3 className="text-sm font-semibold text-slate-800">{title}</h3>
      {hint && <p className="text-xs text-slate-500 mb-2">{hint}</p>}
      <div className={hint ? '' : 'mt-2'}>{children}</div>
    </section>
  )
}

function ForecastStrip({ initiative, board }: { initiative: Initiative; board: PlanningBoard }) {
  const forecast = initiative.forecast
  const row = board.waterline.rows.find(r => r.initiativeId === initiative.id)
  const covered = forecast?.coveredPercent ?? 0
  return (
    <div className="grid grid-cols-4 gap-3 text-center">
      <Stat label="Size" value={initiative.tshirtSize ? `${initiative.tshirtSize} · ${formatEffort(initiative.effort)}` : 'Not sized'} />
      <Stat label="Planned" value={formatEffort(forecast?.allocated)} />
      <Stat
        label="Done by"
        value={forecast?.doneSprintName ?? (forecast?.coveredPercent != null ? `${covered}% this Q` : '—')}
        tone={forecast?.deliveredInQuarter ? 'good' : forecast && forecast.allocated > 0 ? 'warn' : undefined}
      />
      <Stat label="Waterline" value={row ? (row.aboveLine ? 'Fits' : 'Below the line') : '—'} tone={row ? (row.aboveLine ? 'good' : 'warn') : undefined} />
    </div>
  )
}

function Stat({ label, value, tone }: { label: string; value: string; tone?: 'good' | 'warn' }) {
  const color = tone === 'good' ? 'text-green-700' : tone === 'warn' ? 'text-amber-700' : 'text-slate-800'
  return (
    <div className="rounded-lg bg-slate-50 px-2 py-2">
      <p className="text-[11px] uppercase tracking-wide text-slate-500">{label}</p>
      <p className={`text-sm font-semibold ${color}`}>{value}</p>
    </div>
  )
}

function DecisionSection({ initiative, run }: { initiative: Initiative; run: RunChange }) {
  const [note, setNote] = useState(initiative.decisionNote)
  const change = (status: number) => {
    const needsNote = DECISION_STATUSES.includes(status)
    run(() => quarterlyPlanningApi.changeStatus(initiative.id, status, needsNote ? note : undefined))
  }

  return (
    <Section title="Status and decision" hint="In discovery → In Solution Design → Ready for commitment, then commit or drop it and say why.">
      <div className="flex flex-wrap gap-1.5">
        {STATUS_FLOW.map(status => (
          <button
            key={status}
            onClick={() => change(status)}
            disabled={status === initiative.status && status !== InitiativeStatus.Committed}
            className={`text-xs px-2.5 py-1 rounded-full border ${
              status === initiative.status ? `${STATUS_STYLES[status]} border-transparent font-semibold` : 'border-slate-200 text-slate-600 hover:bg-slate-50'
            }`}
          >
            {STATUS_LABELS[status]}
          </button>
        ))}
      </div>
      <input
        value={note}
        onChange={e => setNote(e.target.value)}
        placeholder="Why it is committed or dropped…"
        aria-label="Decision note"
        className="mt-2 w-full px-3 py-2 text-sm border border-slate-300 rounded-lg"
      />
    </Section>
  )
}

function DetailsSection({ board, initiative, run }: { board: PlanningBoard; initiative: Initiative; run: RunChange }) {
  const lane = isLane(initiative)
  const [form, setForm] = useState(() => ({
    ...toUpdateDto(initiative),
    productIds: initiative.products.map(p => p.id)
  }))
  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) => setForm(f => ({ ...f, [key]: value }))
  const sizes = sizeOptions(board)

  const toggleProduct = (id: string) =>
    set('productIds', form.productIds.includes(id) ? form.productIds.filter(p => p !== id) : [...form.productIds, id])

  return (
    <Section title={lane ? 'Lane' : 'Solution design'}>
      <div className="grid grid-cols-2 gap-3 text-sm">
        <label className="col-span-2">
          <span className="text-slate-600">Name</span>
          <input value={form.name} onChange={e => set('name', e.target.value)} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg" />
        </label>
        {!lane && (
          <>
            <label>
              <span className="text-slate-600">T-shirt size</span>
              {canBeSized(initiative.status) ? (
                <select value={form.tshirtSize ?? ''} onChange={e => set('tshirtSize', e.target.value || null)} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg bg-white">
                  <option value="">Not sized yet</option>
                  {sizes.map(s => <option key={s} value={s}>{describeSize(board, s)}</option>)}
                </select>
              ) : (
                <p className="mt-1 px-3 py-2 text-slate-400 border border-dashed border-slate-200 rounded-lg">Sized once it is ready for commitment</p>
              )}
            </label>
            <label>
              <span className="text-slate-600">Work type</span>
              <select value={form.workType} onChange={e => set('workType', Number(e.target.value))} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg bg-white">
                {[WorkType.ProductRoadmap, WorkType.TechRoadmap, WorkType.Maintenance].map(t => (
                  <option key={t} value={t}>{WORK_TYPE_LABELS[t]}</option>
                ))}
              </select>
            </label>
            <label>
              <span className="text-slate-600">Kind</span>
              <select value={form.kind} onChange={e => set('kind', Number(e.target.value))} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg bg-white">
                <option value={InitiativeKind.Option}>Option</option>
                <option value={InitiativeKind.Leftover}>Leftover (priority 0)</option>
              </select>
            </label>
            <label>
              <span className="text-slate-600">Arrived mid-quarter on</span>
              <input type="date" value={form.arrivedOn ?? ''} onChange={e => set('arrivedOn', e.target.value || null)} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg" />
            </label>
            <label>
              <span className="text-slate-600">Due date</span>
              <input type="date" value={form.dueDate ?? ''} onChange={e => set('dueDate', e.target.value || null)} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg" />
            </label>
            <label className="col-span-2">
              <span className="text-slate-600">Solution design link</span>
              <div className="flex gap-2 mt-1">
                <input value={form.url ?? ''} onChange={e => set('url', e.target.value)} placeholder="https://…" className="flex-1 px-3 py-2 border border-slate-300 rounded-lg" />
                {initiative.url && (
                  <a href={initiative.url} target="_blank" rel="noreferrer" className="p-2 border border-slate-300 rounded-lg hover:bg-slate-50" title="Open">
                    <ExternalLink className="w-4 h-4" />
                  </a>
                )}
              </div>
            </label>
          </>
        )}
        <label className="col-span-2">
          <span className="text-slate-600">Notes</span>
          <textarea value={form.description ?? ''} onChange={e => set('description', e.target.value)} rows={3} className="mt-1 w-full px-3 py-2 border border-slate-300 rounded-lg" />
        </label>
      </div>

      {!lane && (
        <>
          <p className="text-sm text-slate-600 mt-3 mb-1">Products it touches</p>
          {board.products.length === 0 ? (
            <p className="text-xs text-slate-400">No products yet: sync them from Jira on the Products page.</p>
          ) : (
            <div className="flex flex-wrap gap-1.5 max-h-28 overflow-auto">
              {board.products.map(p => (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => toggleProduct(p.id)}
                  className={`text-xs px-2 py-1 rounded-full border ${form.productIds.includes(p.id) ? 'bg-amber-100 border-amber-300 text-amber-900' : 'border-slate-200 text-slate-600 hover:bg-slate-50'}`}
                >
                  {p.name}
                </button>
              ))}
            </div>
          )}
        </>
      )}

      <button
        onClick={() => run(() => quarterlyPlanningApi.updateInitiative(initiative.id, form), 'Saved')}
        className="mt-3 flex items-center gap-2 px-3 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600"
      >
        <Save className="w-4 h-4" /> Save details
      </button>
    </Section>
  )
}

function PeopleSection({ board, initiative, run }: { board: PlanningBoard; initiative: Initiative; run: RunChange }) {
  const [personId, setPersonId] = useState('')
  const memberIds = new Set(initiative.members.map(m => m.directReportId))
  const add = (id: string, role: number) => run(() => quarterlyPlanningApi.addMember(initiative.id, id, role))

  return (
    <Section
      title="People"
      hint="Who leads it and who contributes. Suggestions come from the knowledge matrix and the tasks people did on its products."
    >
      <ul className="space-y-1">
        {initiative.members.map(member => (
          <li key={member.id} className="flex items-center gap-2 text-sm">
            <button
              onClick={() => run(() => quarterlyPlanningApi.updateMember(member.id, member.role === InitiativeRole.Lead ? InitiativeRole.Contributor : InitiativeRole.Lead))}
              title={member.role === InitiativeRole.Lead ? 'Lead; click to make a contributor' : 'Make lead'}
              className={member.role === InitiativeRole.Lead ? 'text-amber-500' : 'text-slate-300 hover:text-amber-400'}
            >
              <Crown className="w-4 h-4" />
            </button>
            <span className="font-medium text-slate-800">{member.directReportName}</span>
            <span className="text-xs text-slate-500">{member.role === InitiativeRole.Lead ? 'Lead' : 'Contributor'}</span>
            {member.knowledgeLevel > 0 && <span className="text-xs text-slate-500">· knowledge {member.knowledgeLevel}/5</span>}
            <button onClick={() => run(() => quarterlyPlanningApi.removeMember(member.id))} className="ml-auto text-slate-400 hover:text-red-600" aria-label={`Remove ${member.directReportName}`}>
              <X className="w-4 h-4" />
            </button>
          </li>
        ))}
        {initiative.members.length === 0 && <li className="text-sm text-slate-400">Nobody yet.</li>}
      </ul>

      {initiative.suggestions.filter(s => !s.isMember).length > 0 && (
        <div className="mt-3">
          <p className="text-xs uppercase tracking-wide text-slate-500 mb-1">Suggested</p>
          <ul className="space-y-1">
            {initiative.suggestions.filter(s => !s.isMember).map(s => (
              <li key={s.directReportId} className="flex items-center gap-2 text-sm">
                <span className="text-slate-800">{s.name}</span>
                <span className="text-xs text-slate-500">
                  {s.knowledgeLevel > 0 && `knowledge ${s.knowledgeLevel}/5 · `}
                  {s.taskCount > 0 && `${s.taskCount} tasks · `}
                  {formatEffort(s.freeEffort)} dev-sprints free
                </span>
                <button onClick={() => add(s.directReportId, InitiativeRole.Lead)} className="ml-auto text-xs px-2 py-0.5 rounded-sm border border-amber-300 text-amber-800 hover:bg-amber-50">
                  Lead
                </button>
                <button onClick={() => add(s.directReportId, InitiativeRole.Contributor)} className="text-xs px-2 py-0.5 rounded-sm border border-slate-300 text-slate-700 hover:bg-slate-50">
                  Contributor
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="flex gap-2 mt-3">
        <select value={personId} onChange={e => setPersonId(e.target.value)} aria-label="Add a person" className="flex-1 px-3 py-2 text-sm border border-slate-300 rounded-lg bg-white">
          <option value="">Add someone…</option>
          {board.people.filter(p => !memberIds.has(p.id)).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
        </select>
        <button
          disabled={!personId}
          onClick={() => {
            add(personId, initiative.members.some(m => m.role === InitiativeRole.Lead) ? InitiativeRole.Contributor : InitiativeRole.Lead)
            setPersonId('')
          }}
          className="flex items-center gap-1 px-3 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
        >
          <UserPlus className="w-4 h-4" /> Add
        </button>
      </div>
    </Section>
  )
}

function ExternalDependenciesSection({ board, initiative, run }: { board: PlanningBoard; initiative: Initiative; run: RunChange }) {
  const [team, setTeam] = useState('')
  const [need, setNeed] = useState('')

  return (
    <Section title="Other teams" hint="What it needs from another team (CORE, CS, Research…) and whether it is agreed.">
      <ul className="space-y-1.5">
        {initiative.externalDependencies.map(d => (
          <li key={d.id} className="flex items-center gap-2 text-sm">
            <span className="font-medium text-slate-800 w-24 truncate">{d.team}</span>
            <span className="flex-1 text-slate-600 truncate" title={d.need}>{d.need || '—'}</span>
            <select
              value={d.status}
              onChange={e => run(() => quarterlyPlanningApi.updateExternalDependency(d.id, { team: d.team, need: d.need, status: Number(e.target.value) }))}
              aria-label={`Status of ${d.team}`}
              className="px-2 py-1 text-xs border border-slate-200 rounded-sm bg-white"
            >
              {Object.entries(DEPENDENCY_STATUS_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
            <button onClick={() => run(() => quarterlyPlanningApi.deleteExternalDependency(d.id))} className="text-slate-400 hover:text-red-600" aria-label={`Remove ${d.team}`}>
              <X className="w-4 h-4" />
            </button>
          </li>
        ))}
      </ul>
      <div className="flex gap-2 mt-2">
        <input list="planning-known-teams" value={team} onChange={e => setTeam(e.target.value)} placeholder="Team" aria-label="Team" className="w-32 px-3 py-2 text-sm border border-slate-300 rounded-lg" />
        <datalist id="planning-known-teams">
          {board.knownTeams.map(t => <option key={t} value={t} />)}
        </datalist>
        <input value={need} onChange={e => setNeed(e.target.value)} placeholder="What is needed" aria-label="What is needed" className="flex-1 px-3 py-2 text-sm border border-slate-300 rounded-lg" />
        <button
          disabled={!team.trim()}
          onClick={() => run(async () => {
            await quarterlyPlanningApi.addExternalDependency(initiative.id, { team: team.trim(), need: need.trim(), status: ExternalDependencyStatus.NeedsCoordination })
            setTeam('')
            setNeed('')
          })}
          className="px-3 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
          aria-label="Add team dependency"
        >
          <Plus className="w-4 h-4" />
        </button>
      </div>
    </Section>
  )
}
