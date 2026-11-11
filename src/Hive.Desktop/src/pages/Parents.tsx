import { useEffect, useState, useCallback, useMemo } from 'react'
import { Plus, Layers, Search, Tag, Edit, Trash2, MoreVertical, CheckCircle, X, Zap, CheckCheck, Package } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import { parentsApi, settingsApi, jiraSyncApi } from '../services/api'
import type { Parent } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, LoadingState } from '../components/PageState'
import { JiraSyncButton } from '../components/JiraSyncButton'
import { IssueTypeBadge } from '../components/IssueTypeBadge'
import { issueTypeColor } from '../utils/issueTypeDisplay'
import CollapsibleTags from '../components/CollapsibleTags'
import { describeParentSync } from '../utils/jiraSync'

const HIDE_DONE_KEY = 'hive-parents-hide-done'

function readHideDone(): boolean {
  try {
    return localStorage.getItem(HIDE_DONE_KEY) === 'true'
  } catch {
    return false
  }
}

// Parents that are not from Jira have no issue type
const OTHER_TYPE = 'Other'

const TYPE_PLURALS: Record<string, string> = { Epic: 'Epics', Story: 'Stories', Task: 'Tasks', Bug: 'Bugs', Spike: 'Spikes' }

// "Epic" reads "Epics"; a type with no known plural, such as Support, is shown as it is
const pluralType = (type: string) => TYPE_PLURALS[type] ?? type

// Epics first, parents without a type last, the rest alphabetically
const typeOrder = (a: string, b: string) => {
  const rank = (t: string) => (t === 'Epic' ? 0 : t === OTHER_TYPE ? 2 : 1)
  return rank(a) - rank(b) || a.localeCompare(b)
}

export default function Parents() {
  const { showError, showSuccess, showWarning } = useToast()
  const confirm = useConfirm()
  const [parents, setParents] = useState<Parent[]>([])
  const [jiraBrowseUrl, setJiraBrowseUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    name: '',
    labels: ''
  })

  const resetForm = () => {
    setFormData({ name: '', labels: '' })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  useEffect(() => {
    loadParents()
    // Only needed to link a parent to its epic; the page works without it
    settingsApi.get()
      .then(settings => setJiraBrowseUrl(settings.jiraBrowseUrl ?? null))
      .catch(() => setJiraBrowseUrl(null))
  }, [])

  // Only parents come from Jira on this page: one per epic in the configured space
  const handleJiraSync = async () => {
    try {
      const messages = describeParentSync(await jiraSyncApi.syncParents())
      showSuccess(messages.success)
      if (messages.warning) showWarning(messages.warning)
      await loadParents({ quiet: true })
    } catch (err) {
      console.error('Failed to sync parents from Jira:', err)
      showError(getErrorMessage(err))
    }
  }

  // A quiet reload refreshes the list without swapping the page for the loading state
  const loadParents = async ({ quiet = false }: { quiet?: boolean } = {}) => {
    try {
      if (!quiet) setLoading(true)
      const data = await parentsApi.getAll()
      setParents(data)
    } catch (err) {
      console.error(err)
    } finally {
      if (!quiet) setLoading(false)
    }
  }

  const [selectedLabel, setSelectedLabel] = useState<string | null>(null)
  const [selectedType, setSelectedType] = useState<string | null>(null)
  const [hideDone, setHideDone] = useState(readHideDone)

  const toggleHideDone = () => {
    const next = !hideDone
    setHideDone(next)
    try {
      localStorage.setItem(HIDE_DONE_KEY, String(next))
    } catch {
      // Not remembered, but the toggle still works for this visit
    }
  }

  const doneCount = parents.filter(p => p.isDone).length

  // The Jira issue types present, epics first, so "Epic" is always the first choice
  const allTypes = useMemo(() => {
    const types = new Set(parents.map(p => p.jiraIssueType).filter((t): t is string => !!t))
    return Array.from(types).sort((a, b) =>
      a === 'Epic' ? -1 : b === 'Epic' ? 1 : a.localeCompare(b))
  }, [parents])

  // Get all unique labels from parents
  const allLabels = useMemo(() => {
    const labelSet = new Set<string>()
    parents.forEach(p => {
      if (p.labels) {
        p.labels.split(',').forEach(label => {
          const trimmed = label.trim()
          if (trimmed) labelSet.add(trimmed)
        })
      }
    })
    return Array.from(labelSet).sort()
  }, [parents])

  const clearFilters = () => {
    setSearchInput('')
    setSearchQuery('')
    setSelectedLabel(null)
    setSelectedType(null)
  }

  // The parents shown, after the search, done, type and label filters
  const visibleParents = useMemo(() => {
    let result = parents

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(p =>
        p.name.toLowerCase().includes(query) ||
        p.labels?.toLowerCase().includes(query)
      )
    }

    if (hideDone) {
      result = result.filter(p => !p.isDone)
    }

    // Apply issue type filter
    if (selectedType) {
      result = result.filter(p => p.jiraIssueType === selectedType)
    }

    // Apply label filter
    if (selectedLabel) {
      result = result.filter(p =>
        p.labels?.split(',').some(label => label.trim().toLowerCase() === selectedLabel.toLowerCase())
      )
    }

    return result
  }, [parents, searchQuery, hideDone, selectedType, selectedLabel])

  // Calculate totals from filtered parents
  // Done versus total for the parents shown: per Jira issue type, then their tasks and points.
  // "Done" is the backend's call (isDone, completedTasks, completedStoryPoints).
  const totals = useMemo(() => {
    const filtered = visibleParents
    const byType = new Map<string, { done: number; total: number }>()
    filtered.forEach(p => {
      const type = p.jiraIssueType || OTHER_TYPE
      const entry = byType.get(type) ?? { done: 0, total: 0 }
      entry.total++
      if (p.isDone) entry.done++
      byType.set(type, entry)
    })

    return {
      types: Array.from(byType, ([type, counts]) => ({ type, ...counts })).sort((a, b) => typeOrder(a.type, b.type)),
      totalTasks: filtered.reduce((sum, p) => sum + p.totalTasks, 0),
      completedTasks: filtered.reduce((sum, p) => sum + p.completedTasks, 0),
      totalStoryPoints: filtered.reduce((sum, p) => sum + p.totalStoryPoints, 0),
      completedStoryPoints: filtered.reduce((sum, p) => sum + p.completedStoryPoints, 0)
    }
  }, [visibleParents])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const payload = {
        name: formData.name,
        labels: formData.labels || undefined
      }
      if (editingId) {
        await parentsApi.update(editingId, payload)
      } else {
        await parentsApi.create(payload)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadParents()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (parent: Parent) => {
    setFormData({
      name: parent.name,
      labels: parent.labels || ''
    })
    setEditingId(parent.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    const confirmed = await confirm({
      title: 'Delete this parent?',
      message: 'Its tasks stay, no longer grouped under it.',
      confirmLabel: 'Delete'
    })
    if (!confirmed) return
    try {
      await parentsApi.delete(id)
      loadParents()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  if (loading) {
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Parents"
        subtitle="Track task groups"
        actions={
          <>
            <JiraSyncButton onSync={handleJiraSync} title="Add and update a parent for every epic in the Jira space" />
            <button
              onClick={() => setShowForm(true)}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
            >
              <Plus className="w-5 h-5" />
              New Parent
            </button>
          </>
        }
      />

      {/* Form Modal */}
      {showForm && (
        <Modal title={editingId ? 'Edit Parent' : 'Create Parent'} onClose={closeModal}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Name</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Labels</label>
              <input
                type="text"
                value={formData.labels}
                onChange={(e) => setFormData({ ...formData, labels: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                placeholder="frontend, backend, urgent (comma-separated)"
              />
              <p className="mt-1 text-xs text-slate-500">
                Your own labels, for search and filtering. Syncing from Jira never changes them
              </p>
            </div>
            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={() => { setShowForm(false); setEditingId(null); resetForm() }}
                className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
              >
                {editingId ? 'Update' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search parents... (press Enter)"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
          />
          {(searchInput || selectedLabel || selectedType) && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Jira issue types */}
        {allTypes.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap" role="group" aria-label="Filter by type">
            <Layers className="w-4 h-4 text-slate-400" />
            {allTypes.map((type) => (
              <button
                key={type}
                onClick={() => setSelectedType(selectedType === type ? null : type)}
                aria-pressed={selectedType === type}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedType === type
                    ? `${issueTypeColor(type)} ring-2 ring-offset-1 ring-current`
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                }`}
              >
                {type === 'Epic' ? 'Epics' : type}
              </button>
            ))}
          </div>
        )}

        {/* Done parents */}
        {doneCount > 0 && (
          <div className="flex items-center gap-2">
            <CheckCheck className="w-4 h-4 text-slate-400" />
            <button
              onClick={toggleHideDone}
              aria-pressed={hideDone}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                hideDone
                  ? 'bg-amber-500 text-white'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
            >
              {hideDone ? `Show done (${doneCount})` : `Hide done (${doneCount})`}
            </button>
          </div>
        )}

        {/* Labels/Tags */}
        {allLabels.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Tag className="w-4 h-4 text-slate-400" />
            <CollapsibleTags
              items={allLabels}
              alwaysShow={(label) => label === selectedLabel}
              renderItem={(label) => (
                <button
                  key={label}
                  onClick={() => setSelectedLabel(selectedLabel === label ? null : label)}
                  className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                    selectedLabel === label
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {label}
                </button>
              )}
            />
          </div>
        )}
      </div>

      {/* Totals Summary Bar */}
      {visibleParents.length > 0 && (
        <div
          className="flex items-center gap-x-5 gap-y-2 flex-wrap px-4 py-2.5 bg-slate-50 rounded-lg border border-slate-200"
          aria-label="Totals"
        >
          <span className="text-sm text-slate-500 font-medium">Totals:</span>
          {totals.types.map(({ type, done, total }) => (
            <span key={type} className="flex items-center gap-1.5 text-sm text-slate-600">
              <span className={`px-2 py-0.5 text-xs font-medium rounded-full whitespace-nowrap ${issueTypeColor(type)}`}>
                {pluralType(type)}
              </span>
              {done}/{total}
            </span>
          ))}
          <span className="flex items-center gap-1 text-sm text-slate-600">
            <CheckCheck className="w-4 h-4" />
            {totals.completedTasks}/{totals.totalTasks} tasks
          </span>
          <span className="flex items-center gap-1 text-sm font-semibold text-amber-600">
            <Zap className="w-4 h-4" />
            {totals.completedStoryPoints}/{totals.totalStoryPoints} SP
          </span>
        </div>
      )}

      {/* Parents Grid */}
      {visibleParents.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Layers />}
            title={parents.length === 0
              ? 'No parents found. Parents are automatically created during CSV import when tasks have a "Parent" column.'
              : 'No parents match your search'}
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {visibleParents.map((parent) => (
            <Card key={parent.id}>
              <CardContent>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3 flex-1">
                    <div className="p-2 bg-indigo-100 rounded-lg">
                      <Layers className="w-5 h-5 text-indigo-600" />
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold text-slate-900">{parent.name}</h3>
                        {parent.jiraKey && (jiraBrowseUrl ? (
                          <a
                            href={`${jiraBrowseUrl}/${parent.jiraKey}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            title={`Open ${parent.jiraKey} in Jira`}
                            className="text-xs font-mono text-blue-600 hover:text-blue-800 hover:underline whitespace-nowrap"
                          >
                            {parent.jiraKey}
                          </a>
                        ) : (
                          <span className="text-xs font-mono text-slate-500 whitespace-nowrap">{parent.jiraKey}</span>
                        ))}
                        {parent.jiraIssueType && <IssueTypeBadge type={parent.jiraIssueType} />}
                      </div>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 rounded-sm">
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(parent)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(parent.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>

                {parent.labels && (
                  <div className="flex items-center gap-2 mb-3">
                    <Tag className="w-4 h-4 text-slate-400" />
                    <div className="flex flex-wrap gap-1">
                      <CollapsibleTags
                        items={parent.labels.split(',')}
                        renderItem={(label, idx) => (
                          <span
                            key={idx}
                            className="px-2 py-0.5 text-xs bg-slate-100 text-slate-600 rounded-full"
                          >
                            {label.trim()}
                          </span>
                        )}
                      />
                    </div>
                  </div>
                )}

                {parent.components && (
                  <div className="flex items-center gap-2 mb-3" role="group" aria-label={`Products of ${parent.name}`}>
                    <Package className="w-4 h-4 text-slate-400" />
                    <div className="flex flex-wrap gap-1">
                      <CollapsibleTags
                        items={parent.components.split(',')}
                        renderItem={(component, idx) => (
                          <span
                            key={idx}
                            className="px-2 py-0.5 text-xs bg-amber-50 text-amber-700 rounded-full"
                          >
                            {component.trim()}
                          </span>
                        )}
                      />
                    </div>
                  </div>
                )}

                {/* Stats - only show if there are countable tasks */}
                {parent.totalTasks > 0 && (
                  <>
                    <div className="grid grid-cols-2 gap-3 mt-4">
                      <div className="flex items-center gap-2 text-sm">
                        <CheckCircle className="w-4 h-4 text-green-500" />
                        <span className="text-slate-600">
                          {parent.completedTasks}/{parent.totalTasks} tasks
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-sm">
                        <span className="text-slate-600">
                          {parent.completedStoryPoints}/{parent.totalStoryPoints} SP
                        </span>
                      </div>
                    </div>

                    {/* Progress */}
                    <div className="mt-4">
                      <div className="flex justify-between text-sm mb-1">
                        <span className="text-slate-500">Progress</span>
                        <span className="font-medium text-slate-900">
                          {Math.round((parent.completedTasks / parent.totalTasks) * 100)}%
                        </span>
                      </div>
                      <div className="w-full bg-slate-100 rounded-full h-2">
                        <div
                          className="bg-amber-500 h-2 rounded-full transition-all"
                          style={{
                            width: `${(parent.completedTasks / parent.totalTasks) * 100}%`
                          }}
                        />
                      </div>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
