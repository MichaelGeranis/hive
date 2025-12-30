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
export enum MeetingStatus {
  Scheduled = 0,
  Completed = 1,
  Cancelled = 2,
  Rescheduled = 3
}

export interface OneOnOneMeeting {
  id: string
  directReportId: string
  directReportName: string
  scheduledDate: string
  durationMinutes: number
  location: string
  agenda: string
  status: MeetingStatus
  statusName: string
  completedAt?: string
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
  InProgress = 2,
  InReview = 3,
  Done = 4,
  Cancelled = 5
}

export enum TaskType {
  Task = 0,
  Bug = 1,
  Feature = 2,
  Improvement = 3,
  Research = 4,
  Documentation = 5
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
  averageTaskCompletionDays: number
  tasksCompletedThisWeek: number
  tasksCompletedThisMonth: number
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
export enum LeaveType {
  PTO = 0,
  Vacation = 1,
  SickLeave = 2,
  PersonalLeave = 3,
  FamilyLeave = 4,
  BereavementLeave = 5,
  JuryDuty = 6,
  PublicHoliday = 7,
  Unpaid = 8,
  Other = 9
}

export enum LeaveStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Cancelled = 3
}

export interface Leave {
  id: string
  directReportId: string
  directReportName: string
  type: string
  status: string
  startDate: string
  endDate: string
  daysCount: number
  businessDaysCount: number
  reason?: string
  notes?: string
  createdAt: string
  updatedAt?: string
  approvedAt?: string
  approvedBy?: string
}

export interface CreateLeaveDto {
  directReportId: string
  type: string
  startDate: string
  endDate: string
  reason?: string
  notes?: string
}

export interface UpdateLeaveDto {
  type: string
  startDate: string
  endDate: string
  reason?: string
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
  totalLeaveRequests: number
  pendingRequests: number
  approvedRequests: number
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
  ptoUsed: number
  vacationUsed: number
  sickLeaveUsed: number
  totalUsed: number
  pendingDays: number
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
