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
  UpdateSprintDto,
  SprintCapacity,
  CreateSprintCapacityDto,
  LateTasksReport,
  CapacityAnalysis,
  Document,
  CreateDocumentDto,
  UpdateDocumentDto,
  Parent,
  CreateParentDto,
  UpdateParentDto,
  Skill,
  SkillCategory,
  SkillAssessment,
  SkillMatrix,
  CreateSkillDto,
  UpdateSkillDto,
  CreateSkillAssessmentDto,
  UpdateSkillAssessmentDto,
  ChecklistTemplate,
  ChecklistTemplateWithItems,
  ChecklistTemplateItem,
  ChecklistInstance,
  ChecklistInstanceWithItems,
  ChecklistInstanceItem,
  ChecklistType,
  CreateChecklistTemplateDto,
  UpdateChecklistTemplateDto,
  CreateChecklistTemplateItemDto,
  UpdateChecklistTemplateItemDto,
  CreateInterviewInstanceDto,
  CreateOnboardingInstanceDto,
  CompleteChecklistItemDto,
  SkipChecklistItemDto,
  UpdateChecklistItemDto,
  ReorderItemsDto,
  Activity,
  PagedResult,
  ProjectKnowledge,
  ProjectKnowledgeMatrix,
  CreateOrUpdateProjectKnowledgeDto
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

// Request interceptor - log outgoing requests
api.interceptors.request.use(
  (config) => {
    const method = config.method?.toUpperCase() || 'GET'
    const url = config.url || ''

    // For mutations, log that we're starting the operation
    if (['POST', 'PUT', 'DELETE'].includes(method)) {
      console.log(`API ${method} ${url} - Starting...`)
    }

    return config
  },
  (error) => {
    console.error('API Request Error:', error.message)
    return Promise.reject(error)
  }
)

// Response interceptor - log responses
api.interceptors.response.use(
  (response) => {
    const method = response.config.method?.toUpperCase() || 'GET'
    const url = response.config.url || ''
    const status = response.status

    // Log successful mutations with more detail
    if (['POST', 'PUT', 'DELETE'].includes(method)) {
      const data = response.data
      let message = `API ${method} ${url} - Success (${status})`

      // Add context for specific operations
      if (url.includes('/jiraimport/import') && data) {
        message += ` - Created: ${data.tasksCreated || 0}, Updated: ${data.tasksUpdated || 0}, Skipped: ${data.tasksSkipped || 0}`
      } else if (url.includes('/jiraimport/preview') && data) {
        message += ` - ${data.totalRows || 0} rows to import`
      } else if (url.includes('/backup/export')) {
        message += ' - Data exported successfully'
      } else if (url.includes('/backup/import') && data) {
        message += ` - Restored: ${Object.entries(data).filter(([k, v]) => k !== 'success' && typeof v === 'number').map(([k, v]) => `${k}: ${v}`).join(', ')}`
      } else if (url.includes('/bulk-import') && data) {
        message += ` - Imported: ${data.successCount || 0}, Failed: ${data.failureCount || 0}`
      } else if (url.includes('/bulk-delete')) {
        message += ` - Deleted ${JSON.parse(response.config.data || '[]').length} items`
      } else if (method === 'DELETE') {
        message += ' - Deleted successfully'
      } else if (method === 'POST' && data?.id) {
        message += ` - Created (ID: ${data.id.substring(0, 8)}...)`
      } else if (method === 'PUT' && data?.id) {
        message += ' - Updated successfully'
      }

      console.log(message)
    }

    return response
  },
  (error) => {
    const method = error.config?.method?.toUpperCase() || 'GET'
    const url = error.config?.url || ''
    const status = error.response?.status || 'Network Error'
    const statusText = error.response?.statusText || ''
    const errorData = error.response?.data

    let message = `API ${method} ${url} - Failed (${status} ${statusText})`

    // Add error details if available
    if (errorData) {
      if (typeof errorData === 'string') {
        message += `: ${errorData}`
      } else if (errorData.message) {
        message += `: ${errorData.message}`
      } else if (errorData.title) {
        message += `: ${errorData.title}`
      } else if (errorData.errors) {
        const errors = Object.values(errorData.errors).flat().join(', ')
        message += `: ${errors}`
      }
    }

    console.error(message)
    return Promise.reject(error)
  }
)

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
  getOpenActionItems: () =>
    api.get<MeetingNote[]>('/meetingnotes/action-items/open').then(r => r.data),
  getOverdueActionItems: () =>
    api.get<MeetingNote[]>('/meetingnotes/action-items/overdue').then(r => r.data),
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
  create: (data: any) => api.post<Project>('/projects', data).then(r => r.data),
  update: (id: string, data: any) => api.put<Project>(`/projects/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/projects/${id}`)
}

// Tasks
export const tasksApi = {
  getAll: (pageNumber = 1, pageSize = 20) =>
    api.get<PagedResult<TeamTask>>(`/teamtasks?pageNumber=${pageNumber}&pageSize=${pageSize}`).then(r => r.data),
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
  update: (id: string, data: UpdateSprintDto) => api.put<Sprint>(`/sprints/${id}`, data).then(r => r.data),
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
  getAll: (pageNumber = 1, pageSize = 20) =>
    api.get<PagedResult<ManagerNote>>(`/managernotes?pageNumber=${pageNumber}&pageSize=${pageSize}`).then(r => r.data),
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

// Parents (task groupings like Epics)
export const parentsApi = {
  getAll: () => api.get<Parent[]>('/parents').then(r => r.data),
  getById: (id: string) => api.get<Parent>(`/parents/${id}`).then(r => r.data),
  getByName: (name: string) => api.get<Parent>(`/parents/by-name/${encodeURIComponent(name)}`).then(r => r.data),
  create: (data: CreateParentDto) => api.post<Parent>('/parents', data).then(r => r.data),
  update: (id: string, data: UpdateParentDto) => api.put<Parent>(`/parents/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/parents/${id}`)
}

// Backup & Restore
export const backupApi = {
  export: () => api.get<BackupDto>('/backup/export').then(r => r.data),
  import: (data: BackupDto) => api.post<RestoreResultDto>('/backup/import', data).then(r => r.data)
}

// Skills
export const skillsApi = {
  getAll: (includeInactive: boolean = false) =>
    api.get<Skill[]>(`/skills?includeInactive=${includeInactive}`).then(r => r.data),
  getById: (id: string) =>
    api.get<Skill>(`/skills/${id}`).then(r => r.data),
  getByCategory: (category: SkillCategory) =>
    api.get<Skill[]>(`/skills/by-category/${category}`).then(r => r.data),
  create: (data: CreateSkillDto) =>
    api.post<Skill>('/skills', data).then(r => r.data),
  update: (id: string, data: UpdateSkillDto) =>
    api.put<Skill>(`/skills/${id}`, data).then(r => r.data),
  activate: (id: string) =>
    api.post<Skill>(`/skills/${id}/activate`).then(r => r.data),
  deactivate: (id: string) =>
    api.post<Skill>(`/skills/${id}/deactivate`).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/skills/${id}`)
}

// Skill Assessments
export const skillAssessmentsApi = {
  getAll: () =>
    api.get<SkillAssessment[]>('/skillassessments').then(r => r.data),
  getMatrix: () =>
    api.get<SkillMatrix>('/skillassessments/matrix').then(r => r.data),
  getGaps: () =>
    api.get<SkillAssessment[]>('/skillassessments/gaps').then(r => r.data),
  getById: (id: string) =>
    api.get<SkillAssessment>(`/skillassessments/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<SkillAssessment[]>(`/skillassessments/by-direct-report/${directReportId}`).then(r => r.data),
  getBySkill: (skillId: string) =>
    api.get<SkillAssessment[]>(`/skillassessments/by-skill/${skillId}`).then(r => r.data),
  create: (data: CreateSkillAssessmentDto) =>
    api.post<SkillAssessment>('/skillassessments', data).then(r => r.data),
  update: (id: string, data: UpdateSkillAssessmentDto) =>
    api.put<SkillAssessment>(`/skillassessments/${id}`, data).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/skillassessments/${id}`)
}

// Activity Feed
export const activityFeedApi = {
  getAll: () =>
    api.get<Activity[]>('/activityfeed').then(r => r.data),
  getRecent: (days: number = 7) =>
    api.get<Activity[]>(`/activityfeed/recent?days=${days}`).then(r => r.data),
  getByEntityType: (type: string) =>
    api.get<Activity[]>(`/activityfeed/by-entity-type/${type}`).then(r => r.data)
}

// Project Knowledge
export const projectKnowledgeApi = {
  getAll: () =>
    api.get<ProjectKnowledge[]>('/projectknowledge').then(r => r.data),
  getMatrix: () =>
    api.get<ProjectKnowledgeMatrix>('/projectknowledge/matrix').then(r => r.data),
  getById: (id: string) =>
    api.get<ProjectKnowledge>(`/projectknowledge/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<ProjectKnowledge[]>(`/projectknowledge/by-direct-report/${directReportId}`).then(r => r.data),
  getByProject: (projectId: string) =>
    api.get<ProjectKnowledge[]>(`/projectknowledge/by-project/${projectId}`).then(r => r.data),
  createOrUpdate: (data: CreateOrUpdateProjectKnowledgeDto) =>
    api.put<ProjectKnowledge>('/projectknowledge', data).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/projectknowledge/${id}`)
}

// Checklist Templates
export const checklistTemplatesApi = {
  getAll: () =>
    api.get<ChecklistTemplate[]>('/checklisttemplates').then(r => r.data),
  getById: (id: string) =>
    api.get<ChecklistTemplate>(`/checklisttemplates/${id}`).then(r => r.data),
  getWithItems: (id: string) =>
    api.get<ChecklistTemplateWithItems>(`/checklisttemplates/${id}/with-items`).then(r => r.data),
  getByType: (type: ChecklistType, includeInactive: boolean = false) =>
    api.get<ChecklistTemplate[]>(`/checklisttemplates/by-type/${type}?includeInactive=${includeInactive}`).then(r => r.data),
  getActive: (type?: ChecklistType) => {
    const params = type !== undefined ? `?type=${type}` : ''
    return api.get<ChecklistTemplate[]>(`/checklisttemplates/active${params}`).then(r => r.data)
  },
  create: (data: CreateChecklistTemplateDto) =>
    api.post<ChecklistTemplate>('/checklisttemplates', data).then(r => r.data),
  update: (id: string, data: UpdateChecklistTemplateDto) =>
    api.put<ChecklistTemplate>(`/checklisttemplates/${id}`, data).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/checklisttemplates/${id}`),
  activate: (id: string) =>
    api.post<ChecklistTemplate>(`/checklisttemplates/${id}/activate`).then(r => r.data),
  deactivate: (id: string) =>
    api.post<ChecklistTemplate>(`/checklisttemplates/${id}/deactivate`).then(r => r.data),
  addItem: (templateId: string, data: CreateChecklistTemplateItemDto) =>
    api.post<ChecklistTemplateItem>(`/checklisttemplates/${templateId}/items`, data).then(r => r.data),
  updateItem: (itemId: string, data: UpdateChecklistTemplateItemDto) =>
    api.put<ChecklistTemplateItem>(`/checklisttemplates/items/${itemId}`, data).then(r => r.data),
  deleteItem: (itemId: string) =>
    api.delete(`/checklisttemplates/items/${itemId}`),
  reorderItems: (templateId: string, data: ReorderItemsDto) =>
    api.post(`/checklisttemplates/${templateId}/reorder`, data)
}

// Checklist Instances
export const checklistInstancesApi = {
  getAll: () =>
    api.get<ChecklistInstance[]>('/checklistinstances').then(r => r.data),
  getById: (id: string) =>
    api.get<ChecklistInstance>(`/checklistinstances/${id}`).then(r => r.data),
  getWithItems: (id: string) =>
    api.get<ChecklistInstanceWithItems>(`/checklistinstances/${id}/with-items`).then(r => r.data),
  getByType: (type: ChecklistType) =>
    api.get<ChecklistInstance[]>(`/checklistinstances/by-type/${type}`).then(r => r.data),
  getActive: (type?: ChecklistType) => {
    const params = type !== undefined ? `?type=${type}` : ''
    return api.get<ChecklistInstance[]>(`/checklistinstances/active${params}`).then(r => r.data)
  },
  createInterview: (data: CreateInterviewInstanceDto) =>
    api.post<ChecklistInstance>('/checklistinstances/interview', data).then(r => r.data),
  createOnboarding: (data: CreateOnboardingInstanceDto) =>
    api.post<ChecklistInstance>('/checklistinstances/onboarding', data).then(r => r.data),
  start: (id: string) =>
    api.post<ChecklistInstance>(`/checklistinstances/${id}/start`).then(r => r.data),
  complete: (id: string) =>
    api.post<ChecklistInstance>(`/checklistinstances/${id}/complete`).then(r => r.data),
  cancel: (id: string) =>
    api.post<ChecklistInstance>(`/checklistinstances/${id}/cancel`).then(r => r.data),
  updateNotes: (id: string, notes: string) =>
    api.put<ChecklistInstance>(`/checklistinstances/${id}/notes`, notes).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/checklistinstances/${id}`),
  completeItem: (itemId: string, data: CompleteChecklistItemDto) =>
    api.post<ChecklistInstanceItem>(`/checklistinstances/items/${itemId}/complete`, data).then(r => r.data),
  skipItem: (itemId: string, data?: SkipChecklistItemDto) =>
    api.post<ChecklistInstanceItem>(`/checklistinstances/items/${itemId}/skip`, data || {}).then(r => r.data),
  updateItem: (itemId: string, data: UpdateChecklistItemDto) =>
    api.put<ChecklistInstanceItem>(`/checklistinstances/items/${itemId}`, data).then(r => r.data),
  getOverdueItems: () =>
    api.get<ChecklistInstanceItem[]>('/checklistinstances/items/overdue').then(r => r.data)
}

export default api
