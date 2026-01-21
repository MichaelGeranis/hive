import { useState } from 'react'
import { Plus, Trash2, Loader2, X, Check, Target, Filter } from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import { quarterlyPlanningApi } from '../services/api'
import type {
  Initiative,
  InitiativeStatus,
  InitiativePriority
} from '../types/quarterlyPlanning'

interface InitiativesPanelProps {
  initiatives: Initiative[]
  quarterId: string
  onInitiativeCreated: () => void
  onDragStart?: (initiative: Initiative) => void
}

const INITIATIVE_COLORS = [
  '#6366f1', // Indigo
  '#8b5cf6', // Violet
  '#d946ef', // Fuchsia
  '#ec4899', // Pink
  '#f43f5e', // Rose
  '#ef4444', // Red
  '#f97316', // Orange
  '#f59e0b', // Amber
  '#84cc16', // Lime
  '#22c55e', // Green
  '#14b8a6', // Teal
  '#06b6d4', // Cyan
  '#0ea5e9', // Sky
  '#3b82f6', // Blue
]

const STATUS_OPTIONS: { value: InitiativeStatus; label: string }[] = [
  { value: 0, label: 'Planned' },
  { value: 1, label: 'In Progress' },
  { value: 2, label: 'On Hold' },
  { value: 3, label: 'Completed' },
  { value: 4, label: 'Cancelled' },
]

const PRIORITY_OPTIONS: { value: InitiativePriority; label: string }[] = [
  { value: 0, label: 'Low' },
  { value: 1, label: 'Medium' },
  { value: 2, label: 'High' },
  { value: 3, label: 'Critical' },
]

export default function InitiativesPanel({
  initiatives,
  quarterId,
  onInitiativeCreated,
  onDragStart
}: InitiativesPanelProps) {
  // Modal state
  const [showModal, setShowModal] = useState(false)
  const [editingInitiative, setEditingInitiative] = useState<Initiative | null>(null)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<InitiativePriority>(1)
  const [okrObjective, setOkrObjective] = useState('')
  const [color, setColor] = useState(INITIATIVE_COLORS[0])

  // Filter state
  const [filterStatus, setFilterStatus] = useState<InitiativeStatus | 'all'>('all')
  const [filterPriority, setFilterPriority] = useState<InitiativePriority | 'all'>('all')
  const [showFilters, setShowFilters] = useState(false)

  const resetForm = () => {
    setName('')
    setDescription('')
    setPriority(1)
    setOkrObjective('')
    setColor(INITIATIVE_COLORS[Math.floor(Math.random() * INITIATIVE_COLORS.length)])
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
    setPriority(initiative.priority)
    setOkrObjective(initiative.okrObjective || '')
    setColor(initiative.color || INITIATIVE_COLORS[0])
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
          priority,
          okrObjective,
          color
        })
      } else {
        await quarterlyPlanningApi.createInitiative({
          quarterId,
          name,
          description,
          priority,
          okrObjective,
          color
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

  const handleStatusChange = async (initiative: Initiative, status: InitiativeStatus) => {
    try {
      await quarterlyPlanningApi.updateInitiativeStatus(initiative.id, { status })
      onInitiativeCreated()
    } catch (err) {
      console.error('Failed to update status', err)
    }
  }

  // Filter initiatives
  const filteredInitiatives = initiatives.filter(i => {
    if (filterStatus !== 'all' && i.status !== filterStatus) return false
    if (filterPriority !== 'all' && i.priority !== filterPriority) return false
    return true
  })

  const getStatusLabel = (status: InitiativeStatus) => {
    return STATUS_OPTIONS.find(s => s.value === status)?.label || 'Unknown'
  }

  const getPriorityLabel = (priority: InitiativePriority) => {
    return PRIORITY_OPTIONS.find(p => p.value === priority)?.label || 'Unknown'
  }

  const getStatusColor = (status: InitiativeStatus) => {
    switch (status) {
      case 0: return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300'
      case 1: return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
      case 2: return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
      case 3: return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
      case 4: return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
      default: return 'bg-slate-100 text-slate-700'
    }
  }

  const getPriorityColor = (priority: InitiativePriority) => {
    switch (priority) {
      case 0: return 'text-slate-500'
      case 1: return 'text-blue-500'
      case 2: return 'text-amber-500'
      case 3: return 'text-red-500'
      default: return 'text-slate-500'
    }
  }

  // Count allocated initiatives
  const allocatedCount = initiatives.filter(i => i.allocationCount > 0).length

  return (
    <Card className="h-full flex flex-col">
      <CardHeader className="flex-shrink-0">
        <div className="flex items-center justify-between">
          <h3 className="font-semibold text-slate-800 dark:text-white flex items-center gap-2">
            <Target className="w-4 h-4" />
            Initiatives
          </h3>
          <div className="flex items-center gap-1">
            <button
              onClick={() => setShowFilters(!showFilters)}
              className={`p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 ${showFilters ? 'bg-slate-200 dark:bg-slate-700' : ''}`}
              title="Filter"
            >
              <Filter className="w-4 h-4" />
            </button>
            <button
              onClick={openCreate}
              className="p-1.5 rounded bg-amber-500 text-white hover:bg-amber-600"
              title="Add Initiative"
            >
              <Plus className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Summary */}
        <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
          {initiatives.length} initiatives | {allocatedCount} with allocations
        </div>

        {/* Filters */}
        {showFilters && (
          <div className="mt-3 space-y-2">
            <div>
              <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">Status</label>
              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value === 'all' ? 'all' : parseInt(e.target.value) as InitiativeStatus)}
                className="w-full px-2 py-1 text-xs border border-slate-300 dark:border-slate-600 rounded bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
              >
                <option value="all">All Statuses</option>
                {STATUS_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">Priority</label>
              <select
                value={filterPriority}
                onChange={(e) => setFilterPriority(e.target.value === 'all' ? 'all' : parseInt(e.target.value) as InitiativePriority)}
                className="w-full px-2 py-1 text-xs border border-slate-300 dark:border-slate-600 rounded bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
              >
                <option value="all">All Priorities</option>
                {PRIORITY_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          </div>
        )}
      </CardHeader>

      <CardContent className="flex-1 overflow-auto">
        {filteredInitiatives.length === 0 ? (
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
            {filteredInitiatives.map(initiative => (
              <div
                key={initiative.id}
                draggable
                onDragStart={() => onDragStart?.(initiative)}
                onClick={() => openEdit(initiative)}
                className="p-3 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 cursor-pointer hover:shadow-md transition-shadow"
                style={{ borderLeftWidth: 4, borderLeftColor: initiative.color }}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <h4 className="font-medium text-slate-800 dark:text-white text-sm truncate">
                      {initiative.name}
                    </h4>
                    {initiative.okrObjective && (
                      <p className="text-xs text-slate-500 dark:text-slate-400 truncate mt-0.5">
                        {initiative.okrObjective}
                      </p>
                    )}
                  </div>
                  <span className={`text-xs font-medium ${getPriorityColor(initiative.priority)}`}>
                    {getPriorityLabel(initiative.priority)}
                  </span>
                </div>

                <div className="flex items-center justify-between mt-2">
                  <span className={`text-xs px-2 py-0.5 rounded-full ${getStatusColor(initiative.status)}`}>
                    {getStatusLabel(initiative.status)}
                  </span>
                  <div className="text-xs text-slate-500 dark:text-slate-400">
                    {initiative.allocationCount} allocation{initiative.allocationCount !== 1 ? 's' : ''}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl p-6 w-[420px] max-h-[90vh] overflow-y-auto">
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
                  Priority
                </label>
                <select
                  value={priority}
                  onChange={(e) => setPriority(parseInt(e.target.value) as InitiativePriority)}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                >
                  {PRIORITY_OPTIONS.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  OKR Objective
                </label>
                <input
                  type="text"
                  value={okrObjective}
                  onChange={(e) => setOkrObjective(e.target.value)}
                  placeholder="Which OKR does this support?"
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Color
                </label>
                <div className="flex flex-wrap gap-2">
                  {INITIATIVE_COLORS.map(c => (
                    <button
                      key={c}
                      type="button"
                      onClick={() => setColor(c)}
                      className={`w-7 h-7 rounded-full transition-transform ${color === c ? 'ring-2 ring-offset-2 ring-slate-400 scale-110' : 'hover:scale-110'}`}
                      style={{ backgroundColor: c }}
                    />
                  ))}
                </div>
              </div>

              {editingInitiative && (
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Status
                  </label>
                  <select
                    value={editingInitiative.status}
                    onChange={(e) => handleStatusChange(editingInitiative, parseInt(e.target.value) as InitiativeStatus)}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                  >
                    {STATUS_OPTIONS.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
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
