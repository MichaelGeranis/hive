// Career plans and skill roadmaps. Everything derived (a skill's status and expected level,
// the ladder reading, strengths and weaknesses, XP and badges) is computed by the backend;
// these types only carry it.

export type SkillCategory = 'Hard' | 'Soft'

// The career ladder. A person's role is at one stage; a skill is expected from one
export type CareerStage = 'Junior' | 'Mid' | 'Senior' | 'Staff'

export const CAREER_STAGES: CareerStage[] = ['Junior', 'Mid', 'Senior', 'Staff']

// How many skills a plan at each stage would copy from a roadmap
export type SkillCountsByStage = Record<CareerStage, number>

// Skill roadmaps

export interface RoadmapSkill {
  id: string
  section: string
  title: string
  description: string
  category: SkillCategory
  isOptional: boolean
  sortOrder: number
  // The stage the skill is introduced at; plans aiming earlier leave it out
  fromStage: CareerStage
}

export interface RoadmapSection {
  name: string
  skills: RoadmapSkill[]
}

export interface SkillRoadmapSummary {
  id: string
  name: string
  description: string
  // The built-in roadmap it was added from; null for one made by hand
  libraryKey: string | null
  sectionCount: number
  skillCount: number
  hardSkillCount: number
  softSkillCount: number
  skillCountsByStage: SkillCountsByStage
  createdAt: string
}

export interface SkillRoadmap extends SkillRoadmapSummary {
  sections: RoadmapSection[]
}

export interface SaveSkillRoadmapDto {
  name: string
  description?: string
}

export interface SaveRoadmapSkillDto {
  section: string
  title: string
  description?: string
  category: SkillCategory
  isOptional: boolean
  fromStage: CareerStage
}

export interface RoadmapLibraryItem {
  key: string
  name: string
  description: string
  sectionCount: number
  skillCount: number
  hardSkillCount: number
  softSkillCount: number
  isAdded: boolean
}

// Career plans: one per direct report, ongoing

// WorkingOn wins over Has; Has means level 3 (Practising) or more
export type CareerSkillStatus = 'NotYet' | 'WorkingOn' | 'Has'

// How their skills read against their own role's stage
export type LadderReading = 'NotAssessed' | 'BelowRole' | 'AtRole' | 'AboveRole'

// Deliberately nothing comparable between people: no scores, no counts of what they have
export interface CareerPlanSummary {
  id: string
  directReportId: string
  directReportName: string
  roadmapName: string
  // The stage of their current role: a fact, not a goal
  currentStage: CareerStage
  workingOnCount: number
  createdAt: string
  lastChangedAt: string
  // Nothing changed for 60 days
  isQuiet: boolean
}

export interface CareerPlanSkill {
  id: string
  section: string
  title: string
  description: string
  category: SkillCategory
  isOptional: boolean
  // The stage the skill is expected from
  fromStage: CareerStage
  // 0 Not started to 5 Expert
  level: number
  levelLabel: string
  status: CareerSkillStatus
  isFocus: boolean
  // What their current stage expects; null when the skill belongs to a later stage
  expectedLevel: number | null
  evidence: string
  lastLevelUpAt: string | null
  updatedAt: string | null
}

export interface CareerPlanSection {
  name: string
  skillCount: number
  hasCount: number
  workingOnCount: number
  isCleared: boolean
  skills: CareerPlanSkill[]
}

export interface StageFit {
  stage: CareerStage
  expectedCount: number
  metCount: number
  metPercent: number
}

export interface CareerLadder {
  currentStage: CareerStage
  // The highest stage whose expectations they meet
  skillsStage: CareerStage | null
  reading: LadderReading
  stages: StageFit[]
  strengths: CareerPlanSkill[]
  weaknesses: CareerPlanSkill[]
}

export interface CareerBadge {
  key: string
  name: string
  description: string
  isEarned: boolean
}

// The game, shown only on the person's own plan
export interface CareerGame {
  xp: number
  playerLevel: number
  playerTitle: string
  // XP at which the current player level started, and at which the next one starts
  currentLevelXp: number
  nextLevelXp: number
  skillsLevelledUpRecently: number
  badges: CareerBadge[]
}

export interface CareerPlan extends CareerPlanSummary {
  notes: string
  skillCount: number
  hasCount: number
  notYetCount: number
  sections: CareerPlanSection[]
  ladder: CareerLadder
  game: CareerGame
}

export interface CreateCareerPlanDto {
  directReportId: string
  roadmapId: string
  currentStage: CareerStage
  notes?: string
}

export interface UpdateCareerPlanDto {
  currentStage: CareerStage
  notes?: string
}

export interface AddCareerPlanSkillDto {
  section: string
  title: string
  description?: string
  category: SkillCategory
  isOptional: boolean
  fromStage: CareerStage
}

// A plan after a skill was rated, with what the rating unlocked
export interface SkillLevelChange {
  plan: CareerPlan
  skillLevelledUp: boolean
  playerLevelledUp: boolean
  newBadges: CareerBadge[]
}

// Someone on the dashboard's "what needs me" lists
export interface CareerAttention {
  directReportId: string
  fullName: string
  planId: string | null
  lastChangedAt: string | null
}

export interface CareerOverview {
  directReportsWithoutPlan: CareerAttention[]
  nothingInFocus: CareerAttention[]
  quiet: CareerAttention[]
}
