import { useState } from 'react'
import { Plus, Trash2, Loader2, X, Check, Target, UserPlus, UserMinus } from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import type { Initiative } from '../types/quarterlyPlanning'
import type { DirectReport } from '../types'

interface InitiativesPanelProps {
  initiatives: Initiative[]
  quarterId: string
  teamMembers: DirectReport[]
  onInitiativeCreated: () => void
}

const TSHIRT_SIZES = ['S', 'M', 'L', 'XL']

const WORK_TYPES: { value: number; label: string }[] = [
  { value: 0, label: 'Maintenance' },
  { value: 1, label: 'Product Roadmap' },
  { value: 2, label: 'Tech Roadmap' }
]

const WORK_TYPE_BADGE_COLORS: Record<number, string> = {
  0: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  1: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  2: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400'
}

const WORK_TYPE_SHORT: Record<number, string> = {
  0: 'Maint',
  1: 'Product',
  2: 'Tech'
}

export default function InitiativesPanel({
  initiatives,
  quarterId,
  teamMembers,
  onInitiativeCreated
}: InitiativesPanelProps) {
  // Modal state
  const [showModal, setShowModal] = useState(false)
  const [editingInitiative, setEditingInitiative] = useState<Initiative | null>(null)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [tshirtSize, setTshirtSize] = useState('M')
  const [url, setUrl] = useState('')
  const [workType, setWorkType] = useState<number>(1)

  // Member management
  const [addingMember, setAddingMember] = useState(false)
  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null)

  const resetForm = () => {
    setName('')
    setDescription('')
    setTshirtSize('M')
    setUrl('')
    setWorkType(1)
    setEditingInitiative(null)
  }

  const openCreate = () => {
    resetForm()
    setShowModal(true)
  }

  const openEdit = (initiative: Initiative) => {
    setEditingInitiative(initiative)
    setName(initiative.name)
    setDescription(initiative.description || '')
    setTshirtSize(initiative.tshirtSize || 'M')
    setUrl(initiative.url || '')
    setWorkType(initiative.workType ?? 1)
    setShowModal(true)
  }

  const handleSave = async () => {
    if (!name) return

    try {
      setSaving(true)

      if (editingInitiative) {
        await quarterlyPlanningApi.updateInitiative(editingInitiative.id, {
          name,
          description,
          tshirtSize,
          url,
          workType
        })
      } else {
        await quarterlyPlanningApi.createInitiative({
          quarterId,
          name,
          description,
          tshirtSize,
          url,
          workType
        })
      }

      setShowModal(false)
      resetForm()
      onInitiativeCreated()
    } catch (err) {
      console.error('Failed to save initiative', err)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!editingInitiative) return

    try {
      setDeleting(true)
      await quarterlyPlanningApi.deleteInitiative(editingInitiative.id)
      setShowModal(false)
      resetForm()
      onInitiativeCreated()
    } catch (err) {
      console.error('Failed to delete initiative', err)
    } finally {
      setDeleting(false)
    }
  }

  const handleAddMember = async (directReportId: string) => {
    if (!editingInitiative) return
    try {
      setAddingMember(true)
      await quarterlyPlanningApi.addInitiativeMember(editingInitiative.id, directReportId)
      onInitiativeCreated()
      // Refresh the editing initiative's members
      const updated = await quarterlyPlanningApi.getInitiativeById(editingInitiative.id)
      setEditingInitiative(updated)
    } catch (err) {
      console.error('Failed to add member', err)
    } finally {
      setAddingMember(false)
    }
  }

  const handleRemoveMember = async (memberId: string) => {
    try {
      setRemovingMemberId(memberId)
      await quarterlyPlanningApi.removeInitiativeMember(memberId)
      onInitiativeCreated()
      if (editingInitiative) {
        const updated = await quarterlyPlanningApi.getInitiativeById(editingInitiative.id)
        setEditingInitiative(updated)
      }
    } catch (err) {
      console.error('Failed to remove member', err)
    } finally {
      setRemovingMemberId(null)
    }
  }

  // Get unassigned members for the current initiative
  const getAvailableMembers = () => {
    if (!editingInitiative) return teamMembers
    const assignedIds = new Set((editingInitiative.members || []).map(m => m.directReportId))
    return teamMembers.filter(m => !assignedIds.has(m.id))
  }

  // Count placed initiatives (ones with a start sprint assigned)
  const placedCount = initiatives.filter(i => i.startSprintId).length

  return (
    <Card className="h-full flex flex-col">
      <CardHeader className="flex-shrink-0">
        <div className="flex items-center justify-between">
          <h3 className="font-semibold text-slate-800 dark:text-white flex items-center gap-2">
            <Target className="w-4 h-4" />
            Initiatives
          </h3>
          <button
            onClick={openCreate}
            className="p-1.5 rounded bg-amber-500 text-white hover:bg-amber-600"
            title="Add Initiative"
          >
            <Plus className="w-4 h-4" />
          </button>
        </div>

        {/* Summary */}
        <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
          {initiatives.length} initiatives | {placedCount} placed in sprints
        </div>
      </CardHeader>

      <CardContent className="flex-1 overflow-auto">
        {initiatives.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-32 text-slate-500 dark:text-slate-400">
            <Target className="w-8 h-8 mb-2 opacity-50" />
            <p className="text-sm">No initiatives yet</p>
            <button
              onClick={openCreate}
              className="mt-2 text-xs text-amber-500 hover:text-amber-600"
            >
              Create your first initiative
            </button>
          </div>
        ) : (
          <div className="space-y-2">
            {initiatives.map(initiative => (
              <div
                key={initiative.id}
                onClick={() => openEdit(initiative)}
                className="p-3 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 cursor-pointer hover:shadow-md transition-shadow"
                style={{ borderLeftWidth: 4, borderLeftColor: initiative.color }}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <h4 className="font-medium text-slate-800 dark:text-white text-sm truncate">
                      {initiative.name}
                    </h4>
                    {initiative.description && (
                      <p className="text-xs text-slate-500 dark:text-slate-400 truncate mt-0.5">
                        {initiative.description}
                      </p>
                    )}
                  </div>
                  <span className="text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300">
                    {initiative.tshirtSize}
                  </span>
                </div>

                <div className="flex items-center gap-2 mt-2 flex-wrap">
                  <span className={`text-[10px] font-medium px-1.5 py-0.5 rounded ${WORK_TYPE_BADGE_COLORS[initiative.workType] || WORK_TYPE_BADGE_COLORS[1]}`}>
                    {WORK_TYPE_SHORT[initiative.workType] || 'Product'}
                  </span>
                  {(initiative.members || []).length > 0 && (
                    <span className="text-[10px] text-slate-500 dark:text-slate-400">
                      {initiative.members.length} member{initiative.members.length !== 1 ? 's' : ''}
                    </span>
                  )}
                  {initiative.startSprintId && (
                    <span className="text-[10px] text-green-600 dark:text-green-400">
                      {initiative.sprintSpan}sp
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl p-6 w-[480px] max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-bold text-slate-800 dark:text-white">
                {editingInitiative ? 'Edit Initiative' : 'Create Initiative'}
              </h2>
              <button
                onClick={() => setShowModal(false)}
                className="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Name *
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Initiative name..."
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Description
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                  placeholder="What does this initiative involve?"
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white resize-none"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Work Type
                </label>
                <div className="flex gap-2">
                  {WORK_TYPES.map(wt => (
                    <button
                      key={wt.value}
                      type="button"
                      onClick={() => setWorkType(wt.value)}
                      className={`flex-1 py-2 text-xs font-medium rounded-lg border transition-colors ${
                        workType === wt.value
                          ? 'bg-amber-500 text-white border-amber-500'
                          : 'bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 border-slate-300 dark:border-slate-600 hover:border-amber-400'
                      }`}
                    >
                      {wt.label}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  T-Shirt Size
                </label>
                <div className="flex gap-2">
                  {TSHIRT_SIZES.map(size => (
                    <button
                      key={size}
                      type="button"
                      onClick={() => setTshirtSize(size)}
                      className={`flex-1 py-2 text-sm font-medium rounded-lg border transition-colors ${
                        tshirtSize === size
                          ? 'bg-amber-500 text-white border-amber-500'
                          : 'bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 border-slate-300 dark:border-slate-600 hover:border-amber-400'
                      }`}
                    >
                      {size}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  URL (optional)
                </label>
                <input
                  type="url"
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  placeholder="https://..."
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                />
              </div>

              {editingInitiative && (
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Color
                  </label>
                  <div
                    className="w-full h-8 rounded-lg"
                    style={{ backgroundColor: editingInitiative.color }}
                  />
                  <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                    Colors are automatically assigned to ensure uniqueness
                  </p>
                </div>
              )}

              {/* Member assignment - only in edit mode */}
              {editingInitiative && (
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Assigned Members
                  </label>

                  {/* Current members */}
                  <div className="space-y-1 mb-2">
                    {(editingInitiative.members || []).map(member => (
                      <div key={member.id} className="flex items-center justify-between px-2 py-1 bg-slate-50 dark:bg-slate-700 rounded text-sm">
                        <span className="text-slate-700 dark:text-slate-300">{member.directReportName}</span>
                        <button
                          onClick={() => handleRemoveMember(member.id)}
                          disabled={removingMemberId === member.id}
                          className="p-0.5 text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded disabled:opacity-50"
                        >
                          {removingMemberId === member.id ? (
                            <Loader2 className="w-3 h-3 animate-spin" />
                          ) : (
                            <UserMinus className="w-3 h-3" />
                          )}
                        </button>
                      </div>
                    ))}
                    {(!editingInitiative.members || editingInitiative.members.length === 0) && (
                      <p className="text-xs text-slate-400 dark:text-slate-500 italic">No members assigned</p>
                    )}
                  </div>

                  {/* Add member dropdown */}
                  {getAvailableMembers().length > 0 && (
                    <div className="flex gap-2">
                      <select
                        id="add-member-select"
                        className="flex-1 px-2 py-1 text-sm border border-slate-300 dark:border-slate-600 rounded bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                        defaultValue=""
                      >
                        <option value="" disabled>Add member...</option>
                        {getAvailableMembers().map(m => (
                          <option key={m.id} value={m.id}>{m.fullName}</option>
                        ))}
                      </select>
                      <button
                        onClick={() => {
                          const select = document.getElementById('add-member-select') as HTMLSelectElement
                          if (select?.value) {
                            handleAddMember(select.value)
                            select.value = ''
                          }
                        }}
                        disabled={addingMember}
                        className="p-1.5 bg-amber-500 text-white rounded hover:bg-amber-600 disabled:opacity-50"
                      >
                        {addingMember ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="flex justify-between items-center mt-6">
              <div>
                {editingInitiative && (
                  <button
                    onClick={handleDelete}
                    disabled={deleting || saving}
                    className="flex items-center gap-2 px-3 py-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors disabled:opacity-50"
                  >
                    {deleting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                    Delete
                  </button>
                )}
              </div>
              <div className="flex gap-3">
                <button
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSave}
                  disabled={saving || !name}
                  className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50"
                >
                  {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                  {editingInitiative ? 'Update' : 'Create'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </Card>
  )
}
