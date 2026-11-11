import { useState } from 'react'
import { ExternalLink, GripVertical, Plus } from 'lucide-react'
import { Card, CardHeader } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import { formatDate } from '../utils/dateUtils'
import {
  DEPENDENCY_STATUS_STYLES,
  STATUS_LABELS,
  STATUS_STYLES,
  WORK_TYPE_LABELS,
  formatEffort,
  isLane,
  priorityLabel,
  STARTING_STATUSES,
  canBeSized,
  describeSize,
  sizeOptions,
  toUpdateDto
} from '../utils/planningDisplay'
import { InitiativeKind, InitiativeStatus, WorkType } from '../types/quarterlyPlanning'
import type { Initiative, PlanningBoard } from '../types/quarterlyPlanning'
import type { RunChange } from '../utils/planningDisplay'

interface PlanningOptionsProps {
  board: PlanningBoard
  run: RunChange
  onOpen: (initiativeId: string) => void
}

/**
 * The option catalogue: every candidate the PM brought, in priority order, with its size,
 * status, what it still waits on and whom it depends on. Drag a row to reprioritise.
 */
export default function PlanningOptions({ board, run, onOpen }: PlanningOptionsProps) {
  const [name, setName] = useState('')
  const [status, setStatus] = useState<number>(InitiativeStatus.InDiscovery)
  const [kind, setKind] = useState<number>(InitiativeKind.Option)
  const [isNew, setIsNew] = useState(false)
  const [dragged, setDragged] = useState<string | null>(null)
  const [showDropped, setShowDropped] = useState(false)

  const quarterId = board.quarter.id
  const ranked = board.initiatives.filter(i => !isLane(i) && i.status !== InitiativeStatus.Dropped)
  const dropped = board.initiatives.filter(i => !isLane(i) && i.status === InitiativeStatus.Dropped)
  const lanes = board.initiatives.filter(isLane)
  const missingLanes = [WorkType.Maintenance, WorkType.TechRoadmap].filter(t => !lanes.some(l => l.workType === t))
  const sizes = sizeOptions(board)

  const add = () => {
    if (!name.trim()) return
    run(async () => {
      await quarterlyPlanningApi.createInitiative({
        quarterId,
        name: name.trim(),
        status,
        kind,
        arrivedOn: isNew ? new Date().toISOString().slice(0, 10) : null
      })
      setName('')
      setIsNew(false)
    })
  }

  const drop = (targetId: string) => {
    if (!dragged || dragged === targetId) return
    const ids = ranked.map(i => i.id).filter(id => id !== dragged)
    ids.splice(ids.indexOf(targetId), 0, dragged)
    setDragged(null)
    run(() => quarterlyPlanningApi.reorder(quarterId, ids))
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader
          title="Options"
          subtitle="Leftovers from last quarter first, then the PM's priority. Drag to reorder; click to open."
        />
        {/* Quick add */}
        <div className="px-6 py-3 border-b border-slate-100 flex flex-wrap items-center gap-2 bg-slate-50">
          <input
            value={name}
            onChange={e => setName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && add()}
            placeholder="Add a candidate option…"
            aria-label="New option name"
            className="flex-1 min-w-[16rem] px-3 py-2 border border-slate-300 rounded-lg"
          />
          <select value={status} onChange={e => setStatus(Number(e.target.value))} aria-label="New option status" className="px-2 py-2 border border-slate-300 rounded-lg bg-white">
            {STARTING_STATUSES.map(s => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
          </select>
          <select value={kind} onChange={e => setKind(Number(e.target.value))} aria-label="New option kind" className="px-2 py-2 border border-slate-300 rounded-lg bg-white">
            <option value={InitiativeKind.Option}>Option</option>
            <option value={InitiativeKind.Leftover}>Leftover</option>
          </select>
          <label className="flex items-center gap-1 text-sm text-slate-600">
            <input type="checkbox" checked={isNew} onChange={e => setIsNew(e.target.checked)} />
            Arrived mid-quarter
          </label>
          <button onClick={add} disabled={!name.trim()} className="flex items-center gap-1 px-3 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50">
            <Plus className="w-4 h-4" /> Add
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-slate-600 text-xs uppercase tracking-wide">
              <tr>
                <th className="w-6" />
                <th className="px-2 py-2 text-left">Priority</th>
                <th className="px-2 py-2 text-left">Option</th>
                <th className="px-2 py-2 text-left">Size</th>
                <th className="px-2 py-2 text-left">Status</th>
                <th className="px-2 py-2 text-left">Due</th>
                <th className="px-2 py-2 text-left">Dependencies</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {ranked.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-6 py-8 text-center text-slate-500">
                    No options yet. Add the candidates the PM brought for this quarter.
                  </td>
                </tr>
              )}
              {ranked.map(option => (
                <OptionRow
                  key={option.id}
                  option={option}
                  board={board}
                  sizes={sizes}
                  draggable={option.kind !== InitiativeKind.Leftover}
                  onDragStart={() => setDragged(option.id)}
                  onDrop={() => drop(option.id)}
                  onOpen={() => onOpen(option.id)}
                  onSize={value => run(() => quarterlyPlanningApi.updateInitiative(option.id, toUpdateDto(option, { tshirtSize: value })))}
                />
              ))}
            </tbody>
          </table>
        </div>

        {dropped.length > 0 && (
          <div className="px-6 py-3 border-t border-slate-100">
            <button onClick={() => setShowDropped(s => !s)} className="text-sm text-slate-600 hover:text-slate-900">
              {showDropped ? 'Hide' : 'Show'} {dropped.length} dropped
            </button>
            {showDropped && (
              <ul className="mt-2 space-y-1">
                {dropped.map(option => (
                  <li key={option.id}>
                    <button onClick={() => onOpen(option.id)} className="text-sm text-slate-500 line-through hover:text-slate-800">
                      {option.name}
                    </button>
                    {option.decisionNote && <span className="text-xs text-slate-400 ml-2">{option.decisionNote}</span>}
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </Card>

      <Card>
        <CardHeader title="Standing lanes" subtitle="Work every quarter carries, sized by the mix rather than a T-shirt." />
        <div className="px-6 py-4 flex flex-wrap gap-3">
          {lanes.map(lane => (
            <button
              key={lane.id}
              onClick={() => onOpen(lane.id)}
              className="flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 hover:bg-slate-50"
            >
              <span className="w-3 h-3 rounded-full" style={{ backgroundColor: lane.color }} />
              <span className="font-medium text-slate-800">{lane.name}</span>
              <span className="text-xs text-slate-500">
                {lane.workType === WorkType.Maintenance ? board.quarter.maintenancePercent : board.quarter.techRoadmapPercent}% ·{' '}
                {formatEffort(board.capacity.reservations.find(r => r.workType === lane.workType)?.effort)} dev-sprints
              </span>
            </button>
          ))}
          {missingLanes.map(workType => (
            <button
              key={workType}
              onClick={() => run(() => quarterlyPlanningApi.addLane(quarterId, workType))}
              className="flex items-center gap-1 px-3 py-2 rounded-lg border border-dashed border-slate-300 text-slate-600 hover:bg-slate-50"
            >
              <Plus className="w-4 h-4" /> Add {WORK_TYPE_LABELS[workType]} lane
            </button>
          ))}
        </div>
      </Card>
    </div>
  )
}

function OptionRow({
  option,
  board,
  sizes,
  draggable,
  onDragStart,
  onDrop,
  onOpen,
  onSize
}: {
  option: Initiative
  board: PlanningBoard
  sizes: string[]
  draggable: boolean
  onDragStart: () => void
  onDrop: () => void
  onOpen: () => void
  onSize: (size: string | null) => void
}) {

  return (
    <tr
      className="hover:bg-slate-50 cursor-pointer"
      draggable={draggable}
      onDragStart={onDragStart}
      onDragOver={e => e.preventDefault()}
      onDrop={onDrop}
      onClick={onOpen}
    >
      <td className="pl-2 text-slate-300">{draggable && <GripVertical className="w-4 h-4 cursor-grab" />}</td>
      <td className="px-2 py-2">
        <span
          className="inline-flex items-center justify-center min-w-[2rem] px-1.5 py-0.5 rounded font-semibold text-white text-xs"
          style={{ backgroundColor: option.color }}
        >
          {priorityLabel(option)}
        </span>
      </td>
      <td className="px-2 py-2">
        <div className="flex items-center gap-2">
          <span className="font-medium text-slate-800">{option.name}</span>
          {option.url && (
            <a href={option.url} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()} title="Solution design" className="text-slate-400 hover:text-amber-600">
              <ExternalLink className="w-3.5 h-3.5" />
            </a>
          )}
          {option.workType === WorkType.TechRoadmap && <span className="text-[10px] px-1.5 rounded bg-blue-100 text-blue-800">Tech</span>}
          {option.workType === WorkType.Maintenance && <span className="text-[10px] px-1.5 rounded bg-red-100 text-red-700">Maint.</span>}
        </div>
        {option.products.length > 0 && (
          <div className="text-xs text-slate-500 mt-0.5">{option.products.map(p => p.name).join(', ')}</div>
        )}
      </td>
      <td className="px-2 py-2" onClick={e => e.stopPropagation()}>
        {canBeSized(option.status) ? (
          <select
            value={option.tshirtSize ?? ''}
            onChange={e => onSize(e.target.value || null)}
            aria-label={`Size of ${option.name}`}
            className={`px-1.5 py-1 border rounded bg-white ${option.effort == null ? 'border-red-300 text-red-700' : 'border-slate-200'}`}
          >
            <option value="">Size…</option>
            {sizes.map(s => <option key={s} value={s}>{describeSize(board, s)}</option>)}
          </select>
        ) : (
          <span className="text-xs text-slate-400" title="Sized once it is ready for commitment">—</span>
        )}
      </td>
      <td className="px-2 py-2">
        <span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_STYLES[option.status]}`}>{STATUS_LABELS[option.status]}</span>
      </td>
      <td className="px-2 py-2 text-slate-700 tabular-nums">{option.dueDate ? formatDate(option.dueDate) : ''}</td>
      <td className="px-2 py-2">
        <div className="flex flex-wrap gap-1">
          {option.externalDependencies.map(d => (
            <span key={d.id} className={`text-[11px] px-1.5 py-0.5 rounded ${DEPENDENCY_STATUS_STYLES[d.status]}`} title={d.need}>
              {d.team}
            </span>
          ))}
        </div>
      </td>
    </tr>
  )
}
