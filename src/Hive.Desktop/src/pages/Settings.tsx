import { useEffect, useState } from 'react'
import { Save, Sun, Moon, Monitor, CheckCircle, AlertCircle, XCircle, Clock, Download, Database, Brain, Key, Eye, EyeOff, Loader2, Shirt, Gauge, Tag, X, Plus } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { settingsApi, backupApi, sentimentApi } from '../services/api'
import { useTheme } from '../contexts/ThemeContext'
import type { StoryPointMapping, TshirtSizeMapping, RestoreResultDto } from '../types'

const DEFAULT_MAPPINGS: StoryPointMapping[] = [
  { points: 1, hours: 2, label: '1 SP = 2 hours' },
  { points: 2, hours: 4, label: '2 SP = 4 hours (4 hours)' },
  { points: 3, hours: 8, label: '3 SP = 8 hours (1 day)' },
  { points: 5, hours: 24, label: '5 SP = 24 hours (3 days)' },
  { points: 8, hours: 72, label: '8 SP = 72 hours (1 sprint)' },
  { points: 13, hours: 150, label: '13 SP = 150 hours (2 sprints)' },
  { points: 21, hours: 240, label: '21 SP = 240 hours (1 month)' }
]

const DEFAULT_TSHIRT_MAPPINGS: TshirtSizeMapping[] = [
  { size: 'S', sprints: 0.5, label: 'S = 1/2 sprint' },
  { size: 'M', sprints: 1, label: 'M = 1 sprint' },
  { size: 'L', sprints: 2, label: 'L = 2 sprints' },
  { size: 'XL', sprints: 4, label: 'XL = 4+ sprints' }
]

export default function Settings() {
  const { theme, setTheme, schedule, setSchedule } = useTheme()
  const [mappings, setMappings] = useState<StoryPointMapping[]>(DEFAULT_MAPPINGS)
  const [tshirtMappings, setTshirtMappings] = useState<TshirtSizeMapping[]>(DEFAULT_TSHIRT_MAPPINGS)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // T-shirt size mapping state
  const [savingTshirt, setSavingTshirt] = useState(false)
  const [tshirtSaved, setTshirtSaved] = useState(false)
  const [tshirtError, setTshirtError] = useState<string | null>(null)

  // Backup & Restore state
  const [backupLoading, setBackupLoading] = useState(false)
  const [restoreLoading, setRestoreLoading] = useState(false)
  const [restoreResult, setRestoreResult] = useState<RestoreResultDto | null>(null)
  const [backupError, setBackupError] = useState<string | null>(null)

  // Sentiment Analysis state
  const [sentimentEnabled, setSentimentEnabled] = useState(false)
  const [sentimentDays, setSentimentDays] = useState(90)
  const [hasApiKey, setHasApiKey] = useState(false)
  const [apiKey, setApiKey] = useState('')
  const [showApiKey, setShowApiKey] = useState(false)
  const [validatingKey, setValidatingKey] = useState(false)
  const [keyValidationResult, setKeyValidationResult] = useState<{ valid: boolean; error?: string } | null>(null)
  const [savingSentiment, setSavingSentiment] = useState(false)
  const [sentimentSaved, setSentimentSaved] = useState(false)
  const [sentimentError, setSentimentError] = useState<string | null>(null)

  // Dashboard Thresholds state
  const [maxInProgressTasks, setMaxInProgressTasks] = useState(2)
  const [maxBlockedTasks, setMaxBlockedTasks] = useState(1)
  const [maxInReviewTasks, setMaxInReviewTasks] = useState(1)
  const [minProjectMembers, setMinProjectMembers] = useState(2)
  const [savingThresholds, setSavingThresholds] = useState(false)
  const [thresholdsSaved, setThresholdsSaved] = useState(false)
  const [thresholdsError, setThresholdsError] = useState<string | null>(null)

  // Support & Maintenance Labels state
  const [supportLabels, setSupportLabels] = useState<string[]>(['support'])
  const [maintenanceLabels, setMaintenanceLabels] = useState<string[]>(['maintenance'])
  const [supportLabelInput, setSupportLabelInput] = useState('')
  const [maintenanceLabelInput, setMaintenanceLabelInput] = useState('')
  const [savingLabels, setSavingLabels] = useState(false)
  const [labelsSaved, setLabelsSaved] = useState(false)
  const [labelsError, setLabelsError] = useState<string | null>(null)

  useEffect(() => {
    loadSettings()
  }, [])

  const loadSettings = async () => {
    try {
      setLoading(true)
      setError(null)
      const settings = await settingsApi.get()
      setMappings(settings.storyPointMappings)
      setTshirtMappings(settings.tshirtSizeMappings || DEFAULT_TSHIRT_MAPPINGS)
      // Load sentiment analysis settings
      setSentimentEnabled(settings.sentimentAnalysisEnabled)
      setSentimentDays(settings.sentimentAnalysisDays)
      setHasApiKey(settings.hasClaudeApiKey)
      // Load dashboard threshold settings
      setMaxInProgressTasks(settings.maxInProgressTasks ?? 2)
      setMaxBlockedTasks(settings.maxBlockedTasks ?? 1)
      setMaxInReviewTasks(settings.maxInReviewTasks ?? 1)
      setMinProjectMembers(settings.minProjectMembers ?? 2)
      // Load support & maintenance labels
      setSupportLabels(settings.supportLabels ?? ['support'])
      setMaintenanceLabels(settings.maintenanceLabels ?? ['maintenance'])
    } catch (err) {
      console.error('Failed to load settings', err)
      setError('Failed to load settings. Using default values.')
      setMappings(DEFAULT_MAPPINGS)
      setTshirtMappings(DEFAULT_TSHIRT_MAPPINGS)
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

  // T-shirt size mapping handlers
  const handleTshirtSprintsChange = (index: number, value: string) => {
    const sprints = parseFloat(value)
    if (isNaN(sprints) || sprints < 0.5) return

    const newMappings = [...tshirtMappings]
    newMappings[index] = { ...newMappings[index], sprints }
    setTshirtMappings(newMappings)
    setTshirtSaved(false)
  }

  const handleSaveTshirtMappings = async () => {
    try {
      setSavingTshirt(true)
      setTshirtError(null)
      await settingsApi.update({ tshirtSizeMappings: tshirtMappings })
      setTshirtSaved(true)
      setTimeout(() => setTshirtSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save T-shirt size mappings', err)
      setTshirtError('Failed to save T-shirt size mappings. Please try again.')
    } finally {
      setSavingTshirt(false)
    }
  }

  const handleResetTshirtMappings = async () => {
    try {
      setSavingTshirt(true)
      setTshirtError(null)
      await settingsApi.update({ tshirtSizeMappings: DEFAULT_TSHIRT_MAPPINGS })
      setTshirtMappings(DEFAULT_TSHIRT_MAPPINGS)
      setTshirtSaved(true)
      setTimeout(() => setTshirtSaved(false), 3000)
    } catch (err) {
      console.error('Failed to reset T-shirt size mappings', err)
      setTshirtError('Failed to reset T-shirt size mappings. Please try again.')
    } finally {
      setSavingTshirt(false)
    }
  }

  // Sentiment Analysis handlers
  const handleValidateApiKey = async () => {
    if (!apiKey.trim()) {
      setKeyValidationResult({ valid: false, error: 'Please enter an API key' })
      return
    }

    try {
      setValidatingKey(true)
      setKeyValidationResult(null)
      const result = await sentimentApi.validateApiKey(apiKey)
      setKeyValidationResult(result)
    } catch (err: any) {
      console.error('Failed to validate API key', err)
      setKeyValidationResult({ valid: false, error: err.response?.data?.message || 'Failed to validate API key' })
    } finally {
      setValidatingKey(false)
    }
  }

  const handleSaveSentimentSettings = async () => {
    try {
      setSavingSentiment(true)
      setSentimentError(null)

      const updateData: any = {
        sentimentAnalysisEnabled: sentimentEnabled,
        sentimentAnalysisDays: sentimentDays
      }

      // Only include API key if it was entered
      if (apiKey.trim()) {
        updateData.claudeApiKey = apiKey
      }

      await settingsApi.update(updateData)

      // Reload settings to get updated hasApiKey state
      const settings = await settingsApi.get()
      setHasApiKey(settings.hasClaudeApiKey)
      setApiKey('') // Clear the API key field after saving
      setKeyValidationResult(null)

      setSentimentSaved(true)
      setTimeout(() => setSentimentSaved(false), 3000)
    } catch (err: any) {
      console.error('Failed to save sentiment settings', err)
      setSentimentError(err.response?.data?.message || 'Failed to save sentiment settings')
    } finally {
      setSavingSentiment(false)
    }
  }

  const handleClearApiKey = async () => {
    if (!confirm('Are you sure you want to remove the API key? Sentiment analysis will be disabled.')) {
      return
    }

    try {
      setSavingSentiment(true)
      setSentimentError(null)

      await settingsApi.update({
        claudeApiKey: '',
        sentimentAnalysisEnabled: false
      })

      setHasApiKey(false)
      setSentimentEnabled(false)
      setApiKey('')
      setKeyValidationResult(null)

      setSentimentSaved(true)
      setTimeout(() => setSentimentSaved(false), 3000)
    } catch (err: any) {
      console.error('Failed to clear API key', err)
      setSentimentError(err.response?.data?.message || 'Failed to clear API key')
    } finally {
      setSavingSentiment(false)
    }
  }

  // Dashboard Thresholds handlers
  const handleSaveThresholds = async () => {
    try {
      setSavingThresholds(true)
      setThresholdsError(null)
      await settingsApi.update({
        maxInProgressTasks,
        maxBlockedTasks,
        maxInReviewTasks,
        minProjectMembers
      })
      setThresholdsSaved(true)
      setTimeout(() => setThresholdsSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save threshold settings', err)
      setThresholdsError('Failed to save threshold settings. Please try again.')
    } finally {
      setSavingThresholds(false)
    }
  }

  const handleResetThresholds = async () => {
    try {
      setSavingThresholds(true)
      setThresholdsError(null)
      await settingsApi.update({
        maxInProgressTasks: 2,
        maxBlockedTasks: 1,
        maxInReviewTasks: 1,
        minProjectMembers: 2
      })
      setMaxInProgressTasks(2)
      setMaxBlockedTasks(1)
      setMaxInReviewTasks(1)
      setMinProjectMembers(2)
      setThresholdsSaved(true)
      setTimeout(() => setThresholdsSaved(false), 3000)
    } catch (err) {
      console.error('Failed to reset threshold settings', err)
      setThresholdsError('Failed to reset threshold settings. Please try again.')
    } finally {
      setSavingThresholds(false)
    }
  }

  // Support & Maintenance Labels handlers
  const handleAddSupportLabel = () => {
    const label = supportLabelInput.trim()
    if (label && !supportLabels.includes(label.toLowerCase())) {
      setSupportLabels([...supportLabels, label.toLowerCase()])
      setSupportLabelInput('')
    }
  }

  const handleRemoveSupportLabel = (label: string) => {
    setSupportLabels(supportLabels.filter(l => l !== label))
  }

  const handleAddMaintenanceLabel = () => {
    const label = maintenanceLabelInput.trim()
    if (label && !maintenanceLabels.includes(label.toLowerCase())) {
      setMaintenanceLabels([...maintenanceLabels, label.toLowerCase()])
      setMaintenanceLabelInput('')
    }
  }

  const handleRemoveMaintenanceLabel = (label: string) => {
    setMaintenanceLabels(maintenanceLabels.filter(l => l !== label))
  }

  const handleSaveLabels = async () => {
    try {
      setSavingLabels(true)
      setLabelsError(null)
      await settingsApi.update({
        supportLabels,
        maintenanceLabels
      })
      setLabelsSaved(true)
      setTimeout(() => setLabelsSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save label settings', err)
      setLabelsError('Failed to save label settings. Please try again.')
    } finally {
      setSavingLabels(false)
    }
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

      {/* AI Sentiment Analysis */}
      <Card>
        <CardHeader
          title="AI Sentiment Analysis"
          subtitle="Analyze meeting notes for team morale indicators using Claude AI"
          action={<Brain className="w-5 h-5 text-purple-500" />}
        />
        <CardContent>
          <div className="space-y-6">
            {/* Status Indicator */}
            <div className="flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
              <div className="flex items-center gap-3">
                <div className={`w-3 h-3 rounded-full ${hasApiKey && sentimentEnabled ? 'bg-green-500' : hasApiKey ? 'bg-yellow-500' : 'bg-slate-400'}`}></div>
                <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  {hasApiKey && sentimentEnabled ? 'Enabled' : hasApiKey ? 'Configured but disabled' : 'Not configured'}
                </span>
              </div>
              {hasApiKey && (
                <span className="text-xs text-slate-500 dark:text-slate-400">
                  Analyzing last {sentimentDays} days
                </span>
              )}
            </div>

            {/* Enable/Disable Toggle */}
            <div className="flex items-center justify-between">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Enable Sentiment Analysis
                </label>
                <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
                  Analyze meeting notes to detect team sentiment trends
                </p>
              </div>
              <button
                onClick={() => setSentimentEnabled(!sentimentEnabled)}
                disabled={!hasApiKey && !apiKey.trim()}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  sentimentEnabled ? 'bg-purple-500' : 'bg-slate-300 dark:bg-slate-600'
                } ${!hasApiKey && !apiKey.trim() ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    sentimentEnabled ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>

            {/* API Key Input */}
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                <div className="flex items-center gap-2">
                  <Key className="w-4 h-4" />
                  Claude API Key
                </div>
              </label>
              {hasApiKey ? (
                <div className="flex items-center gap-3">
                  <div className="flex-1 px-3 py-2 bg-slate-100 dark:bg-slate-700 rounded-lg text-slate-600 dark:text-slate-400 text-sm">
                    API key configured
                  </div>
                  <button
                    onClick={handleClearApiKey}
                    disabled={savingSentiment}
                    className="px-3 py-2 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors text-sm"
                  >
                    Remove
                  </button>
                </div>
              ) : (
                <div className="space-y-2">
                  <div className="flex gap-2">
                    <div className="relative flex-1">
                      <input
                        type={showApiKey ? 'text' : 'password'}
                        value={apiKey}
                        onChange={(e) => {
                          setApiKey(e.target.value)
                          setKeyValidationResult(null)
                        }}
                        placeholder="sk-ant-api03-..."
                        className="w-full px-3 py-2 pr-10 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                      />
                      <button
                        type="button"
                        onClick={() => setShowApiKey(!showApiKey)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
                      >
                        {showApiKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>
                    <button
                      onClick={handleValidateApiKey}
                      disabled={validatingKey || !apiKey.trim()}
                      className="px-4 py-2 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                    >
                      {validatingKey ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        'Validate'
                      )}
                    </button>
                  </div>
                  {keyValidationResult && (
                    <div className={`flex items-center gap-2 text-sm ${keyValidationResult.valid ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                      {keyValidationResult.valid ? (
                        <>
                          <CheckCircle className="w-4 h-4" />
                          API key is valid
                        </>
                      ) : (
                        <>
                          <XCircle className="w-4 h-4" />
                          {keyValidationResult.error || 'Invalid API key'}
                        </>
                      )}
                    </div>
                  )}
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    Get your API key from{' '}
                    <a
                      href="https://console.anthropic.com/settings/keys"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-purple-500 hover:underline"
                    >
                      console.anthropic.com
                    </a>
                  </p>
                </div>
              )}
            </div>

            {/* Analysis Period */}
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                Analysis Period
              </label>
              <select
                value={sentimentDays}
                onChange={(e) => setSentimentDays(parseInt(e.target.value))}
                className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
              >
                <option value={30}>Last 30 days</option>
                <option value={60}>Last 60 days</option>
                <option value={90}>Last 90 days (recommended)</option>
                <option value={180}>Last 180 days</option>
              </select>
              <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                Meeting notes from this period will be analyzed for sentiment
              </p>
            </div>

            {/* Error Message */}
            {sentimentError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm flex items-center gap-2">
                <XCircle className="w-4 h-4 flex-shrink-0" />
                {sentimentError}
              </div>
            )}

            {/* Save Button */}
            <div className="pt-4 border-t dark:border-slate-700">
              <button
                onClick={handleSaveSentimentSettings}
                disabled={savingSentiment}
                className="flex items-center gap-2 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {savingSentiment ? (
                  <Loader2 className="w-5 h-5 animate-spin" />
                ) : (
                  <Save className="w-5 h-5" />
                )}
                {savingSentiment ? 'Saving...' : sentimentSaved ? 'Saved!' : 'Save Settings'}
              </button>
            </div>

            {sentimentSaved && (
              <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-400 text-sm flex items-center gap-2">
                <CheckCircle className="w-4 h-4" />
                Sentiment analysis settings saved successfully!
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

      {/* T-Shirt Size to Sprints Mapping */}
      <Card>
        <CardHeader
          title="T-Shirt Size to Sprints Mapping"
          subtitle="Configure how T-shirt sizes translate to number of sprints for initiative estimation"
          action={<Shirt className="w-5 h-5 text-blue-500" />}
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Use this to estimate initiative duration based on T-shirt size complexity.
            </p>

            <div className="space-y-3">
              {tshirtMappings.map((mapping, index) => (
                <div key={mapping.size} className="flex items-center gap-4 p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <div className="flex-shrink-0 w-16">
                    <span className="text-lg font-semibold text-slate-900 dark:text-slate-100">{mapping.size}</span>
                  </div>
                  <span className="text-slate-500 dark:text-slate-400">=</span>
                  <div className="flex items-center gap-2">
                    <input
                      type="number"
                      value={mapping.sprints}
                      onChange={(e) => handleTshirtSprintsChange(index, e.target.value)}
                      className="w-24 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      min="0.5"
                      step="0.5"
                      disabled={savingTshirt}
                    />
                    <span className="text-slate-700 dark:text-slate-300">sprint{mapping.sprints !== 1 ? 's' : ''}</span>
                  </div>
                </div>
              ))}
            </div>

            {tshirtError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {tshirtError}
              </div>
            )}

            <div className="pt-4 border-t dark:border-slate-700 flex gap-3">
              <button
                onClick={handleSaveTshirtMappings}
                disabled={savingTshirt}
                className="flex items-center gap-2 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Save className="w-5 h-5" />
                {savingTshirt ? 'Saving...' : tshirtSaved ? 'Saved!' : 'Save Changes'}
              </button>
              <button
                onClick={handleResetTshirtMappings}
                disabled={savingTshirt}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Reset to Defaults
              </button>
            </div>

            {tshirtSaved && (
              <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-400 text-sm">
                T-shirt size mappings saved successfully!
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Dashboard Thresholds */}
      <Card>
        <CardHeader
          title="Dashboard Warning Thresholds"
          subtitle="Configure when warnings appear on the dashboard for workload and knowledge silos"
          action={<Gauge className="w-5 h-5 text-orange-500" />}
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Adjust these thresholds to control when the dashboard shows warnings about team workload and project coverage.
            </p>

            {/* Workload Thresholds */}
            <div className="space-y-3">
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Workload Warnings</h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Show a warning when a team member exceeds these task counts
              </p>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <label className="block text-sm text-slate-600 dark:text-slate-400 mb-2">
                    Max In Progress Tasks
                  </label>
                  <input
                    type="number"
                    value={maxInProgressTasks}
                    onChange={(e) => {
                      const val = parseInt(e.target.value)
                      if (!isNaN(val) && val >= 1) setMaxInProgressTasks(val)
                    }}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                    min="1"
                    disabled={savingThresholds}
                  />
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    Warn if &gt; {maxInProgressTasks} in progress
                  </p>
                </div>

                <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <label className="block text-sm text-slate-600 dark:text-slate-400 mb-2">
                    Max Blocked Tasks
                  </label>
                  <input
                    type="number"
                    value={maxBlockedTasks}
                    onChange={(e) => {
                      const val = parseInt(e.target.value)
                      if (!isNaN(val) && val >= 1) setMaxBlockedTasks(val)
                    }}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                    min="1"
                    disabled={savingThresholds}
                  />
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    Warn if &gt; {maxBlockedTasks} blocked
                  </p>
                </div>

                <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <label className="block text-sm text-slate-600 dark:text-slate-400 mb-2">
                    Max In Review Tasks
                  </label>
                  <input
                    type="number"
                    value={maxInReviewTasks}
                    onChange={(e) => {
                      const val = parseInt(e.target.value)
                      if (!isNaN(val) && val >= 1) setMaxInReviewTasks(val)
                    }}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                    min="1"
                    disabled={savingThresholds}
                  />
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    Warn if &gt; {maxInReviewTasks} in review
                  </p>
                </div>
              </div>
            </div>

            {/* Knowledge Silo Threshold */}
            <div className="pt-4 border-t dark:border-slate-700 space-y-3">
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Knowledge Silo Detection</h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Flag projects as knowledge silos when they have fewer team members than this threshold
              </p>

              <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg max-w-xs">
                <label className="block text-sm text-slate-600 dark:text-slate-400 mb-2">
                  Minimum Project Members
                </label>
                <input
                  type="number"
                  value={minProjectMembers}
                  onChange={(e) => {
                    const val = parseInt(e.target.value)
                    if (!isNaN(val) && val >= 1) setMinProjectMembers(val)
                  }}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                  min="1"
                  disabled={savingThresholds}
                />
                <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                  Warn if &lt; {minProjectMembers} members on a project
                </p>
              </div>
            </div>

            {thresholdsError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {thresholdsError}
              </div>
            )}

            <div className="pt-4 border-t dark:border-slate-700 flex gap-3">
              <button
                onClick={handleSaveThresholds}
                disabled={savingThresholds}
                className="flex items-center gap-2 px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Save className="w-5 h-5" />
                {savingThresholds ? 'Saving...' : thresholdsSaved ? 'Saved!' : 'Save Changes'}
              </button>
              <button
                onClick={handleResetThresholds}
                disabled={savingThresholds}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Reset to Defaults
              </button>
            </div>

            {thresholdsSaved && (
              <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-400 text-sm">
                Dashboard threshold settings saved successfully!
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Support & Maintenance Labels */}
      <Card>
        <CardHeader
          title="Support & Maintenance Labels"
          subtitle="Configure which labels identify support and maintenance work"
          action={<Tag className="w-5 h-5 text-purple-500" />}
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Define which labels/tags should be counted as "support" vs "maintenance" work in the Dashboard widgets.
            </p>

            {/* Support Labels */}
            <div className="space-y-3">
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Support Labels</h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Tasks with these labels/tags will be counted in the Support Hours widgets
              </p>

              <div className="flex flex-wrap gap-2 mb-3">
                {supportLabels.map(label => (
                  <span
                    key={label}
                    className="inline-flex items-center gap-1 px-3 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded-full text-sm"
                  >
                    {label}
                    <button
                      onClick={() => handleRemoveSupportLabel(label)}
                      className="ml-1 hover:bg-green-200 dark:hover:bg-green-800 rounded-full p-0.5"
                      disabled={savingLabels}
                    >
                      <X className="w-3 h-3" />
                    </button>
                  </span>
                ))}
              </div>

              <div className="flex gap-2">
                <input
                  type="text"
                  value={supportLabelInput}
                  onChange={(e) => setSupportLabelInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleAddSupportLabel()}
                  placeholder="e.g., bug, hotfix, incident"
                  className="flex-1 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                  disabled={savingLabels}
                />
                <button
                  onClick={handleAddSupportLabel}
                  disabled={savingLabels || !supportLabelInput.trim()}
                  className="flex items-center gap-2 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Plus className="w-4 h-4" />
                  Add
                </button>
              </div>
            </div>

            {/* Maintenance Labels */}
            <div className="pt-4 border-t dark:border-slate-700 space-y-3">
              <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Maintenance Labels</h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Tasks with these labels/tags will be counted in the Maintenance Hours widgets
              </p>

              <div className="flex flex-wrap gap-2 mb-3">
                {maintenanceLabels.map(label => (
                  <span
                    key={label}
                    className="inline-flex items-center gap-1 px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded-full text-sm"
                  >
                    {label}
                    <button
                      onClick={() => handleRemoveMaintenanceLabel(label)}
                      className="ml-1 hover:bg-blue-200 dark:hover:bg-blue-800 rounded-full p-0.5"
                      disabled={savingLabels}
                    >
                      <X className="w-3 h-3" />
                    </button>
                  </span>
                ))}
              </div>

              <div className="flex gap-2">
                <input
                  type="text"
                  value={maintenanceLabelInput}
                  onChange={(e) => setMaintenanceLabelInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleAddMaintenanceLabel()}
                  placeholder="e.g., tech-debt, refactor, chore"
                  className="flex-1 px-3 py-2 border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                  disabled={savingLabels}
                />
                <button
                  onClick={handleAddMaintenanceLabel}
                  disabled={savingLabels || !maintenanceLabelInput.trim()}
                  className="flex items-center gap-2 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Plus className="w-4 h-4" />
                  Add
                </button>
              </div>
            </div>

            {labelsError && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {labelsError}
              </div>
            )}

            <div className="pt-4 border-t dark:border-slate-700 flex gap-3">
              <button
                onClick={handleSaveLabels}
                disabled={savingLabels}
                className="flex items-center gap-2 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Save className="w-5 h-5" />
                {savingLabels ? 'Saving...' : labelsSaved ? 'Saved!' : 'Save Changes'}
              </button>
            </div>

            {labelsSaved && (
              <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-400 text-sm">
                Label settings saved successfully!
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

    </div>
  )
}
