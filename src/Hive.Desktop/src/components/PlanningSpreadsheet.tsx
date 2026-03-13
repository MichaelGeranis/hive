import { useState, useMemo } from 'react'
import { Loader2, MessageSquare, ExternalLink, X } from 'lucide-react'
import { quarterlyPlanningApi } from '../services/api'
import type {
  Initiative,
  SprintGoal,
  InitiativeMember,
  WorkType
} from '../types/quarterlyPlanning'
import type { Sprint } from '../types'

interface PlanningSpreadsheetProps {
  sprints: Sprint[]
  initiatives: Initiative[]
  sprintGoals: SprintGoal[]
  initiativeMembers: InitiativeMember[]
  quarterId: string
  onDataChanged: () => void
}

const WORK_TYPE_LABELS: Record<number, string> = {
  0: 'Maint',
  1: 'Product',
  2: 'Tech'
}

const WORK_TYPE_COLORS: Record<number, string> = {
  0: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  1: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  2: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400'
}

export default function PlanningSpreadsheet({
  sprints,
  initiatives,
  sprintGoals,
  initiativeMembers,
  quarterId,
  onDataChanged
}: PlanningSpreadsheetProps) {
  const [assigning, setAssigning] = useState<string | null>(null)
  const [hoveredNotes, setHoveredNotes] = useState<string | null>(null)
  const [editingNotes, setEditingNotes] = useState<{ sprintId: string; notes: string } | null>(null)
  const [savingNotes, setSavingNotes] = useState(false)

  // Sort sprints by name (chronological)
  const sortedSprints = useMemo(
    () => [...sprints].sort((a, b) => a.name.localeCompare(b.name)),
    [sprints]
  )

  // Build a map of sprintId -> index for span calculation
  const sprintIndexMap = useMemo(() => {
    const map = new Map<string, number>()
    sortedSprints.forEach((s, i) => map.set(s.id, i))
    return map
  }, [sortedSprints])

  // Build sprintGoal lookup
  const goalMap = useMemo(() => {
    const map = new Map<string, SprintGoal>()
    sprintGoals.forEach(g => map.set(g.sprintId, g))
    return map
  }, [sprintGoals])

  // Compute which cells are covered by each initiative
  const initiativeCells = useMemo(() => {
    const cells = new Map<string, Set<number>>() // initiativeId -> set of sprint indices
    initiatives.forEach(init => {
      if (!init.startSprintId) return
      const startIdx = sprintIndexMap.get(init.startSprintId)
      if (startIdx === undefined) return
      const indices = new Set<number>()
      for (let i = startIdx; i < startIdx + init.sprintSpan && i < sortedSprints.length; i++) {
        indices.add(i)
      }
      cells.set(init.id, indices)
    })
    return cells
  }, [initiatives, sprintIndexMap, sortedSprints.length])

  // Compute capacity per sprint (count of members across initiatives spanning that sprint)
  const sprintCapacity = useMemo(() => {
    const capacity = new Map<number, number>()
    initiatives.forEach(init => {
      const indices = initiativeCells.get(init.id)
      if (!indices) return
      const memberCount = init.members?.length || 0
      indices.forEach(idx => {
        capacity.set(idx, (capacity.get(idx) || 0) + memberCount)
      })
    })
    return capacity
  }, [initiatives, initiativeCells])

  const handleCellClick = async (initiative: Initiative, sprintIdx: number) => {
    const sprint = sortedSprints[sprintIdx]
    if (!sprint) return

    const currentIndices = initiativeCells.get(initiative.id)

    // If clicking on a filled cell, clear the assignment
    if (currentIndices?.has(sprintIdx)) {
      try {
        setAssigning(initiative.id)
        await quarterlyPlanningApi.assignInitiativeToSprint(initiative.id, null)
        onDataChanged()
      } catch (err) {
        console.error('Failed to unassign initiative', err)
      } finally {
        setAssigning(null)
      }
      return
    }

    // Otherwise, assign to this sprint
    try {
      setAssigning(initiative.id)
      await quarterlyPlanningApi.assignInitiativeToSprint(initiative.id, sprint.id)
      onDataChanged()
    } catch (err) {
      console.error('Failed to assign initiative', err)
    } finally {
      setAssigning(null)
    }
  }

  const handleSaveNotes = async () => {
    if (!editingNotes) return
    try {
      setSavingNotes(true)
      const goal = goalMap.get(editingNotes.sprintId)
      await quarterlyPlanningApi.upsertSprintGoal({
        quarterId,
        sprintId: editingNotes.sprintId,
        goal: goal?.goal,
        notes: editingNotes.notes
      })
      setEditingNotes(null)
      onDataChanged()
    } catch (err) {
      console.error('Failed to save notes', err)
    } finally {
      setSavingNotes(false)
    }
  }

  const formatSprintDates = (sprint: Sprint) => {
    if (!sprint.startDate || !sprint.endDate) return ''
    const start = new Date(sprint.startDate)
    const end = new Date(sprint.endDate)
    const fmt = (d: Date) => `${d.getDate()}/${d.getMonth() + 1}`
    return `${fmt(start)} - ${fmt(end)}`
  }

  if (sortedSprints.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
        <p>No sprints found for this quarter. Please create sprints first.</p>
      </div>
    )
  }

  return (
    <div className="border border-slate-200 dark:border-slate-700 rounded-lg overflow-auto bg-white dark:bg-slate-900">
      <table className="w-full border-collapse min-w-max">
        {/* Header */}
        <thead>
          <tr className="bg-slate-50 dark:bg-slate-800">
            {/* Sticky left columns */}
            <th className="sticky left-0 z-20 bg-slate-50 dark:bg-slate-800 px-3 py-2 text-left text-xs font-semibold text-slate-600 dark:text-slate-300 border-b border-r border-slate-200 dark:border-slate-700 min-w-[200px]">
              Initiative
            </th>
            <th className="sticky left-[200px] z-20 bg-slate-50 dark:bg-slate-800 px-2 py-2 text-center text-xs font-semibold text-slate-600 dark:text-slate-300 border-b border-r border-slate-200 dark:border-slate-700 w-16">
              Type
            </th>
            <th className="sticky left-[264px] z-20 bg-slate-50 dark:bg-slate-800 px-2 py-2 text-center text-xs font-semibold text-slate-600 dark:text-slate-300 border-b border-r border-slate-200 dark:border-slate-700 w-10">
              Size
            </th>
            <th className="sticky left-[314px] z-20 bg-slate-50 dark:bg-slate-800 px-2 py-2 text-left text-xs font-semibold text-slate-600 dark:text-slate-300 border-b border-r border-slate-200 dark:border-slate-700 min-w-[120px]">
              Members
            </th>

            {/* Sprint columns */}
            {sortedSprints.map(sprint => {
              const goal = goalMap.get(sprint.id)
              return (
                <th
                  key={sprint.id}
                  className="px-2 py-2 text-center text-xs font-semibold text-slate-600 dark:text-slate-300 border-b border-r border-slate-200 dark:border-slate-700 min-w-[100px] relative"
                >
                  <div className="flex items-center justify-center gap-1">
                    <span>{sprint.name}</span>
                    {goal?.notes && (
                      <button
                        className="relative"
                        onMouseEnter={() => setHoveredNotes(sprint.id)}
                        onMouseLeave={() => setHoveredNotes(null)}
                        onClick={() => setEditingNotes({ sprintId: sprint.id, notes: goal.notes })}
                      >
                        <MessageSquare className="w-3 h-3 text-amber-500" />
                        {hoveredNotes === sprint.id && (
                          <div className="absolute top-5 left-1/2 -translate-x-1/2 z-30 bg-slate-800 text-white text-xs p-2 rounded shadow-lg whitespace-pre-wrap max-w-[200px] text-left">
                            {goal.notes}
                          </div>
                        )}
                      </button>
                    )}
                    {!goal?.notes && (
                      <button
                        className="opacity-30 hover:opacity-100"
                        onClick={() => setEditingNotes({ sprintId: sprint.id, notes: '' })}
                      >
                        <MessageSquare className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                  <div className="text-[10px] font-normal text-slate-400 dark:text-slate-500">
                    {formatSprintDates(sprint)}
                  </div>
                </th>
              )
            })}
          </tr>
        </thead>

        <tbody>
          {initiatives.map((initiative, rowIdx) => {
            const indices = initiativeCells.get(initiative.id)
            const isAssigning = assigning === initiative.id

            return (
              <tr
                key={initiative.id}
                className={rowIdx % 2 === 0 ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-800/30'}
              >
                {/* Initiative name */}
                <td className="sticky left-0 z-10 px-3 py-1.5 text-sm border-b border-r border-slate-200 dark:border-slate-700"
                    style={{
                      backgroundColor: rowIdx % 2 === 0 ? undefined : undefined,
                      borderLeftWidth: 3,
                      borderLeftColor: initiative.color
                    }}
                >
                  <div className={`font-medium text-slate-800 dark:text-white truncate max-w-[180px] ${rowIdx % 2 === 0 ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-800/30'}`}>
                    {initiative.url ? (
                      <a
                        href={initiative.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="hover:text-amber-600 dark:hover:text-amber-400 inline-flex items-center gap-1"
                        onClick={e => e.stopPropagation()}
                      >
                        {initiative.name}
                        <ExternalLink className="w-3 h-3 flex-shrink-0" />
                      </a>
                    ) : (
                      initiative.name
                    )}
                  </div>
                </td>

                {/* Work type */}
                <td className={`sticky left-[200px] z-10 px-2 py-1.5 text-center border-b border-r border-slate-200 dark:border-slate-700 ${rowIdx % 2 === 0 ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-800/30'}`}>
                  <span className={`text-[10px] font-medium px-1.5 py-0.5 rounded ${WORK_TYPE_COLORS[initiative.workType] || WORK_TYPE_COLORS[1]}`}>
                    {WORK_TYPE_LABELS[initiative.workType] || 'Product'}
                  </span>
                </td>

                {/* T-shirt size */}
                <td className={`sticky left-[264px] z-10 px-2 py-1.5 text-center border-b border-r border-slate-200 dark:border-slate-700 ${rowIdx % 2 === 0 ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-800/30'}`}>
                  <span className="text-xs font-semibold text-slate-600 dark:text-slate-300">
                    {initiative.tshirtSize}
                  </span>
                </td>

                {/* Members */}
                <td className={`sticky left-[314px] z-10 px-2 py-1.5 text-xs border-b border-r border-slate-200 dark:border-slate-700 ${rowIdx % 2 === 0 ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-800/30'}`}>
                  <div className="flex flex-wrap gap-0.5">
                    {(initiative.members || []).map(m => (
                      <span
                        key={m.id}
                        className="text-[10px] bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 px-1 py-0.5 rounded truncate max-w-[50px]"
                        title={m.directReportName}
                      >
                        {m.directReportName.split(' ')[0]}
                      </span>
                    ))}
                    {(!initiative.members || initiative.members.length === 0) && (
                      <span className="text-slate-400 dark:text-slate-500 text-[10px]">-</span>
                    )}
                  </div>
                </td>

                {/* Sprint cells */}
                {sortedSprints.map((sprint, sprintIdx) => {
                  const isFilled = indices?.has(sprintIdx) || false
                  const isStart = initiative.startSprintId === sprint.id

                  return (
                    <td
                      key={sprint.id}
                      onClick={() => !isAssigning && handleCellClick(initiative, sprintIdx)}
                      className={`px-2 py-1.5 text-center border-b border-r border-slate-200 dark:border-slate-700 cursor-pointer transition-colors ${
                        isFilled
                          ? 'hover:opacity-80'
                          : 'hover:bg-slate-100 dark:hover:bg-slate-800'
                      }`}
                      style={isFilled ? {
                        backgroundColor: initiative.color + '33',
                        borderBottom: `2px solid ${initiative.color}`
                      } : undefined}
                    >
                      {isAssigning ? (
                        <Loader2 className="w-3 h-3 animate-spin mx-auto text-slate-400" />
                      ) : isFilled ? (
                        <div
                          className="w-full h-4 rounded-sm"
                          style={{ backgroundColor: initiative.color + '66' }}
                          title={`${initiative.name} (${isStart ? 'starts here' : 'continues'})`}
                        />
                      ) : null}
                    </td>
                  )
                })}
              </tr>
            )
          })}

          {/* Capacity summary row */}
          <tr className="bg-slate-100 dark:bg-slate-800 font-semibold">
            <td className="sticky left-0 z-10 bg-slate-100 dark:bg-slate-800 px-3 py-2 text-xs text-slate-600 dark:text-slate-300 border-t-2 border-r border-slate-300 dark:border-slate-600" colSpan={4}>
              Members Committed
            </td>
            {sortedSprints.map((_, sprintIdx) => {
              const count = sprintCapacity.get(sprintIdx) || 0
              return (
                <td
                  key={sprintIdx}
                  className="px-2 py-2 text-center text-xs text-slate-600 dark:text-slate-300 border-t-2 border-r border-slate-300 dark:border-slate-600"
                >
                  {count > 0 ? count : '-'}
                </td>
              )
            })}
          </tr>
        </tbody>
      </table>

      {/* Edit Notes Modal */}
      {editingNotes && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl p-6 w-96">
            <div className="flex justify-between items-center mb-4">
              <h3 className="font-semibold text-slate-800 dark:text-white">Sprint Notes</h3>
              <button onClick={() => setEditingNotes(null)} className="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded">
                <X className="w-4 h-4" />
              </button>
            </div>
            <textarea
              value={editingNotes.notes}
              onChange={e => setEditingNotes({ ...editingNotes, notes: e.target.value })}
              rows={4}
              className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white resize-none"
              placeholder="Add notes for this sprint..."
            />
            <div className="flex justify-end gap-2 mt-4">
              <button
                onClick={() => setEditingNotes(null)}
                className="px-3 py-1.5 text-sm text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
              >
                Cancel
              </button>
              <button
                onClick={handleSaveNotes}
                disabled={savingNotes}
                className="px-3 py-1.5 text-sm bg-amber-500 text-white rounded hover:bg-amber-600 disabled:opacity-50"
              >
                {savingNotes ? 'Saving...' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
