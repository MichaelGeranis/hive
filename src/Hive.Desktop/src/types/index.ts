// Pagination
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

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
  isDirect: boolean
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
  isDirect: boolean
}

// Bulk Import Direct Reports
export interface BulkImportDirectReportsDto {
  csvContent: string
  skipDuplicates: boolean
}

export interface BulkImportResultDto {
  totalRows: number
  successCount: number
  skippedCount: number
  errorCount: number
  results: BulkImportRowResult[]
  errors: string[]
}

export interface BulkImportRowResult {
  rowNumber: number
  email: string
  status: 'Created' | 'Skipped' | 'Error'
  message?: string
  directReport?: DirectReport
}

// Performance Reviews
export enum PerformanceRating {
  NotRated = 0,
  NeedsImprovement = 1,
  MeetsExpectations = 2,
  ExceedsExpectations = 3,
  Outstanding = 4
}

export interface PerformanceReview {
  id: string
  directReportId: string
  directReportName: string
  reviewPeriod: string
  reviewDate: string
  rating: PerformanceRating
  ratingDescription: string
  strengths: string
  areasForImprovement: string
  managerNotes: string
  createdAt: string
  updatedAt?: string
}

// One-on-One Meetings
export enum NoteCategory {
  Discussion = 0,
  ActionItem = 1,
  Feedback = 2,
  Achievement = 3
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
  actionDueDate?: string
  actionAssignee?: string
}

export interface UpdateMeetingNoteDto {
  content: string
  category: NoteCategory
  actionDueDate?: string
  actionAssignee?: string
}

// Projects
export interface Project {
  id: string
  name: string
  description: string
  labels: string
  url: string
  totalTasks: number
  completedTasks: number
  openTasks: number
  parentCount: number
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
  matchedProjectNames: string[]
  parentId?: string
  parentName?: string
  dueDate?: string
  estimatedHours?: number
  storyPoints?: number
  actualHours?: number
  tags: string
  labels: string
  components?: string
  sprint: string
  timeSpentMinutes?: number
  previousSprintsStoryPoints?: number | null
  newSprintsStoryPoints?: number | null
  overriddenFields: string
  isOverdue: boolean
  isParentTask: boolean
}

export interface TaskPagedResult extends PagedResult<TeamTask> {
  totalStoryPoints: number
  totalEstimatedHours: number
  totalTimeSpentMinutes: number
}

export interface OverrideTeamTaskFieldsDto {
  assigneeId?: string | null
  hasAssigneeOverride: boolean
  storyPoints?: number | null
  hasEstimationOverride: boolean
  timeSpentMinutes?: number | null
  hasTimeSpentOverride: boolean
  previousSprintsStoryPoints?: number | null
  hasPreviousSprintsStoryPointsOverride: boolean
  sprint?: string | null
  hasSprintOverride: boolean
}

export interface ClearTeamTaskOverridesDto {
  fields: string[]
}

export interface TaskSummaryDto {
  totalTasks: number
  backlogTasks: number
  todoTasks: number
  blockedTasks: number
  inProgressTasks: number
  inReviewTasks: number
  inTestTasks: number
  poAcceptanceTasks: number
  readyToReleaseTasks: number
  doneTasks: number
  cancelledTasks: number
  overdueTasks: number
  unassignedTasks: number
  allLabels: string[]
  allSprints: string[]
}

// Dashboard & Reports
export interface DashboardOverview {
  team: TeamOverview
  reviews: ReviewsOverview
  oneOnOnes: OneOnOnesOverview
  tasks: TasksOverview
  insights: DashboardInsights
  generatedAt: string
}

export interface DashboardInsights {
  workloadWarnings: WorkloadWarning[]
  knowledgeSilos: KnowledgeSilo[]
  unengagedMembers: UnengagedMember[]
  unmatchedTaskCount: number
}

export interface WorkloadWarning {
  assigneeId?: string
  assigneeName: string
  inProgressTasks: number
  blockedTasks: number
  inReviewTasks: number
  issues: string[]
}

export interface KnowledgeSilo {
  projectId: string
  projectName: string
  memberCount: number
  memberNames: string[]
}

export interface UnengagedMember {
  directReportId: string
  fullName: string
  isOnLeave: boolean
}

export interface TeamOverview {
  totalReports: number
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
  tasksByTypeSP: TasksByTypeSP[]
  tasksByTypeHours: TasksByTypeHours[]
  tasksByPriority: TasksByPriority[]
  tasksByLabel: TasksByLabel[]
  tasksByComponent: TasksByComponent[]
  supportDistribution: SupportDistribution
  productivity: ProductivityMetrics
}

export interface TasksByTypeSP {
  type: TaskType
  typeName: string
  totalStoryPoints: number
  taskCount: number
}

export interface TasksByTypeHours {
  type: TaskType
  typeName: string
  totalHours: number
  taskCount: number
}

export interface TasksByLabel {
  label: string
  totalTasks: number
  completedTasks: number
  totalStoryPoints: number
  completedStoryPoints: number
  percentageOfTotal: number
}

export interface TasksByComponent {
  component: string
  totalTasks: number
  completedTasks: number
  totalStoryPoints: number
  completedStoryPoints: number
  totalHours: number
  completedHours: number
  percentageOfTotal: number
  percentageOfTotalHours: number
}

export interface SupportDistribution {
  completedHours: number
  completedTaskCount: number
  allHours: number
  allTaskCount: number
  maintenanceCompletedHours: number
  maintenanceCompletedTaskCount: number
  maintenanceAllHours: number
  maintenanceAllTaskCount: number
  byAssignee: SupportByAssignee[]
}

export interface SupportByAssignee {
  assigneeId?: string
  assigneeName: string
  completedHours: number
  completedTaskCount: number
  allHours: number
  allTaskCount: number
  maintenanceCompletedHours: number
  maintenanceCompletedTaskCount: number
  maintenanceAllHours: number
  maintenanceAllTaskCount: number
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
  blockedTasks: number
  inReviewTasks: number
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
  newStoryPointsCompleted: number
  carriedOverStoryPoints: number
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
  Other = 2,
  PublicHoliday = 3
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

export interface CreatePublicHolidayLeaveDto {
  startDate: string
  endDate: string
  name: string
  notes?: string
}

export interface CreatePublicHolidayResultDto {
  totalCreated: number
  teamMembersAffected: string[]
  skippedMembers: string[]
  createdLeaves: Leave[]
}

// Settings
export interface StoryPointMapping {
  points: number
  hours: number
  label: string
}

export interface TshirtSizeMapping {
  size: string
  sprints: number
  label: string
}

export interface AppSettings {
  id: string
  storyPointMappings: StoryPointMapping[]
  tshirtSizeMappings: TshirtSizeMapping[]
  hasClaudeApiKey: boolean
  sentimentAnalysisDays: number
  sentimentAnalysisEnabled: boolean
  sprintTeamFilter?: string | null
  maxInProgressTasks: number
  maxBlockedTasks: number
  maxInReviewTasks: number
  minProjectMembers: number
  supportLabels: string[]
  maintenanceLabels: string[]
  createdAt: string
  updatedAt?: string
}

export interface UpdateAppSettings {
  storyPointMappings?: StoryPointMapping[]
  tshirtSizeMappings?: TshirtSizeMapping[]
  claudeApiKey?: string
  sentimentAnalysisDays?: number
  sentimentAnalysisEnabled?: boolean
  sprintTeamFilter?: string | null
  clearSprintTeamFilter?: boolean
  maxInProgressTasks?: number
  maxBlockedTasks?: number
  maxInReviewTasks?: number
  minProjectMembers?: number
  supportLabels?: string[]
  maintenanceLabels?: string[]
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
  startDate?: string
  endDate?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateSprintDto {
  name: string
}

export interface UpdateSprintDto {
  startDate?: string
  endDate?: string
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
}

export interface UpdateSprintCapacityDto {
  totalCapacityPoints: number
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
  newCompletedPoints: number
  carriedOverPoints: number
  totalStoryPoints: number
  utilizationPercentage: number
  predictedPoints?: number
  status: 'Past' | 'Current' | 'Future'
}

// Documents
export interface Document {
  id: string
  title: string
  content: string
  url?: string
  tags: string
  createdAt: string
  updatedAt?: string
}

export interface CreateDocumentDto {
  title: string
  content: string
  url?: string
  tags?: string
}

export interface UpdateDocumentDto {
  title: string
  content: string
  url?: string
  tags?: string
}

// Parents (task groupings like Epics)
export interface Parent {
  id: string
  name: string
  labels: string
  totalTasks: number
  completedTasks: number
  openTasks: number
  totalStoryPoints: number
  totalTimeSpentMinutes: number
  timeSpentMinutes?: number
  teamTaskId?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateParentDto {
  name: string
  labels?: string
}

export interface UpdateParentDto {
  name: string
  labels?: string
}

// Backup & Restore
export interface BackupDto {
  version: string
  exportedAt: string
  directReports: any[]
  projects: any[]
  tasks: any[]
  performanceReviews: any[]
  meetings: any[]
  meetingNotes: any[]
  leaves: any[]
  managerNotes: any[]
  sprints: any[]
  sprintCapacities: any[]
  documents: any[]
  settings?: any
}

export interface RestoreResultDto {
  success: boolean
  directReportsRestored: number
  projectsRestored: number
  tasksRestored: number
  performanceReviewsRestored: number
  meetingsRestored: number
  meetingNotesRestored: number
  leavesRestored: number
  managerNotesRestored: number
  sprintsRestored: number
  sprintCapacitiesRestored: number
  documentsRestored: number
  settingsRestored: boolean
  errors: string[]
  warnings: string[]
}

// Skills & Assessments

// Skill Category Entity (database-backed)
export interface SkillCategoryEntity {
  id: string
  name: string
  description: string
  sortOrder: number
  isActive: boolean
  skillCount: number
  createdAt: string
  updatedAt?: string
}

export interface CreateSkillCategoryDto {
  name: string
  description: string
  sortOrder: number
}

export interface UpdateSkillCategoryDto {
  name: string
  description: string
  sortOrder: number
}

// Legacy enum kept for backward compatibility with assessments
export enum SkillCategory {
  Technical = 0,
  SoftSkills = 1,
  Leadership = 2,
  DomainKnowledge = 3,
  Tools = 4
}

export enum ProficiencyLevel {
  None = 0,
  Novice = 1,
  Beginner = 2,
  Intermediate = 3,
  Advanced = 4,
  Expert = 5
}

export interface Skill {
  id: string
  name: string
  description: string
  categoryId: string
  categoryName: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface SkillAssessment {
  id: string
  directReportId: string
  directReportName: string
  skillId: string
  skillName: string
  skillCategoryId: string
  skillCategoryName: string
  level: ProficiencyLevel
  levelName: string
  targetLevel?: ProficiencyLevel
  targetLevelName?: string
  skillGap?: number
  meetsTarget: boolean
  notes: string
  updatedAt?: string
}

export interface DirectReportSkills {
  directReportId: string
  directReportName: string
  assessments: SkillAssessment[]
}

export interface SkillMatrix {
  skills: Skill[]
  directReports: DirectReportSkills[]
}

export interface CreateSkillDto {
  name: string
  description: string
  categoryId: string
}

export interface UpdateSkillDto {
  name: string
  description: string
  categoryId: string
}

export interface CreateSkillAssessmentDto {
  directReportId: string
  skillId: string
  level: ProficiencyLevel
  targetLevel?: ProficiencyLevel
  notes: string
}

export interface UpdateSkillAssessmentDto {
  level: ProficiencyLevel
  targetLevel?: ProficiencyLevel
  notes: string
}

export interface SkillsSummaryDto {
  totalSkills: number
  activeSkills: number
  totalAssessments: number
  directReportCount: number
  skillGapCount: number
  skillsByCategory: SkillCategoryCount[]
  proficiencyDistribution: ProficiencyLevelCount[]
}

export interface SkillCategoryCount {
  name: string
  value: number
}

export interface ProficiencyLevelCount {
  name: string
  value: number
}

// Activity Feed
export enum ActivityType {
  Created = 0,
  Updated = 1,
  StatusChanged = 2,
  Approved = 3,
  Rejected = 4,
  Completed = 5
}

export enum EntityType {
  Review = 0,
  Task = 1,
  Leave = 2
}

export interface Activity {
  id: string
  activityType: string
  activityTypeName: string
  entityType: string
  entityTypeName: string
  entityId: string
  entityName: string
  description: string
  timestamp: string
  createdAt: string
}

// Checklists (Interview & Onboarding)
export enum ChecklistType {
  Interview = 0,
  Onboarding = 1
}

export enum ChecklistItemType {
  Question = 0,
  Topic = 1,
  Task = 2,
  Document = 3,
  Training = 4
}

export enum ChecklistItemStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Skipped = 3,
  NotApplicable = 4
}

export enum ChecklistInstanceStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export interface ChecklistTemplate {
  id: string
  name: string
  description: string
  type: ChecklistType
  typeName: string
  isActive: boolean
  itemCount: number
  createdAt: string
  updatedAt?: string
}

export interface ChecklistTemplateItem {
  id: string
  templateId: string
  sortOrder: number
  content: string
  itemType: ChecklistItemType
  itemTypeName: string
  isRequired: boolean
  helpText?: string
  estimatedMinutes?: number
  createdAt: string
  updatedAt?: string
}

export interface ChecklistTemplateWithItems extends ChecklistTemplate {
  items: ChecklistTemplateItem[]
}

export interface ChecklistInstance {
  id: string
  templateId: string
  templateName: string
  type: ChecklistType
  typeName: string
  title: string
  status: ChecklistInstanceStatus
  statusName: string
  candidateName?: string
  position?: string
  interviewDate?: string
  newHireName?: string
  startDate?: string
  targetCompletionDate?: string
  notes: string
  totalItems: number
  completedItems: number
  progressPercent: number
  createdAt: string
  updatedAt?: string
  completedAt?: string
}

export interface ChecklistInstanceItem {
  id: string
  instanceId: string
  templateItemId: string
  sortOrder: number
  content: string
  itemType: ChecklistItemType
  itemTypeName: string
  isRequired: boolean
  status: ChecklistItemStatus
  statusName: string
  notes: string
  score?: number
  assignee?: string
  dueDate?: string
  isOverdue: boolean
  completedAt?: string
  createdAt: string
  updatedAt?: string
}

export interface ChecklistInstanceWithItems extends ChecklistInstance {
  items: ChecklistInstanceItem[]
}

export interface CreateChecklistTemplateDto {
  name: string
  description: string
  type: ChecklistType
}

export interface UpdateChecklistTemplateDto {
  name: string
  description: string
}

export interface CreateChecklistTemplateItemDto {
  content: string
  itemType: ChecklistItemType
  isRequired: boolean
  helpText?: string
  estimatedMinutes?: number
}

export interface UpdateChecklistTemplateItemDto {
  sortOrder: number
  content: string
  itemType: ChecklistItemType
  isRequired: boolean
  helpText?: string
  estimatedMinutes?: number
}

export interface CreateInterviewInstanceDto {
  templateId: string
  title: string
  candidateName: string
  position: string
  interviewDate: string
}

export interface CreateOnboardingInstanceDto {
  templateId: string
  title: string
  newHireName: string
  startDate: string
  targetCompletionDate?: string
}

export interface CompleteChecklistItemDto {
  notes?: string
  score?: number
}

export interface SkipChecklistItemDto {
  notes?: string
}

export interface UpdateChecklistItemDto {
  notes?: string
  score?: number
  assignee?: string
  dueDate?: string
}

export interface ReorderItemsDto {
  itemIds: string[]
}

// Project Knowledge
export interface ProjectKnowledge {
  id: string
  directReportId: string
  directReportName: string
  projectId: string
  projectName: string
  knowledgeLevel: number
  knowledgeLevelLabel: string
  updatedAt?: string
}

export interface CreateOrUpdateProjectKnowledgeDto {
  directReportId: string
  projectId: string
  knowledgeLevel: number
}

export interface ProjectKnowledgeMatrix {
  projects: KnowledgeMatrixProject[]
  directReports: KnowledgeMatrixMember[]
  scores: ProjectKnowledge[]
}

export interface KnowledgeMatrixProject {
  id: string
  name: string
}

export interface KnowledgeMatrixMember {
  id: string
  name: string
}

export interface KnowledgeProgressionEntry {
  id: string
  directReportId: string
  directReportName: string
  projectId: string
  projectName: string
  oldLevel?: number
  newLevel?: number
  change?: number
  manualPoints?: number
  automaticPoints?: number
  totalPoints?: number
  entryType: string  // "LevelChange", "PointsChange", or "Both"
  timestamp: string
}

// Knowledge Points
export interface KnowledgePoint {
  id: string
  directReportId: string
  directReportName: string
  projectId: string
  projectName: string
  manualPoints: number
  automaticPoints: number
  totalPoints: number
  currentKnowledgeLevel?: number
  suggestLevelIncrease: boolean
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateOrUpdateKnowledgePointDto {
  directReportId: string
  projectId: string
  manualPoints: number
  notes?: string
}

export interface AddKnowledgePointsDto {
  directReportId: string
  projectId: string
  pointsToAdd: number
  notes?: string
}

export interface KnowledgeLevelSuggestion {
  directReportId: string
  directReportName: string
  projectId: string
  projectName: string
  totalPoints: number
  currentLevel?: number
  suggestedLevel: number
}

export interface ProjectKnowledgeMatrixWithPoints extends ProjectKnowledgeMatrix {
  points: KnowledgePoint[]
  suggestions: KnowledgeLevelSuggestion[]
}

// Sentiment Analysis
export interface SentimentStatus {
  isEnabled: boolean
  isConfigured: boolean
  analysisDays: number
}

export interface SentimentScore {
  positive: number
  neutral: number
  negative: number
  overallSentiment: string
}

export interface SentimentTrend {
  year: number
  month: number
  monthName: string
  positiveScore: number
  neutralScore: number
  negativeScore: number
  notesCount: number
}

export interface SentimentAnalysis {
  directReportId: string
  directReportName: string
  score: SentimentScore
  keyThemes: string[]
  trend: SentimentTrend[]
  analyzedAt: string
  notesAnalyzed: number
  daysAnalyzed: number
}

export interface DirectReportSentimentSummary {
  directReportId: string
  directReportName: string
  overallSentiment: string
  positiveScore: number
  notesCount: number
  trendDirection: string
}

export interface TeamSentimentOverview {
  averagePositive: number
  averageNeutral: number
  averageNegative: number
  overallTeamSentiment: string
  byDirectReport: DirectReportSentimentSummary[]
  commonThemes: string[]
  totalNotesAnalyzed: number
  directReportsAnalyzed: number
}

export interface ApiKeyValidationResult {
  valid: boolean
  error?: string
}

// Re-export quarterly planning types
export * from './quarterlyPlanning'
