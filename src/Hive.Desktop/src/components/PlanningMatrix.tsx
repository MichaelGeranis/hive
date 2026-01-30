import { useState, useMemo } from 'react'
import { Plus, Trash2, Loader2, Calendar, X } from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import type {
  Allocation,
  Initiative,
  SprintGoal
} from '../types/quarterlyPlanning'
import type { Sprint, DirectReport, Leave } from '../types'

interface PlanningMatrixProps {
  sprints: Sprint[]
  teamMembers: DirectReport[]
  allocations: Allocation[]
  initiatives: Initiative[]
  leaves: Leave[]
  sprintGoals: SprintGoal[]
  quarterId: string
  onAllocationCreated: () => void
}

export default function PlanningMatrix({
  sprints,
  teamMembers,
  allocations,
  initiatives,
  leaves,
  sprintGoals,
  quarterId,
  onAllocationCreated
}: PlanningMatrixProps) {
  // Modal state for creating allocations
  const [showAllocationModal, setShowAllocationModal] = useState(false)
  const [selectedCell, setSelectedCell] = useState<{ memberId: string; sprintId: string } | null>(null)
  const [selectedInitiativeId, setSelectedInitiativeId] = useState('')
  const [saving, setSaving] = useState(false)
  const [deletingAllocation, setDeletingAllocation] = useState<Allocation | null>(null)

  // Sprint goals editing
  const [editingGoalSprint, setEditingGoalSprint] = useState<string | null>(null)
  const [goalText, setGoalText] = useState('')
  const [savingGoal, setSavingGoal] = useState(false)

  // Build lookup maps
  const allocationsByCell = useMemo(() => {
    const map = new Map<string, Allocation[]>()
    allocations.forEach(a => {
      const key = `${a.directReportId}-${a.sprintId}`
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(a)
    })
    return map
  }, [allocations])

  const sprintGoalMap = useMemo(() => {
    const map = new Map<string, SprintGoal>()
    sprintGoals.forEach(g => map.set(g.sprintId, g))
    return map
  }, [sprintGoals])

  // Check if member is on leave during sprint
  const isOnLeave = (memberId: string, sprint: Sprint) => {
    if (!sprint.startDate || !sprint.endDate) return false
    const sprintStart = new Date(sprint.startDate)
    const sprintEnd = new Date(sprint.endDate)

    return leaves.some(l => {
      if (l.directReportId !== memberId) return false
      const leaveStart = new Date(l.startDate)
      const leaveEnd = new Date(l.endDate)
      return leaveStart <= sprintEnd && leaveEnd >= sprintStart
    })
  }

  const handleCellClick = (memberId: string, sprintId: string) => {
    setSelectedCell({ memberId, sprintId })
    setSelectedInitiativeId('')
    setDeletingAllocation(null)
    setShowAllocationModal(true)
  }

  const handleAllocationClick = (allocation: Allocation, e: React.MouseEvent) => {
    e.stopPropagation()
    setDeletingAllocation(allocation)
    setSelectedCell({ memberId: allocation.directReportId, sprintId: allocation.sprintId })
    setSelectedInitiativeId(allocation.initiativeId)
    setShowAllocationModal(true)
  }

  const handleSaveAllocation = async () => {
    if (!selectedCell || !selectedInitiativeId) return

    try {
      setSaving(true)
      await quarterlyPlanningApi.createAllocation({
        initiativeId: selectedInitiativeId,
        directReportId: selectedCell.memberId,
        sprintId: selectedCell.sprintId
      })

      setShowAllocationModal(false)
      onAllocationCreated()
    } catch (err) {
      console.error('Failed to save allocation', err)
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteAllocation = async () => {
    if (!deletingAllocation) return

    try {
      setSaving(true)
      await quarterlyPlanningApi.deleteAllocation(deletingAllocation.id)
      setShowAllocationModal(false)
      onAllocationCreated()
    } catch (err) {
      console.error('Failed to delete allocation', err)
    } finally {
      setSaving(false)
    }
  }

  const handleEditGoal = (sprintId: string) => {
    const existingGoal = sprintGoalMap.get(sprintId)
    setGoalText(existingGoal?.goal || '')
    setEditingGoalSprint(sprintId)
  }

  const handleSaveGoal = async () => {
    if (!editingGoalSprint) return

    try {
      setSavingGoal(true)
      await quarterlyPlanningApi.upsertSprintGoal({
        quarterId,
        sprintId: editingGoalSprint,
        goal: goalText
      })
      setEditingGoalSprint(null)
      onAllocationCreated() // Refresh data
    } catch (err) {
      console.error('Failed to save sprint goal', err)
    } finally {
      setSavingGoal(false)
    }
  }

  if (sprints.length === 0) {
    return (
      <Card>
        <CardContent>
          <div className="flex flex-col items-center justify-center h-64 text-slate-500 dark:text-slate-400">
            <Calendar className="w-12 h-12 mb-4 opacity-50" />
            <p className="text-lg font-medium mb-2">No sprints found for this quarter</p>
            <p className="text-sm">Create sprints on the Sprints page first, then they will appear here.</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <h3 className="font-semibold text-slate-800 dark:text-white">Planning Board</h3>
      </CardHeader>
      <CardContent>
        <div className="min-w-max">
          <table className="w-full border-collapse">
            <thead>
              <tr>
                <th className="sticky left-0 z-20 bg-slate-100 dark:bg-slate-800 p-3 text-left font-semibold text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700 min-w-[180px]">
                  Team Member
                </th>
                {sprints.map(sprint => (
                  <th
                    key={sprint.id}
                    className="p-3 text-center font-semibold text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700 min-w-[160px] bg-slate-50 dark:bg-slate-800/50"
                  >
                    <div className="text-sm">{sprint.name}</div>
                    {sprint.startDate && sprint.endDate && (
                      <div className="text-xs text-slate-500 dark:text-slate-400">
                        {new Date(sprint.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} -
                        {new Date(sprint.endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      </div>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {/* Sprint Goals Row */}
              <tr className="bg-amber-50 dark:bg-amber-900/20">
                <td className="sticky left-0 z-10 bg-amber-50 dark:bg-amber-900/20 p-3 font-medium text-amber-800 dark:text-amber-300 border-b border-slate-200 dark:border-slate-700">
                  Sprint Goals
                </td>
                {sprints.map(sprint => {
                  const goal = sprintGoalMap.get(sprint.id)
                  return (
                    <td
                      key={sprint.id}
                      className="p-2 border-b border-slate-200 dark:border-slate-700"
                    >
                      {editingGoalSprint === sprint.id ? (
                        <div className="flex flex-col gap-1">
                          <textarea
                            value={goalText}
                            onChange={(e) => setGoalText(e.target.value)}
                            className="w-full p-2 text-xs border border-amber-300 dark:border-amber-600 rounded bg-white dark:bg-slate-800 text-slate-800 dark:text-white resize-none"
                            rows={2}
                            autoFocus
                          />
                          <div className="flex gap-1">
                            <button
                              onClick={handleSaveGoal}
                              disabled={savingGoal}
                              className="px-2 py-1 text-xs bg-amber-500 text-white rounded hover:bg-amber-600 disabled:opacity-50"
                            >
                              {savingGoal ? 'Saving...' : 'Save'}
                            </button>
                            <button
                              onClick={() => setEditingGoalSprint(null)}
                              className="px-2 py-1 text-xs text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 rounded"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : (
                        <div
                          onClick={() => handleEditGoal(sprint.id)}
                          className="min-h-[40px] p-2 text-xs text-slate-600 dark:text-slate-400 cursor-pointer hover:bg-amber-100 dark:hover:bg-amber-900/30 rounded transition-colors"
                        >
                          {goal?.goal || <span className="text-slate-400 dark:text-slate-500 italic">Click to add goal...</span>}
                        </div>
                      )}
                    </td>
                  )
                })}
              </tr>

              {/* Team Member Rows */}
              {teamMembers.map(member => (
                <tr key={member.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/30">
                  <td className="sticky left-0 z-10 bg-white dark:bg-slate-900 p-3 border-b border-slate-200 dark:border-slate-700">
                    <div className="font-medium text-slate-800 dark:text-white">
                      {member.fullName}
                    </div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">
                      {member.jobTitle}
                    </div>
                  </td>
                  {sprints.map(sprint => {
                    const cellKey = `${member.id}-${sprint.id}`
                    const cellAllocations = allocationsByCell.get(cellKey) || []
                    const onLeave = isOnLeave(member.id, sprint)

                    return (
                      <td
                        key={sprint.id}
                        onClick={() => !onLeave && handleCellClick(member.id, sprint.id)}
                        className={`p-2 border-b border-r border-slate-200 dark:border-slate-700 cursor-pointer transition-colors min-h-[60px] ${
                          onLeave
                            ? 'bg-slate-200 dark:bg-slate-700 cursor-not-allowed'
                            : 'hover:bg-slate-100 dark:hover:bg-slate-800'
                        }`}
                      >
                        {onLeave && (
                          <div className="text-xs text-slate-500 dark:text-slate-400 italic text-center">
                            On Leave
                          </div>
                        )}
                        <div className="flex flex-wrap gap-1">
                          {cellAllocations.map(allocation => (
                            <div
                              key={allocation.id}
                              onClick={(e) => handleAllocationClick(allocation, e)}
                              className="px-2 py-1 rounded text-xs font-medium text-white cursor-pointer hover:opacity-80 transition-opacity"
                              style={{ backgroundColor: allocation.initiativeColor || '#6366f1' }}
                              title={allocation.initiativeName}
                            >
                              <span className="truncate max-w-[100px] block">{allocation.initiativeName}</span>
                            </div>
                          ))}
                        </div>
                        {cellAllocations.length === 0 && !onLeave && (
                          <div className="flex items-center justify-center h-10 text-slate-300 dark:text-slate-600">
                            <Plus className="w-4 h-4" />
                          </div>
                        )}
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>

      {/* Allocation Modal */}
      {showAllocationModal && selectedCell && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl p-6 w-96">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-bold text-slate-800 dark:text-white">
                {deletingAllocation ? 'Manage Allocation' : 'Add Allocation'}
              </h2>
              <button
                onClick={() => setShowAllocationModal(false)}
                className="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              {deletingAllocation ? (
                <div>
                  <p className="text-sm text-slate-600 dark:text-slate-400 mb-2">
                    Current allocation:
                  </p>
                  <div
                    className="px-3 py-2 rounded text-sm font-medium text-white"
                    style={{ backgroundColor: deletingAllocation.initiativeColor || '#6366f1' }}
                  >
                    {deletingAllocation.initiativeName}
                  </div>
                </div>
              ) : (
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Initiative
                  </label>
                  <select
                    value={selectedInitiativeId}
                    onChange={(e) => setSelectedInitiativeId(e.target.value)}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                  >
                    <option value="">Select initiative...</option>
                    {initiatives.map(init => (
                      <option key={init.id} value={init.id}>
                        {init.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </div>

            <div className="flex justify-between items-center mt-6">
              <div>
                {deletingAllocation && (
                  <button
                    onClick={handleDeleteAllocation}
                    disabled={saving}
                    className="flex items-center gap-2 px-3 py-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                    Remove
                  </button>
                )}
              </div>
              <div className="flex gap-3">
                <button
                  onClick={() => setShowAllocationModal(false)}
                  className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                {!deletingAllocation && (
                  <button
                    onClick={handleSaveAllocation}
                    disabled={saving || !selectedInitiativeId}
                    className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50"
                  >
                    {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                    Add
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </Card>
  )
}
