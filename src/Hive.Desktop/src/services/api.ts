import axios from 'axios'
import type {
  DirectReport,
  CreateDirectReportDto,
  BulkImportDirectReportsDto,
  BulkImportResultDto,
  BackupDto,
  RestoreResultDto,
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
  UpdateManagerNoteDto,
  Sprint,
  CreateSprintDto,
  SprintCapacity,
  CreateSprintCapacityDto,
  LateTasksReport,
  CapacityAnalysis,
  Document,
  CreateDocumentDto,
  UpdateDocumentDto
} from '../types'

const API_BASE_URL = 'http://localhost:5002/api'

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
  delete: (id: string) => api.delete(`/directreports/${id}`),
  bulkImport: (data: BulkImportDirectReportsDto) => api.post<BulkImportResultDto>('/directreports/bulk-import', data).then(r => r.data)
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
  create: (data: any) => api.post<OneOnOneMeeting>('/oneononemeetings', data).then(r => r.data),
  update: (id: string, data: any) => api.put<OneOnOneMeeting>(`/oneononemeetings/${id}`, data).then(r => r.data),
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
  hold: (id: string) => api.post<Project>(`/projects/${id}/hold`).then(r => r.data),
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
  getDashboard: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<DashboardOverview>(`/reports/dashboard${params}`).then(r => r.data);
  },
  getReviewsAnalytics: () => api.get<ReviewsOverview>('/reports/reviews').then(r => r.data),
  getOneOnOnesAnalytics: () => api.get<OneOnOnesOverview>('/reports/one-on-ones').then(r => r.data),
  getTasksAnalytics: () => api.get<TasksOverview>('/reports/tasks').then(r => r.data),
  getDirectReportAnalytics: (id: string) => api.get(`/reports/direct-reports/${id}`).then(r => r.data),
  getOneOnOneFrequency: () => api.get<OneOnOneFrequency[]>('/reports/one-on-one-frequency').then(r => r.data),
  getActionItemsSummary: () => api.get<ActionItemsSummary>('/reports/action-items').then(r => r.data),
  getTasksByAssignee: () => api.get<TasksByAssignee[]>('/reports/tasks-by-assignee').then(r => r.data),
  getTeamVelocity: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<TeamVelocity>(`/reports/team-velocity${params}`).then(r => r.data);
  },
  getEstimationAccuracy: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<EstimationAccuracy>(`/reports/estimation-accuracy${params}`).then(r => r.data);
  },
  getLateTasks: () => api.get<LateTasksReport>('/reports/late-tasks').then(r => r.data),
  getCapacityAnalysis: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<CapacityAnalysis>(`/reports/capacity-analysis${params}`).then(r => r.data);
  }
}

// Sprints
export const sprintsApi = {
  getAll: () => api.get<Sprint[]>('/sprints').then(r => r.data),
  getById: (id: string) => api.get<Sprint>(`/sprints/${id}`).then(r => r.data),
  getByName: (name: string) => api.get<Sprint>(`/sprints/by-name/${encodeURIComponent(name)}`).then(r => r.data),
  getByTeam: (teamName: string) => api.get<Sprint[]>(`/sprints/team/${encodeURIComponent(teamName)}`).then(r => r.data),
  getByYear: (year: number) => api.get<Sprint[]>(`/sprints/year/${year}`).then(r => r.data),
  getByYearQuarter: (year: number, quarter: number) =>
    api.get<Sprint[]>(`/sprints/year/${year}/quarter/${quarter}`).then(r => r.data),
  create: (data: CreateSprintDto) => api.post<Sprint>('/sprints', data).then(r => r.data),
  delete: (id: string) => api.delete(`/sprints/${id}`)
}

// Sprint Capacity
export const sprintCapacityApi = {
  getAll: () => api.get<SprintCapacity[]>('/sprint-capacity').then(r => r.data),
  getById: (id: string) => api.get<SprintCapacity>(`/sprint-capacity/${id}`).then(r => r.data),
  getBySprintId: (sprintId: string) => api.get<SprintCapacity>(`/sprint-capacity/sprint/${sprintId}`).then(r => r.data),
  createOrUpdate: (data: CreateSprintCapacityDto) => api.post<SprintCapacity>('/sprint-capacity', data).then(r => r.data),
  delete: (id: string) => api.delete(`/sprint-capacity/${id}`)
}

// Settings
export const settingsApi = {
  get: () => api.get<AppSettings>('/settings').then(r => r.data),
  update: (data: UpdateAppSettings) => api.put<AppSettings>('/settings', data).then(r => r.data)
}

// Leaves
// Simple tracking for capacity planning - approvals handled externally (e.g., HiBob)
export const leavesApi = {
  getAll: () => api.get<Leave[]>('/leaves').then(r => r.data),
  getById: (id: string) => api.get<Leave>(`/leaves/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<Leave[]>(`/leaves/by-member/${directReportId}`).then(r => r.data),
  getByDateRange: (startDate: string, endDate: string) =>
    api.get<Leave[]>(`/leaves/by-date-range?startDate=${startDate}&endDate=${endDate}`).then(r => r.data),
  getUpcoming: (days: number = 30) => api.get<Leave[]>(`/leaves/upcoming?days=${days}`).then(r => r.data),
  getOverview: () => api.get<TeamLeaveOverview>('/leaves/overview').then(r => r.data),
  getMonthlyTrend: (months: number = 12) =>
    api.get<MonthlyLeaveSummary[]>(`/leaves/monthly-trend?months=${months}`).then(r => r.data),
  getBalances: (year: number) => api.get<LeaveBalance[]>(`/leaves/balances/${year}`).then(r => r.data),
  create: (data: CreateLeaveDto) => api.post<Leave>('/leaves', data).then(r => r.data),
  update: (id: string, data: UpdateLeaveDto) => api.put<Leave>(`/leaves/${id}`, data).then(r => r.data),
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
  getByTag: (tag: string) => api.get<ManagerNote[]>(`/managernotes/by-tag/${encodeURIComponent(tag)}`).then(r => r.data),
  search: (q?: string, tag?: string) => {
    const params = new URLSearchParams()
    if (q) params.append('q', q)
    if (tag) params.append('tag', tag)
    return api.get<ManagerNote[]>(`/managernotes/search?${params.toString()}`).then(r => r.data)
  },
  getTags: () => api.get<string[]>('/managernotes/tags').then(r => r.data),
  getById: (id: string) => api.get<ManagerNote>(`/managernotes/${id}`).then(r => r.data),
  create: (data: CreateManagerNoteDto) => api.post<ManagerNote>('/managernotes', data).then(r => r.data),
  update: (id: string, data: UpdateManagerNoteDto) => api.put<ManagerNote>(`/managernotes/${id}`, data).then(r => r.data),
  toggle: (id: string) => api.post<ManagerNote>(`/managernotes/${id}/toggle`).then(r => r.data),
  delete: (id: string) => api.delete(`/managernotes/${id}`)
}

// Documents
export const documentsApi = {
  getAll: () => api.get<Document[]>('/documents').then(r => r.data),
  getById: (id: string) => api.get<Document>(`/documents/${id}`).then(r => r.data),
  getByTags: (tags: string) => api.get<Document[]>(`/documents/by-tags?tags=${encodeURIComponent(tags)}`).then(r => r.data),
  create: (data: CreateDocumentDto) => api.post<Document>('/documents', data).then(r => r.data),
  update: (id: string, data: UpdateDocumentDto) => api.put<Document>(`/documents/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/documents/${id}`)
}

// Backup & Restore
export const backupApi = {
  export: () => api.get<BackupDto>('/backup/export').then(r => r.data),
  import: (data: BackupDto) => api.post<RestoreResultDto>('/backup/import', data).then(r => r.data)
}

export default api
