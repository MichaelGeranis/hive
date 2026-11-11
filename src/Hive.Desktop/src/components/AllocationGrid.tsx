import { useEffect, useMemo, useRef, useState } from 'react'
import { Eraser, Loader2, MousePointer2, Sparkles, X } from 'lucide-react'
import { Card, CardContent, CardHeader } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { Modal, ModalCancelButton } from './Modal'
import { formatDate } from '../utils/dateUtils'
import {
  WORK_TYPE_COLORS,
  WORK_TYPE_LABELS,
  formatEffort,
  isLane,
  leadOf,
  mixEntries,
  paintCell
} from '../utils/planningDisplay'
import type { Brush } from '../utils/planningDisplay'
import { InitiativeStatus } from '../types/quarterlyPlanning'
import type { Allocation, Cell, CellShare, Initiative, PlanDraft, PlanningBoard } from '../types/quarterlyPlanning'
import type { RunChange } from '../utils/planningDisplay'

interface AllocationGridProps {
  board: PlanningBoard
  run: RunChange
  onOpen: (initiativeId: string) => void
}

type View = 'options' | 'worktype'

const cellKey = (personId: string, sprintId: string) => `${personId}:${sprintId}`

/**
 * People by sprint, as in the Plan-Catalogue: each cell says who works on what, for which
 * share, and how much of the sprint the person is there for.
 */
export default function AllocationGrid({ board, run, onOpen }: AllocationGridProps) {
  const { showError } = useToast()
  const [brush, setBrush] = useState<Brush>(null)
  const [view, setView] = useState<View>('options')
  // The cells painted since the mouse went down: kept in a ref so every mouse event sees the
  // latest, and mirrored in state so the grid shows them before they are saved
  const pendingRef = useRef<Map<string, CellShare[]>>(new Map())
  const [pending, setPending] = useState<Map<string, CellShare[]>>(new Map())
  const [editing, setEditing] = useState<{ personId: string; sprintId: string } | null>(null)
  const [draft, setDraft] = useState<PlanDraft | null>(null)
  const [drafting, setDrafting] = useState(false)
  const painting = useRef(false)

  const initiatives = useMemo(() => new Map(board.initiatives.map(i => [i.id, i])), [board.initiatives])
  const palette = board.initiatives.filter(i => i.status !== InitiativeStatus.Dropped && i.status !== InitiativeStatus.Delivered)

  const cells = useMemo(() => {
    const map = new Map<string, Allocation[]>()
    for (const a of board.allocations) {
      const key = cellKey(a.directReportId, a.sprintId)
      map.set(key, [...(map.get(key) ?? []), a])
    }
    return map
  }, [board.allocations])

  const sharesOf = (personId: string, sprintId: string): CellShare[] => {
    const key = cellKey(personId, sprintId)
    return pendingRef.current.get(key) ?? pending.get(key) ?? (cells.get(key) ?? []).map(a => ({ initiativeId: a.initiativeId, percent: a.percent }))
  }

  const paint = (personId: string, sprintId: string) => {
    const next = new Map(pendingRef.current)
    next.set(cellKey(personId, sprintId), paintCell(sharesOf(personId, sprintId), brush))
    pendingRef.current = next
    setPending(next)
  }

  // Painting ends wherever the mouse is released; the painted cells are saved in one call
  useEffect(() => {
    const finish = () => {
      if (!painting.current) return
      painting.current = false
      const painted = pendingRef.current
      pendingRef.current = new Map()
      if (painted.size === 0) return
      const payload: Cell[] = Array.from(painted.entries()).map(([key, shares]) => {
        const [directReportId, sprintId] = key.split(':')
        return { directReportId, sprintId, shares }
      })
      run(() => quarterlyPlanningApi.setCells(board.quarter.id, payload)).finally(() => setPending(new Map()))
    }
    window.addEventListener('mouseup', finish)
    return () => window.removeEventListener('mouseup', finish)
  }, [board.quarter.id, run])

  const openDraft = async () => {
    try {
      setDrafting(true)
      setDraft(await quarterlyPlanningApi.draft(board.quarter.id, false))
    } catch (err) {
      showError(getErrorMessage(err))
    } finally {
      setDrafting(false)
    }
  }

  const colorOf = (initiativeId: string) => {
    const initiative = initiatives.get(initiativeId)
    if (!initiative) return '#94A3B8'
    return view === 'worktype' ? WORK_TYPE_COLORS[initiative.workType] : initiative.color
  }

  const labelOf = (initiativeId: string) => {
    const initiative = initiatives.get(initiativeId)
    if (!initiative) return '?'
    return view === 'worktype' ? WORK_TYPE_LABELS[initiative.workType] : initiative.name
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader
          title="Allocation"
          subtitle="Pick a brush, then click or drag across cells. Without a brush, click a cell to set its shares exactly."
          action={
            <div className="flex items-center gap-2">
              <div className="flex rounded-lg border border-slate-300 overflow-hidden text-sm">
                {(['options', 'worktype'] as View[]).map(v => (
                  <button key={v} onClick={() => setView(v)} className={`px-3 py-1.5 ${view === v ? 'bg-slate-800 text-white' : 'bg-white text-slate-600 hover:bg-slate-50'}`}>
                    {v === 'options' ? 'By option' : 'By work type'}
                  </button>
                ))}
              </div>
              <button
                onClick={openDraft}
                disabled={drafting}
                className="flex items-center gap-2 px-3 py-1.5 text-sm bg-violet-600 text-white rounded-lg hover:bg-violet-700 disabled:opacity-50"
                title="Fill the free cells: options above the line first, then the lanes"
              >
                {drafting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
                Draft plan
              </button>
            </div>
          }
        />

        {/* Brushes */}
        <div className="px-6 py-3 border-b border-slate-100 flex flex-wrap items-center gap-2 bg-slate-50">
          <button
            onClick={() => setBrush(null)}
            className={`flex items-center gap-1 text-xs px-2 py-1 rounded-full border ${brush === null ? 'bg-slate-800 text-white border-slate-800' : 'bg-white border-slate-300'}`}
          >
            <MousePointer2 className="w-3 h-3" /> Select
          </button>
          <button
            onClick={() => setBrush('erase')}
            className={`flex items-center gap-1 text-xs px-2 py-1 rounded-full border ${brush === 'erase' ? 'bg-slate-800 text-white border-slate-800' : 'bg-white border-slate-300'}`}
          >
            <Eraser className="w-3 h-3" /> Clear
          </button>
          <span className="w-px h-5 bg-slate-300 mx-1" />
          {palette.map(i => (
            <button
              key={i.id}
              onClick={() => setBrush(i.id)}
              className={`flex items-center gap-1.5 text-xs px-2 py-1 rounded-full border ${brush === i.id ? 'ring-2 ring-offset-1 ring-slate-800 border-transparent' : 'border-slate-300 bg-white'}`}
            >
              <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: i.color }} />
              {i.name}
            </button>
          ))}
        </div>

        <div className="overflow-x-auto select-none">
          <table className="w-full text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 text-slate-600">
                <th className="px-3 py-2 text-left w-40">Person</th>
                {board.sprints.map(s => {
                  const capacity = board.capacity.sprints.find(c => c.sprintId === s.id)
                  return (
                    <th key={s.id} className="px-2 py-2 text-left font-medium min-w-36">
                      <div className="text-slate-800">{s.name}</div>
                      {s.startDate && s.endDate && <div className="font-normal text-slate-400">{formatDate(s.startDate)}–{formatDate(s.endDate)}</div>}
                      {capacity && (
                        <>
                          <div className="font-normal text-slate-500 mt-0.5">
                            {formatEffort(capacity.allocated)}/{formatEffort(capacity.supply)} planned
                          </div>
                          <div className="flex h-1.5 mt-1 rounded-sm overflow-hidden bg-slate-200">
                            {mixEntries(capacity.mix).map(m => m.percent > 0 && (
                              <div key={m.workType} style={{ width: `${m.percent}%`, backgroundColor: WORK_TYPE_COLORS[m.workType] }} title={`${WORK_TYPE_LABELS[m.workType]} ${m.percent}%`} />
                            ))}
                          </div>
                        </>
                      )}
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody>
              {board.roster.map(member => (
                <tr key={member.directReportId} className="border-t border-slate-100">
                  <td className="px-3 py-1.5 text-slate-800 font-medium">
                    {member.name}
                    {member.capacityPercent < 100 && <span className="text-slate-400 font-normal"> · {member.capacityPercent}%</span>}
                  </td>
                  {member.sprints.map(ms => {
                    const shares = sharesOf(member.directReportId, ms.sprintId)
                    const total = shares.reduce((sum, s) => sum + s.percent, 0)
                    return (
                      <td
                        key={ms.sprintId}
                        className={`relative p-0.5 align-top cursor-pointer ${ms.availablePercent < 100 ? 'border-b-2 border-b-red-400' : ''}`}
                        onMouseDown={e => {
                          e.preventDefault()
                          if (brush === null) {
                            setEditing({ personId: member.directReportId, sprintId: ms.sprintId })
                            return
                          }
                          painting.current = true
                          paint(member.directReportId, ms.sprintId)
                        }}
                        onMouseEnter={() => painting.current && paint(member.directReportId, ms.sprintId)}
                        title={ms.leaveDays ? `${ms.leaveDays} working days off: ${ms.availablePercent}% available` : undefined}
                      >
                        <div className="flex h-10 rounded-sm overflow-hidden bg-slate-50 border border-slate-100">
                          {shares.map(s => (
                            <div
                              key={s.initiativeId}
                              className="flex items-center justify-center px-1 text-[11px] text-white overflow-hidden"
                              style={{ width: `${s.percent}%`, backgroundColor: colorOf(s.initiativeId) }}
                            >
                              <span className="truncate">
                                {labelOf(s.initiativeId)}{shares.length > 1 || s.percent < 100 ? ` ${s.percent}%` : ''}
                              </span>
                            </div>
                          ))}
                          {total < 100 && total > 0 && <div className="flex-1" />}
                        </div>
                        {ms.availablePercent < 100 && (
                          <span className="absolute top-0 right-0.5 text-[10px] font-semibold text-red-600 bg-white/80 rounded-sm px-0.5">
                            {ms.availablePercent}%
                          </span>
                        )}
                      </td>
                    )
                  })}
                </tr>
              ))}
              <tr className="border-t border-slate-200 bg-slate-50">
                <td className="px-3 py-2 text-slate-600 font-medium align-top">Notes</td>
                {board.sprints.map(s => (
                  <td key={s.id} className="p-1 align-top">
                    <textarea
                      defaultValue={s.notes}
                      onBlur={e => e.target.value !== s.notes && run(() => quarterlyPlanningApi.setSprintNotes(board.quarter.id, s.id, e.target.value))}
                      rows={2}
                      aria-label={`Notes for ${s.name}`}
                      placeholder="e.g. Euterpe BE ready"
                      className="w-full px-1.5 py-1 text-[11px] border border-slate-200 rounded-sm bg-white resize-y"
                    />
                  </td>
                ))}
              </tr>
            </tbody>
          </table>
          {board.roster.length === 0 && <p className="px-6 py-6 text-sm text-slate-500">Nobody is on this quarter's plan. Add people on the Capacity step.</p>}
          {board.sprints.length === 0 && <p className="px-6 py-6 text-sm text-amber-700">This quarter has no sprints yet.</p>}
        </div>
      </Card>

      <Forecasts board={board} onOpen={onOpen} />

      {editing && (
        <CellEditor
          board={board}
          personId={editing.personId}
          sprintId={editing.sprintId}
          shares={sharesOf(editing.personId, editing.sprintId)}
          onClose={() => setEditing(null)}
          onSave={shares => {
            setEditing(null)
            run(() => quarterlyPlanningApi.setCells(board.quarter.id, [{ directReportId: editing.personId, sprintId: editing.sprintId, shares }]))
          }}
        />
      )}

      {draft && (
        <DraftPreview
          draft={draft}
          board={board}
          onClose={() => setDraft(null)}
          onApply={() => {
            setDraft(null)
            run(() => quarterlyPlanningApi.draft(board.quarter.id, true), 'Draft applied')
          }}
        />
      )}
    </div>
  )
}

function Forecasts({ board, onOpen }: { board: PlanningBoard; onOpen: (id: string) => void }) {
  const options = board.initiatives.filter(i => !isLane(i) && i.status !== InitiativeStatus.Dropped)
  const lanes = board.initiatives.filter(isLane)
  const aboveLine = new Set(board.waterline.rows.filter(r => r.aboveLine).map(r => r.initiativeId))

  return (
    <Card>
      <CardHeader title="What the plan delivers" subtitle="Planned dev-sprints against each option's size, and the sprint it is covered by." />
      <CardContent className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="text-xs text-slate-500">
            <tr>
              <th className="text-left py-1">Option</th>
              <th className="text-left">Lead</th>
              <th className="text-right">Size</th>
              <th className="text-right">Planned</th>
              <th className="w-48 px-3">Covered</th>
              <th className="text-left">Done by</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {options.map(option => <ForecastRow key={option.id} option={option} aboveLine={aboveLine.has(option.id)} onOpen={onOpen} />)}
            {lanes.map(lane => {
              const reservation = board.capacity.reservations.find(r => r.workType === lane.workType)
              const covered = reservation?.effort ? Math.round((lane.allocatedEffort / reservation.effort) * 100) : 0
              return (
                <tr key={lane.id} className="text-slate-600">
                  <td className="py-1.5">
                    <button onClick={() => onOpen(lane.id)} className="flex items-center gap-2 hover:underline">
                      <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: lane.color }} />
                      {lane.name}
                    </button>
                  </td>
                  <td />
                  <td className="text-right tabular-nums">{formatEffort(reservation?.effort)}</td>
                  <td className="text-right tabular-nums">{formatEffort(lane.allocatedEffort)}</td>
                  <td className="px-3"><CoverageBar percent={covered} /></td>
                  <td className="text-xs text-slate-500">{covered}% of its reservation</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </CardContent>
    </Card>
  )
}

function ForecastRow({ option, aboveLine, onOpen }: { option: Initiative; aboveLine: boolean; onOpen: (id: string) => void }) {
  const forecast = option.forecast
  const covered = forecast?.coveredPercent ?? 0
  return (
    <tr>
      <td className="py-1.5">
        <button onClick={() => onOpen(option.id)} className="flex items-center gap-2 hover:underline text-slate-800">
          <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: option.color }} />
          {option.name}
          {!aboveLine && option.status !== InitiativeStatus.Committed && <span className="text-[10px] text-slate-400">below line</span>}
        </button>
      </td>
      <td className="text-slate-600">{leadOf(option)?.directReportName ?? <span className="text-amber-600 text-xs">no lead</span>}</td>
      <td className="text-right tabular-nums">{option.tshirtSize ? `${option.tshirtSize} · ${formatEffort(option.effort)}` : '—'}</td>
      <td className="text-right tabular-nums">{formatEffort(forecast?.allocated)}</td>
      <td className="px-3"><CoverageBar percent={covered} /></td>
      <td className={forecast?.deliveredInQuarter ? 'text-green-700' : forecast && forecast.allocated > 0 ? 'text-amber-700' : 'text-slate-400'}>
        {forecast?.doneSprintName ?? (forecast && forecast.allocated > 0 ? `not this quarter (${covered}%)` : '—')}
      </td>
    </tr>
  )
}

function CoverageBar({ percent }: { percent: number }) {
  const color = percent >= 100 ? 'bg-green-500' : percent >= 50 ? 'bg-amber-400' : 'bg-red-400'
  return (
    <div className="h-2 bg-slate-100 rounded-sm" title={`${percent}%`}>
      <div className={`h-2 rounded-sm ${color}`} style={{ width: `${Math.min(100, percent)}%` }} />
    </div>
  )
}

function CellEditor({
  board,
  personId,
  sprintId,
  shares,
  onClose,
  onSave
}: {
  board: PlanningBoard
  personId: string
  sprintId: string
  shares: CellShare[]
  onClose: () => void
  onSave: (shares: CellShare[]) => void
}) {
  const [list, setList] = useState(shares)
  const [adding, setAdding] = useState('')
  const member = board.roster.find(r => r.directReportId === personId)
  const sprint = board.sprints.find(s => s.id === sprintId)
  const availability = member?.sprints.find(s => s.sprintId === sprintId)
  const total = list.reduce((sum, s) => sum + s.percent, 0)
  const options = board.initiatives.filter(i => i.status !== InitiativeStatus.Dropped && !list.some(s => s.initiativeId === i.id))

  return (
    <Modal
      title={`${member?.name} · ${sprint?.name}`}
      subtitle={availability && availability.availablePercent < 100 && (
        <span className="text-xs text-red-600">{availability.leaveDays} working days off: {availability.availablePercent}% available</span>
      )}
      onClose={onClose}
      size="sm"
      closeOnBackdrop
      footer={
        <>
          <ModalCancelButton onClick={onClose} />
          <button onClick={() => onSave(list)} className="px-4 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600">Save</button>
        </>
      }
    >
      <ul className="space-y-2">
        {list.map(share => {
          const initiative = board.initiatives.find(i => i.id === share.initiativeId)
          return (
            <li key={share.initiativeId} className="flex items-center gap-2 text-sm">
              <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: initiative?.color }} />
              <span className="flex-1 truncate">{initiative?.name}</span>
              <input
                type="number"
                min={1}
                max={100}
                step={5}
                value={share.percent}
                onChange={e => setList(l => l.map(s => s.initiativeId === share.initiativeId ? { ...s, percent: Number(e.target.value) } : s))}
                aria-label={`Share of ${initiative?.name}`}
                className="w-16 px-2 py-1 border border-slate-300 rounded-sm"
              />
              %
              <button onClick={() => setList(l => l.filter(s => s.initiativeId !== share.initiativeId))} aria-label={`Remove ${initiative?.name}`} className="text-slate-400 hover:text-red-600">
                <X className="w-4 h-4" />
              </button>
            </li>
          )
        })}
      </ul>
      <div className="flex gap-2 mt-3">
        <select value={adding} onChange={e => setAdding(e.target.value)} aria-label="Add an initiative" className="flex-1 px-2 py-1.5 text-sm border border-slate-300 rounded-sm bg-white">
          <option value="">Add…</option>
          {options.map(i => <option key={i.id} value={i.id}>{i.name}</option>)}
        </select>
        <button
          disabled={!adding}
          onClick={() => {
            setList(l => [...l, { initiativeId: adding, percent: Math.max(5, 100 - total) }])
            setAdding('')
          }}
          className="px-3 py-1.5 text-sm border border-slate-300 rounded-sm disabled:opacity-40"
        >
          Add
        </button>
      </div>
      <p className={`text-xs mt-2 ${total > 100 ? 'text-red-600' : 'text-slate-500'}`}>{total}% of the sprint</p>
    </Modal>
  )
}

function DraftPreview({ draft, board, onClose, onApply }: { draft: PlanDraft; board: PlanningBoard; onClose: () => void; onApply: () => void }) {
  const byInitiative = new Map<string, typeof draft.allocations>()
  for (const a of draft.allocations) byInitiative.set(a.initiativeId, [...(byInitiative.get(a.initiativeId) ?? []), a])
  const improved = draft.options.filter(o => o.after.allocated > o.before.allocated)

  return (
    <Modal
      title="Draft plan"
      subtitle={
        <>
          Fills only the free cells: options above the line (or committed) in priority order, their lead first, never taking the
          lanes' share of a sprint; then the lanes towards the target mix. Nothing already planned changes.
        </>
      }
      onClose={onClose}
      size="lg"
      closeOnBackdrop
      footer={
        <>
          <ModalCancelButton onClick={onClose} />
          <button
            onClick={onApply}
            disabled={draft.allocations.length === 0}
            className="px-4 py-2 text-sm bg-violet-600 text-white rounded-lg hover:bg-violet-700 disabled:opacity-50"
          >
            Apply {draft.allocations.length} allocation{draft.allocations.length === 1 ? '' : 's'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {draft.allocations.length === 0 ? (
          <p className="text-sm text-slate-500">Nothing to add: the free cells are already used, or no option above the line needs more.</p>
        ) : (
          <>
            {improved.length > 0 && (
              <table className="w-full text-sm">
                <thead className="text-xs text-slate-500"><tr><th className="text-left">Option</th><th className="text-left">Now</th><th className="text-left">With the draft</th></tr></thead>
                <tbody>
                  {improved.map(o => (
                    <tr key={o.initiativeId} className="border-t border-slate-100">
                      <td className="py-1">{o.name}</td>
                      <td>{o.before.doneSprintName ?? `${o.before.coveredPercent ?? 0}%`}</td>
                      <td className={o.after.deliveredInQuarter ? 'text-green-700' : 'text-amber-700'}>{o.after.doneSprintName ?? `${o.after.coveredPercent ?? 0}%`}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            {Array.from(byInitiative.entries()).map(([initiativeId, allocations]) => {
              const initiative = board.initiatives.find(i => i.id === initiativeId)
              return (
                <div key={initiativeId}>
                  <p className="flex items-center gap-2 text-sm font-medium text-slate-800">
                    <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: initiative?.color }} />
                    {allocations[0].initiativeName}
                  </p>
                  <p className="text-xs text-slate-600 ml-4">
                    {allocations.map(a => `${a.directReportName} ${a.sprintName}${a.percent < 100 ? ` ${a.percent}%` : ''}`).join(' · ')}
                  </p>
                </div>
              )
            })}
          </>
        )}
      </div>
    </Modal>
  )
}
