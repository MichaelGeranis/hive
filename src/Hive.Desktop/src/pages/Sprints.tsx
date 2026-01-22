import { useEffect, useState } from 'react'
import { Calendar, Plus, TrendingUp, Users, Edit2, Trash2, Save, Filter, X } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { sprintsApi, sprintCapacityApi, settingsApi } from '../services/api'
import type { Sprint, SprintCapacity, AppSettings } from '../types'

export default function Sprints() {
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [sprintCapacities, setSprintCapacities] = useState<SprintCapacity[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Unified sprint editing state (dates + capacity)
  const [editingSprint, setEditingSprint] = useState<{
    sprintId: string;
    startDate: string;
    endDate: string;
    points: number;
    members: number;
  } | null>(null)
  const [sprintSaving, setSprintSaving] = useState(false)
  const [sprintError, setSprintError] = useState<string | null>(null)

  // New sprint state
  const [creatingNewSprint, setCreatingNewSprint] = useState(false)
  const [teamName, setTeamName] = useState('')
  const [quarter, setQuarter] = useState(1)
  const [year, setYear] = useState(new Date().getFullYear())
  const [sprintNumber, setSprintNumber] = useState(1)

  // Sprint team filter settings
  const [settings, setSettings] = useState<AppSettings | null>(null)
  const [teamFilterInput, setTeamFilterInput] = useState('')
  const [savingFilter, setSavingFilter] = useState(false)

  // Generate sprint name from components
  const generateSprintName = () => {
    if (!teamName.trim()) return ''
    const yearShort = year.toString().slice(-2)
    return `${teamName}_${quarter}Q${yearShort}_S${sprintNumber}`
  }

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      setError(null)
      const [sprintsData, capacitiesData, settingsData] = await Promise.all([
        sprintsApi.getAll(),
        sprintCapacityApi.getAll(),
        settingsApi.get()
      ])
      // Sort sprints by year, quarter, sprint number (most recent first)
      const sortedSprints = sprintsData.sort((a, b) => {
        const aSort = a.year * 1000 + a.quarter * 100 + a.sprintNumber
        const bSort = b.year * 1000 + b.quarter * 100 + b.sprintNumber
        return bSort - aSort
      })
      setSprints(sortedSprints)
      setSprintCapacities(capacitiesData)
      setSettings(settingsData)
      setTeamFilterInput(settingsData.sprintTeamFilter || '')
    } catch (err) {
      console.error('Failed to load sprints', err)
      setError('Failed to load sprints. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const getCapacityForSprint = (sprintId: string): SprintCapacity | undefined => {
    return sprintCapacities.find(c => c.sprintId === sprintId)
  }

  const handleEditSprint = (sprint: Sprint) => {
    const existingCapacity = getCapacityForSprint(sprint.id)
    setEditingSprint({
      sprintId: sprint.id,
      startDate: sprint.startDate || '',
      endDate: sprint.endDate || '',
      points: existingCapacity?.totalCapacityPoints ?? 0,
      members: existingCapacity?.availableMembers ?? 0
    })
    setSprintError(null)
  }

  const handleSaveSprint = async () => {
    if (!editingSprint) return

    try {
      setSprintSaving(true)
      setSprintError(null)

      // Save dates and capacity in parallel
      await Promise.all([
        sprintsApi.update(editingSprint.sprintId, {
          startDate: editingSprint.startDate || undefined,
          endDate: editingSprint.endDate || undefined
        }),
        sprintCapacityApi.createOrUpdate({
          sprintId: editingSprint.sprintId,
          totalCapacityPoints: editingSprint.points,
          availableMembers: editingSprint.members
        })
      ])

      await loadData()
      setEditingSprint(null)
    } catch (err) {
      console.error('Failed to save sprint', err)
      setSprintError('Failed to save sprint. Please try again.')
    } finally {
      setSprintSaving(false)
    }
  }

  const handleCreateSprint = async () => {
    const sprintName = generateSprintName()
    if (!sprintName) {
      setError('Team name is required to create a sprint')
      return
    }

    try {
      setLoading(true)
      setError(null)
      await sprintsApi.create({ name: sprintName })
      await loadData()
      setCreatingNewSprint(false)
      // Reset form
      setTeamName('')
      setQuarter(1)
      setYear(new Date().getFullYear())
      setSprintNumber(1)
    } catch (err: any) {
      console.error('Failed to create sprint', err)
      setError(err.response?.data?.message || err.message || 'Failed to create sprint. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteSprint = async (sprintId: string, sprintName: string) => {
    if (!confirm(`Are you sure you want to delete sprint "${sprintName}"?`)) {
      return
    }

    try {
      setError(null)
      await sprintsApi.delete(sprintId)
      await loadData()
    } catch (err: any) {
      console.error('Failed to delete sprint', err)
      setError(err.response?.data || 'Failed to delete sprint. Please try again.')
    }
  }

  const handleSaveTeamFilter = async () => {
    try {
      setSavingFilter(true)
      setError(null)
      const trimmedFilter = teamFilterInput.trim()
      await settingsApi.update({
        sprintTeamFilter: trimmedFilter || null,
        clearSprintTeamFilter: !trimmedFilter
      })
      const updatedSettings = await settingsApi.get()
      setSettings(updatedSettings)
      setTeamFilterInput(updatedSettings.sprintTeamFilter || '')
    } catch (err: any) {
      console.error('Failed to save team filter', err)
      setError(err.response?.data?.message || 'Failed to save team filter. Please try again.')
    } finally {
      setSavingFilter(false)
    }
  }

  const handleClearTeamFilter = async () => {
    try {
      setSavingFilter(true)
      setError(null)
      await settingsApi.update({
        sprintTeamFilter: null,
        clearSprintTeamFilter: true
      })
      const updatedSettings = await settingsApi.get()
      setSettings(updatedSettings)
      setTeamFilterInput('')
    } catch (err: any) {
      console.error('Failed to clear team filter', err)
      setError(err.response?.data?.message || 'Failed to clear team filter. Please try again.')
    } finally {
      setSavingFilter(false)
    }
  }

  if (loading && sprints.length === 0) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Sprints</h1>
          <p className="text-slate-500 dark:text-slate-400">Manage sprints and capacity planning</p>
        </div>
        <button
          onClick={() => setCreatingNewSprint(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New Sprint
        </button>
      </div>

      {error && (
        <div className="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400">
          {error}
        </div>
      )}

      {/* Create New Sprint Form */}
      {creatingNewSprint && (
        <Card>
          <CardHeader title="Create New Sprint" subtitle="Add a new sprint to track" />
          <CardContent>
            <div className="space-y-4">
              <div className="p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
                <p className="text-sm text-blue-700 dark:text-blue-300">
                  Sprint names follow the format: <code className="font-mono bg-blue-100 dark:bg-blue-900 px-1 rounded">TeamName_QuarterQYearShort_SSprintNumber</code>
                </p>
                <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                  Example: LP_4Q25_S6 (Team LP, Q4 2025, Sprint 6)
                </p>
              </div>

              <div className="grid grid-cols-4 gap-4">
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Team Name *
                  </label>
                  <input
                    type="text"
                    value={teamName}
                    onChange={(e) => setTeamName(e.target.value)}
                    placeholder="e.g., LP"
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Quarter
                  </label>
                  <select
                    value={quarter}
                    onChange={(e) => setQuarter(parseInt(e.target.value))}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                  >
                    <option value={1}>Q1</option>
                    <option value={2}>Q2</option>
                    <option value={3}>Q3</option>
                    <option value={4}>Q4</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Year
                  </label>
                  <input
                    type="number"
                    value={year}
                    onChange={(e) => setYear(parseInt(e.target.value))}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Sprint Number
                </label>
                <input
                  type="number"
                  value={sprintNumber}
                  onChange={(e) => setSprintNumber(parseInt(e.target.value))}
                  min="1"
                  className="w-32 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                />
              </div>

              {/* Sprint Name Preview */}
              {teamName && (
                <div className="p-3 bg-slate-100 dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700">
                  <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                    Generated Sprint Name:
                  </label>
                  <div className="font-mono text-lg text-slate-900 dark:text-slate-100">
                    {generateSprintName()}
                  </div>
                </div>
              )}

              <div className="flex gap-3 pt-4 border-t dark:border-slate-700">
                <button
                  onClick={handleCreateSprint}
                  disabled={loading || !teamName}
                  className="flex items-center gap-2 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Save className="w-5 h-5" />
                  Create Sprint
                </button>
                <button
                  onClick={() => {
                    setCreatingNewSprint(false)
                    setTeamName('')
                    setQuarter(1)
                    setYear(new Date().getFullYear())
                    setSprintNumber(1)
                  }}
                  className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Sprint Configuration */}
      <Card>
        <CardHeader
          title="Sprint Configuration"
          subtitle="Configure dates, committed points, and capacity for each sprint"
        />
        <CardContent>
          <div className="space-y-4">
            {/* Team Filter Setting */}
            <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
              <div className="flex items-start gap-3">
                <Filter className="w-5 h-5 text-blue-500 dark:text-blue-400 mt-0.5 shrink-0" />
                <div className="flex-1 space-y-3">
                  <div>
                    <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100">Sprint Import Team Filter</h4>
                    <p className="text-xs text-blue-700 dark:text-blue-300 mt-1">
                      When set, only sprints from this team will be imported. Leave empty to import all teams.
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <input
                      type="text"
                      value={teamFilterInput}
                      onChange={(e) => setTeamFilterInput(e.target.value)}
                      placeholder="e.g., LP"
                      className="flex-1 max-w-xs px-3 py-2 text-sm border border-blue-300 dark:border-blue-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                    <button
                      onClick={handleSaveTeamFilter}
                      disabled={savingFilter || teamFilterInput === (settings?.sprintTeamFilter || '')}
                      className="px-3 py-2 text-sm bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {savingFilter ? 'Saving...' : 'Save'}
                    </button>
                    {settings?.sprintTeamFilter && (
                      <button
                        onClick={handleClearTeamFilter}
                        disabled={savingFilter}
                        className="p-2 text-blue-600 dark:text-blue-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-blue-100 dark:hover:bg-blue-800 rounded-lg transition-colors"
                        title="Clear filter"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                  {settings?.sprintTeamFilter && (
                    <div className="flex items-center gap-2 text-xs">
                      <span className="text-blue-700 dark:text-blue-300">Active filter:</span>
                      <span className="px-2 py-0.5 bg-blue-200 dark:bg-blue-800 text-blue-800 dark:text-blue-200 rounded font-mono">
                        {settings.sprintTeamFilter}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {sprints.length === 0 ? (
              <div className="p-8 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                <Calendar className="w-12 h-12 text-slate-400 mx-auto mb-3" />
                <p className="text-slate-500 dark:text-slate-400">
                  No sprints found. Create a sprint or import tasks with sprint data to get started.
                </p>
              </div>
            ) : (
              <div className="space-y-2 max-h-[600px] overflow-y-auto">
                {sprints.map(sprint => {
                  const capacity = getCapacityForSprint(sprint.id)
                  const isEditing = editingSprint?.sprintId === sprint.id

                  return (
                    <div
                      key={sprint.id}
                      className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                      {isEditing ? (
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <div>
                              <div className="font-medium text-slate-900 dark:text-slate-100">
                                {sprint.name}
                              </div>
                              <div className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                                {sprint.teamName} | Q{sprint.quarter} {sprint.year} | Sprint #{sprint.sprintNumber}
                              </div>
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-4">
                            <div className="flex items-center gap-2">
                              <Calendar className="w-4 h-4 text-slate-400 shrink-0" />
                              <label className="text-xs text-slate-500 dark:text-slate-400 shrink-0">Start:</label>
                              <input
                                type="date"
                                value={editingSprint.startDate}
                                onChange={(e) => setEditingSprint({
                                  ...editingSprint,
                                  startDate: e.target.value
                                })}
                                className="flex-1 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded focus:ring-2 focus:ring-amber-500"
                              />
                            </div>
                            <div className="flex items-center gap-2">
                              <Calendar className="w-4 h-4 text-slate-400 shrink-0" />
                              <label className="text-xs text-slate-500 dark:text-slate-400 shrink-0">End:</label>
                              <input
                                type="date"
                                value={editingSprint.endDate}
                                onChange={(e) => setEditingSprint({
                                  ...editingSprint,
                                  endDate: e.target.value
                                })}
                                className="flex-1 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded focus:ring-2 focus:ring-amber-500"
                              />
                            </div>
                            <div className="flex items-center gap-2">
                              <TrendingUp className="w-4 h-4 text-slate-400 shrink-0" />
                              <label className="text-xs text-slate-500 dark:text-slate-400 shrink-0">Committed:</label>
                              <input
                                type="number"
                                value={editingSprint.points}
                                onChange={(e) => setEditingSprint({
                                  ...editingSprint,
                                  points: parseInt(e.target.value) || 0
                                })}
                                className="w-24 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded focus:ring-2 focus:ring-amber-500"
                                min="0"
                                placeholder="Points"
                              />
                              <span className="text-xs text-slate-500 dark:text-slate-400">SP</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <Users className="w-4 h-4 text-slate-400 shrink-0" />
                              <label className="text-xs text-slate-500 dark:text-slate-400 shrink-0">Team:</label>
                              <input
                                type="number"
                                value={editingSprint.members}
                                onChange={(e) => setEditingSprint({
                                  ...editingSprint,
                                  members: parseInt(e.target.value) || 0
                                })}
                                className="w-20 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded focus:ring-2 focus:ring-amber-500"
                                min="0"
                                placeholder="Members"
                              />
                              <span className="text-xs text-slate-500 dark:text-slate-400">people</span>
                            </div>
                          </div>

                          <div className="flex items-center gap-3 pt-2 border-t dark:border-slate-600">
                            <button
                              onClick={handleSaveSprint}
                              disabled={sprintSaving}
                              className="px-3 py-2 text-sm bg-green-500 text-white rounded hover:bg-green-600 disabled:opacity-50 transition-colors"
                            >
                              {sprintSaving ? 'Saving...' : 'Save'}
                            </button>
                            <button
                              onClick={() => setEditingSprint(null)}
                              className="px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : (
                        <div className="flex items-center gap-4">
                          <div className="flex-1 min-w-0">
                            <div className="font-medium text-slate-900 dark:text-slate-100 truncate">
                              {sprint.name}
                            </div>
                            <div className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                              {sprint.teamName} | Q{sprint.quarter} {sprint.year} | Sprint #{sprint.sprintNumber}
                            </div>
                          </div>

                          <div className="flex items-center gap-6 text-sm">
                            {sprint.startDate || sprint.endDate ? (
                              <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
                                <Calendar className="w-4 h-4" />
                                <span className="font-medium">
                                  {sprint.startDate ? new Date(sprint.startDate).toLocaleDateString() : '?'}
                                </span>
                                <span className="text-xs">→</span>
                                <span className="font-medium">
                                  {sprint.endDate ? new Date(sprint.endDate).toLocaleDateString() : '?'}
                                </span>
                              </div>
                            ) : (
                              <span className="text-slate-400 dark:text-slate-500 italic text-xs">
                                No dates
                              </span>
                            )}

                            {capacity ? (
                              <>
                                <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
                                  <TrendingUp className="w-4 h-4" />
                                  <span className="font-medium">{capacity.totalCapacityPoints}</span>
                                  <span className="text-xs">SP</span>
                                </div>
                                <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
                                  <Users className="w-4 h-4" />
                                  <span className="font-medium">{capacity.availableMembers}</span>
                                  <span className="text-xs">people</span>
                                </div>
                              </>
                            ) : (
                              <span className="text-slate-400 dark:text-slate-500 italic text-xs">
                                No capacity
                              </span>
                            )}
                          </div>

                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleEditSprint(sprint)}
                              className="p-2 text-slate-600 dark:text-slate-400 hover:text-amber-600 dark:hover:text-amber-400 hover:bg-slate-200 dark:hover:bg-slate-600 rounded transition-colors"
                              title="Edit sprint"
                            >
                              <Edit2 className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleDeleteSprint(sprint.id, sprint.name)}
                              className="p-2 text-slate-600 dark:text-slate-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-slate-200 dark:hover:bg-slate-600 rounded transition-colors"
                              title="Delete sprint"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            )}

            {sprintError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {sprintError}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
