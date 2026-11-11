import type { TaskReadinessVerdict } from '../types'

export const VERDICT_LABELS: Record<TaskReadinessVerdict, string> = {
  Ready: 'Ready',
  NeedsWork: 'Needs work',
  Unclear: 'Unclear'
}

// The ring of the AI bee badge takes the verdict's colour
export const VERDICT_STYLES: Record<TaskReadinessVerdict, string> = {
  Ready: 'border-emerald-300 bg-emerald-50 text-emerald-700',
  NeedsWork: 'border-amber-300 bg-amber-50 text-amber-800',
  Unclear: 'border-slate-300 bg-slate-50 text-slate-600'
}
