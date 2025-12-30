import { useEffect, useState } from 'react'
import { Save, Sun, Moon, Monitor } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { settingsApi } from '../services/api'
import { useTheme } from '../contexts/ThemeContext'
import type { StoryPointMapping } from '../types'

const DEFAULT_MAPPINGS: StoryPointMapping[] = [
  { points: 1, hours: 4, label: '1 SP = 4 hours' },
  { points: 2, hours: 8, label: '2 SP = 8 hours (1 day)' },
  { points: 3, hours: 12, label: '3 SP = 12 hours (1.5 days)' },
  { points: 5, hours: 24, label: '5 SP = 24 hours (3 days)' },
  { points: 8, hours: 40, label: '8 SP = 40 hours (1 week)' },
]

export default function Settings() {
  const { theme, setTheme } = useTheme()
  const [mappings, setMappings] = useState<StoryPointMapping[]>(DEFAULT_MAPPINGS)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
              <div className="flex gap-3">
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
              </div>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                {theme === 'system'
                  ? 'Automatically matches your system preferences'
                  : `Using ${theme} mode`}
              </p>
            </div>
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
              Use this to standardize task estimation across your team. Configure how many hours each story point represents.
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
    </div>
  )
}
