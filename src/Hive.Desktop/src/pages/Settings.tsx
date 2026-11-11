import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { formatDateTime } from '../utils/dateUtils'
import { Bee } from '../components/AiBeeBadge'
import { Save, CheckCircle, AlertCircle, XCircle, Download, Database, Key, Eye, EyeOff, Loader2, Shirt, Gauge, Tag, X, Plus, ExternalLink, CalendarDays, Cpu, Cloud, LayoutDashboard, Target, Plug } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { PageHeader } from '../components/PageHeader'
import { LoadingState } from '../components/PageState'
import { useConfirm } from '../contexts/ConfirmContext'
import { useHiveStatus } from '../contexts/HiveStatusContext'
import CollapsibleTags from '../components/CollapsibleTags'
import { settingsApi, backupApi, sentimentApi } from '../services/api'
import { apiErrorMessage, getErrorMessage } from '../utils/apiErrors'
import { WorkCategoryNamesCard } from '../components/WorkCategoryNamesCard'
import { AutomaticBackupsPanel } from '../components/AutomaticBackupsPanel'
import type { TshirtSizeMapping, RestoreResultDto, SentimentProvider, UpdateAppSettings, WorkCategoryKey } from '../types'

const DEFAULT_TSHIRT_MAPPINGS: TshirtSizeMapping[] = [
  { size: 'S', sprints: 1, label: '' },
  { size: 'M', sprints: 2, label: '' },
  { size: 'L', sprints: 4, label: '' },
  { size: 'XL', sprints: 6, label: '' },
  { size: 'XXL', sprints: 12, label: '' }
]

// A backup table's name for people: TeamTask -> Team tasks, OneOnOneMeeting -> 1:1s
const TABLE_LABELS: Record<string, string> = {
  DirectReport: 'Team members',
  TeamTask: 'Tasks',
  OneOnOneMeeting: '1:1s',
  ManagerNote: 'Notes',
  SentimentAnalysisCache: 'Sentiment results',
  ProjectKnowledge: 'Project knowledge',
  Activity: 'Activity'
}

function tableLabel(table: string) {
  if (TABLE_LABELS[table]) return TABLE_LABELS[table]
  const words = table.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  const plural = /[^aeiou]y$/.test(words) ? `${words.slice(0, -1)}ies` : `${words}s`
  return `${plural.charAt(0).toUpperCase()}${plural.slice(1)}`
}

type SettingsSection = 'dashboard' | 'planning' | 'integrations' | 'ai' | 'backup'

// One section shows at a time; ?section= opens one directly
// The AI section carries the bee, like every AI feature
function BeeIcon({ className }: { className?: string }) {
  return <span className={`${className ?? ''} inline-flex items-center justify-center`}><Bee size={16} /></span>
}

const SECTIONS: { key: SettingsSection; label: string; icon: typeof Gauge | typeof BeeIcon }[] = [
  { key: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { key: 'planning', label: 'Planning', icon: Target },
  { key: 'integrations', label: 'Integrations', icon: Plug },
  { key: 'ai', label: 'AI analysis', icon: BeeIcon },
  { key: 'backup', label: 'Backup & restore', icon: Database }
]

export default function Settings() {
  const [searchParams, setSearchParams] = useSearchParams()
  const requested = searchParams.get('section')
  const section: SettingsSection = SECTIONS.some(s => s.key === requested) ? (requested as SettingsSection) : 'dashboard'
  const openSection = (key: SettingsSection) => setSearchParams({ section: key }, { replace: true })
  const confirm = useConfirm()
  const { trackWork } = useHiveStatus()
  const [tshirtMappings, setTshirtMappings] = useState<TshirtSizeMapping[]>(DEFAULT_TSHIRT_MAPPINGS)
  const [loading, setLoading] = useState(true)
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

  // AI model state: shared by every AI feature
  const [sentimentProvider, setSentimentProvider] = useState<SentimentProvider>('Ollama')
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState('')
  const [ollamaModel, setOllamaModel] = useState('')
  const [testingOllama, setTestingOllama] = useState(false)
  const [ollamaTestResult, setOllamaTestResult] = useState<{ valid: boolean; error?: string } | null>(null)
  const [hasApiKey, setHasApiKey] = useState(false)
  const [apiKey, setApiKey] = useState('')
  const [showApiKey, setShowApiKey] = useState(false)
  const [validatingKey, setValidatingKey] = useState(false)
  const [keyValidationResult, setKeyValidationResult] = useState<{ valid: boolean; error?: string } | null>(null)
  const [savingModel, setSavingModel] = useState(false)
  const [modelSaved, setModelSaved] = useState(false)
  const [modelError, setModelError] = useState<string | null>(null)

  // Sentiment Analysis state
  const [sentimentEnabled, setSentimentEnabled] = useState(false)
  const [sentimentDays, setSentimentDays] = useState(90)
  const [savingSentiment, setSavingSentiment] = useState(false)
  const [sentimentSaved, setSentimentSaved] = useState(false)
  const [sentimentError, setSentimentError] = useState<string | null>(null)

  // Retro summary state: uses the AI model
  const [retroEnabled, setRetroEnabled] = useState(false)
  const [retroCount, setRetroCount] = useState(6)
  const [savingRetro, setSavingRetro] = useState(false)
  const [retroSaved, setRetroSaved] = useState(false)
  const [retroError, setRetroError] = useState<string | null>(null)

  // Task readiness state: uses the AI model
  const [taskReviewEnabled, setTaskReviewEnabled] = useState(false)
  const [savingTaskReview, setSavingTaskReview] = useState(false)
  const [taskReviewSaved, setTaskReviewSaved] = useState(false)
  const [taskReviewError, setTaskReviewError] = useState<string | null>(null)

  // Dashboard Thresholds state
  const [maxInProgressTasks, setMaxInProgressTasks] = useState(2)
  const [maxBlockedTasks, setMaxBlockedTasks] = useState(1)
  const [maxInReviewTasks, setMaxInReviewTasks] = useState(1)
  const [minProductMembers, setMinProductMembers] = useState(2)
  const [minMemberProducts, setMinMemberProducts] = useState(2)
  const [oneOnOneCadenceDays, setOneOnOneCadenceDays] = useState(14)
  const [staleTaskDays, setStaleTaskDays] = useState(5)
  const [savingThresholds, setSavingThresholds] = useState(false)
  const [thresholdsSaved, setThresholdsSaved] = useState(false)
  const [thresholdsError, setThresholdsError] = useState<string | null>(null)

  // Jira Integration state
  const [jiraBaseUrl, setJiraBaseUrl] = useState('')
  const [jiraEmail, setJiraEmail] = useState('')
  // The token is write-only: the API only says whether one is stored
  const [jiraApiToken, setJiraApiToken] = useState('')
  const [hasJiraApiToken, setHasJiraApiToken] = useState(false)
  const [showJiraApiToken, setShowJiraApiToken] = useState(false)
  const [jiraBoardId, setJiraBoardId] = useState('')
  const [jiraProjectKey, setJiraProjectKey] = useState('')
  const [savingJira, setSavingJira] = useState(false)
  const [jiraSaved, setJiraSaved] = useState(false)
  const [jiraError, setJiraError] = useState<string | null>(null)

  // Google Calendar state. The secret address is write-only, like the Jira token
  const [googleCalendarUrl, setGoogleCalendarUrl] = useState('')
  const [hasGoogleCalendarUrl, setHasGoogleCalendarUrl] = useState(false)
  const [showGoogleCalendarUrl, setShowGoogleCalendarUrl] = useState(false)
  const [savingGoogleCalendar, setSavingGoogleCalendar] = useState(false)
  const [googleCalendarSaved, setGoogleCalendarSaved] = useState(false)
  const [googleCalendarError, setGoogleCalendarError] = useState<string | null>(null)

  // Maintenance labels state (support is recognised by its task type)
  const [maintenanceLabels, setMaintenanceLabels] = useState<string[]>(['maintenance'])
  // Null until the settings are loaded; the card edits and saves them itself
  const [workCategoryNames, setWorkCategoryNames] = useState<Record<WorkCategoryKey, string[]> | null>(null)
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
      setTshirtMappings(settings.tshirtSizeMappings || DEFAULT_TSHIRT_MAPPINGS)
      // Load sentiment analysis settings
      setSentimentEnabled(settings.sentimentAnalysisEnabled)
      setSentimentDays(settings.sentimentAnalysisDays)
      setHasApiKey(settings.hasClaudeApiKey)
      setSentimentProvider(settings.sentimentProvider ?? 'Ollama')
      setRetroEnabled(settings.retroSummaryEnabled ?? false)
      setRetroCount(settings.retroSummaryCount ?? 6)
      setTaskReviewEnabled(settings.taskReviewEnabled ?? false)
      setOllamaBaseUrl(settings.ollamaBaseUrl ?? '')
      setOllamaModel(settings.ollamaModel ?? '')
      // Load dashboard threshold settings
      setMaxInProgressTasks(settings.maxInProgressTasks ?? 2)
      setMaxBlockedTasks(settings.maxBlockedTasks ?? 1)
      setMaxInReviewTasks(settings.maxInReviewTasks ?? 1)
      setMinProductMembers(settings.minProductMembers ?? 2)
      setMinMemberProducts(settings.minMemberProducts ?? 2)
      setOneOnOneCadenceDays(settings.oneOnOneCadenceDays ?? 14)
      setStaleTaskDays(settings.staleTaskDays ?? 5)
      // Load maintenance labels
      setMaintenanceLabels(settings.maintenanceLabels ?? ['maintenance'])
      setWorkCategoryNames(settings.workCategoryEpicNames)
      // Load Jira integration settings
      setJiraBaseUrl(settings.jiraBaseUrl ?? '')
      setJiraEmail(settings.jiraEmail ?? '')
      setHasJiraApiToken(settings.hasJiraApiToken ?? false)
      setJiraBoardId(settings.jiraBoardId ? String(settings.jiraBoardId) : '')
      setJiraProjectKey(settings.jiraProjectKey ?? '')
      setHasGoogleCalendarUrl(settings.hasGoogleCalendarUrl ?? false)
    } catch (err) {
      console.error('Failed to load settings', err)
      setError('Failed to load settings. Using default values.')
      setTshirtMappings(DEFAULT_TSHIRT_MAPPINGS)
    } finally {
      setLoading(false)
    }
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

  // Ollama needs nothing saved to be usable; Claude needs its key
  const providerReady = sentimentProvider === 'Ollama' || hasApiKey

  // Sentiment Analysis handlers
  const handleTestOllama = async () => {
    try {
      setTestingOllama(true)
      setOllamaTestResult(null)
      const result = await sentimentApi.testOllama(ollamaBaseUrl.trim(), ollamaModel.trim())
      setOllamaTestResult({ valid: result.valid, error: result.error ?? undefined })
    } catch (err) {
      console.error('Failed to test the Ollama connection', err)
      setOllamaTestResult({ valid: false, error: apiErrorMessage(err) ?? 'Failed to test the connection' })
    } finally {
      setTestingOllama(false)
    }
  }

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
    } catch (err) {
      console.error('Failed to validate API key', err)
      setKeyValidationResult({ valid: false, error: apiErrorMessage(err) ?? 'Failed to validate API key' })
    } finally {
      setValidatingKey(false)
    }
  }

  const handleSaveModelSettings = async () => {
    try {
      setSavingModel(true)
      setModelError(null)

      const updateData: UpdateAppSettings = {
        sentimentProvider,
        ollamaBaseUrl: ollamaBaseUrl.trim(),
        ollamaModel: ollamaModel.trim()
      }

      // Only include API key if it was entered
      if (apiKey.trim()) {
        updateData.claudeApiKey = apiKey
      }

      await settingsApi.update(updateData)

      // Reload settings to get updated hasApiKey state
      const settings = await settingsApi.get()
      setHasApiKey(settings.hasClaudeApiKey)
      setOllamaBaseUrl(settings.ollamaBaseUrl)
      setOllamaModel(settings.ollamaModel)
      setApiKey('') // Clear the API key field after saving
      setKeyValidationResult(null)

      setModelSaved(true)
      setTimeout(() => setModelSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save AI model settings', err)
      setModelError(apiErrorMessage(err) ?? 'Failed to save AI model settings')
    } finally {
      setSavingModel(false)
    }
  }

  const handleSaveSentimentSettings = async () => {
    try {
      setSavingSentiment(true)
      setSentimentError(null)
      await settingsApi.update({ sentimentAnalysisEnabled: sentimentEnabled, sentimentAnalysisDays: sentimentDays })
      setSentimentSaved(true)
      setTimeout(() => setSentimentSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save sentiment settings', err)
      setSentimentError(apiErrorMessage(err) ?? 'Failed to save sentiment settings')
    } finally {
      setSavingSentiment(false)
    }
  }

  const handleSaveRetroSettings = async () => {
    try {
      setSavingRetro(true)
      setRetroError(null)
      await settingsApi.update({ retroSummaryEnabled: retroEnabled, retroSummaryCount: retroCount })
      setRetroSaved(true)
      setTimeout(() => setRetroSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save retro summary settings', err)
      setRetroError(apiErrorMessage(err) ?? 'Failed to save retro summary settings')
    } finally {
      setSavingRetro(false)
    }
  }

  const handleSaveTaskReview = async () => {
    try {
      setSavingTaskReview(true)
      setTaskReviewError(null)
      await settingsApi.update({ taskReviewEnabled })
      setTaskReviewSaved(true)
      setTimeout(() => setTaskReviewSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save task readiness settings', err)
      setTaskReviewError(apiErrorMessage(err) ?? 'Failed to save task readiness settings')
    } finally {
      setSavingTaskReview(false)
    }
  }

  const handleClearApiKey = async () => {
    if (!(await confirm({
      title: 'Remove the Claude API key?',
      message: 'The AI features cannot use Claude, and sentiment analysis is turned off, until a key is entered again.',
      confirmLabel: 'Remove'
    }))) {
      return
    }

    try {
      setSavingModel(true)
      setModelError(null)

      await settingsApi.update({
        claudeApiKey: '',
        sentimentAnalysisEnabled: false
      })

      setHasApiKey(false)
      setSentimentEnabled(false)
      setApiKey('')
      setKeyValidationResult(null)

      setModelSaved(true)
      setTimeout(() => setModelSaved(false), 3000)
    } catch (err) {
      console.error('Failed to clear API key', err)
      setModelError(apiErrorMessage(err) ?? 'Failed to clear API key')
    } finally {
      setSavingModel(false)
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
        minProductMembers,
        minMemberProducts,
        oneOnOneCadenceDays,
        staleTaskDays
      })
      setThresholdsSaved(true)
      setTimeout(() => setThresholdsSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save threshold settings', err)
      setThresholdsError(`Failed to save threshold settings. ${getErrorMessage(err)}`)
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
        minProductMembers: 2,
        minMemberProducts: 2,
        oneOnOneCadenceDays: 14,
        staleTaskDays: 5
      })
      setMaxInProgressTasks(2)
      setMaxBlockedTasks(1)
      setMaxInReviewTasks(1)
      setMinProductMembers(2)
      setMinMemberProducts(2)
      setOneOnOneCadenceDays(14)
      setStaleTaskDays(5)
      setThresholdsSaved(true)
      setTimeout(() => setThresholdsSaved(false), 3000)
    } catch (err) {
      console.error('Failed to reset threshold settings', err)
      setThresholdsError('Failed to reset threshold settings. Please try again.')
    } finally {
      setSavingThresholds(false)
    }
  }

  // Jira Integration handlers
  const handleSaveJira = async () => {
    try {
      setSavingJira(true)
      setJiraError(null)
      setJiraSaved(false)
      const boardId = jiraBoardId.trim()
      const updated = await settingsApi.update({
        jiraBaseUrl: jiraBaseUrl || null,
        jiraEmail: jiraEmail.trim(),
        jiraProjectKey: jiraProjectKey.trim(),
        // A blank token field means "keep the stored token"
        ...(jiraApiToken.trim() ? { jiraApiToken: jiraApiToken.trim() } : {}),
        ...(boardId ? { jiraBoardId: Number(boardId) } : { clearJiraBoardId: true })
      })
      setHasJiraApiToken(updated.hasJiraApiToken)
      setJiraProjectKey(updated.jiraProjectKey ?? '')
      setJiraApiToken('')
      setJiraSaved(true)
      setTimeout(() => setJiraSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save Jira settings', err)
      setJiraError(`Failed to save Jira settings. ${getErrorMessage(err)}`)
    } finally {
      setSavingJira(false)
    }
  }

  const handleRemoveJiraApiToken = async () => {
    try {
      setSavingJira(true)
      setJiraError(null)
      const updated = await settingsApi.update({ jiraApiToken: '' })
      setHasJiraApiToken(updated.hasJiraApiToken)
      setJiraApiToken('')
    } catch (err) {
      console.error('Failed to remove Jira API token', err)
      setJiraError(`Failed to remove the Jira API token. ${getErrorMessage(err)}`)
    } finally {
      setSavingJira(false)
    }
  }

  // Google Calendar handlers
  const saveGoogleCalendarUrl = async (url: string) => {
    try {
      setSavingGoogleCalendar(true)
      setGoogleCalendarError(null)
      setGoogleCalendarSaved(false)
      const updated = await settingsApi.update({ googleCalendarUrl: url })
      setHasGoogleCalendarUrl(updated.hasGoogleCalendarUrl)
      setGoogleCalendarUrl('')
      if (url) {
        setGoogleCalendarSaved(true)
        setTimeout(() => setGoogleCalendarSaved(false), 3000)
      }
    } catch (err) {
      console.error('Failed to save Google Calendar address', err)
      setGoogleCalendarError(`Failed to save the Google Calendar address. ${getErrorMessage(err)}`)
    } finally {
      setSavingGoogleCalendar(false)
    }
  }

  // Maintenance labels handlers
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
      const backup = await trackWork(backupApi.export())

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
    } catch (err) {
      console.error('Failed to export backup', err)
      setBackupError(apiErrorMessage(err) ?? (err instanceof Error ? err.message : 'Failed to export backup'))
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

        if (!(await confirm({
          title: `Restore the backup from ${formatDateTime(backup.exportedAt)}?`,
          message: 'It adds what is missing and keeps the records you already have. Settings are taken from the backup, except your keys and addresses.',
          confirmLabel: 'Restore',
          tone: 'default'
        }))) {
          setRestoreLoading(false)
          return
        }

        const result = await trackWork(backupApi.import(backup))
        setRestoreResult(result)
      } catch (err) {
        console.error('Failed to restore backup', err)
        setBackupError(apiErrorMessage(err) ?? (err instanceof Error ? err.message : 'Failed to restore backup'))
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
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Settings" subtitle="Configure application preferences" />

      {error && (
        <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error}
        </div>
      )}

      <div className="flex flex-col gap-6 md:flex-row md:items-start">
        <nav aria-label="Settings sections" className="md:sticky md:top-4 md:w-52 md:shrink-0">
          <ul className="flex gap-1 overflow-x-auto md:flex-col">
            {SECTIONS.map(({ key, label, icon: Icon }) => (
              <li key={key}>
                <button
                  type="button"
                  onClick={() => openSection(key)}
                  aria-current={section === key ? 'page' : undefined}
                  className={`flex w-full items-center gap-2 whitespace-nowrap rounded-lg px-3 py-2 text-left text-sm font-medium transition-colors ${
                    section === key ? 'bg-amber-50 text-amber-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                  }`}
                >
                  <Icon className="h-4 w-4" />
                  {label}
                </button>
              </li>
            ))}
          </ul>
        </nav>

        <div className="min-w-0 flex-1 space-y-6">
          {section === 'dashboard' && (
            <>
            {/* Dashboard Thresholds */}
            <Card>
              <CardHeader
                title="Dashboard Warning Thresholds"
                subtitle="Configure when warnings appear on the dashboard for 1:1s, workload and knowledge silos"
                action={<Gauge className="w-5 h-5 text-orange-500" />}
              />
              <CardContent>
                <div className="space-y-4">
                  <p className="text-sm text-slate-600">
                    Adjust these thresholds to control when the dashboard shows warnings about team workload and project coverage.
                  </p>

                  {/* Workload Thresholds */}
                  <div className="space-y-3">
                    <h3 className="text-sm font-medium text-slate-700">Workload Warnings</h3>
                    <p className="text-xs text-slate-500">
                      Show a warning when a team member exceeds these task counts
                    </p>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div className="p-3 bg-slate-50 rounded-lg">
                        <label className="block text-sm text-slate-600 mb-2">
                          Max In Progress Tasks
                        </label>
                        <input
                          type="number"
                          value={maxInProgressTasks}
                          onChange={(e) => {
                            const val = parseInt(e.target.value)
                            if (!isNaN(val) && val >= 1) setMaxInProgressTasks(val)
                          }}
                          className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                          min="1"
                          disabled={savingThresholds}
                        />
                        <p className="mt-1 text-xs text-slate-500">
                          Warn if &gt; {maxInProgressTasks} in progress
                        </p>
                      </div>

                      <div className="p-3 bg-slate-50 rounded-lg">
                        <label className="block text-sm text-slate-600 mb-2">
                          Max Blocked Tasks
                        </label>
                        <input
                          type="number"
                          value={maxBlockedTasks}
                          onChange={(e) => {
                            const val = parseInt(e.target.value)
                            if (!isNaN(val) && val >= 1) setMaxBlockedTasks(val)
                          }}
                          className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                          min="1"
                          disabled={savingThresholds}
                        />
                        <p className="mt-1 text-xs text-slate-500">
                          Warn if &gt; {maxBlockedTasks} blocked
                        </p>
                      </div>

                      <div className="p-3 bg-slate-50 rounded-lg">
                        <label className="block text-sm text-slate-600 mb-2">
                          Max In Review Tasks
                        </label>
                        <input
                          type="number"
                          value={maxInReviewTasks}
                          onChange={(e) => {
                            const val = parseInt(e.target.value)
                            if (!isNaN(val) && val >= 1) setMaxInReviewTasks(val)
                          }}
                          className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                          min="1"
                          disabled={savingThresholds}
                        />
                        <p className="mt-1 text-xs text-slate-500">
                          Warn if &gt; {maxInReviewTasks} in review
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* Product engagement thresholds */}
                  <div className="pt-4 border-t space-y-3">
                    <h3 className="text-sm font-medium text-slate-700">Product Engagement</h3>
                    <p className="text-xs text-slate-500">
                      Flag products that too few people have worked on, and people who have worked on too few products
                    </p>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="p-3 bg-slate-50 rounded-lg">
                        <label className="block text-sm text-slate-600 mb-2">
                          Minimum Product Members
                        </label>
                        <input
                          type="number"
                          value={minProductMembers}
                          onChange={(e) => {
                            const val = parseInt(e.target.value)
                            if (!isNaN(val) && val >= 1) setMinProductMembers(val)
                          }}
                          className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                          min="1"
                          disabled={savingThresholds}
                        />
                        <p className="mt-1 text-xs text-slate-500">
                          Warn if &lt; {minProductMembers} members on a product
                        </p>
                      </div>

                      <div className="p-3 bg-slate-50 rounded-lg">
                        <label htmlFor="min-member-products" className="block text-sm text-slate-600 mb-2">
                          Minimum Products per Person
                        </label>
                        <input
                          id="min-member-products"
                          type="number"
                          value={minMemberProducts}
                          onChange={(e) => {
                            const val = parseInt(e.target.value)
                            if (!isNaN(val)) setMinMemberProducts(val)
                          }}
                          className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                          min="1"
                          max="50"
                          disabled={savingThresholds}
                        />
                        <p className="mt-1 text-xs text-slate-500">
                          Warn if someone has worked on &lt; {minMemberProducts} products
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* 1:1 cadence */}
                  <div className="pt-4 border-t space-y-3">
                    <h3 className="text-sm font-medium text-slate-700">1:1 Cadence</h3>
                    <p className="text-xs text-slate-500">
                      How often you aim to hold a 1:1 with each direct report. A 1:1 is overdue once this many days have passed since the last one.
                    </p>

                    <div className="p-3 bg-slate-50 rounded-lg max-w-xs">
                      <label htmlFor="one-on-one-cadence" className="block text-sm text-slate-600 mb-2">
                        Days between 1:1s
                      </label>
                      <input
                        id="one-on-one-cadence"
                        type="number"
                        value={oneOnOneCadenceDays}
                        onChange={(e) => {
                          const val = parseInt(e.target.value)
                          if (!isNaN(val)) setOneOnOneCadenceDays(val)
                        }}
                        className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                        min="1"
                        max="90"
                        disabled={savingThresholds}
                      />
                      <p className="mt-1 text-xs text-slate-500">
                        Overdue after {oneOnOneCadenceDays} days
                      </p>
                    </div>
                  </div>

                  {/* Stale tasks */}
                  <div className="pt-4 border-t space-y-3">
                    <h3 className="text-sm font-medium text-slate-700">Stale Tasks</h3>
                    <p className="text-xs text-slate-500">
                      A task in progress, blocked, in review, in test, awaiting PO acceptance or ready to release is stale once its status has not changed for this many days. Hive counts from the sync, import or edit in which it saw the status change.
                    </p>

                    <div className="p-3 bg-slate-50 rounded-lg max-w-xs">
                      <label htmlFor="stale-task-days" className="block text-sm text-slate-600 mb-2">
                        Days in one status
                      </label>
                      <input
                        id="stale-task-days"
                        type="number"
                        value={staleTaskDays}
                        onChange={(e) => {
                          const val = parseInt(e.target.value)
                          if (!isNaN(val)) setStaleTaskDays(val)
                        }}
                        className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
                        min="1"
                        max="60"
                        disabled={savingThresholds}
                      />
                      <p className="mt-1 text-xs text-slate-500">
                        Stale after {staleTaskDays} days
                      </p>
                    </div>
                  </div>

                  {thresholdsError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                      {thresholdsError}
                    </div>
                  )}

                  <div className="pt-4 border-t flex gap-3">
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
                      className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Reset to Defaults
                    </button>
                  </div>

                  {thresholdsSaved && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
                      Dashboard threshold settings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
            {/* Maintenance Labels */}
            <Card>
              <CardHeader
                title="Maintenance Labels"
                subtitle="Configure which labels identify maintenance work"
                action={<Tag className="w-5 h-5 text-purple-500" />}
              />
              <CardContent>
                <div className="space-y-4">
                  <p className="text-sm text-slate-600">
                    Define which labels/tags count as maintenance work in the Dashboard widgets. Support work is recognised by its Jira issue type, Support.
                  </p>

                  {/* Maintenance Labels */}
                  <div className="space-y-3">
                    <h3 className="text-sm font-medium text-slate-700">Maintenance Labels</h3>
                    <p className="text-xs text-slate-500">
                      Tasks with these labels/tags will be counted in the Maintenance story point widgets
                    </p>

                    <div className="flex flex-wrap gap-2 mb-3">
                      <CollapsibleTags
                        items={maintenanceLabels}
                        renderItem={(label) => (
                          <span
                            key={label}
                            className="inline-flex items-center gap-1 px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-sm"
                          >
                            {label}
                            <button
                              onClick={() => handleRemoveMaintenanceLabel(label)}
                              className="ml-1 hover:bg-blue-200 rounded-full p-0.5"
                              disabled={savingLabels}
                            >
                              <X className="w-3 h-3" />
                            </button>
                          </span>
                        )}
                      />
                    </div>

                    <div className="flex gap-2">
                      <input
                        type="text"
                        value={maintenanceLabelInput}
                        onChange={(e) => setMaintenanceLabelInput(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleAddMaintenanceLabel()}
                        placeholder="e.g., tech-debt, refactor, chore"
                        className="flex-1 px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
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
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                      {labelsError}
                    </div>
                  )}

                  <div className="pt-4 border-t flex gap-3">
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
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
                      Label settings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {workCategoryNames && <WorkCategoryNamesCard initial={workCategoryNames} />}
            </>
          )}

          {section === 'planning' && (
            <>
            {/* T-Shirt Size to Sprints Mapping */}
            <Card>
              <CardHeader
                title="T-Shirt Size to Sprints Mapping"
                subtitle="Configure how T-shirt sizes translate to number of sprints for initiative estimation"
                action={<Shirt className="w-5 h-5 text-blue-500" />}
              />
              <CardContent>
                <div className="space-y-4">
                  <p className="text-sm text-slate-600">
                    Use this to estimate initiative duration based on T-shirt size complexity.
                  </p>

                  <div className="space-y-3">
                    {tshirtMappings.map((mapping, index) => (
                      <div key={mapping.size} className="flex items-center gap-4 p-3 bg-slate-50 rounded-lg">
                        <div className="flex-shrink-0 w-16">
                          <span className="text-lg font-semibold text-slate-900">{mapping.size}</span>
                        </div>
                        <span className="text-slate-500">=</span>
                        <div className="flex items-center gap-2">
                          <input
                            type="number"
                            value={mapping.sprints}
                            onChange={(e) => handleTshirtSprintsChange(index, e.target.value)}
                            className="w-24 px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                            min="0.5"
                            step="0.5"
                            disabled={savingTshirt}
                          />
                          <span className="text-slate-700">sprint{mapping.sprints !== 1 ? 's' : ''}</span>
                        </div>
                      </div>
                    ))}
                  </div>

                  {tshirtError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                      {tshirtError}
                    </div>
                  )}

                  <div className="pt-4 border-t flex gap-3">
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
                      className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Reset to Defaults
                    </button>
                  </div>

                  {tshirtSaved && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
                      T-shirt size mappings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
            </>
          )}

          {section === 'integrations' && (
            <>
            {/* Jira Integration */}
            <Card>
              <CardHeader
                title="Jira Integration"
                subtitle="Link tasks to Jira issues and sync sprint dates from Jira Cloud"
                action={<ExternalLink className="w-5 h-5 text-blue-500" />}
              />
              <CardContent>
                <div className="space-y-4">
                  <p className="text-sm text-slate-600">
                    Set your Jira site URL to link tasks and parents to their issues in Jira, e.g. <code className="text-xs bg-slate-100 px-1 py-0.5 rounded">https://yourteam.atlassian.net</code>. Issues open at <code className="text-xs bg-slate-100 px-1 py-0.5 rounded">…/browse/MAR-128</code>.
                  </p>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">
                      Jira Base URL
                    </label>
                    <input
                      type="url"
                      value={jiraBaseUrl}
                      onChange={(e) => setJiraBaseUrl(e.target.value)}
                      placeholder="https://yourteam.atlassian.net"
                      className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      disabled={savingJira}
                    />
                  </div>

                  <div className="pt-2 border-t border-slate-200">
                    <h4 className="text-sm font-semibold text-slate-900 mt-2 mb-1">Jira Cloud API</h4>
                    <p className="text-sm text-slate-600">
                      Used by <strong>Sync Jira</strong> to copy sprint dates from the board and to keep a parent for every
                      epic (and every other issue with sub-tasks) in the space. Hive only reads from Jira. Create an API token at{' '}
                      <code className="text-xs bg-slate-100 px-1 py-0.5 rounded">id.atlassian.com/manage-profile/security/api-tokens</code>.
                      The token is stored in Hive's local database.
                    </p>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label htmlFor="jira-email" className="block text-sm font-medium text-slate-700 mb-1">
                        Atlassian Account Email
                      </label>
                      <input
                        id="jira-email"
                        type="email"
                        value={jiraEmail}
                        onChange={(e) => setJiraEmail(e.target.value)}
                        placeholder="you@company.com"
                        className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        disabled={savingJira}
                      />
                    </div>

                    <div>
                      <label htmlFor="jira-board-id" className="block text-sm font-medium text-slate-700 mb-1">
                        Board ID
                      </label>
                      <input
                        id="jira-board-id"
                        type="number"
                        min={1}
                        value={jiraBoardId}
                        onChange={(e) => setJiraBoardId(e.target.value)}
                        placeholder="e.g. 42"
                        className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        disabled={savingJira}
                      />
                      <p className="mt-1 text-xs text-slate-500">
                        The number in the board URL: <code>…/boards/<strong>42</strong></code>. Used for sprint dates.
                      </p>
                    </div>

                    <div>
                      <label htmlFor="jira-project-key" className="block text-sm font-medium text-slate-700 mb-1">
                        Space Key
                      </label>
                      <input
                        id="jira-project-key"
                        type="text"
                        value={jiraProjectKey}
                        onChange={(e) => setJiraProjectKey(e.target.value.toUpperCase())}
                        placeholder="e.g. MAR"
                        className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 font-mono"
                        disabled={savingJira}
                      />
                      <p className="mt-1 text-xs text-slate-500">
                        The prefix of the space's issue keys (<code><strong>MAR</strong>-128</code>). Its epics, and any other issue with sub-tasks, become parents.
                      </p>
                    </div>
                  </div>

                  <div>
                    <label htmlFor="jira-api-token" className="block text-sm font-medium text-slate-700 mb-1">
                      API Token
                    </label>
                    <div className="flex gap-2">
                      <div className="relative flex-1">
                        <input
                          id="jira-api-token"
                          type={showJiraApiToken ? 'text' : 'password'}
                          value={jiraApiToken}
                          onChange={(e) => setJiraApiToken(e.target.value)}
                          placeholder={hasJiraApiToken ? 'Token saved. Leave blank to keep it' : 'Paste your Atlassian API token'}
                          autoComplete="off"
                          className="w-full px-3 py-2 pr-10 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                          disabled={savingJira}
                        />
                        <button
                          type="button"
                          onClick={() => setShowJiraApiToken(!showJiraApiToken)}
                          aria-label={showJiraApiToken ? 'Hide token' : 'Show token'}
                          className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                        >
                          {showJiraApiToken ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                        </button>
                      </div>
                      {hasJiraApiToken && (
                        <button
                          type="button"
                          onClick={handleRemoveJiraApiToken}
                          disabled={savingJira}
                          className="px-3 py-2 text-sm border border-red-300 text-red-600 rounded-lg hover:bg-red-50 transition-colors disabled:opacity-50"
                        >
                          Remove
                        </button>
                      )}
                    </div>
                    {hasJiraApiToken && (
                      <p className="mt-1 flex items-center gap-1 text-xs text-green-600">
                        <CheckCircle className="w-3 h-3" /> API token configured
                      </p>
                    )}
                  </div>

                  {jiraError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                      {jiraError}
                    </div>
                  )}

                  <div className="flex gap-3">
                    <button
                      onClick={handleSaveJira}
                      disabled={savingJira}
                      className="flex items-center gap-2 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      <Save className="w-5 h-5" />
                      {savingJira ? 'Saving...' : jiraSaved ? 'Saved!' : 'Save'}
                    </button>
                  </div>

                  {jiraSaved && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm">
                      Jira settings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
            {/* Google Calendar */}
            <Card>
              <CardHeader
                title="Google Calendar"
                subtitle="Show your Google Calendar events on the Daily Planner"
                action={<CalendarDays className="w-5 h-5 text-sky-500" />}
              />
              <CardContent>
                <div className="space-y-4">
                  <p className="text-sm text-slate-600">
                    In Google Calendar open <strong>Settings</strong>, pick your calendar, and under{' '}
                    <strong>Integrate calendar</strong> copy the <strong>Secret address in iCal format</strong>. No sign-in
                    or API key is needed: the address is the key, so treat it like a password. Hive only reads from it, and
                    stores it in its local database. If it ever leaks, reset it in Google Calendar and paste the new one here.
                  </p>

                  <div>
                    <label htmlFor="google-calendar-url" className="block text-sm font-medium text-slate-700 mb-1">
                      Secret Address in iCal Format
                    </label>
                    <div className="flex gap-2">
                      <div className="relative flex-1">
                        <input
                          id="google-calendar-url"
                          type={showGoogleCalendarUrl ? 'text' : 'password'}
                          value={googleCalendarUrl}
                          onChange={(e) => setGoogleCalendarUrl(e.target.value)}
                          placeholder={hasGoogleCalendarUrl
                            ? 'Address saved. Paste a new one to replace it'
                            : 'https://calendar.google.com/calendar/ical/…/private-…/basic.ics'}
                          autoComplete="off"
                          className="w-full px-3 py-2 pr-10 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-sky-500"
                          disabled={savingGoogleCalendar}
                        />
                        <button
                          type="button"
                          onClick={() => setShowGoogleCalendarUrl(!showGoogleCalendarUrl)}
                          aria-label={showGoogleCalendarUrl ? 'Hide address' : 'Show address'}
                          className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                        >
                          {showGoogleCalendarUrl ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                        </button>
                      </div>
                      {hasGoogleCalendarUrl && (
                        <button
                          type="button"
                          onClick={() => saveGoogleCalendarUrl('')}
                          disabled={savingGoogleCalendar}
                          className="px-3 py-2 text-sm border border-red-300 text-red-600 rounded-lg hover:bg-red-50 transition-colors disabled:opacity-50"
                        >
                          Remove
                        </button>
                      )}
                    </div>
                    {hasGoogleCalendarUrl && (
                      <p className="mt-1 flex items-center gap-1 text-xs text-green-600">
                        <CheckCircle className="w-3 h-3" /> Google Calendar connected
                      </p>
                    )}
                  </div>

                  {googleCalendarError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                      {googleCalendarError}
                    </div>
                  )}

                  <div className="flex gap-3">
                    <button
                      onClick={() => saveGoogleCalendarUrl(googleCalendarUrl.trim())}
                      disabled={savingGoogleCalendar || !googleCalendarUrl.trim()}
                      className="flex items-center gap-2 px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      <Save className="w-5 h-5" />
                      {savingGoogleCalendar ? 'Saving...' : googleCalendarSaved ? 'Saved!' : 'Save'}
                    </button>
                  </div>
                </div>
              </CardContent>
            </Card>
            </>
          )}

          {section === 'ai' && (
            <>
            {/* AI Model: shared by every AI feature */}
            <Card>
              <CardHeader
                ai
                title="AI Model"
                subtitle="The model the AI bees work with, for every AI feature: a local model or Claude"
              />
              <CardContent>
                <div className="space-y-6">
                  {/* Status Indicator */}
                  <div className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
                    <div className="flex items-center gap-3">
                      <div className={`w-3 h-3 rounded-full ${providerReady ? 'bg-green-500' : 'bg-slate-400'}`}></div>
                      <span className="text-sm font-medium text-slate-700">
                        {providerReady ? 'Configured' : 'Not configured'}
                      </span>
                    </div>
                    {providerReady && (
                      <span className="text-xs text-slate-500">
                        {sentimentProvider === 'Ollama' ? `Local model ${ollamaModel || 'llama3.1:8b'}` : 'Claude'}
                      </span>
                    )}
                  </div>

                  {/* Provider */}
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">Model</label>
                    <div className="grid grid-cols-2 gap-3" role="radiogroup" aria-label="AI model">
                      {([
                        { value: 'Ollama', icon: Cpu, title: 'Local (Ollama)', body: 'Runs on this machine. Notes never leave it.' },
                        { value: 'Claude', icon: Cloud, title: 'Claude API', body: 'Better judgement. Notes are sent to Anthropic.' },
                      ] as const).map(option => (
                        <button
                          key={option.value}
                          type="button"
                          role="radio"
                          aria-checked={sentimentProvider === option.value}
                          onClick={() => setSentimentProvider(option.value)}
                          className={`flex items-start gap-3 p-3 text-left rounded-lg border transition-colors ${
                            sentimentProvider === option.value
                              ? 'border-purple-500 bg-purple-50 ring-1 ring-purple-500'
                              : 'border-slate-200 bg-white hover:bg-slate-50'
                          }`}
                        >
                          <option.icon className={`w-5 h-5 mt-0.5 flex-shrink-0 ${sentimentProvider === option.value ? 'text-purple-600' : 'text-slate-400'}`} />
                          <span>
                            <span className="block text-sm font-medium text-slate-900">{option.title}</span>
                            <span className="block text-xs text-slate-500 mt-0.5">{option.body}</span>
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {sentimentProvider === 'Ollama' ? (
                    <div className="space-y-3">
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <label htmlFor="ollama-url" className="block text-sm font-medium text-slate-700 mb-1">Ollama address</label>
                          <input
                            id="ollama-url"
                            type="text"
                            value={ollamaBaseUrl}
                            onChange={(e) => { setOllamaBaseUrl(e.target.value); setOllamaTestResult(null) }}
                            placeholder="http://localhost:11434"
                            className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                          />
                        </div>
                        <div>
                          <label htmlFor="ollama-model" className="block text-sm font-medium text-slate-700 mb-1">Model</label>
                          <input
                            id="ollama-model"
                            type="text"
                            value={ollamaModel}
                            onChange={(e) => { setOllamaModel(e.target.value); setOllamaTestResult(null) }}
                            placeholder="llama3.1:8b"
                            className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                          />
                        </div>
                      </div>
                      <div className="flex items-center gap-3">
                        <button
                          onClick={handleTestOllama}
                          disabled={testingOllama}
                          className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 text-sm"
                        >
                          {testingOllama && <Loader2 className="w-4 h-4 animate-spin" />}
                          Test connection
                        </button>
                        {ollamaTestResult && (
                          <div className={`flex items-center gap-2 text-sm ${ollamaTestResult.valid ? 'text-green-600' : 'text-red-600'}`}>
                            {ollamaTestResult.valid ? <CheckCircle className="w-4 h-4 flex-shrink-0" /> : <XCircle className="w-4 h-4 flex-shrink-0" />}
                            {ollamaTestResult.valid ? 'Ollama is running and has the model' : ollamaTestResult.error}
                          </div>
                        )}
                      </div>
                      <p className="text-xs text-slate-500">
                        Install Ollama from{' '}
                        <a href="https://ollama.com/download" target="_blank" rel="noopener noreferrer" className="text-purple-500 hover:underline">
                          ollama.com
                        </a>
                        , then run <code className="px-1 py-0.5 bg-slate-100 rounded">ollama pull {ollamaModel.trim() || 'llama3.1:8b'}</code>.
                        A local model takes longer than Claude, from seconds to a few minutes a request.
                      </p>
                    </div>
                  ) : (
                    <div>
                      <label className="block text-sm font-medium text-slate-700 mb-2">
                        <div className="flex items-center gap-2">
                          <Key className="w-4 h-4" />
                          Claude API Key
                        </div>
                      </label>
                      {hasApiKey ? (
                        <div className="flex items-center gap-3">
                          <div className="flex-1 px-3 py-2 bg-slate-100 rounded-lg text-slate-600 text-sm">
                            API key configured
                          </div>
                          <button
                            onClick={handleClearApiKey}
                            disabled={savingModel}
                            className="px-3 py-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors text-sm"
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
                                className="w-full px-3 py-2 pr-10 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                              />
                              <button
                                type="button"
                                onClick={() => setShowApiKey(!showApiKey)}
                                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                              >
                                {showApiKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                              </button>
                            </div>
                            <button
                              onClick={handleValidateApiKey}
                              disabled={validatingKey || !apiKey.trim()}
                              className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                            >
                              {validatingKey ? (
                                <Loader2 className="w-4 h-4 animate-spin" />
                              ) : (
                                'Validate'
                              )}
                            </button>
                          </div>
                          {keyValidationResult && (
                            <div className={`flex items-center gap-2 text-sm ${keyValidationResult.valid ? 'text-green-600' : 'text-red-600'}`}>
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
                          <p className="text-xs text-slate-500">
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
                  )}

                  {/* Error Message */}
                  {modelError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {modelError}
                    </div>
                  )}

                  {/* Save Button */}
                  <div className="pt-4 border-t">
                    <button
                      onClick={handleSaveModelSettings}
                      disabled={savingModel}
                      className="flex items-center gap-2 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {savingModel ? (
                        <Loader2 className="w-5 h-5 animate-spin" />
                      ) : (
                        <Save className="w-5 h-5" />
                      )}
                      {savingModel ? 'Saving...' : modelSaved ? 'Saved!' : 'Save Model Settings'}
                    </button>
                  </div>

                  {modelSaved && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm flex items-center gap-2">
                      <CheckCircle className="w-4 h-4" />
                      AI model settings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* AI Sentiment Analysis */}
            <Card>
              <CardHeader
                ai
                title="AI Sentiment Analysis"
                subtitle="Analyze 1:1 notes for team morale, with the model chosen above"
              />
              <CardContent>
                <div className="space-y-6">
                  {/* Enable/Disable Toggle */}
                  <div className="flex items-center justify-between">
                    <div>
                      <label className="block text-sm font-medium text-slate-700">
                        Enable Sentiment Analysis
                      </label>
                      <p className="text-sm text-slate-500 mt-1">
                        Reads the 1:1 notes of each person and scores their morale, with the themes behind it.
                        Shows a sentiment trend for the team and for each person.
                      </p>
                    </div>
                    <button
                      onClick={() => setSentimentEnabled(!sentimentEnabled)}
                      disabled={!providerReady}
                      aria-label="Enable Sentiment Analysis"
                      aria-pressed={sentimentEnabled}
                      className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors ${
                        sentimentEnabled ? 'bg-purple-500' : 'bg-slate-300'
                      } ${!providerReady ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          sentimentEnabled ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>

                  {!providerReady && (
                    <p className="text-xs text-slate-500">Enter a Claude API key in AI Model above to turn this on.</p>
                  )}

                  {/* Analysis Period */}
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Analysis Period
                    </label>
                    <select
                      value={sentimentDays}
                      onChange={(e) => setSentimentDays(parseInt(e.target.value))}
                      className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                    >
                      <option value={30}>Last 30 days</option>
                      <option value={60}>Last 60 days</option>
                      <option value={90}>Last 90 days (recommended)</option>
                      <option value={180}>Last 180 days</option>
                    </select>
                    <p className="mt-1 text-xs text-slate-500">
                      Meeting notes from this period will be analyzed for sentiment.
                      {sentimentProvider === 'Ollama' ? ' They never leave this machine.' : ' They are sent to Anthropic.'}
                    </p>
                  </div>

                  {/* Error Message */}
                  {sentimentError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {sentimentError}
                    </div>
                  )}

                  {/* Save Button */}
                  <div className="pt-4 border-t">
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
                      {savingSentiment ? 'Saving...' : sentimentSaved ? 'Saved!' : 'Save Sentiment Settings'}
                    </button>
                  </div>

                  {sentimentSaved && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-green-700 text-sm flex items-center gap-2">
                      <CheckCircle className="w-4 h-4" />
                      Sentiment analysis settings saved successfully!
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* AI Task Readiness */}
            <Card>
              <CardHeader
                ai
                title="AI Task Readiness"
                subtitle="Review the unstarted work of the current and next sprint before it is built, with the model chosen above"
              />
              <CardContent>
                <div className="space-y-6">
                  <div className="flex items-center justify-between">
                    <div>
                      <label className="block text-sm font-medium text-slate-700">
                        Enable Task Review
                      </label>
                      <p className="text-sm text-slate-500 mt-1">
                        Reads each task&apos;s title and description and says what is missing, such as acceptance
                        criteria or steps to reproduce. Reviewed tasks carry the AI bee badge. Tasks without acceptance
                        criteria are flagged either way, without AI.
                      </p>
                    </div>
                    <button
                      onClick={() => setTaskReviewEnabled(!taskReviewEnabled)}
                      aria-label="Enable Task Review"
                      aria-pressed={taskReviewEnabled}
                      className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors cursor-pointer ${
                        taskReviewEnabled ? 'bg-purple-500' : 'bg-slate-300'
                      }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          taskReviewEnabled ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>

                  <p className="text-xs text-slate-500">
                    {sentimentProvider === 'Ollama'
                      ? 'Tasks never leave this machine. A local model takes about 10 to 30 seconds a task; the Dashboard reviews them one by one while it is open.'
                      : 'Task titles and descriptions are sent to Anthropic.'}
                  </p>

                  {taskReviewError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {taskReviewError}
                    </div>
                  )}

                  <div className="pt-4 border-t">
                    <button
                      onClick={handleSaveTaskReview}
                      disabled={savingTaskReview}
                      className="flex items-center gap-2 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {savingTaskReview ? <Loader2 className="w-5 h-5 animate-spin" /> : <Save className="w-5 h-5" />}
                      {savingTaskReview ? 'Saving...' : taskReviewSaved ? 'Saved!' : 'Save Task Review Settings'}
                    </button>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* AI Retro Summary */}
            <Card>
              <CardHeader
                ai
                title="AI Retro Summary"
                subtitle="Summarise the notes in your Retro folder on the dashboard, with the model chosen above"
              />
              <CardContent>
                <div className="space-y-6">
                  <div className="flex items-center justify-between">
                    <div>
                      <label className="block text-sm font-medium text-slate-700">
                        Enable Retro Summary
                      </label>
                      <p className="text-sm text-slate-500 mt-1">
                        Reads the notes in the Notes folder named Retro, one per retro, titled by its date.
                        Adds a summary, action items, suggestions and warnings to Team health.
                      </p>
                    </div>
                    <button
                      onClick={() => setRetroEnabled(!retroEnabled)}
                      aria-label="Enable Retro Summary"
                      aria-pressed={retroEnabled}
                      className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors cursor-pointer ${
                        retroEnabled ? 'bg-purple-500' : 'bg-slate-300'
                      }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          retroEnabled ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>

                  <div>
                    <label htmlFor="retro-count" className="block text-sm font-medium text-slate-700 mb-2">
                      Retros to read
                    </label>
                    <select
                      id="retro-count"
                      value={retroCount}
                      onChange={(e) => setRetroCount(parseInt(e.target.value))}
                      className="w-full px-3 py-2 border border-slate-300 bg-white text-slate-900 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                    >
                      {[3, 6, 10, 20].map(count => (
                        <option key={count} value={count}>
                          Latest {count}{count === 6 ? ' (recommended)' : ''}
                        </option>
                      ))}
                    </select>
                    <p className="mt-1 text-xs text-slate-500">
                      {sentimentProvider === 'Ollama'
                        ? 'The retros never leave this machine. A local model can take a minute or more.'
                        : 'The retros are sent to Anthropic.'}
                    </p>
                  </div>

                  {retroError && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {retroError}
                    </div>
                  )}

                  <div className="pt-4 border-t">
                    <button
                      onClick={handleSaveRetroSettings}
                      disabled={savingRetro}
                      className="flex items-center gap-2 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {savingRetro ? <Loader2 className="w-5 h-5 animate-spin" /> : <Save className="w-5 h-5" />}
                      {savingRetro ? 'Saving...' : retroSaved ? 'Saved!' : 'Save Retro Settings'}
                    </button>
                  </div>
                </div>
              </CardContent>
            </Card>
            </>
          )}

          {section === 'backup' && (
            <>
            {/* Backup & Restore */}
            <Card>
              <CardHeader
                title="Backup & Restore"
                subtitle="Export all data to JSON or restore from a backup"
              />
              <CardContent>
                <div className="space-y-4">
                  <AutomaticBackupsPanel />

                  {/* Export Section */}
                  <div className="pt-4 border-t">
                    <h3 className="text-sm font-medium text-slate-700 mb-2">Export Data</h3>
                    <p className="text-sm text-slate-600 mb-3">
                      Download a complete backup of everything in Hive: every table and every field, and your settings.
                      Your Claude API key, Jira API token and Google Calendar address are never included.
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
                  <div className="pt-4 border-t">
                    <h3 className="text-sm font-medium text-slate-700 mb-2">Restore Data</h3>
                    <p className="text-sm text-slate-600 mb-3">
                      Import a previously exported backup file. Records already in Hive are kept as they are; settings are
                      taken from the backup, except the keys and addresses above, which stay as they are.
                    </p>
                    <label className="flex items-center gap-2 px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors cursor-pointer w-fit">
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
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center gap-2">
                      <XCircle className="w-4 h-4 flex-shrink-0" />
                      {backupError}
                    </div>
                  )}

                  {/* Restore Result */}
                  {restoreResult && (
                    <div className="space-y-4 pt-4 border-t">
                      <h3 className="text-sm font-medium text-slate-700">Restore Results</h3>

                      {restoreResult.success ? (
                        <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
                          <div className="flex items-center gap-2 text-green-700 mb-2">
                            <CheckCircle className="w-5 h-5" />
                            <span className="font-medium">Restore completed successfully!</span>
                          </div>
                        </div>
                      ) : (
                        <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                          <div className="flex items-center gap-2 text-yellow-700 mb-2">
                            <AlertCircle className="w-5 h-5" />
                            <span className="font-medium">Restore completed with some issues</span>
                          </div>
                        </div>
                      )}

                      {Object.keys(restoreResult.restored ?? {}).length > 0 || restoreResult.settingsRestored ? (
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                          {Object.entries(restoreResult.restored ?? {}).map(([table, count]) => (
                            <div key={table} className="p-3 bg-slate-50 rounded-lg text-center">
                              <div className="text-xl font-bold text-slate-900">{count}</div>
                              <div className="text-xs text-slate-500">{tableLabel(table)}</div>
                            </div>
                          ))}
                          {restoreResult.settingsRestored && (
                            <div className="p-3 bg-slate-50 rounded-lg text-center">
                              <div className="text-xl font-bold text-slate-900">✓</div>
                              <div className="text-xs text-slate-500">Settings</div>
                            </div>
                          )}
                        </div>
                      ) : (
                        <p className="text-sm text-slate-600">Nothing new to restore: everything in the backup is already in Hive.</p>
                      )}

                      {restoreResult.warnings.length > 0 && (
                        <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                          <h4 className="text-sm font-medium text-yellow-800 mb-2">Warnings ({restoreResult.warnings.length})</h4>
                          <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 max-h-24 overflow-y-auto">
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
                        <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                          <h4 className="text-sm font-medium text-red-800 mb-2">Errors ({restoreResult.errors.length})</h4>
                          <ul className="list-disc list-inside space-y-1 text-sm text-red-700 max-h-24 overflow-y-auto">
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
            </>
          )}
        </div>
      </div>

    </div>
  )
}
