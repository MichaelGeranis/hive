import axios from 'axios'
import type {
  DirectReport,
  CreateDirectReportDto,
  PerformanceReview,
  OneOnOneMeeting,
  MeetingNote,
  CreateMeetingNoteDto,
  UpdateMeetingNoteDto,
  Project,
  TeamTask,
  DashboardOverview,
  ReviewsOverview,
  OneOnOnesOverview,
  TasksOverview,
  OneOnOneFrequency,
  ActionItemsSummary,
  TasksByAssignee,
  TeamVelocity,
  EstimationAccuracy,
  AppSettings,
  UpdateAppSettings,
  Leave,
  CreateLeaveDto,
  UpdateLeaveDto,
  TeamLeaveOverview,
  MonthlyLeaveSummary,
  LeaveBalance,
  JiraImportRequest,
  JiraImportResult,
  JiraImportPreview,
  ManagerNote,
  CreateManagerNoteDto,
  UpdateManagerNoteDto
} from '../types'

const API_BASE_URL = 'http://localhost:5000/api'

// Create axios instance with default config
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Basic ' + btoa('admin:admin123')
  }
})

// Direct Reports
export const directReportsApi = {
  getAll: () => api.get<DirectReport[]>('/directreports').then(r => r.data),
  getById: (id: string) => api.get<DirectReport>(`/directreports/${id}`).then(r => r.data),
  create: (data: CreateDirectReportDto) => api.post<DirectReport>('/directreports', data).then(r => r.data),
  update: (id: string, data: Partial<CreateDirectReportDto>) => api.put<DirectReport>(`/directreports/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/directreports/${id}`)
}

// Performance Reviews
export const reviewsApi = {
  getAll: () => api.get<PerformanceReview[]>('/performancereviews').then(r => r.data),
  getById: (id: string) => api.get<PerformanceReview>(`/performancereviews/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<PerformanceReview[]>(`/performancereviews/direct-report/${directReportId}`).then(r => r.data),
  create: (data: any) => api.post<PerformanceReview>('/performancereviews', data).then(r => r.data),
  update: (id: string, data: any) => api.put<PerformanceReview>(`/performancereviews/${id}/content`, data).then(r => r.data),
  submit: (id: string) => api.post<PerformanceReview>(`/performancereviews/${id}/submit`).then(r => r.data),
  acknowledge: (id: string) => api.post<PerformanceReview>(`/performancereviews/${id}/acknowledge`).then(r => r.data),
  complete: (id: string) => api.post<PerformanceReview>(`/performancereviews/${id}/complete`).then(r => r.data),
  delete: (id: string) => api.delete(`/performancereviews/${id}`)
}

// One-on-One Meetings
export const meetingsApi = {
  getAll: () => api.get<OneOnOneMeeting[]>('/oneononemeetings').then(r => r.data),
  getById: (id: string) => api.get<OneOnOneMeeting>(`/oneononemeetings/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<OneOnOneMeeting[]>(`/oneononemeetings/direct-report/${directReportId}`).then(r => r.data),
  getUpcoming: (days: number = 7) => api.get<OneOnOneMeeting[]>(`/oneononemeetings/upcoming?days=${days}`).then(r => r.data),
  create: (data: any) => api.post<OneOnOneMeeting>('/oneononemeetings', data).then(r => r.data),
  update: (id: string, data: any) => api.put<OneOnOneMeeting>(`/oneononemeetings/${id}`, data).then(r => r.data),
  complete: (id: string) => api.post<OneOnOneMeeting>(`/oneononemeetings/${id}/complete`).then(r => r.data),
  cancel: (id: string) => api.post<OneOnOneMeeting>(`/oneononemeetings/${id}/cancel`).then(r => r.data),
  delete: (id: string) => api.delete(`/oneononemeetings/${id}`)
}

// Meeting Notes
export const meetingNotesApi = {
  getByMeeting: (meetingId: string) =>
    api.get<MeetingNote[]>(`/meetingnotes/by-meeting/${meetingId}`).then(r => r.data),
  create: (data: CreateMeetingNoteDto) =>
    api.post<MeetingNote>('/meetingnotes', data).then(r => r.data),
  update: (id: string, data: UpdateMeetingNoteDto) =>
    api.put<MeetingNote>(`/meetingnotes/${id}`, data).then(r => r.data),
  completeAction: (id: string) =>
    api.post<MeetingNote>(`/meetingnotes/${id}/complete`).then(r => r.data),
  delete: (id: string) => api.delete(`/meetingnotes/${id}`)
}

// Projects
export const projectsApi = {
  getAll: () => api.get<Project[]>('/projects').then(r => r.data),
  getById: (id: string) => api.get<Project>(`/projects/${id}`).then(r => r.data),
  getActive: () => api.get<Project[]>('/projects/active').then(r => r.data),
  create: (data: any) => api.post<Project>('/projects', data).then(r => r.data),
  update: (id: string, data: any) => api.put<Project>(`/projects/${id}`, data).then(r => r.data),
  activate: (id: string) => api.post<Project>(`/projects/${id}/activate`).then(r => r.data),
  complete: (id: string) => api.post<Project>(`/projects/${id}/complete`).then(r => r.data),
  reopen: (id: string) => api.post<Project>(`/projects/${id}/reopen`).then(r => r.data),
  cancel: (id: string) => api.post<Project>(`/projects/${id}/cancel`).then(r => r.data),
  delete: (id: string) => api.delete(`/projects/${id}`)
}

// Tasks
export const tasksApi = {
  getAll: () => api.get<TeamTask[]>('/teamtasks').then(r => r.data),
  getById: (id: string) => api.get<TeamTask>(`/teamtasks/${id}`).then(r => r.data),
  getByAssignee: (assigneeId: string) => api.get<TeamTask[]>(`/teamtasks/assignee/${assigneeId}`).then(r => r.data),
  getByProject: (projectId: string) => api.get<TeamTask[]>(`/teamtasks/project/${projectId}`).then(r => r.data),
  getOverdue: () => api.get<TeamTask[]>('/teamtasks/overdue').then(r => r.data),
  create: (data: any) => api.post<TeamTask>('/teamtasks', data).then(r => r.data),
  update: (id: string, data: any) => api.put<TeamTask>(`/teamtasks/${id}`, data).then(r => r.data),
  assign: (id: string, assigneeId: string | null) =>
    api.post<TeamTask>(`/teamtasks/${id}/assign`, { assigneeId }).then(r => r.data),
  start: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/start`).then(r => r.data),
  complete: (id: string, actualHours?: number) =>
    api.post<TeamTask>(`/teamtasks/${id}/complete`, { actualHours }).then(r => r.data),
  reopen: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/reopen`).then(r => r.data),
  delete: (id: string) => api.delete(`/teamtasks/${id}`),
  bulkDelete: (ids: string[]) => api.post('/teamtasks/bulk-delete', ids)
}

// Reports
export const reportsApi = {
  getDashboard: () => api.get<DashboardOverview>('/reports/dashboard').then(r => r.data),
  getReviewsAnalytics: () => api.get<ReviewsOverview>('/reports/reviews').then(r => r.data),
  getOneOnOnesAnalytics: () => api.get<OneOnOnesOverview>('/reports/one-on-ones').then(r => r.data),
  getTasksAnalytics: () => api.get<TasksOverview>('/reports/tasks').then(r => r.data),
  getDirectReportAnalytics: (id: string) => api.get(`/reports/direct-reports/${id}`).then(r => r.data),
  getOneOnOneFrequency: () => api.get<OneOnOneFrequency[]>('/reports/one-on-one-frequency').then(r => r.data),
  getActionItemsSummary: () => api.get<ActionItemsSummary>('/reports/action-items').then(r => r.data),
  getTasksByAssignee: () => api.get<TasksByAssignee[]>('/reports/tasks-by-assignee').then(r => r.data),
  getTeamVelocity: () => api.get<TeamVelocity>('/reports/team-velocity').then(r => r.data),
  getEstimationAccuracy: () => api.get<EstimationAccuracy>('/reports/estimation-accuracy').then(r => r.data)
}

// Settings
export const settingsApi = {
  get: () => api.get<AppSettings>('/settings').then(r => r.data),
  update: (data: UpdateAppSettings) => api.put<AppSettings>('/settings', data).then(r => r.data)
}

// Leaves
export const leavesApi = {
  getAll: () => api.get<Leave[]>('/leaves').then(r => r.data),
  getById: (id: string) => api.get<Leave>(`/leaves/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<Leave[]>(`/leaves/by-member/${directReportId}`).then(r => r.data),
  getByStatus: (status: string) => api.get<Leave[]>(`/leaves/by-status/${status}`).then(r => r.data),
  getUpcoming: (days: number = 30) => api.get<Leave[]>(`/leaves/upcoming?days=${days}`).then(r => r.data),
  getOverview: () => api.get<TeamLeaveOverview>('/leaves/overview').then(r => r.data),
  getMonthlyTrend: (months: number = 12) =>
    api.get<MonthlyLeaveSummary[]>(`/leaves/monthly-trend?months=${months}`).then(r => r.data),
  getBalances: (year: number) => api.get<LeaveBalance[]>(`/leaves/balances/${year}`).then(r => r.data),
  create: (data: CreateLeaveDto) => api.post<Leave>('/leaves', data).then(r => r.data),
  update: (id: string, data: UpdateLeaveDto) => api.put<Leave>(`/leaves/${id}`, data).then(r => r.data),
  approve: (id: string, approvedBy: string) =>
    api.post<Leave>(`/leaves/${id}/approve`, { approvedBy }).then(r => r.data),
  reject: (id: string, notes?: string) =>
    api.post<Leave>(`/leaves/${id}/reject`, { notes }).then(r => r.data),
  cancel: (id: string) => api.post<Leave>(`/leaves/${id}/cancel`).then(r => r.data),
  delete: (id: string) => api.delete(`/leaves/${id}`)
}

// Jira Import
export const jiraImportApi = {
  preview: (csvContent: string) =>
    api.post<JiraImportPreview>('/jiraimport/preview', { csvContent }).then(r => r.data),
  import: (data: JiraImportRequest) =>
    api.post<JiraImportResult>('/jiraimport/import', data).then(r => r.data)
}

// Manager Notes (TODOs)
export const notesApi = {
  getAll: () => api.get<ManagerNote[]>('/managernotes').then(r => r.data),
  getPending: () => api.get<ManagerNote[]>('/managernotes/pending').then(r => r.data),
  getCompleted: () => api.get<ManagerNote[]>('/managernotes/completed').then(r => r.data),
  getOverdue: () => api.get<ManagerNote[]>('/managernotes/overdue').then(r => r.data),
  getById: (id: string) => api.get<ManagerNote>(`/managernotes/${id}`).then(r => r.data),
  create: (data: CreateManagerNoteDto) => api.post<ManagerNote>('/managernotes', data).then(r => r.data),
  update: (id: string, data: UpdateManagerNoteDto) => api.put<ManagerNote>(`/managernotes/${id}`, data).then(r => r.data),
  toggle: (id: string) => api.post<ManagerNote>(`/managernotes/${id}/toggle`).then(r => r.data),
  delete: (id: string) => api.delete(`/managernotes/${id}`)
}

export default api
