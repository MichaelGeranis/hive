import { useEffect, useState } from 'react'
import { Save, Sun, Moon, Monitor, Upload, FileText, CheckCircle, AlertCircle, XCircle, Clock, Download, Database } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { settingsApi, jiraImportApi, backupApi } from '../services/api'
import { useTheme } from '../contexts/ThemeContext'
import type { StoryPointMapping, JiraImportPreview, JiraImportResult, JiraImportRequest, RestoreResultDto } from '../types'

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

  // Jira Import state
  const [csvContent, setCsvContent] = useState<string>('')
  const [preview, setPreview] = useState<JiraImportPreview | null>(null)
  const [result, setResult] = useState<JiraImportResult | null>(null)
  const [importLoading, setImportLoading] = useState(false)
  const [importing, setImporting] = useState(false)
  const [updateExisting, setUpdateExisting] = useState(true)
  const [matchField, setMatchField] = useState<'IssueKey' | 'Title'>('IssueKey')
  const [importError, setImportError] = useState<string | null>(null)

  // Backup & Restore state
  const [backupLoading, setBackupLoading] = useState(false)
  const [restoreLoading, setRestoreLoading] = useState(false)
  const [restoreResult, setRestoreResult] = useState<RestoreResultDto | null>(null)
  const [backupError, setBackupError] = useState<string | null>(null)

  useEffect(() => {
    loadSettings()
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

  // Jira Import handlers
  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (e) => {
      const content = e.target?.result as string
      setCsvContent(content)
      setPreview(null)
      setResult(null)
      setImportError(null)
    }
    reader.onerror = () => {
      setImportError('Failed to read file')
    }
    reader.readAsText(file)
  }

  const handlePreview = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    try {
      setImportLoading(true)
      setImportError(null)
      setResult(null)
      const previewData = await jiraImportApi.preview(csvContent)
      setPreview(previewData)
    } catch (err: any) {
      console.error('Failed to preview import', err)
      setImportError(err.response?.data || err.message || 'Failed to preview CSV')
    } finally {
      setImportLoading(false)
    }
  }

  const handleImport = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    if (!confirm(`Import ${preview?.validRows || 0} tasks from Jira? ${updateExisting ? 'Existing tasks will be updated.' : 'Existing tasks will be skipped.'}`)) {
      return
    }

    try {
      setImporting(true)
      setImportError(null)
      const importRequest: JiraImportRequest = {
        csvContent,
        updateExisting,
        matchField
      }
      const importResult = await jiraImportApi.import(importRequest)
      setResult(importResult)
      setPreview(null)
    } catch (err: any) {
      console.error('Failed to import', err)
      setImportError(err.response?.data || err.message || 'Failed to import CSV')
    } finally {
      setImporting(false)
    }
  }

  const handleResetImport = () => {
    setCsvContent('')
    setPreview(null)
    setResult(null)
    setImportError(null)
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

      {/* Jira Import */}
      <Card>
        <CardHeader
          title="Import from Jira"
          subtitle="Import tasks from Jira CSV export into Hive"
        />
        <CardContent>
          <div className="space-y-4">
            {/* Instructions */}
            <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
              <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">How to Export from Jira</h4>
              <ol className="list-decimal list-inside space-y-1 text-sm text-blue-700 dark:text-blue-300">
                <li>Go to your Jira project and navigate to All work</li>
                <li>Get all issues except EPIC from LP project from the last 5 sprints including the current in progress sprint</li>
                <li>eg: project = LP AND issuetype != Epic AND cf[10020] in (LP_4Q25_S1, LP_4Q25_S2, LP_4Q25_S3, LP_4Q25_S4, LP_4Q25_S5, LP_4Q25_S6) ORDER BY created DESC</li>
                <li>Choose "Export CSV (all fields)" or "Export CSV (current fields)"</li>
                <li>Save the exported CSV file and upload it below</li>
              </ol>
            </div>

            {/* Upload Section */}
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                Select Jira CSV Export
              </label>
              <div className="flex items-center gap-4">
                <input
                  type="file"
                  accept=".csv"
                  onChange={handleFileChange}
                  className="block w-full text-sm text-slate-500 dark:text-slate-400
                    file:mr-4 file:py-2 file:px-4
                    file:rounded-lg file:border-0
                    file:text-sm file:font-semibold
                    file:bg-amber-50 dark:file:bg-amber-900/20 file:text-amber-700 dark:file:text-amber-400
                    hover:file:bg-amber-100 dark:hover:file:bg-amber-900/30
                    cursor-pointer"
                  disabled={importLoading || importing}
                />
                {csvContent && (
                  <button
                    onClick={handleResetImport}
                    className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                    disabled={importLoading || importing}
                  >
                    Clear
                  </button>
                )}
              </div>
            </div>

            {csvContent && !preview && !result && (
              <div className="flex gap-3">
                <button
                  onClick={handlePreview}
                  disabled={importLoading || importing}
                  className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <FileText className="w-5 h-5" />
                  {importLoading ? 'Loading Preview...' : 'Preview Import'}
                </button>
              </div>
            )}

            {/* Import Options */}
            {csvContent && (
              <div className="space-y-3 pt-4 border-t dark:border-slate-700">
                <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Import Options</h3>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="updateExisting"
                    checked={updateExisting}
                    onChange={(e) => setUpdateExisting(e.target.checked)}
                    className="w-4 h-4 text-amber-500 bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 rounded focus:ring-amber-500"
                    disabled={importLoading || importing}
                  />
                  <label htmlFor="updateExisting" className="text-sm text-slate-700 dark:text-slate-300">
                    Update existing tasks (if unchecked, existing tasks will be skipped)
                  </label>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Match existing tasks by:
                  </label>
                  <div className="flex gap-3">
                    <button
                      onClick={() => setMatchField('IssueKey')}
                      disabled={importLoading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'IssueKey'
                          ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                          : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                      }`}
                    >
                      Issue Key (Recommended)
                    </button>
                    <button
                      onClick={() => setMatchField('Title')}
                      disabled={importLoading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'Title'
                          ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                          : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                      }`}
                    >
                      Title
                    </button>
                  </div>
                </div>
              </div>
            )}

            {importError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {importError}
              </div>
            )}

            {/* Preview Section */}
            {preview && (
              <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  Import Preview - {preview.totalRows} rows ({preview.validRows} valid, {preview.invalidRows} invalid)
                </h3>

                <div className="grid grid-cols-3 gap-4">
                  <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                    <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{preview.totalRows}</div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">Total Rows</div>
                  </div>
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <div className="text-2xl font-bold text-green-700 dark:text-green-400">{preview.validRows}</div>
                    <div className="text-sm text-green-600 dark:text-green-500">Valid</div>
                  </div>
                  <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                    <div className="text-2xl font-bold text-red-700 dark:text-red-400">{preview.invalidRows}</div>
                    <div className="text-sm text-red-600 dark:text-red-500">Invalid</div>
                  </div>
                </div>

                {preview.mappingWarnings.length > 0 && (
                  <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                    <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2">Warnings:</h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500">
                      {preview.mappingWarnings.map((warning, idx) => (
                        <li key={idx}>{warning}</li>
                      ))}
                    </ul>
                  </div>
                )}

                <button
                  onClick={handleImport}
                  disabled={importing || preview.validRows === 0}
                  className="flex items-center gap-2 px-6 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Upload className="w-5 h-5" />
                  {importing ? 'Importing...' : `Import ${preview.validRows} Tasks`}
                </button>
              </div>
            )}

            {/* Result Section */}
            {result && (
              <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Import Results</h3>

                <div className="grid grid-cols-4 gap-4">
                  <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                    <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{result.totalRows}</div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">Total</div>
                  </div>
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <div className="text-2xl font-bold text-green-700 dark:text-green-400">{result.successCount}</div>
                    <div className="text-sm text-green-600 dark:text-green-500">Imported</div>
                  </div>
                  <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                    <div className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">{result.skippedCount}</div>
                    <div className="text-sm text-yellow-600 dark:text-yellow-500">Skipped</div>
                  </div>
                  <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                    <div className="text-2xl font-bold text-red-700 dark:text-red-400">{result.errorCount}</div>
                    <div className="text-sm text-red-600 dark:text-red-500">Errors</div>
                  </div>
                </div>

                {result.successCount > 0 && (
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                    <div className="flex items-center gap-2 text-green-700 dark:text-green-400">
                      <CheckCircle className="w-5 h-5" />
                      <span className="font-medium">Successfully imported {result.successCount} tasks!</span>
                    </div>
                    <div className="mt-2 text-sm text-green-600 dark:text-green-500">
                      {result.importedTasks.filter(t => t.isNew).length} new tasks created, {result.importedTasks.filter(t => t.isUpdated).length} tasks updated
                    </div>
                  </div>
                )}

                {result.warnings.length > 0 && (
                  <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                    <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2 flex items-center gap-2">
                      <AlertCircle className="w-4 h-4" />
                      Warnings ({result.warnings.length}):
                    </h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500 max-h-32 overflow-y-auto">
                      {result.warnings.map((warning, idx) => (
                        <li key={idx}>{warning}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {result.errors.length > 0 && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                    <h4 className="text-sm font-medium text-red-800 dark:text-red-400 mb-2 flex items-center gap-2">
                      <XCircle className="w-4 h-4" />
                      Errors ({result.errors.length}):
                    </h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-red-700 dark:text-red-500 max-h-32 overflow-y-auto">
                      {result.errors.map((error, idx) => (
                        <li key={idx}>{error}</li>
                      ))}
                    </ul>
                  </div>
                )}

                <div className="flex gap-3">
                  <button
                    onClick={handleResetImport}
                    className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                  >
                    Import Another File
                  </button>
                  <a
                    href="/tasks"
                    className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors inline-block"
                  >
                    View Tasks
                  </a>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

    </div>
  )
}
