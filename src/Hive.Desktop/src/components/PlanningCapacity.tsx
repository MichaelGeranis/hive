import { useState } from 'react'
import { History, X } from 'lucide-react'
import { Card, CardContent, CardHeader } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import { useConfirm } from '../contexts/ConfirmContext'
import { formatDate } from '../utils/dateUtils'
import {
  STATUS_LABELS,
  STATUS_STYLES,
  WORK_TYPE_COLORS,
  WORK_TYPE_LABELS,
  formatEffort,
  mixEntries,
  priorityLabel
} from '../utils/planningDisplay'
import { WorkType } from '../types/quarterlyPlanning'
import type { MixShares, PlanningBoard } from '../types/quarterlyPlanning'
import type { RunChange } from '../utils/planningDisplay'

interface PlanningCapacityProps {
  board: PlanningBoard
  run: RunChange
  onOpen: (initiativeId: string) => void
}

/**
 * What the team can take on: who is on the plan and how available they are, the target mix
 * against the plan and last quarter, and the waterline of options against what is left.
 */
export default function PlanningCapacity({ board, run, onOpen }: PlanningCapacityProps) {
  return (
    <div className="space-y-4">
      <CapacityTiles board={board} />
      <Waterline board={board} run={run} onOpen={onOpen} />
      <MixCard board={board} run={run} />
      <Roster board={board} run={run} />
    </div>
  )
}

function CapacityTiles({ board }: { board: PlanningBoard }) {
  const { capacity, waterline } = board
  const maintenance = capacity.reservations.find(r => r.workType === 0)
  const tech = capacity.reservations.find(r => r.workType === 2)
  const over = waterline.committedEffort > waterline.capacity
  const tiles = [
    { label: 'Team supply', value: formatEffort(capacity.totalSupply), note: 'dev-sprints after leave' },
    { label: 'Maintenance', value: formatEffort(maintenance?.effort), note: `${maintenance?.percent ?? 0}% reserved` },
    { label: 'Tech roadmap', value: formatEffort(tech?.effort), note: `${tech?.percent ?? 0}% reserved` },
    { label: 'For options', value: formatEffort(waterline.capacity), note: 'what the waterline fills' },
    {
      label: 'Committed',
      value: formatEffort(waterline.committedEffort),
      note: over ? 'more than there is room for' : `of ${formatEffort(waterline.capacity)}`,
      tone: over ? 'text-red-600' : 'text-slate-900'
    }
  ]

  return (
    <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
      {tiles.map(tile => (
        <div key={tile.label} className="bg-white rounded-xl border border-slate-200 px-4 py-3">
          <p className="text-xs uppercase tracking-wide text-slate-500">{tile.label}</p>
          <p className={`text-2xl font-semibold ${tile.tone ?? 'text-slate-900'}`}>{tile.value}</p>
          <p className="text-xs text-slate-500">{tile.note}</p>
        </div>
      ))}
    </div>
  )
}

function Waterline({ board, run, onOpen }: PlanningCapacityProps) {
  const { waterline } = board
  const [dragged, setDragged] = useState<string | null>(null)
  const max = Math.max(waterline.capacity, ...waterline.rows.map(r => r.cumulative), 1)
  const linePosition = (waterline.capacity / max) * 100
  const ranked = waterline.rows.filter(r => r.kind !== 1)

  const drop = (targetId: string) => {
    if (!dragged || dragged === targetId) return
    const ids = ranked.map(r => r.initiativeId).filter(id => id !== dragged)
    ids.splice(ids.indexOf(targetId), 0, dragged)
    setDragged(null)
    run(() => quarterlyPlanningApi.reorder(board.quarter.id, ids))
  }

  return (
    <Card>
      <CardHeader
        title="Waterline"
        subtitle={`Options in priority order against ${formatEffort(waterline.capacity)} dev-sprints for options. Above the line fits; drag to reprioritise.`}
      />
      <CardContent>
        {waterline.rows.length === 0 ? (
          <p className="text-sm text-slate-500">No options yet.</p>
        ) : (
          <ol className="space-y-1">
            {waterline.rows.map((row, index) => {
              const start = ((row.cumulative - (row.effort ?? 0)) / max) * 100
              const width = ((row.effort ?? 0) / max) * 100
              const lineAfter = row.aboveLine && !waterline.rows[index + 1]?.aboveLine
              return (
                <li key={row.initiativeId}>
                  <div
                    className="flex items-center gap-3 text-sm py-1 rounded hover:bg-slate-50 cursor-pointer"
                    draggable={row.kind !== 1}
                    onDragStart={() => setDragged(row.initiativeId)}
                    onDragOver={e => e.preventDefault()}
                    onDrop={() => drop(row.initiativeId)}
                    onClick={() => onOpen(row.initiativeId)}
                  >
                    <span className="w-8 text-center text-xs font-semibold text-white rounded" style={{ backgroundColor: row.color }}>
                      {priorityLabel({ kind: row.kind, rank: row.rank, arrivedOn: null })}
                    </span>
                    <span className={`w-56 truncate ${row.aboveLine ? 'text-slate-800' : 'text-slate-400'}`}>{row.name}</span>
                    <span className="w-16 text-xs text-slate-500">{row.tshirtSize ? `${row.tshirtSize} · ${formatEffort(row.effort)}` : 'not sized'}</span>
                    <div className="relative flex-1 h-5 bg-slate-50 rounded">
                      <div
                        className="absolute top-0 h-5 rounded"
                        style={{ left: `${start}%`, width: `${Math.max(width, 0.5)}%`, backgroundColor: row.color, opacity: row.aboveLine ? 0.9 : 0.35 }}
                      />
                      <div className="absolute top-[-4px] bottom-[-4px] border-l-2 border-dashed border-blue-500" style={{ left: `${linePosition}%` }} />
                    </div>
                    <span className="w-12 text-right text-xs tabular-nums text-slate-500">{formatEffort(row.cumulative)}</span>
                    <span className={`w-20 text-center text-[11px] px-1.5 py-0.5 rounded-full ${STATUS_STYLES[row.status]}`}>{STATUS_LABELS[row.status]}</span>
                  </div>
                  {lineAfter && (
                    <div className="flex items-center gap-2 my-1 text-[11px] font-medium text-blue-700">
                      <div className="flex-1 border-t-2 border-dashed border-blue-400" />
                      commit line · {formatEffort(waterline.capacity)} dev-sprints
                      <div className="flex-1 border-t-2 border-dashed border-blue-400" />
                    </div>
                  )}
                </li>
              )
            })}
          </ol>
        )}
      </CardContent>
    </Card>
  )
}

// The Mix card calls the tech roadmap tech optimisation
const MIX_LABELS: Record<number, string> = { ...WORK_TYPE_LABELS, [WorkType.TechRoadmap]: 'Tech optimisation' }

function MixBar({ label, mix }: { label: string; mix: MixShares }) {
  return (
    <div className="flex items-center gap-3 text-sm">
      <span className="w-28 text-slate-600">{label}</span>
      <div className="flex-1 flex h-5 rounded overflow-hidden bg-slate-100">
        {mixEntries(mix).map(entry => entry.percent > 0 && (
          <div
            key={entry.workType}
            className="flex items-center justify-center text-[11px] text-white"
            style={{ width: `${entry.percent}%`, backgroundColor: WORK_TYPE_COLORS[entry.workType] }}
            title={`${MIX_LABELS[entry.workType]} ${entry.percent}%`}
          >
            {entry.percent >= 8 && `${entry.percent}%`}
          </div>
        ))}
      </div>
    </div>
  )
}

function MixCard({ board, run }: { board: PlanningBoard; run: RunChange }) {
  const { mix, quarter } = board
  const [product, setProduct] = useState(quarter.productRoadmapPercent)
  const [tech, setTech] = useState(quarter.techRoadmapPercent)
  const [maintenance, setMaintenance] = useState(quarter.maintenancePercent)
  const total = product + tech + maintenance
  const changed = product !== quarter.productRoadmapPercent || tech !== quarter.techRoadmapPercent || maintenance !== quarter.maintenancePercent
  const average = mix.pastWork
  const useAverage = () => {
    if (!average) return
    setProduct(average.productRoadmap)
    setTech(average.techRoadmap)
    setMaintenance(average.maintenance)
  }

  return (
    <Card>
      <CardHeader
        title="Mix"
        subtitle="Product roadmap / tech optimisation / maintenance: the target, the plan and last quarter."
        action={
          <button
            onClick={useAverage}
            disabled={!average}
            title={
              average
                ? `The split of the team's ${mix.pastWorkStoryPoints} story points so far, by the Dashboard's work categories; save it to keep it`
                : 'No task has story points yet'
            }
            className="flex items-center gap-2 px-3 py-1.5 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
          >
            <History className="w-4 h-4" /> Use the team's average
          </button>
        }
      />
      <CardContent>
        <div className="flex flex-wrap items-end gap-3 mb-4">
          {[
            { label: 'Product', value: product, set: setProduct },
            { label: 'Tech', value: tech, set: setTech },
            { label: 'Maintenance', value: maintenance, set: setMaintenance }
          ].map(field => (
            <label key={field.label} className="text-sm text-slate-600">
              {field.label} %
              <input
                type="number"
                min={0}
                max={100}
                value={field.value}
                onChange={e => field.set(Number(e.target.value))}
                className="mt-1 block w-20 px-2 py-1.5 border border-slate-300 rounded-lg"
              />
            </label>
          ))}
          <span className={`text-sm ${total === 100 ? 'text-slate-500' : 'text-red-600'}`}>= {total}%</span>
          {changed && (
            <button
              onClick={() => run(() => quarterlyPlanningApi.updateQuarter(quarter.id, {
                okrReference: quarter.okrReference,
                productRoadmapPercent: product,
                techRoadmapPercent: tech,
                maintenancePercent: maintenance
              }), 'Mix target saved')}
              className="px-3 py-1.5 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600"
            >
              Save target
            </button>
          )}
        </div>

        <div className="space-y-2">
          <MixBar label="Target" mix={mix.target} />
          {mix.hasPlan ? <MixBar label="This plan" mix={mix.planned} /> : <p className="text-xs text-slate-400">Nothing allocated yet.</p>}
          {mix.previousQuarter && <MixBar label="Last quarter" mix={mix.previousQuarter} />}
        </div>
        {mix.recentUnplannedPercent != null && (
          <p className={`mt-3 text-xs ${mix.recentUnplannedPercent > mix.target.maintenance ? 'text-amber-700' : 'text-slate-500'}`}>
            Last quarter {mix.recentUnplannedPercent}% of {mix.recentTaskCount} tickets were bugs, support or maintenance
            {mix.recentUnplannedPercent > mix.target.maintenance
              ? `: reserving ${mix.target.maintenance}% for maintenance may be optimistic.`
              : '.'}
          </p>
        )}
        <div className="flex gap-3 mt-3 text-xs text-slate-500">
          {[1, 2, 0].map(t => (
            <span key={t} className="flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded-sm" style={{ backgroundColor: WORK_TYPE_COLORS[t] }} />
              {MIX_LABELS[t]}
            </span>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}

function Roster({ board, run }: { board: PlanningBoard; run: RunChange }) {
  const confirm = useConfirm()
  const [adding, setAdding] = useState('')
  const onRoster = new Set(board.roster.map(r => r.directReportId))
  const available = board.people.filter(p => !onRoster.has(p.id))
  const quarterId = board.quarter.id

  return (
    <Card>
      <CardHeader
        title="Team this quarter"
        subtitle="Planned capacity per person (100% full-time), and availability per sprint after leave and public holidays."
      />
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-xs text-slate-600">
            <tr>
              <th className="px-4 py-2 text-left">Person</th>
              <th className="px-2 py-2 text-left">Capacity</th>
              {board.sprints.map(s => (
                <th key={s.id} className="px-2 py-2 text-center font-medium">
                  <div>{s.name}</div>
                  {s.startDate && s.endDate && <div className="font-normal text-slate-400">{formatDate(s.startDate)}–{formatDate(s.endDate)}</div>}
                </th>
              ))}
              <th className="px-2 py-2 text-right" title="Dev-days: one person for one working day">Dev-days</th>
              <th />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {board.roster.map(member => (
              <tr key={member.directReportId}>
                <td className="px-4 py-2 text-slate-800">{member.name}{!member.isDirect && <span className="text-xs text-slate-400 ml-1">(not direct)</span>}</td>
                <td className="px-2 py-2">
                  <select
                    value={member.capacityPercent}
                    onChange={e => run(() => quarterlyPlanningApi.setRosterMember(quarterId, member.directReportId, Number(e.target.value)))}
                    aria-label={`Capacity of ${member.name}`}
                    className="px-1.5 py-1 border border-slate-200 rounded bg-white"
                  >
                    {[100, 90, 80, 75, 60, 50, 40, 25, 20, 10].map(v => <option key={v} value={v}>{v}%</option>)}
                  </select>
                </td>
                {member.sprints.map(s => (
                  <td key={s.sprintId} className="px-2 py-2 text-center">
                    <span
                      className={`text-xs px-1.5 py-0.5 rounded ${s.availablePercent < 100 ? (s.availablePercent < 50 ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-800') : 'text-slate-400'}`}
                      title={s.leaveDays ? `${s.leaveDays} working days off` : undefined}
                    >
                      {s.availablePercent}%
                    </span>
                  </td>
                ))}
                <td className="px-2 py-2 text-right tabular-nums">{formatEffort(member.totalDays)}</td>
                <td className="px-2">
                  <button
                    onClick={async () => {
                      if (await confirm({
                        title: `Take ${member.name} off this quarter's plan?`,
                        message: 'Their allocations in this quarter go too.',
                        confirmLabel: 'Remove'
                      })) {
                        run(() => quarterlyPlanningApi.removeRosterMember(quarterId, member.directReportId))
                      }
                    }}
                    className="text-slate-400 hover:text-red-600"
                    aria-label={`Remove ${member.name}`}
                  >
                    <X className="w-4 h-4" />
                  </button>
                </td>
              </tr>
            ))}
            <tr className="bg-slate-50 text-xs text-slate-600">
              <td className="px-4 py-2 font-medium" colSpan={2}>Team</td>
              {board.capacity.sprints.map(s => (
                <td key={s.sprintId} className="px-2 py-2 text-center tabular-nums">
                  {formatEffort(s.supply)}/{formatEffort(s.fullSupply)}
                  {s.fullSupply > 0 && ` · ${Math.round((s.supply / s.fullSupply) * 100)}%`}
                </td>
              ))}
              <td className="px-2 py-2 text-right font-semibold tabular-nums">{formatEffort(board.capacity.totalDays)}</td>
              <td />
            </tr>
          </tbody>
        </table>
      </div>
      {available.length > 0 && (
        <div className="px-4 py-3 border-t border-slate-100 flex gap-2">
          <select value={adding} onChange={e => setAdding(e.target.value)} aria-label="Add to the team" className="px-2 py-1.5 text-sm border border-slate-300 rounded-lg bg-white">
            <option value="">Add someone to this quarter…</option>
            {available.map(p => <option key={p.id} value={p.id}>{p.name}{p.isDirect ? '' : ' (not direct)'}</option>)}
          </select>
          <button
            disabled={!adding}
            onClick={() => run(async () => {
              await quarterlyPlanningApi.setRosterMember(quarterId, adding, 100)
              setAdding('')
            })}
            className="px-3 py-1.5 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
          >
            Add
          </button>
        </div>
      )}
      {board.sprints.length === 0 && (
        <p className="px-4 py-3 text-sm text-amber-700">This quarter has no sprints yet. Create them on the Sprints page or sync them from Jira.</p>
      )}
    </Card>
  )
}
