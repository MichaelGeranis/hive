import { useEffect, useState } from 'react'
import { Save, Sun, Moon, Monitor, CheckCircle, AlertCircle, XCircle, Clock, Download, Database, Calendar, RefreshCw } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { settingsApi, backupApi } from '../services/api'
import { useTheme } from '../contexts/ThemeContext'
import type { StoryPointMapping, RestoreResultDto } from '../types'

const DEFAULT_MAPPINGS: StoryPointMapping[] = [
  { points: 1, hours: 2, label: '1 SP = 2 hours' },
  { points: 2, hours: 4, label: '2 SP = 4 hours (4 hours)' },
  { points: 3, hours: 8, label: '3 SP = 8 hours (1 day)' },
  { points: 5, hours: 24, label: '5 SP = 24 hours (3 days)' },
  { points: 8, hours: 72, label: '8 SP = 72 hours (1 sprint)' },
  { points: 13, hours: 150, label: '13 SP = 150 hours (2 sprints)' },
  { points: 21, hours: 240, label: '21 SP = 240 hours (1 month)' }
]

export default function Settings() {
  const { theme, setTheme, schedule, setSchedule } = useTheme()
  const [mappings, setMappings] = useState<StoryPointMapping[]>(DEFAULT_MAPPINGS)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Backup & Restore state
  const [backupLoading, setBackupLoading] = useState(false)
  const [restoreLoading, setRestoreLoading] = useState(false)
  const [restoreResult, setRestoreResult] = useState<RestoreResultDto | null>(null)
  const [backupError, setBackupError] = useState<string | null>(null)

  // Calendar Sync state
  const [calendarSyncing, setCalendarSyncing] = useState(false)
  const [calendarSyncResult, setCalendarSyncResult] = useState<any>(null)
  const [calendarSyncError, setCalendarSyncError] = useState<string | null>(null)
  const [calendarAccessChecked, setCalendarAccessChecked] = useState(false)
  const [hasCalendarAccess, setHasCalendarAccess] = useState(false)

  useEffect(() => {
    loadSettings()
    checkCalendarAccess()
  }, [])

  const loadSettings = async () => {
    try {
      setLoading(true)
      setError(null)
      const settings = await settingsApi.get()
      setMappings(settings.storyPointMappings)
    } catch (err) {
      console.error('Failed to load settings', err)
      setError('Failed to load settings. Using default values.')
      setMappings(DEFAULT_MAPPINGS)
    } finally {
      setLoading(false)
    }
  }

  const handleHoursChange = (index: number, value: string) => {
    const hours = parseFloat(value)
    if (isNaN(hours) || hours < 0) return

    const newMappings = [...mappings]
    newMappings[index] = { ...newMappings[index], hours }
    setMappings(newMappings)
    setSaved(false)
  }

  const handleSave = async () => {
    try {
      setSaving(true)
      setError(null)
      await settingsApi.update({ storyPointMappings: mappings })
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save settings', err)
      setError('Failed to save settings. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  const handleReset = async () => {
    try {
      setSaving(true)
      setError(null)
      await settingsApi.update({ storyPointMappings: DEFAULT_MAPPINGS })
      setMappings(DEFAULT_MAPPINGS)
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      console.error('Failed to reset settings', err)
      setError('Failed to reset settings. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  const getDaysLabel = (hours: number): string => {
    if (hours === 0) return '0 hours'
    if (hours < 8) return `${hours} hour${hours !== 1 ? 's' : ''}`
    
    const days = hours / 8
    if (days === 1) return '1 day'
    if (days < 1) return `${hours} hours`
    if (Number.isInteger(days)) return `${days} days`
    return `${days.toFixed(1)} days`
  }

  // Backup & Restore handlers
  const handleExportBackup = async () => {
    try {
      setBackupLoading(true)
      setBackupError(null)
      const backup = await backupApi.export()

      // Download as JSON file
      const blob = new Blob([JSON.stringify(backup, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `hive-backup-${new Date().toISOString().split('T')[0]}.json`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch (err: any) {
      console.error('Failed to export backup', err)
      setBackupError(err.response?.data?.message || err.message || 'Failed to export backup')
    } finally {
      setBackupLoading(false)
    }
  }

  const handleRestoreFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = async (e) => {
      try {
        setRestoreLoading(true)
        setBackupError(null)
        setRestoreResult(null)

        const content = e.target?.result as string
        const backup = JSON.parse(content)

        if (!backup.version || !backup.exportedAt) {
          throw new Error('Invalid backup file format')
        }

        if (!confirm(`Restore backup from ${new Date(backup.exportedAt).toLocaleString()}?\n\nThis will import data but will not overwrite existing records.`)) {
          setRestoreLoading(false)
          return
        }

        const result = await backupApi.import(backup)
        setRestoreResult(result)
      } catch (err: any) {
        console.error('Failed to restore backup', err)
        setBackupError(err.message || 'Failed to restore backup')
      } finally {
        setRestoreLoading(false)
      }
    }
    reader.onerror = () => {
      setBackupError('Failed to read file')
    }
    reader.readAsText(file)

    // Reset file input
    event.target.value = ''
  }

  // Calendar sync handlers
  const checkCalendarAccess = async () => {
    // Only available in Electron on macOS
    if (!window.electronAPI?.calendar || window.electronAPI.platform !== 'darwin') {
      setCalendarAccessChecked(true)
      setHasCalendarAccess(false)
      return
    }

    try {
      const result = await window.electronAPI.calendar.checkAccess()
      setHasCalendarAccess(result.hasAccess || false)
    } catch (error) {
      console.error('Failed to check calendar access', error)
      setHasCalendarAccess(false)
    } finally {
      setCalendarAccessChecked(true)
    }
  }

  const handleCalendarSync = async () => {
    if (!window.electronAPI?.calendar) {
      setCalendarSyncError('Calendar sync is only available in the desktop app')
      return
    }

    try {
      setCalendarSyncing(true)
      setCalendarSyncError(null)
      setCalendarSyncResult(null)

      const result = await window.electronAPI.calendar.sync('michael.geranis@orfium.com')

      if (result.success) {
        setCalendarSyncResult(result.result)
      } else {
        setCalendarSyncError(result.error || 'Failed to sync calendar')
      }
    } catch (error: any) {
      console.error('Calendar sync failed', error)
      setCalendarSyncError(error.message || 'Failed to sync calendar')
    } finally {
      setCalendarSyncing(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Settings</h1>
        <p className="text-slate-500 dark:text-slate-400 mt-1">Configure application preferences</p>
      </div>

      {/* Appearance Settings */}
      <Card>
        <CardHeader
          title="Appearance"
          subtitle="Customize how Hive looks"
        />
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">
                Theme
              </label>
              <div className="flex flex-wrap gap-3">
                <button
                  onClick={() => setTheme('light')}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    theme === 'light'
                      ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                      : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                  }`}
                >
                  <Sun className="w-4 h-4" />
                  Light
                </button>
                <button
                  onClick={() => setTheme('dark')}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    theme === 'dark'
                      ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                      : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                  }`}
                >
                  <Moon className="w-4 h-4" />
                  Dark
                </button>
                <button
                  onClick={() => setTheme('system')}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    theme === 'system'
                      ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                      : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                  }`}
                >
                  <Monitor className="w-4 h-4" />
                  System
                </button>
                <button
                  onClick={() => setTheme('schedule')}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    theme === 'schedule'
                      ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                      : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                  }`}
                >
                  <Clock className="w-4 h-4" />
                  Schedule
                </button>
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                {theme === 'system'
                  ? 'Automatically matches your system preferences'
                  : theme === 'schedule'
                    ? 'Automatically switches based on time of day'
                    : `Using ${theme} mode`}
              </p>
            </div>

            {/* Schedule Settings */}
            {theme === 'schedule' && (
              <div className="pt-4 border-t dark:border-slate-700">
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">
                  Dark Mode Schedule
                </label>
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <label className="text-sm text-slate-600 dark:text-slate-400">Dark from</label>
                    <input
                      type="time"
                      value={schedule.darkStart}
                      onChange={(e) => setSchedule({ ...schedule, darkStart: e.target.value })}
                      className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    />
                  </div>
                  <div className="flex items-center gap-2">
                    <label className="text-sm text-slate-600 dark:text-slate-400">to</label>
                    <input
                      type="time"
                      value={schedule.darkEnd}
                      onChange={(e) => setSchedule({ ...schedule, darkEnd: e.target.value })}
                      className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    />
                  </div>
                </div>
                <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                  Dark mode will be active from {schedule.darkStart} to {schedule.darkEnd}
                </p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Story Points Mapping */}
      <Card>
        <CardHeader 
          title="Story Points to Hours Mapping" 
          subtitle="Configure how story points translate to estimated hours"
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Use this to compare estimated story points against actual hours worked logged by team members.
            </p>

            <div className="space-y-3">
              {mappings.map((mapping, index) => (
                <div key={mapping.points} className="flex items-center gap-4 p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <div className="flex-shrink-0 w-16">
                    <span className="text-lg font-semibold text-slate-900 dark:text-slate-100">{mapping.points} SP</span>
                  </div>
                  <span className="text-slate-500 dark:text-slate-400">=</span>
                  <div className="flex items-center gap-2">
                    <input
                      type="number"
                      value={mapping.hours}
                      onChange={(e) => handleHoursChange(index, e.target.value)}
                      className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                      min="0"
                      step="0.5"
                      disabled={saving}
                    />
                    <span className="text-slate-700 dark:text-slate-300">hours</span>
                  </div>
                  <div className="flex-1">
                    <span className="text-sm text-slate-500 dark:text-slate-400">({getDaysLabel(mapping.hours)})</span>
                  </div>
                </div>
              ))}
            </div>

            {error && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {error}
              </div>
            )}

            <div className="pt-4 border-t dark:border-slate-700 flex gap-3">
              <button
                onClick={handleSave}
                disabled={saving}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Save className="w-5 h-5" />
                {saving ? 'Saving...' : saved ? 'Saved!' : 'Save Changes'}
              </button>
              <button
                onClick={handleReset}
                disabled={saving}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Reset to Defaults
              </button>
            </div>

            {saved && (
              <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-400 text-sm">
                Settings saved successfully!
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Backup & Restore */}
      <Card>
        <CardHeader
          title="Backup & Restore"
          subtitle="Export all data to JSON or restore from a backup"
        />
        <CardContent>
          <div className="space-y-4">
            {/* Export Section */}
            <div>
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Export Data</h3>
              <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
                Download a complete backup of all your data including team members, projects, tasks, reviews, meetings, notes, and settings.
              </p>
              <button
                onClick={handleExportBackup}
                disabled={backupLoading}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Download className="w-5 h-5" />
                {backupLoading ? 'Exporting...' : 'Export Backup'}
              </button>
            </div>

            {/* Restore Section */}
            <div className="pt-4 border-t dark:border-slate-700">
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Restore Data</h3>
              <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
                Import data from a previously exported backup file. Existing records will not be overwritten.
              </p>
              <label className="flex items-center gap-2 px-4 py-2 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors cursor-pointer w-fit">
                <Database className="w-5 h-5" />
                {restoreLoading ? 'Restoring...' : 'Choose Backup File'}
                <input
                  type="file"
                  accept=".json"
                  onChange={handleRestoreFileChange}
                  className="hidden"
                  disabled={restoreLoading}
                />
              </label>
            </div>

            {/* Error Message */}
            {backupError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm flex items-center gap-2">
                <XCircle className="w-4 h-4 flex-shrink-0" />
                {backupError}
              </div>
            )}

            {/* Restore Result */}
            {restoreResult && (
              <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Restore Results</h3>

                {restoreResult.success ? (
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                    <div className="flex items-center gap-2 text-green-700 dark:text-green-400 mb-2">
                      <CheckCircle className="w-5 h-5" />
                      <span className="font-medium">Restore completed successfully!</span>
                    </div>
                  </div>
                ) : (
                  <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                    <div className="flex items-center gap-2 text-yellow-700 dark:text-yellow-400 mb-2">
                      <AlertCircle className="w-5 h-5" />
                      <span className="font-medium">Restore completed with some issues</span>
                    </div>
                  </div>
                )}

                <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.directReportsRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Team Members</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.projectsRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Projects</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.tasksRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Tasks</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.meetingsRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Meetings</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.performanceReviewsRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Reviews</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.leavesRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Leaves</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.managerNotesRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Notes</div>
                  </div>
                  <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                    <div className="text-xl font-bold text-slate-900 dark:text-slate-100">{restoreResult.documentsRestored}</div>
                    <div className="text-xs text-slate-500 dark:text-slate-400">Documents</div>
                  </div>
                </div>

                {restoreResult.warnings.length > 0 && (
                  <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                    <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2">Warnings ({restoreResult.warnings.length})</h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500 max-h-24 overflow-y-auto">
                      {restoreResult.warnings.slice(0, 10).map((warning, idx) => (
                        <li key={idx}>{warning}</li>
                      ))}
                      {restoreResult.warnings.length > 10 && (
                        <li>...and {restoreResult.warnings.length - 10} more</li>
                      )}
                    </ul>
                  </div>
                )}

                {restoreResult.errors.length > 0 && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                    <h4 className="text-sm font-medium text-red-800 dark:text-red-400 mb-2">Errors ({restoreResult.errors.length})</h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-red-700 dark:text-red-500 max-h-24 overflow-y-auto">
                      {restoreResult.errors.slice(0, 10).map((error, idx) => (
                        <li key={idx}>{error}</li>
                      ))}
                      {restoreResult.errors.length > 10 && (
                        <li>...and {restoreResult.errors.length - 10} more</li>
                      )}
                    </ul>
                  </div>
                )}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Apple Calendar Sync */}
      {calendarAccessChecked && (
        <Card>
          <CardHeader
            title="Apple Calendar Sync"
            subtitle="Automatically sync 1:1 meetings from your Apple Calendar"
          />
          <CardContent>
            <div className="space-y-4">
              {!hasCalendarAccess && window.electronAPI?.platform === 'darwin' ? (
                <>
                  <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                    <div className="flex items-center gap-2 text-yellow-700 dark:text-yellow-400">
                      <AlertCircle className="w-5 h-5" />
                      <span className="font-medium">Calendar access needed</span>
                    </div>
                    <p className="mt-2 text-sm text-yellow-600 dark:text-yellow-500">
                      Click the button below to request Calendar access. macOS will show a permission dialog.
                    </p>
                  </div>

                  <div className="pt-4 border-t dark:border-slate-700">
                    <button
                      onClick={handleCalendarSync}
                      disabled={calendarSyncing}
                      className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {calendarSyncing ? (
                        <>
                          <RefreshCw className="w-5 h-5 animate-spin" />
                          Requesting access...
                        </>
                      ) : (
                        <>
                          <Calendar className="w-5 h-5" />
                          Request Calendar Access
                        </>
                      )}
                    </button>
                    <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                      After granting access, you can find Hive in System Settings → Privacy & Security → Calendar
                    </p>
                  </div>

                  {/* Sync Error */}
                  {calendarSyncError && (
                    <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                      <div className="flex items-center gap-2 mb-2">
                        <XCircle className="w-4 h-4 flex-shrink-0" />
                        <span className="font-medium">Calendar sync failed</span>
                      </div>
                      <p className="text-sm">{calendarSyncError.replace('PERMISSION_ERROR: ', '')}</p>
                      {calendarSyncError.includes('Automation') && (
                        <div className="mt-3 p-3 bg-white dark:bg-slate-700 rounded border border-red-200 dark:border-red-700">
                          <p className="text-xs font-medium text-slate-700 dark:text-slate-300 mb-2">How to grant Automation permission:</p>
                          <ol className="text-xs text-slate-600 dark:text-slate-400 list-decimal list-inside space-y-1">
                            <li>Open System Settings</li>
                            <li>Go to Privacy & Security → Automation</li>
                            <li>Find Hive in the list</li>
                            <li>Enable the toggle next to Calendar</li>
                            <li>Click "Request Calendar Access" again</li>
                          </ol>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Sync Result */}
                  {calendarSyncResult && (
                    <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                      <div className="flex items-center gap-2 text-green-700 dark:text-green-400 mb-3">
                        <CheckCircle className="w-5 h-5" />
                        <span className="font-medium">Calendar access granted and sync completed!</span>
                      </div>

                      <div className="grid grid-cols-3 gap-3">
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-green-600 dark:text-green-400">{calendarSyncResult.createdCount}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Created</div>
                        </div>
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-blue-600 dark:text-blue-400">{calendarSyncResult.updatedCount}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Updated</div>
                        </div>
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-slate-600 dark:text-slate-400">{calendarSyncResult.totalEvents}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Total Events</div>
                        </div>
                      </div>
                    </div>
                  )}
                </>

              ) : window.electronAPI?.platform !== 'darwin' ? (
                <div className="p-4 bg-slate-50 dark:bg-slate-700/50 border border-slate-200 dark:border-slate-600 rounded-lg">
                  <p className="text-sm text-slate-600 dark:text-slate-400">
                    Apple Calendar sync is only available on macOS
                  </p>
                </div>
              ) : (
                <>
                  <div>
                    <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">How it works</h3>
                    <ul className="text-sm text-slate-600 dark:text-slate-400 space-y-1 list-disc list-inside">
                      <li>Click "Sync Now" to manually sync your calendar</li>
                      <li>Reads events from your "michael.geranis@orfium.com" calendar</li>
                      <li>Matches meeting titles with pattern "[DirectReport FirstName] / [Manager Name]"</li>
                      <li>Creates or updates 1:1 meeting records in Hive</li>
                      <li>Only fetches events for the next 10 days</li>
                    </ul>
                  </div>

                  <div className="pt-4 border-t dark:border-slate-700">
                    <button
                      onClick={handleCalendarSync}
                      disabled={calendarSyncing}
                      className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {calendarSyncing ? (
                        <>
                          <RefreshCw className="w-5 h-5 animate-spin" />
                          Syncing...
                        </>
                      ) : (
                        <>
                          <Calendar className="w-5 h-5" />
                          Sync Now
                        </>
                      )}
                    </button>
                  </div>

                  {/* Sync Error */}
                  {calendarSyncError && (
                    <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {calendarSyncError}
                    </div>
                  )}

                  {/* Sync Result */}
                  {calendarSyncResult && (
                    <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                      <div className="flex items-center gap-2 text-green-700 dark:text-green-400 mb-3">
                        <CheckCircle className="w-5 h-5" />
                        <span className="font-medium">Calendar sync completed!</span>
                      </div>

                      <div className="grid grid-cols-3 gap-3 mb-3">
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-green-600 dark:text-green-400">{calendarSyncResult.createdCount}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Created</div>
                        </div>
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-blue-600 dark:text-blue-400">{calendarSyncResult.updatedCount}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Updated</div>
                        </div>
                        <div className="p-2 bg-white dark:bg-slate-700 rounded text-center">
                          <div className="text-lg font-bold text-slate-600 dark:text-slate-400">{calendarSyncResult.totalEvents}</div>
                          <div className="text-xs text-slate-500 dark:text-slate-400">Total Events</div>
                        </div>
                      </div>

                      {calendarSyncResult.skippedEvents?.length > 0 && (
                        <details className="mt-2">
                          <summary className="text-xs text-slate-600 dark:text-slate-400 cursor-pointer hover:text-slate-700 dark:hover:text-slate-300">
                            {calendarSyncResult.skippedEvents.length} events skipped
                          </summary>
                          <ul className="mt-2 text-xs text-slate-600 dark:text-slate-400 space-y-1 max-h-32 overflow-y-auto">
                            {calendarSyncResult.skippedEvents.map((msg: string, idx: number) => (
                              <li key={idx} className="pl-2">• {msg}</li>
                            ))}
                          </ul>
                        </details>
                      )}

                      {calendarSyncResult.errors?.length > 0 && (
                        <details className="mt-2">
                          <summary className="text-xs text-red-600 dark:text-red-400 cursor-pointer hover:text-red-700 dark:hover:text-red-300">
                            {calendarSyncResult.errors.length} errors occurred
                          </summary>
                          <ul className="mt-2 text-xs text-red-600 dark:text-red-400 space-y-1 max-h-32 overflow-y-auto">
                            {calendarSyncResult.errors.map((msg: string, idx: number) => (
                              <li key={idx} className="pl-2">• {msg}</li>
                            ))}
                          </ul>
                        </details>
                      )}
                    </div>
                  )}
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

    </div>
  )
}
