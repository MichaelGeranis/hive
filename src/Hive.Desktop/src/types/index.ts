// Direct Reports
export interface DirectReport {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email: string
  jobTitle: string
  department: string
  hireDate: string
  createdAt: string
  updatedAt?: string
}

export interface CreateDirectReportDto {
  firstName: string
  lastName: string
  email: string
  jobTitle: string
  department: string
  hireDate: string
}

// Performance Reviews
export enum PerformanceRating {
  NotRated = 0,
  NeedsImprovement = 1,
  MeetsExpectations = 2,
  ExceedsExpectations = 3,
  Outstanding = 4
}

export enum ReviewStatus {
  Draft = 0,
  Submitted = 1,
  Acknowledged = 2,
  Completed = 3
}

export interface PerformanceReview {
  id: string
  directReportId: string
  directReportName: string
  reviewPeriod: string
  reviewDate: string
  rating: PerformanceRating
  ratingDescription: string
  status: ReviewStatus
  statusDescription: string
  strengths: string
  areasForImprovement: string
  goalsForNextPeriod: string
  managerNotes: string
  employeeSelfAssessment: string
}

// One-on-One Meetings
export enum NoteCategory {
  Discussion = 0,
  ActionItem = 1,
  Feedback = 2,
  CareerDevelopment = 3,
  Blocker = 4,
  Achievement = 5,
  Personal = 6,
  FollowUp = 7,
  Agenda = 8  // Pre-meeting topics for preparation
}

export enum ActionItemStatus {
  Open = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export interface OneOnOneMeeting {
  id: string
  directReportId: string
  directReportName: string
  meetingDate: string
  durationMinutes: number
  location: string
  agenda: string
  noteCount: number
  openActionItemCount: number
  createdAt: string
  updatedAt?: string
}

export interface MeetingNote {
  id: string
  meetingId: string
  meetingDate: string
  directReportName: string
  content: string
  category: NoteCategory
  categoryName: string
  isPrivate: boolean
  actionStatus?: ActionItemStatus
  actionStatusName?: string
  actionDueDate?: string
  actionAssignee?: string
  isOverdue: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateMeetingNoteDto {
  meetingId: string
  content: string
  category: NoteCategory
  isPrivate: boolean
  actionDueDate?: string
  actionAssignee?: string
}

export interface UpdateMeetingNoteDto {
  content: string
  category: NoteCategory
  isPrivate: boolean
  actionDueDate?: string
  actionAssignee?: string
}

// Projects
export enum ProjectStatus {
  Planning = 0,
  Active = 1,
  OnHold = 2,
  Completed = 3,
  Cancelled = 4
}

export interface Project {
  id: string
  name: string
  description: string
  labels: string
  status: ProjectStatus
  statusName: string
  startDate?: string
  targetEndDate?: string
  actualEndDate?: string
  totalTasks: number
  completedTasks: number
  openTasks: number
}

// Tasks
export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3
}

export enum TaskStatus {
  Backlog = 0,
  Todo = 1,
  Blocked = 2,
  InProgress = 3,
  InReview = 4,
  InTest = 5,
  POAcceptance = 6,
  ReadyToRelease = 7,
  Done = 8,
  Cancelled = 9
}

export enum TaskType {
  Task = 0,
  Epic = 1,
  Story = 2,
  SubTask = 3,
  Bug = 4,
  Spike = 5,
  Support = 6
}

export interface TeamTask {
  id: string
  title: string
  description: string
  type: TaskType
  typeName: string
  priority: TaskPriority
  priorityName: string
  status: TaskStatus
  statusName: string
  assigneeId?: string
  assigneeName?: string
  projectId?: string
  projectName?: string
  dueDate?: string
  estimatedHours?: number
  storyPoints?: number
  actualHours?: number
  tags: string
  labels: string
  sprint: string
  timeSpentMinutes?: number
  isOverdue: boolean
}

// Dashboard & Reports
export interface DashboardOverview {
  team: TeamOverview
  reviews: ReviewsOverview
  oneOnOnes: OneOnOnesOverview
  tasks: TasksOverview
  generatedAt: string
}

export interface TeamOverview {
  totalDirectReports: number
  directReports: DirectReportSummary[]
}

export interface DirectReportSummary {
  id: string
  fullName: string
  jobTitle: string
  department: string
  hireDate: string
  tenureMonths: number
}

export interface ReviewsOverview {
  totalReviews: number
  draftReviews: number
  submittedReviews: number
  acknowledgedReviews: number
  completedReviews: number
  completionRate: number
  ratingDistribution: RatingDistribution[]
  reviewsByPeriod: ReviewByPeriod[]
}

export interface RatingDistribution {
  rating: PerformanceRating
  ratingName: string
  count: number
  percentage: number
}

export interface ReviewByPeriod {
  period: string
  totalReviews: number
  completedReviews: number
  averageRating: number
}

export interface OneOnOnesOverview {
  totalMeetings: number
  completedMeetings: number
  scheduledMeetings: number
  cancelledMeetings: number
  rescheduledMeetings: number
  completionRate: number
  totalMeetingMinutes: number
  averageMeetingDuration: number
  frequencyByDirectReport: OneOnOneFrequency[]
  actionItemsSummary: ActionItemsSummary[]
}

export interface OneOnOneFrequency {
  directReportId: string
  directReportName: string
  totalMeetings: number
  completedMeetings: number
  lastMeetingDate?: string
  nextScheduledDate?: string
  daysSinceLastMeeting: number
  averageFrequencyDays: number
  frequencyStatus: string
}

export interface ActionItemsSummary {
  totalActionItems: number
  openItems: number
  inProgressItems: number
  completedItems: number
  cancelledItems: number
  overdueItems: number
  completionRate: number
}

export interface TasksOverview {
  projects: ProjectsSummary
  tasks: TasksSummary
  tasksByAssignee: TasksByAssignee[]
  tasksByType: TasksByType[]
  tasksByPriority: TasksByPriority[]
  productivity: ProductivityMetrics
}

export interface ProjectsSummary {
  totalProjects: number
  planningProjects: number
  activeProjects: number
  onHoldProjects: number
  completedProjects: number
  cancelledProjects: number
  completionRate: number
}

export interface TasksSummary {
  totalTasks: number
  backlogTasks: number
  todoTasks: number
  inProgressTasks: number
  inReviewTasks: number
  doneTasks: number
  cancelledTasks: number
  overdueTasks: number
  unassignedTasks: number
  completionRate: number
}

export interface TasksByAssignee {
  assigneeId?: string
  assigneeName: string
  totalTasks: number
  completedTasks: number
  inProgressTasks: number
  overdueTasks: number
  completionRate: number
}

export interface TasksByType {
  type: TaskType
  typeName: string
  totalTasks: number
  completedTasks: number
  completionRate: number
}

export interface TasksByPriority {
  priority: TaskPriority
  priorityName: string
  totalTasks: number
  completedTasks: number
  overdueTasks: number
  completionRate: number
}

export interface ProductivityMetrics {
  totalEstimatedHours: number
  totalActualHours: number
  estimationAccuracy: number
}

// Team Velocity
export interface TeamVelocity {
  sprints: SprintVelocity[]
  averageVelocity: number
  totalStoryPointsCompleted: number
  completionTrend: number
}

export interface SprintVelocity {
  sprintName: string
  startDate: string
  endDate: string
  storyPointsCompleted: number
  tasksCompleted: number
  totalTimeSpentMinutes: number
  totalEstimatedHours: number
}

// Estimation Accuracy
export interface EstimationAccuracy {
  sprints: SprintAccuracy[]
  byAssignee: AssigneeAccuracy[]
  byProject: ProjectAccuracy[]
  overallAccuracyPercentage: number
  totalEstimatedHours: number
  totalActualHours: number
  totalVarianceHours: number
}

export interface SprintAccuracy {
  sprintName: string
  tasksCompleted: number
  storyPointsCompleted: number
  estimatedHours: number
  actualHours: number
  varianceHours: number
  accuracyPercentage: number
}

export interface AssigneeAccuracy {
  assigneeId?: string
  assigneeName: string
  tasksCompleted: number
  estimatedHours: number
  actualHours: number
  varianceHours: number
  accuracyPercentage: number
}

export interface ProjectAccuracy {
  projectId?: string
  projectName: string
  tasksCompleted: number
  estimatedHours: number
  actualHours: number
  varianceHours: number
  accuracyPercentage: number
}

// Leaves
// Simple tracking for capacity planning - approvals handled externally (e.g., HiBob)
export enum LeaveType {
  Vacation = 0,
  Sick = 1,
  Other = 2
}

export interface Leave {
  id: string
  directReportId: string
  directReportName: string
  type: string
  startDate: string
  endDate: string
  daysCount: number
  businessDaysCount: number
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateLeaveDto {
  directReportId: string
  type: string
  startDate: string
  endDate: string
  notes?: string
}

export interface UpdateLeaveDto {
  type: string
  startDate: string
  endDate: string
  notes?: string
}

export interface LeaveTypeSummary {
  type: string
  count: number
  totalDays: number
  totalBusinessDays: number
}

export interface MonthlyLeaveSummary {
  year: number
  month: number
  monthName: string
  totalLeaves: number
  totalDays: number
  totalBusinessDays: number
  byType: LeaveTypeSummary[]
}

export interface TeamLeaveOverview {
  totalLeaveRecords: number
  teamMembersOnLeaveToday: number
  teamMembersOnLeaveThisWeek: number
  upcomingLeaves: Leave[]
  currentLeaves: Leave[]
  monthlyTrend: MonthlyLeaveSummary[]
}

export interface LeaveBalance {
  directReportId: string
  directReportName: string
  year: number
  vacationUsed: number
  sickLeaveUsed: number
  otherUsed: number
  totalUsed: number
}

// Settings
export interface StoryPointMapping {
  points: number
  hours: number
  label: string
}

export interface AppSettings {
  id: string
  storyPointMappings: StoryPointMapping[]
  createdAt: string
  updatedAt?: string
}

export interface UpdateAppSettings {
  storyPointMappings: StoryPointMapping[]
}

// Jira Import
export interface JiraImportRequest {
  csvContent: string
  updateExisting: boolean
  matchField: 'IssueKey' | 'Title'
}

export interface JiraImportResult {
  totalRows: number
  successCount: number
  skippedCount: number
  errorCount: number
  errors: string[]
  warnings: string[]
  importedTasks: JiraImportedTask[]
}

export interface JiraImportedTask {
  taskId?: string
  issueKey: string
  summary: string
  isNew: boolean
  isUpdated: boolean
}

export interface JiraImportPreview {
  totalRows: number
  validRows: number
  invalidRows: number
  detectedColumns: string[]
  mappingWarnings: string[]
  sampleRows: JiraImportPreviewRow[]
}

export interface JiraImportPreviewRow {
  rowNumber: number
  issueKey: string
  summary: string
  issueType: string
  status: string
  priority: string
  assignee: string
  storyPoints?: string
  isValid: boolean
  validationErrors: string[]
}

// Manager Notes (TODOs)
export enum NotePriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Urgent = 3
}

export interface ManagerNote {
  id: string
  title: string
  content: string
  tags: string
  tagsList: string[]
  priority: NotePriority
  priorityName: string
  isCompleted: boolean
  dueDate?: string
  isOverdue: boolean
  createdAt: string
  updatedAt?: string
  completedAt?: string
}

export interface CreateManagerNoteDto {
  title: string
  content: string
  tags?: string
  priority: NotePriority
  dueDate?: string
}

export interface UpdateManagerNoteDto {
  title: string
  content: string
  tags?: string
  priority: NotePriority
  dueDate?: string
}

// Sprints
export interface Sprint {
  id: string
  name: string
  teamName: string
  quarter: number
  year: number
  sprintNumber: number
  createdAt: string
  updatedAt?: string
}

export interface CreateSprintDto {
  name: string
}

// Sprint Capacity
export interface SprintCapacity {
  id: string
  sprintId: string
  sprintName: string
  totalCapacityPoints: number
  availableMembers: number
  createdAt: string
  updatedAt?: string
}

export interface CreateSprintCapacityDto {
  sprintId: string
  totalCapacityPoints: number
  availableMembers: number
}

export interface UpdateSprintCapacityDto {
  totalCapacityPoints: number
  availableMembers: number
}

// Late Tasks Report
export interface LateTasksReport {
  totalLateTasks: number
  averageDelayDays: number
  lateTaskPercentage: number
  lateTasks: LateTask[]
  bySprintBreakdown: LateTasksBySprint[]
  byAssigneeBreakdown: LateTasksByAssignee[]
}

export interface LateTask {
  taskId: string
  taskTitle: string
  issueKey: string
  assigneeName: string
  sprintName: string
  dueDate: string
  completedAt: string
  delayDays: number
  storyPoints?: number
}

export interface LateTasksBySprint {
  sprintName: string
  lateTaskCount: number
  totalCompletedTasks: number
  lateTaskPercentage: number
  averageDelayDays: number
}

export interface LateTasksByAssignee {
  assigneeId?: string
  assigneeName: string
  lateTaskCount: number
  totalCompletedTasks: number
  lateTaskPercentage: number
  averageDelayDays: number
}

// Capacity Analysis
export interface CapacityAnalysis {
  pastSprints: SprintCapacityAnalysis[]
  currentSprint?: SprintCapacityAnalysis
  futureSprints: SprintCapacityAnalysis[]
  averageUtilization: number
  totalCommittedPoints: number
  totalCompletedPoints: number
}

export interface SprintCapacityAnalysis {
  sprintId: string
  sprintName: string
  quarter: number
  year: number
  sprintNumber: number
  committedPoints: number
  completedPoints: number
  utilizationPercentage: number
  status: 'Past' | 'Current' | 'Future'
}
