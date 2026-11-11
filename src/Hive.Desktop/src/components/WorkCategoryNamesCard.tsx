import { useState } from 'react'
import { Layers, Plus, Save, X } from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import { settingsApi } from '../services/api'
import { apiErrorMessage } from '../utils/apiErrors'
import type { WorkCategoryKey } from '../types'

const CATEGORIES: { key: WorkCategoryKey; label: string; example: string }[] = [
  { key: 'TechMaintenance', label: 'Tech Maintenance', example: 'e.g., KTLO, Upgrades' },
  { key: 'TechRoadmap', label: 'Tech Roadmap', example: 'e.g., Platform, Migration' },
  { key: 'TechDebt', label: 'Tech Debt', example: 'e.g., Refactoring, Cleanup' },
  { key: 'SystemImprovements', label: 'System Improvements', example: 'e.g., Performance, Observability' }
]

/**
 * The epic names that make a task Tech Maintenance, Tech Roadmap, Tech Debt or System
 * Improvements on Work mix; every other epic is Productisation. The server checks the names.
 */
export function WorkCategoryNamesCard({ initial }: { initial: Record<WorkCategoryKey, string[]> }) {
  const [names, setNames] = useState(initial)
  const [inputs, setInputs] = useState<Record<WorkCategoryKey, string>>({
    TechMaintenance: '', TechRoadmap: '', TechDebt: '', SystemImprovements: ''
  })
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const add = (key: WorkCategoryKey) => {
    const name = inputs[key].trim()
    if (!name || names[key].some(existing => existing.toLowerCase() === name.toLowerCase())) return
    setNames({ ...names, [key]: [...names[key], name] })
    setInputs({ ...inputs, [key]: '' })
  }

  const remove = (key: WorkCategoryKey, name: string) =>
    setNames({ ...names, [key]: names[key].filter(existing => existing !== name) })

  const save = async () => {
    try {
      setSaving(true)
      setError(null)
      const settings = await settingsApi.update({ workCategoryEpicNames: names })
      setNames(settings.workCategoryEpicNames)
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      console.error('Failed to save the work category names', err)
      setError(apiErrorMessage(err) ?? 'Failed to save the work category names. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader
        title="Work Categories"
        subtitle="Which epics count as which kind of work on Work mix"
        action={<Layers className="w-5 h-5 text-purple-500" />}
      />
      <CardContent>
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            A task takes the category of its epic when the epic&apos;s name contains one of these names, ignoring
            case, spaces and punctuation. Every other task is Productisation.
          </p>

          {CATEGORIES.map(({ key, label, example }) => (
            <div key={key} className="space-y-2 border-t pt-3 first-of-type:border-t-0 first-of-type:pt-0">
              <h3 className="text-sm font-medium text-slate-700">{label}</h3>
              <div className="flex flex-wrap gap-2">
                {names[key].length === 0 && <span className="text-xs text-slate-400">No names: no epic counts as {label}</span>}
                {names[key].map(name => (
                  <span key={name} className="inline-flex items-center gap-1 rounded-full bg-purple-100 px-3 py-1 text-sm text-purple-700">
                    {name}
                    <button
                      type="button"
                      onClick={() => remove(key, name)}
                      aria-label={`Remove ${name} from ${label}`}
                      className="ml-1 rounded-full p-0.5 hover:bg-purple-200"
                      disabled={saving}
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </span>
                ))}
              </div>
              <div className="flex gap-2">
                <input
                  type="text"
                  aria-label={`Add an epic name to ${label}`}
                  value={inputs[key]}
                  onChange={e => setInputs({ ...inputs, [key]: e.target.value })}
                  onKeyDown={e => e.key === 'Enter' && add(key)}
                  placeholder={example}
                  className="flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 focus:border-purple-500 focus:ring-2 focus:ring-purple-500"
                  disabled={saving}
                />
                <button
                  type="button"
                  onClick={() => add(key)}
                  disabled={saving || !inputs[key].trim()}
                  className="flex items-center gap-2 rounded-lg bg-purple-500 px-4 py-2 text-white transition-colors hover:bg-purple-600 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <Plus className="h-4 w-4" />
                  Add
                </button>
              </div>
            </div>
          ))}

          {error && <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>}

          <div className="flex gap-3 border-t pt-4">
            <button
              type="button"
              onClick={save}
              disabled={saving}
              className="flex items-center gap-2 rounded-lg bg-purple-500 px-4 py-2 text-white transition-colors hover:bg-purple-600 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Save className="h-5 w-5" />
              {saving ? 'Saving...' : saved ? 'Saved!' : 'Save Work Categories'}
            </button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
