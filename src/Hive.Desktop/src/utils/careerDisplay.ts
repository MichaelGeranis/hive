import type { CareerSkillStatus, CareerStage, SkillCategory } from '../types'

/**
 * How a skill level and status look: names and colours. The backend owns what a level means
 * and which status a skill is in; this is only how each is shown.
 */
// The name of each level, 0 to 5, for the level picker
export const SKILL_LEVEL_LABELS = ['Not started', 'Aware', 'Learning', 'Practising', 'Proficient', 'Expert']

// Chip colours by skill level: grey when not started, deepening amber toward expert
export const LEVEL_CHIP_STYLES = [
  'bg-slate-50 border-slate-300 text-slate-500',
  'bg-amber-50 border-amber-200 text-amber-800',
  'bg-amber-100 border-amber-300 text-amber-900',
  'bg-amber-200 border-amber-400 text-amber-900',
  'bg-amber-400 border-amber-500 text-white',
  'bg-amber-600 border-amber-700 text-white'
]


export const SKILL_STATUS_LABELS: Record<CareerSkillStatus, string> = {
  Has: 'Has',
  WorkingOn: 'Working on',
  NotYet: 'Not yet'
}

// Chip colours by status: has it green, working on it amber, not yet grey
export const SKILL_STATUS_STYLES: Record<CareerSkillStatus, string> = {
  Has: 'bg-emerald-50 border-emerald-400 text-emerald-900',
  WorkingOn: 'bg-amber-100 border-amber-500 text-amber-900',
  NotYet: 'bg-slate-50 border-slate-300 text-slate-500'
}

// A skill being added or edited, on a roadmap or a plan
export interface SkillFormState {
  section: string
  title: string
  description: string
  category: SkillCategory
  isOptional: boolean
  fromStage: CareerStage
}

export const EMPTY_SKILL_FORM: SkillFormState = { section: '', title: '', description: '', category: 'Hard', isOptional: false, fromStage: 'Junior' }
