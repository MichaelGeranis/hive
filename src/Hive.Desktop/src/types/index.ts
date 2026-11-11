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

// One-on-One Meetings
export interface OneOnOneMeeting {
  id: string
  directReportId?: string | null
  directReportName?: string | null
  isUnlinked: boolean
  meetingDate: string
  title: string
  content: string
  tags: string
  tagsList: string[]
  snippet: string
  createdAt: string
  updatedAt?: string
}

export interface CreateBlankMeetingDto {
  meetingDate?: string
  tags?: string
}

export interface MeetingCount {
  directReportId?: string | null
  count: number
}

// Projects: code repositories, each serving one or more products
export interface Project {
  id: string
  name: string
  description: string
  url: string
  products: ProjectProduct[]
}

// A product as listed on a project
export interface ProjectProduct {
  id: string
  name: string
}

export interface CreateProjectDto {
  name: string
  description: string
  url: string
  productIds: string[]
}

// Products: Jira components. Tasks and parents belong to the products their components name
export interface Product {
  id: string
  name: string
  description: string
  // The Jira component id; absent for a product made by hand
  jiraComponentId?: string | null
  // Jira owns the name and description of a product from Jira
  isFromJira: boolean
  totalTasks: number
  // Done or Won't Do; decided by the backend
  completedTasks: number
  openTasks: number
  parentCount: number
  projects: ProductProject[]
  createdAt: string
  updatedAt?: string
}

// A project as listed on a product
export interface ProductProject {
  id: string
  name: string
  url: string
}

export interface CreateProductDto {
  name: string
  description: string
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
  // When Hive saw the status change (a sync, import or edit), not Jira's transition time
  statusChangedAt: string
  assigneeId?: string
  assigneeName?: string
  projectId?: string
  projectName?: string
  parentId?: string
  parentName?: string
  dueDate?: string
  storyPoints?: number
  tags: string
  labels: string
  components?: string
  sprint: string
  previousSprintsStoryPoints?: number | null
  newSprintsStoryPoints?: number | null
  overriddenFields: string
  isOverdue: boolean
  isParentTask: boolean
  // The AI readiness review, when the task has had one: shown as the bee badge
  aiReview?: TaskAiReview | null
}

export interface TaskPagedResult extends PagedResult<TeamTask> {
  totalStoryPoints: number
}

export interface OverrideTeamTaskFieldsDto {
  assigneeId?: string | null
  hasAssigneeOverride: boolean
  storyPoints?: number | null
  hasStoryPointsOverride: boolean
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
  // In-flight tasks whose status has not changed for staleTaskDays days or more
  staleTasks: number
  staleTaskDays: number
  unassignedTasks: number
  allLabels: string[]
  allSprints: string[]
}

// Dashboard & Reports
export interface DashboardOverview {
  team: TeamOverview
  tasks: TasksOverview
  insights: DashboardInsights
  generatedAt: string
}

export interface DashboardInsights {
  workloadWarnings: WorkloadWarning[]
  knowledgeSilos: KnowledgeSilo[]
  unengagedMembers: UnengagedMember[]
  narrowlyEngagedMembers: NarrowlyEngagedMember[]
  unworkedProducts: UnworkedProduct[]
  unmatchedTaskCount: number
  warnings: DashboardWarning[]
}

// What needs the manager's attention now. Independent of any widget's sprint window.
export interface DashboardFocus {
  today: string
  currentSprint?: CurrentSprintFocus | null
  oneOnOnes: OneOnOneCadenceOverview
  todos: TodoFocus
  leave: LeaveFocus
  // Milestones in the coming week, soonest first; empty hides the tile
  tenureMilestones: TenureMilestone[]
}

export interface TenureMilestone {
  directReportId: string
  fullName: string
  hireDate: string
  kind: 'SixMonths' | 'Anniversary'
  // Full years reached; 0 for the six-month mark
  years: number
  date: string
  // 0 on the day itself
  daysUntil: number
}

export type SprintPace = 'NotStarted' | 'Ahead' | 'OnPace' | 'Behind'

export interface CurrentSprintFocus {
  sprintId: string
  name: string
  startDate: string
  endDate: string
  isActive: boolean
  workingDays: number
  workingDaysLeft: number
  elapsedPercent: number
  committedPoints: number
  newPoints: number
  newCompletedPoints: number
  carriedOverPoints: number
  completedPercent: number
  pace: SprintPace
  scopeCreepPoints: number
  totalTasks: number
  doneTasks: number
  byStatus: SprintStatusCount[]
  blockedTasks: FocusTask[]
}

export interface SprintStatusCount {
  status: TaskStatus
  statusName: string
  taskCount: number
  storyPoints: number
}

export interface FocusTask {
  id: string
  title: string
  jiraKey?: string | null
  jiraUrl?: string | null
  assigneeName?: string | null
  storyPoints?: number | null
}

export type OneOnOneCadenceStatus = 'OnTrack' | 'DueSoon' | 'Overdue' | 'NeverMet'

export interface OneOnOneCadenceOverview {
  cadenceDays: number
  needsAttentionCount: number
  people: OneOnOneCadenceEntry[]
}

export interface OneOnOneCadenceEntry {
  directReportId: string
  fullName: string
  lastMeetingDate?: string | null
  daysSinceLastMeeting?: number | null
  daysUntilDue?: number | null
  status: OneOnOneCadenceStatus
  isOnLeave: boolean
}

/** The to-dos: the "hot" bullets of the latest note in the Journal folder. */
export interface TodoFocus {
  hasJournal: boolean
  noteId?: string | null
  noteTitle?: string | null
  items: string[]
}

export interface LeaveFocus {
  lookAheadDays: number
  outToday: LeaveFocusItem[]
  upcoming: LeaveFocusItem[]
}

export interface LeaveFocusItem {
  leaveId: string
  directReportId: string
  fullName: string
  type: string
  startDate: string
  endDate: string
  businessDays: number
}

// The members who have worked on a product, through the components of their tasks
// Who has worked on which product: direct reports only, every task but an epic.
// months is null for all time.
export interface ProductEngagement {
  months: number | null
  membersByProduct: ProductMembership[]
  productsByMember: MemberProducts[]
  knowledgeSilos: KnowledgeSilo[]
  unworkedProducts: UnworkedProduct[]
  unengagedMembers: UnengagedMember[]
  // Worked on at least one product, but fewer than minMemberProducts
  narrowlyEngagedMembers: NarrowlyEngagedMember[]
  minProductMembers: number
  minMemberProducts: number
}

export interface UnworkedProduct {
  productId: string
  productName: string
}

export interface ProductMembership {
  productId: string
  productName: string
  memberCount: number
  memberNames: string[]
  isSilo: boolean
}

export interface MemberProducts {
  directReportId?: string
  memberName: string
  productCount: number
  productIds: string[]
  // Fewer products than the minimum set in Settings
  isNarrow: boolean
}

export type DashboardWarningCategory =
  | 'OneOnOnes'
  | 'OverdueTasks'
  | 'StaleTasks'
  | 'Workload'
  | 'UnengagedMembers'
  | 'NarrowlyEngagedMembers'
  | 'UnworkedProducts'
  | 'KnowledgeSilos'
  | 'ScopeCreep'
  | 'UnmatchedTasks'
  | 'EstimatedSpikes'
  | 'EstimatedSupport'
  | 'UnestimatedTasks'
  | 'MissingAcceptanceCriteria'
  | 'LowUtilization'
  | 'VelocityTrend'
  | 'CareerPlans'

export interface DashboardWarning {
  category: DashboardWarningCategory
  title: string
  detail: string
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
  productId: string
  productName: string
  memberCount: number
  memberNames: string[]
}

export interface NarrowlyEngagedMember {
  directReportId: string
  fullName: string
  productCount: number
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

export interface TasksOverview {
  projects: ProjectsSummary
  tasks: TasksSummary
  tasksByAssignee: TasksByAssignee[]
  tasksByType: TasksByType[]
  tasksByTypeSP: TasksByTypeSP[]
  tasksByPriority: TasksByPriority[]
  tasksByLabel: TasksByLabel[]
  tasksByComponent: TasksByComponent[]
  supportDistribution: SupportDistribution
  // Every work category, in a fixed order, even those with no tasks
  tasksByWorkCategory: WorkCategoryShare[]
}

// The kind of work a task is, from the name of its epic
export interface WorkCategoryShare {
  category: number
  categoryName: string
  taskCount: number
  storyPoints: number
  percentageOfTasks: number
  percentageOfStoryPoints: number
}

export interface TasksByTypeSP {
  type: TaskType
  typeName: string
  totalStoryPoints: number
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
  percentageOfTotal: number
  percentageOfTotalStoryPoints: number
}

export interface SupportDistribution {
  completedStoryPoints: number
  completedTaskCount: number
  allStoryPoints: number
  allTaskCount: number
  maintenanceCompletedStoryPoints: number
  maintenanceCompletedTaskCount: number
  maintenanceAllStoryPoints: number
  maintenanceAllTaskCount: number
  byAssignee: SupportByAssignee[]
}

export interface SupportByAssignee {
  assigneeId?: string
  assigneeName: string
  completedStoryPoints: number
  completedTaskCount: number
  allStoryPoints: number
  allTaskCount: number
  maintenanceCompletedStoryPoints: number
  maintenanceCompletedTaskCount: number
  maintenanceAllStoryPoints: number
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
  backlogTasks: number
  todoTasks: number
  blockedTasks: number
  inProgressTasks: number
  inReviewTasks: number
  inTestTasks: number
  poAcceptanceTasks: number
  readyToReleaseTasks: number
  completedTasks: number
  cancelledTasks: number
  overdueTasks: number
  completionRate: number
  totalStoryPoints: number
  completedStoryPoints: number
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

// Carry-over trend: work each sprint carried in from an earlier one
export interface CarryOverTrend {
  sprints: SprintCarryOver[]
  averageCarriedInPercent: number
}

export interface SprintCarryOver {
  sprintName: string
  totalTasks: number
  totalPoints: number
  carriedInTasks: number
  carriedInPoints: number
  newPoints: number
  carriedInPercent: number
  carriedOutTasks: number
  carriedOutPoints: number
}

// Bug & support inflow: new tickets per sprint, counted in tickets (often not estimated)
export interface BugSupportInflow {
  sprints: SprintInflow[]
  averageBugsPerSprint: number
  averageSupportPerSprint: number
  averageMaintenancePerSprint: number
  inflowPercent: number
  // The labels that mark a task as maintenance, from settings
  maintenanceLabels: string[]
}

export interface SprintInflow {
  sprintName: string
  newTasks: number
  // Jira issue type Bug
  bugTasks: number
  // Jira issue type Support
  supportTasks: number
  // Any other type carrying a Maintenance label
  maintenanceTasks: number
  inflowPercent: number
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
export interface TshirtSizeMapping {
  size: string
  sprints: number
  label: string
}

// Which model reads the 1:1 notes: a local Ollama model, or Claude with an API key
export type SentimentProvider = 'Ollama' | 'Claude'

export interface AppSettings {
  id: string
  tshirtSizeMappings: TshirtSizeMapping[]
  sentimentProvider: SentimentProvider
  hasClaudeApiKey: boolean
  ollamaBaseUrl: string
  ollamaModel: string
  sentimentAnalysisDays: number
  sentimentAnalysisEnabled: boolean
  // The retro summary uses the same model as sentiment, switched on separately
  retroSummaryEnabled: boolean
  retroSummaryCount: number
  // The AI review of upcoming tasks, with the same model
  taskReviewEnabled: boolean
  sprintTeamFilter?: string | null
  maxInProgressTasks: number
  maxBlockedTasks: number
  maxInReviewTasks: number
  minProductMembers: number
  minMemberProducts: number
  oneOnOneCadenceDays: number
  staleTaskDays: number
  maintenanceLabels: string[]
  // The epic names that name each work category, by category; every other epic is Productisation
  workCategoryEpicNames: Record<WorkCategoryKey, string[]>
  jiraBaseUrl?: string | null
  // Append `/${issueKey}` to open an issue in Jira
  jiraBrowseUrl?: string | null
  jiraEmail?: string | null
  hasJiraApiToken: boolean
  jiraBoardId?: number | null
  jiraProjectKey?: string | null
  // The secret iCal address is write-only: the API only says whether one is stored
  hasGoogleCalendarUrl: boolean
  createdAt: string
  updatedAt?: string
}

export interface UpdateAppSettings {
  tshirtSizeMappings?: TshirtSizeMapping[]
  sentimentProvider?: SentimentProvider
  claudeApiKey?: string
  // An empty string goes back to the default
  ollamaBaseUrl?: string
  ollamaModel?: string
  sentimentAnalysisDays?: number
  sentimentAnalysisEnabled?: boolean
  retroSummaryEnabled?: boolean
  retroSummaryCount?: number
  taskReviewEnabled?: boolean
  sprintTeamFilter?: string | null
  clearSprintTeamFilter?: boolean
  maxInProgressTasks?: number
  maxBlockedTasks?: number
  maxInReviewTasks?: number
  minProductMembers?: number
  minMemberProducts?: number
  oneOnOneCadenceDays?: number
  staleTaskDays?: number
  maintenanceLabels?: string[]
  workCategoryEpicNames?: Record<WorkCategoryKey, string[]>
  jiraBaseUrl?: string | null
  // Omit to leave unchanged; an empty string clears
  jiraEmail?: string
  jiraApiToken?: string
  jiraBoardId?: number
  clearJiraBoardId?: boolean
  jiraProjectKey?: string
  // Omit to leave unchanged; an empty string clears
  googleCalendarUrl?: string
}

// Google Calendar - read from the calendar's secret iCal address
export interface GoogleCalendarEvent {
  id: string
  title: string
  location?: string | null
  isAllDay: boolean
  // A timed event has startsAt/endsAt (UTC instants)
  startsAt?: string | null
  endsAt?: string | null
  // An all-day event has startDate/endDate (yyyy-MM-dd, endDate inclusive)
  startDate?: string | null
  endDate?: string | null
}

export interface GoogleCalendarEvents {
  isConfigured: boolean
  // False until the calendar is synced, and again 30 minutes after a sync
  isLoaded: boolean
  syncedAt?: string | null
  events: GoogleCalendarEvent[]
}

// Jira Sync
export interface SprintDateChange {
  sprintId: string
  name: string
  previousStartDate?: string | null
  previousEndDate?: string | null
  startDate: string
  endDate: string
}

export interface JiraSprintSyncResult {
  jiraSprintCount: number
  // Future and active team sprints created in Hive
  created?: string[]
  updated: SprintDateChange[]
  unchangedCount: number
  missingDatesInJira: string[]
  notFoundInJira: string[]
  ambiguousInJira: string[]
  errors: string[]
  syncedAt: string
}

export interface ParentSyncChange {
  parentId: string
  jiraKey: string
  name: string
  previousName?: string | null
}

export interface ProductSyncChange {
  productId: string
  jiraComponentId: string
  name: string
  previousName?: string | null
}

export interface JiraProductSyncResult {
  jiraComponentCount: number
  created: ProductSyncChange[]
  updated: ProductSyncChange[]
  unchangedCount: number
  // Products from Jira whose component is gone, removed from Hive
  removed: ProductSyncChange[]
  errors: string[]
  syncedAt: string
}

export interface JiraParentSyncResult {
  jiraParentCount: number
  created: ParentSyncChange[]
  updated: ParentSyncChange[]
  unchangedCount: number
  // Parents from Jira whose issue is gone, removed from Hive
  removed: ParentSyncChange[]
  errors: string[]
  syncedAt: string
}

export interface TaskSyncChange {
  taskId: string
  jiraKey: string
  title: string
}

export interface JiraTaskSyncResult {
  // Work items in the team's sprints in Jira
  jiraTaskCount: number
  created: TaskSyncChange[]
  updated: TaskSyncChange[]
  unchangedCount: number
  // Tasks no longer in the team's sprints in Jira, removed from Hive
  removed: TaskSyncChange[]
  outOfScopeCount: number
  sprintsCreated: string[]
  errors: string[]
  // The parent sync that runs first
  parents?: JiraParentSyncResult | null
  syncedAt: string
}

// Each part runs on its own: a part that fails carries an error instead of a result
export interface JiraSyncAllResult {
  sprintDates?: JiraSprintSyncResult | null
  sprintDatesError?: string | null
  products?: JiraProductSyncResult | null
  productsError?: string | null
  parents?: JiraParentSyncResult | null
  parentsError?: string | null
  tasks?: JiraTaskSyncResult | null
  tasksError?: string | null
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

// Manager Notes
export interface ManagerNote {
  id: string
  title: string
  content: string
  tags: string
  tagsList: string[]
  folderId?: string | null
  isPinned: boolean
  snippet: string
  createdAt: string
  updatedAt?: string
}

export interface CreateManagerNoteDto {
  title: string
  content: string
  tags?: string
  folderId?: string | null
}

export interface UpdateManagerNoteDto {
  title: string
  content: string
  tags?: string
  folderId?: string | null
}

export interface NoteFolder {
  id: string
  name: string
  parentFolderId?: string | null
  sortOrder: number
  noteCount: number
  isJournal: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateNoteFolderDto {
  name: string
  parentFolderId?: string | null
}

export interface UpdateNoteFolderDto {
  name: string
  parentFolderId?: string | null
  sortOrder: number
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

// Capacity Analysis
export interface CapacityAnalysis {
  pastSprints: SprintCapacityAnalysis[]
  currentSprint?: SprintCapacityAnalysis
  futureSprints: SprintCapacityAnalysis[]
  // The sprint after the current one, whatever sprint window the widget shows
  nextSprint?: NextSprintPrediction | null
  averageUtilization: number
  totalCommittedPoints: number
  totalCompletedPoints: number
}

export interface NextSprintPrediction {
  sprintId?: string | null
  // Null when projected: Hive has no such sprint yet
  sprintName?: string | null
  isProjected: boolean
  startDate: string
  endDate: string
  teamSize: number
  availableMembers: number
  predictedPoints: number
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
  totalTasks: number
  doneTasks: number
  utilizationPercentage: number
  predictedPoints?: number
  status: 'Past' | 'Current' | 'Future'
}

// Parents (task groupings like Epics)
export interface Parent {
  id: string
  name: string
  labels: string
  totalTasks: number
  completedTasks: number
  openTasks: number
  // Has tasks and all of them are done or won't do; decided by the backend
  isDone: boolean
  totalStoryPoints: number
  // Story points of the done and won't do tasks
  completedStoryPoints: number
  teamTaskId?: string
  // The Jira issue this parent represents, e.g. MAR-128; absent for hand-made parents
  jiraKey?: string | null
  // Its Jira issue type: Epic, or any other type that has sub-tasks, e.g. Story
  jiraIssueType?: string | null
  // Its Jira components, comma-separated: the products it belongs to
  components?: string
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

// A task as the create and update endpoints take it
export interface SaveTeamTaskDto {
  title: string
  description?: string
  type?: TaskType
  priority?: TaskPriority
  assigneeId?: string | null
  projectId?: string | null
  parentId?: string | null
  dueDate?: string | null
  storyPoints?: number | null
  tags?: string
  labels?: string
  components?: string | null
  sprint?: string
}

// Backup & Restore
export interface BackupDto {
  version: string
  exportedAt: string
  // Format 2.0: every table by entity name, each row every stored field of one record
  tables?: Record<string, unknown[]>
  // Format 1.0, read on restore only; the client only passes them through
  directReports?: unknown[]
  projects?: unknown[]
  tasks?: unknown[]
  meetings?: unknown[]
  meetingNotes?: unknown[]
  leaves?: unknown[]
  managerNotes?: unknown[]
  sprints?: unknown[]
  sprintCapacities?: unknown[]
}

// The automatic backups of the database file: at startup and every hour
export interface AutomaticBackupStatus {
  // False with the in-memory database, which has no file to back up
  enabled: boolean
  folder?: string | null
  count: number
  // UTC; null before the first backup
  lastBackupAt?: string | null
  intervalHours: number
}

// The work categories whose epic names the manager sets; Productisation is every other epic
export type WorkCategoryKey = 'TechMaintenance' | 'TechRoadmap' | 'TechDebt' | 'SystemImprovements'

export interface RestoreResultDto {
  success: boolean
  directReportsRestored: number
  projectsRestored: number
  tasksRestored: number
  meetingsRestored: number
  meetingNotesRestored: number
  leavesRestored: number
  managerNotesRestored: number
  sprintsRestored: number
  sprintCapacitiesRestored: number
  settingsRestored: boolean
  // Rows added, and rows already there and kept, by table (e.g. TeamTask)
  restored?: Record<string, number>
  skipped?: Record<string, number>
  errors: string[]
  warnings: string[]
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
  Task = 1,
  Leave = 2,
  SkillRoadmap = 22,
  CareerPlan = 23
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

// Task readiness: facts Hive checks, and an AI review of each upcoming task
export type TaskReadinessVerdict = 'Ready' | 'NeedsWork' | 'Unclear'

export interface TaskAiReview {
  verdict: TaskReadinessVerdict
  summary: string
  missing: string[]
  model: string
  analyzedAt: string
  // False when the task changed since it was reviewed
  isUpToDate: boolean
}

export interface TaskReadinessItem {
  taskId: string
  title: string
  typeName: string
  statusName: string
  sprint: string
  jiraKey?: string | null
  // A story or task, which must say what done means
  needsAcceptanceCriteria: boolean
  hasAcceptanceCriteria: boolean
  review?: TaskAiReview | null
}

export interface TaskReadinessOverview {
  isEnabled: boolean
  isConfigured: boolean
  currentSprint?: string | null
  nextSprint?: string | null
  // Not started, in the current or next sprint: current sprint first, then by priority
  tasks: TaskReadinessItem[]
  // Upcoming tasks with no up-to-date review; the card reviews them one at a time
  pendingCount: number
  error?: string | null
}

// Retro summary: the AI reading of the notes in the Retro folder
export interface RetroNote {
  noteId: string
  title: string
  retroDate: string
}

export interface RetroActionItem {
  text: string
  retroDate?: string | null
  // Empty when the retro names nobody
  owner: string
}

export interface RetroSummary {
  isEnabled: boolean
  isConfigured: boolean
  hasRetroFolder: boolean
  // The latest retros, newest first: the ones the summary reads
  retros: RetroNote[]
  // Null until a summary has been made
  summary?: string | null
  warnings: string[]
  actionItems: RetroActionItem[]
  suggestions: string[]
  model?: string | null
  retrosAnalyzed: number
  analyzedAt?: string | null
  isUpToDate: boolean
  // Why the summary could not be updated; the last one is still shown
  error?: string | null
}

// Sentiment Analysis
export interface SentimentStatus {
  isEnabled: boolean
  isConfigured: boolean
  provider: SentimentProvider
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
  // Why the model judged the notes as it did, in a few sentences
  reasoning: string
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
  reasoning: string
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

// Re-export career plan types
export * from './careerPlans'
