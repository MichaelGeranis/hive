// Quarterly planning types. Enums cross the API as numbers; their meanings are in
// utils/planningDisplay.ts.

import type { TshirtSizeMapping } from './index'

export const QuarterStatus = { Planning: 0, Active: 1, Completed: 2 } as const
export const WorkType = { Maintenance: 0, ProductRoadmap: 1, TechRoadmap: 2 } as const
export const InitiativeKind = { Option: 0, Leftover: 1, Bucket: 2 } as const
export const InitiativeStatus = {
  InDiscovery: 0,
  InSolutionDesign: 1,
  ReadyForCommitment: 2,
  Committed: 3,
  Stretch: 4,
  Dropped: 5,
  Delivered: 6
} as const
export const ReadinessIssue = {
  RequirementsPending: 1,
  EstimationPending: 2,
  ReleaseCoordination: 4,
  SolutionDesignPending: 8
} as const
export const InitiativeRole = { Contributor: 0, Lead: 1 } as const
export const ExternalDependencyStatus = { NeedsCoordination: 0, Agreed: 1, AtRisk: 2, Done: 3 } as const

export interface Quarter {
  id: string
  year: number
  quarterNumber: number
  name: string
  status: number
  okrReference: string
  productRoadmapPercent: number
  techRoadmapPercent: number
  maintenancePercent: number
  committedAt?: string | null
  createdAt: string
  updatedAt?: string
}

export interface CreateQuarterDto {
  year: number
  quarterNumber: number
  okrReference?: string
}

export interface UpdateQuarterDto {
  okrReference?: string
  productRoadmapPercent?: number
  techRoadmapPercent?: number
  maintenancePercent?: number
}

export interface Forecast {
  estimate?: number | null
  allocated: number
  coveredPercent?: number | null
  firstSprintId?: string | null
  doneSprintId?: string | null
  doneSprintName?: string | null
  deliveredInQuarter: boolean
}

export interface InitiativeMember {
  id: string
  initiativeId: string
  directReportId: string
  directReportName: string
  role: number
  knowledgeLevel: number
  createdAt: string
}

export interface PersonSuggestion {
  directReportId: string
  name: string
  knowledgeLevel: number
  taskCount: number
  freeEffort: number
  isMember: boolean
}

export interface ProductRef {
  id: string
  name: string
}

export interface PersonRef {
  id: string
  name: string
  isDirect: boolean
}

export interface ExternalDependency {
  id: string
  initiativeId: string
  team: string
  need: string
  status: number
}

export interface SaveExternalDependencyDto {
  team: string
  need?: string
  status: number
}

export interface InitiativeDependency {
  id: string
  dependentInitiativeId: string
  dependentInitiativeName: string
  dependencyInitiativeId: string
  dependencyInitiativeName: string
  type: number
  notes: string
  createdAt: string
}

export interface Initiative {
  id: string
  quarterId: string
  name: string
  description: string
  color: string
  tshirtSize: string | null
  url: string
  workType: number
  kind: number
  status: number
  rank: number
  readiness: number
  decisionNote: string
  earliestSprintId?: string | null
  arrivedOn?: string | null
  dueDate?: string | null
  effort?: number | null
  allocatedEffort: number
  forecast?: Forecast | null
  members: InitiativeMember[]
  products: ProductRef[]
  externalDependencies: ExternalDependency[]
  suggestions: PersonSuggestion[]
  createdAt: string
  updatedAt?: string
}

export interface CreateInitiativeDto {
  quarterId: string
  name: string
  description?: string
  tshirtSize?: string | null
  url?: string
  workType?: number
  kind?: number
  status?: number
  arrivedOn?: string | null
  dueDate?: string | null
}

export interface UpdateInitiativeDto {
  name: string
  description?: string
  color?: string
  tshirtSize?: string | null
  url?: string
  workType?: number
  kind?: number
  arrivedOn?: string | null
  dueDate?: string | null
  readiness: number
  earliestSprintId?: string | null
  productIds?: string[]
}

export interface Allocation {
  id: string
  initiativeId: string
  directReportId: string
  sprintId: string
  percent: number
  effort: number
}

export interface CellShare {
  initiativeId: string
  percent: number
}

export interface Cell {
  directReportId: string
  sprintId: string
  shares: CellShare[]
}

export interface MemberSprint {
  sprintId: string
  availablePercent: number
  leaveDays: number
  supply: number
  allocatedPercent: number
}

export interface RosterMember {
  directReportId: string
  name: string
  isDirect: boolean
  capacityPercent: number
  totalSupply: number
  totalDays: number
  sprints: MemberSprint[]
}

export interface PlanningSprint {
  id: string
  name: string
  sprintNumber: number
  startDate?: string | null
  endDate?: string | null
  workingDays: number
  notes: string
}

export interface MixShares {
  productRoadmap: number
  techRoadmap: number
  maintenance: number
}

export interface Reservation {
  workType: number
  percent: number
  effort: number
}

export interface PlanSprintCapacity {
  sprintId: string
  supply: number
  fullSupply: number
  allocated: number
  mix: MixShares
}

export interface Capacity {
  totalSupply: number
  totalDays: number
  totalAllocated: number
  reservations: Reservation[]
  optionCapacity: number
  sprints: PlanSprintCapacity[]
}

export interface WaterlineRow {
  initiativeId: string
  name: string
  color: string
  kind: number
  status: number
  rank: number
  tshirtSize: string | null
  effort?: number | null
  cumulative: number
  aboveLine: boolean
}

export interface Waterline {
  capacity: number
  rows: WaterlineRow[]
  committedEffort: number
}

export interface Mix {
  target: MixShares
  planned: MixShares
  hasPlan: boolean
  previousQuarter?: MixShares | null
  recentUnplannedPercent?: number | null
  recentTaskCount: number
  /** The team's work so far by story points, from the Dashboard's work categories; null without points. */
  pastWork?: MixShares | null
  pastWorkStoryPoints: number
}

export interface PlanningInsight {
  type: string
  severity: number
  title: string
  message: string
  initiativeId?: string | null
  directReportId?: string | null
  sprintId?: string | null
}

export interface PlanningChecklist {
  optionCount: number
  inDiscoveryCount: number
  unsizedCount: number
  notReadyCount: number
  openDependencyCount: number
  withoutLeadCount: number
  undecidedCount: number
  unallocatedCommittedCount: number
  hasSprints: boolean
  hasRoster: boolean
  mixOnTarget: boolean
  committed: boolean
}

export interface PlanningBoard {
  quarter: Quarter
  sprints: PlanningSprint[]
  roster: RosterMember[]
  initiatives: Initiative[]
  dependencies: InitiativeDependency[]
  allocations: Allocation[]
  capacity: Capacity
  waterline: Waterline
  mix: Mix
  insights: PlanningInsight[]
  checklist: PlanningChecklist
  sizeMappings: TshirtSizeMapping[]
  products: ProductRef[]
  people: PersonRef[]
  knownTeams: string[]
}

export interface DraftAllocation {
  initiativeId: string
  initiativeName: string
  directReportId: string
  directReportName: string
  sprintId: string
  sprintName: string
  percent: number
}

export interface ScenarioOption {
  initiativeId: string
  name: string
  before: Forecast
  after: Forecast
  aboveLineBefore: boolean
  aboveLineAfter: boolean
}

export interface PlanDraft {
  applied: boolean
  allocations: DraftAllocation[]
  options: ScenarioOption[]
}

export interface ScenarioRequest {
  absences: { directReportId: string; sprintIds: string[] }[]
  delays: { initiativeId: string; notBeforeSprintId: string }[]
}

export interface Scenario {
  supplyBefore: number
  supplyAfter: number
  optionCapacityBefore: number
  optionCapacityAfter: number
  options: ScenarioOption[]
}

export interface BaselineOption {
  initiativeId: string
  name: string
  status: number
  tshirtSize: string | null
  estimate?: number | null
  allocated: number
  coveredPercent?: number | null
  doneSprintName?: string | null
  aboveLine: boolean
}

export interface ReviewOption {
  initiativeId: string
  name: string
  color: string
  atCommit?: BaselineOption | null
  statusNow: number
  allocatedNow: number
  coveredNow?: number | null
  doneSprintNow?: string | null
}

export interface PlanReview {
  committedAt?: string | null
  options: ReviewOption[]
  summary: string
}

export interface CalibrationRow {
  size: string
  estimate?: number | null
  deliveredCount: number
  averageActual: number
  ratio?: number | null
}

export interface Calibration {
  sizes: CalibrationRow[]
  deliveredCount: number
}
