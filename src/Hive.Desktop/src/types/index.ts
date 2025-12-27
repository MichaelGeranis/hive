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
  ratingName: string
  status: ReviewStatus
  statusName: string
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
