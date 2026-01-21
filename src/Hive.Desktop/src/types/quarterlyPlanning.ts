// Quarterly Planning Types

// Enums
export enum QuarterStatus {
  Planning = 0,
  Active = 1,
  Completed = 2
}

export enum InitiativeStatus {
  Planned = 0,
  InProgress = 1,
  OnHold = 2,
  Completed = 3,
  Cancelled = 4
}

export enum InitiativePriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3
}

export enum DependencyType {
  FinishToStart = 0,
  StartToStart = 1,
  FinishToFinish = 2
}

export enum InsightSeverity {
  Info = 0,
  Warning = 1,
  Critical = 2
}

export enum InsightType {
  LeaveConflict = 0,
  DependencyRisk = 1,
  Bottleneck = 2,
  UnassignedWork = 3
}

// Quarter types
export interface Quarter {
  id: string
  year: number
  quarterNumber: number
  name: string
  status: QuarterStatus
  okrReference: string
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
}

// Initiative types
export interface Initiative {
  id: string
  quarterId: string
  name: string
  description: string
  status: InitiativeStatus
  priority: InitiativePriority
  okrObjective: string
  color: string
  projectId?: string
  projectName?: string
  allocationCount: number
  createdAt: string
  updatedAt?: string
}

export interface CreateInitiativeDto {
  quarterId: string
  name: string
  description?: string
  priority?: InitiativePriority
  okrObjective?: string
  color?: string
  projectId?: string
}

export interface UpdateInitiativeDto {
  name: string
  description?: string
  priority: InitiativePriority
  okrObjective?: string
  color?: string
  projectId?: string
}

export interface UpdateInitiativeStatusDto {
  status: InitiativeStatus
}

// Allocation types
export interface Allocation {
  id: string
  initiativeId: string
  initiativeName: string
  initiativeColor: string
  directReportId: string
  directReportName: string
  sprintId: string
  sprintName: string
  createdAt: string
  updatedAt?: string
}

export interface CreateAllocationDto {
  initiativeId: string
  directReportId: string
  sprintId: string
}

// Sprint Goal types
export interface SprintGoal {
  id: string
  quarterId: string
  sprintId: string
  sprintName: string
  goal: string
  notes: string
  createdAt: string
  updatedAt?: string
}

export interface UpsertSprintGoalDto {
  quarterId: string
  sprintId: string
  goal?: string
  notes?: string
}

// Dependency types
export interface InitiativeDependency {
  id: string
  dependentInitiativeId: string
  dependentInitiativeName: string
  dependencyInitiativeId: string
  dependencyInitiativeName: string
  type: DependencyType
  notes: string
  createdAt: string
}

export interface CreateInitiativeDependencyDto {
  dependentInitiativeId: string
  dependencyInitiativeId: string
  type?: DependencyType
  notes?: string
}

// Planning Board types
export interface PlanningBoard {
  quarter: Quarter
  initiatives: Initiative[]
  sprints: Sprint[]
  teamMembers: DirectReport[]
  allocations: Allocation[]
  sprintGoals: SprintGoal[]
  dependencies: InitiativeDependency[]
  leaves: Leave[]
}

// Import from existing types
import type { Sprint, DirectReport, Leave } from './index'

// Team Member Summary
export interface SprintAllocationSummary {
  sprintId: string
  allocationCount: number
  hasLeave: boolean
  leaveDays: number
}

export interface TeamMemberSummary {
  directReportId: string
  name: string
  initiativeCount: number
  sprintAllocations: SprintAllocationSummary[]
}

// Insights types
export interface PlanningInsight {
  type: InsightType
  severity: InsightSeverity
  title: string
  message: string
  relatedInitiativeId?: string
  relatedDirectReportId?: string
  relatedSprintId?: string
  affectedCells: string[]
}

export interface PlanningInsights {
  totalInitiatives: number
  allocatedInitiatives: number
  issueCount: number
  warningCount: number
  criticalCount: number
  insights: PlanningInsight[]
  teamMemberSummaries: TeamMemberSummary[]
}

// Helper functions
export function getQuarterStatusLabel(status: QuarterStatus): string {
  switch (status) {
    case QuarterStatus.Planning: return 'Planning'
    case QuarterStatus.Active: return 'Active'
    case QuarterStatus.Completed: return 'Completed'
    default: return 'Unknown'
  }
}

export function getInitiativeStatusLabel(status: InitiativeStatus): string {
  switch (status) {
    case InitiativeStatus.Planned: return 'Planned'
    case InitiativeStatus.InProgress: return 'In Progress'
    case InitiativeStatus.OnHold: return 'On Hold'
    case InitiativeStatus.Completed: return 'Completed'
    case InitiativeStatus.Cancelled: return 'Cancelled'
    default: return 'Unknown'
  }
}

export function getInitiativePriorityLabel(priority: InitiativePriority): string {
  switch (priority) {
    case InitiativePriority.Low: return 'Low'
    case InitiativePriority.Medium: return 'Medium'
    case InitiativePriority.High: return 'High'
    case InitiativePriority.Critical: return 'Critical'
    default: return 'Unknown'
  }
}

export function getInsightSeverityColor(severity: InsightSeverity): string {
  switch (severity) {
    case InsightSeverity.Info: return 'blue'
    case InsightSeverity.Warning: return 'amber'
    case InsightSeverity.Critical: return 'red'
    default: return 'gray'
  }
}

export function getInsightTypeIcon(type: InsightType): string {
  switch (type) {
    case InsightType.LeaveConflict: return 'Calendar'
    case InsightType.DependencyRisk: return 'GitBranch'
    case InsightType.Bottleneck: return 'Users'
    case InsightType.UnassignedWork: return 'AlertCircle'
    default: return 'Info'
  }
}
