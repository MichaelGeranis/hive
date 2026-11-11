import axios from 'axios'
import { recordLog } from './logStore'
import type {
  DirectReport,
  CreateDirectReportDto,
  BulkImportDirectReportsDto,
  BulkImportResultDto,
  BackupDto,
  RestoreResultDto,
  AutomaticBackupStatus,
  SaveTeamTaskDto,
  OneOnOneMeeting,
  CreateBlankMeetingDto,
  MeetingCount,
  Project,
  CreateProjectDto,
  Product,
  CreateProductDto,
  TeamTask,
  DashboardOverview,
  DashboardFocus,
  ProductEngagement,
  TasksOverview,
  TeamVelocity,
  AppSettings,
  UpdateAppSettings,
  Leave,
  CreateLeaveDto,
  UpdateLeaveDto,
  CreatePublicHolidayLeaveDto,
  CreatePublicHolidayResultDto,
  TeamLeaveOverview,
  MonthlyLeaveSummary,
  LeaveBalance,
  JiraImportRequest,
  JiraImportResult,
  JiraSprintSyncResult,
  JiraParentSyncResult,
  JiraProductSyncResult,
  JiraTaskSyncResult,
  JiraSyncAllResult,
  GoogleCalendarEvents,
  JiraImportPreview,
  ManagerNote,
  CreateManagerNoteDto,
  UpdateManagerNoteDto,
  NoteFolder,
  CreateNoteFolderDto,
  UpdateNoteFolderDto,
  Sprint,
  CreateSprintDto,
  UpdateSprintDto,
  SprintCapacity,
  CreateSprintCapacityDto,
  CapacityAnalysis,
  CarryOverTrend,
  BugSupportInflow,
  Parent,
  CreateParentDto,
  UpdateParentDto,
  Activity,
  PagedResult,
  ProjectKnowledge,
  ProjectKnowledgeMatrix,
  CreateOrUpdateProjectKnowledgeDto,
  SentimentStatus,
  RetroSummary,
  TaskReadinessOverview,
  SentimentAnalysis,
  TeamSentimentOverview,
  ApiKeyValidationResult,
  TaskSummaryDto,
  TaskPagedResult,
  OverrideTeamTaskFieldsDto,
  ClearTeamTaskOverridesDto
} from '../types'

const API_BASE_URL = 'http://localhost:5002/api'

// Admin credentials for the local backend. These match the backend's default
// (admin/admin123) unless overridden at build time via VITE_ADMIN_USERNAME /
// VITE_ADMIN_PASSWORD, which must match whatever the backend was configured
// with via HIVE_ADMIN_USERNAME / HIVE_ADMIN_PASSWORD. See README.md.
const ADMIN_USERNAME = import.meta.env.VITE_ADMIN_USERNAME || 'admin'
const ADMIN_PASSWORD = import.meta.env.VITE_ADMIN_PASSWORD || 'admin123'

// Create axios instance with default config
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Basic ' + btoa(`${ADMIN_USERNAME}:${ADMIN_PASSWORD}`)
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
    // A request the page cancelled on purpose is not a failure
    if (axios.isCancel(error)) return Promise.reject(error)

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

    // A 4xx is usually the backend rejecting input, which the page shows; a 5xx or no
    // response at all is something for the developers
    const level = typeof status === 'number' && status < 500 ? 'warn' : 'error'
    const details = [
      errorData !== undefined && errorData !== '' ? `Response: ${typeof errorData === 'string' ? errorData : JSON.stringify(errorData, null, 2)}` : '',
      error.code ? `Code: ${error.code}` : '',
      error.config?.params ? `Params: ${JSON.stringify(error.config.params)}` : '',
    ].filter(Boolean).join('\n')
    recordLog(level, message, 'api', details || undefined)
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

// One-on-One Meetings - each 1:1 is one markdown note
export const meetingsApi = {
  getAll: (
    pageNumber = 1,
    pageSize = 100,
    directReportId?: string | null,
    unlinked = false,
    search?: string
  ) => {
    const params = new URLSearchParams()
    params.append('pageNumber', pageNumber.toString())
    params.append('pageSize', pageSize.toString())
    if (directReportId) params.append('directReportId', directReportId)
    if (unlinked) params.append('unlinked', 'true')
    if (search) params.append('search', search)
    return api.get<PagedResult<OneOnOneMeeting>>(`/oneononemeetings?${params.toString()}`).then(r => r.data)
  },
  getById: (id: string) => api.get<OneOnOneMeeting>(`/oneononemeetings/${id}`).then(r => r.data),
  getByDirectReport: (directReportId: string) =>
    api.get<OneOnOneMeeting[]>(`/oneononemeetings/by-report/${directReportId}`).then(r => r.data),
  getCount: () => api.get<number>('/oneononemeetings/count').then(r => r.data),
  getCounts: () => api.get<MeetingCount[]>('/oneononemeetings/counts').then(r => r.data),
  createBlank: (data: CreateBlankMeetingDto = {}) =>
    api.post<OneOnOneMeeting>('/oneononemeetings/blank', data).then(r => r.data),
  updateContent: (id: string, content: string) =>
    api.put<OneOnOneMeeting>(`/oneononemeetings/${id}/content`, { content }).then(r => r.data),
  updateTags: (id: string, tags: string) =>
    api.put<OneOnOneMeeting>(`/oneononemeetings/${id}/tags`, { tags }).then(r => r.data),
  updateDate: (id: string, meetingDate: string) =>
    api.put<OneOnOneMeeting>(`/oneononemeetings/${id}/date`, { meetingDate }).then(r => r.data),
  delete: (id: string) => api.delete(`/oneononemeetings/${id}`)
}

// Projects
export const projectsApi = {
  getAll: () => api.get<Project[]>('/projects').then(r => r.data),
  getById: (id: string) => api.get<Project>(`/projects/${id}`).then(r => r.data),
  create: (data: CreateProjectDto) => api.post<Project>('/projects', data).then(r => r.data),
  update: (id: string, data: CreateProjectDto) => api.put<Project>(`/projects/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/projects/${id}`)
}

// Products (Jira components)
export const productsApi = {
  getAll: () => api.get<Product[]>('/products').then(r => r.data),
  getById: (id: string) => api.get<Product>(`/products/${id}`).then(r => r.data),
  create: (data: CreateProductDto) => api.post<Product>('/products', data).then(r => r.data),
  update: (id: string, data: CreateProductDto) => api.put<Product>(`/products/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/products/${id}`)
}

// Tasks
export interface TaskFilters {
  status?: number
  filter?: 'all' | 'overdue' | 'stale'
  search?: string
  label?: string
  sprint?: string
  component?: string
  // A TaskType name, e.g. 'Bug'
  type?: string
  // A direct report's id, or 'unassigned'
  assignee?: string
  // Only tasks whose components name no product
  unattributed?: boolean
  // Only tasks missing the story points they should have
  unestimated?: boolean
  // Only the upcoming stories and tasks with no acceptance criteria
  noAcceptanceCriteria?: boolean
  excludeParents?: boolean
}

export const tasksApi = {
  getAll: (pageNumber = 1, pageSize = 20, filters?: TaskFilters) => {
    const params = new URLSearchParams()
    params.append('pageNumber', pageNumber.toString())
    params.append('pageSize', pageSize.toString())
    if (filters?.status !== undefined) params.append('status', filters.status.toString())
    if (filters?.filter && filters.filter !== 'all') params.append('filter', filters.filter)
    if (filters?.search) params.append('search', filters.search)
    if (filters?.label) params.append('label', filters.label)
    if (filters?.sprint) params.append('sprint', filters.sprint)
    if (filters?.component) params.append('component', filters.component)
    if (filters?.type) params.append('type', filters.type)
    if (filters?.assignee) params.append('assignee', filters.assignee)
    if (filters?.unattributed) params.append('unattributed', 'true')
    if (filters?.unestimated) params.append('unestimated', 'true')
    if (filters?.noAcceptanceCriteria) params.append('noAcceptanceCriteria', 'true')
    if (filters?.excludeParents) params.append('excludeParents', 'true')
    return api.get<TaskPagedResult>(`/teamtasks?${params.toString()}`).then(r => r.data)
  },
  getSummary: (projectId?: string) => {
    const params = projectId ? `?projectId=${projectId}` : ''
    return api.get<TaskSummaryDto>(`/teamtasks/summary${params}`).then(r => r.data)
  },
  getById: (id: string) => api.get<TeamTask>(`/teamtasks/${id}`).then(r => r.data),
  getByAssignee: (assigneeId: string) => api.get<TeamTask[]>(`/teamtasks/assignee/${assigneeId}`).then(r => r.data),
  getByProject: (projectId: string) => api.get<TeamTask[]>(`/teamtasks/project/${projectId}`).then(r => r.data),
  getOverdue: () => api.get<TeamTask[]>('/teamtasks/overdue').then(r => r.data),
  create: (data: SaveTeamTaskDto) => api.post<TeamTask>('/teamtasks', data).then(r => r.data),
  update: (id: string, data: SaveTeamTaskDto) => api.put<TeamTask>(`/teamtasks/${id}`, data).then(r => r.data),
  duplicate: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/duplicate`).then(r => r.data),
  assign: (id: string, assigneeId: string | null) =>
    api.post<TeamTask>(`/teamtasks/${id}/assign`, { assigneeId }).then(r => r.data),
  start: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/start`).then(r => r.data),
  complete: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/complete`).then(r => r.data),
  reopen: (id: string) => api.post<TeamTask>(`/teamtasks/${id}/reopen`).then(r => r.data),
  overrideFields: (id: string, data: OverrideTeamTaskFieldsDto) =>
    api.patch<TeamTask>(`/teamtasks/${id}/overrides`, data).then(r => r.data),
  clearOverrides: (id: string, data: ClearTeamTaskOverridesDto) =>
    api.post<TeamTask>(`/teamtasks/${id}/clear-overrides`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/teamtasks/${id}`),
  bulkDelete: (ids: string[]) => api.post('/teamtasks/bulk-delete', ids)
}

// Reports
export const reportsApi = {
  getDashboard: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<DashboardOverview>(`/reports/dashboard${params}`).then(r => r.data);
  },
  getFocus: () => api.get<DashboardFocus>('/reports/focus').then(r => r.data),
  // months omitted means all time
  getProductEngagement: (months?: number) => {
    const params = months ? `?months=${months}` : '';
    return api.get<ProductEngagement>(`/reports/product-engagement${params}`).then(r => r.data);
  },
  getTasksAnalytics: () => api.get<TasksOverview>('/reports/tasks').then(r => r.data),
  getDirectReportAnalytics: (id: string) => api.get(`/reports/direct-reports/${id}`).then(r => r.data),
  getTeamVelocity: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<TeamVelocity>(`/reports/team-velocity${params}`).then(r => r.data);
  },
  getCarryOverTrend: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<CarryOverTrend>(`/reports/carry-over${params}`).then(r => r.data);
  },
  getBugSupportInflow: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<BugSupportInflow>(`/reports/bug-support-inflow${params}`).then(r => r.data);
  },
  getCapacityAnalysis: (sprintCount?: number) => {
    const params = sprintCount ? `?sprintCount=${sprintCount}` : '';
    return api.get<CapacityAnalysis>(`/reports/capacity-analysis${params}`).then(r => r.data);
  }
}

// Sprints
export const sprintsApi = {
  getAll: () => api.get<Sprint[]>('/sprints').then(r => r.data),
  // The sprint the team is in today; null when there are no sprints yet
  getCurrent: () => api.get<Sprint>('/sprints/current')
    .then(r => r.data as Sprint | null)
    .catch(err => {
      if (axios.isAxiosError(err) && err.response?.status === 404) return null
      throw err
    }),
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
  // A public holiday is one leave per team member, created together
  createPublicHoliday: (data: CreatePublicHolidayLeaveDto) =>
    api.post<CreatePublicHolidayResultDto>('/leaves/public-holiday', data).then(r => r.data),
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

// Google Calendar - read on demand from the calendar's secret iCal address
export const googleCalendarApi = {
  // from and to are yyyy-MM-dd, both inclusive. Only what the last sync fetched; never asks Google
  getEvents: (from: string, to: string) =>
    api.get<GoogleCalendarEvents>('/google-calendar/events', { params: { from, to } }).then(r => r.data),
  // Asks Google unless the calendar was synced in the last 30 minutes
  sync: (from: string, to: string) =>
    api.post<GoogleCalendarEvents>('/google-calendar/sync', null, { params: { from, to } }).then(r => r.data)
}

// Jira Sync - pulls data from the Jira Cloud API on demand
export const jiraSyncApi = {
  syncAll: () =>
    api.post<JiraSyncAllResult>('/jira-sync').then(r => r.data),
  syncSprintDates: () =>
    api.post<JiraSprintSyncResult>('/jira-sync/sprint-dates').then(r => r.data),
  syncProducts: () =>
    api.post<JiraProductSyncResult>('/jira-sync/products').then(r => r.data),
  syncParents: () =>
    api.post<JiraParentSyncResult>('/jira-sync/parents').then(r => r.data),
  syncTasks: () =>
    api.post<JiraTaskSyncResult>('/jira-sync/tasks').then(r => r.data)
}

// Manager Notes (TODOs)
export const notesApi = {
  getAll: (
    pageNumber = 1,
    pageSize = 20,
    search?: string,
    tag?: string,
    folderId?: string | null
  ) => {
    const params = new URLSearchParams()
    params.append('pageNumber', pageNumber.toString())
    params.append('pageSize', pageSize.toString())
    if (search) params.append('search', search)
    if (tag) params.append('tag', tag)
    if (folderId) params.append('folderId', folderId)
    return api.get<PagedResult<ManagerNote>>(`/managernotes?${params.toString()}`).then(r => r.data)
  },
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
  createBlank: (folderId?: string | null) =>
    api.post<ManagerNote>('/managernotes/blank', { folderId: folderId ?? null }).then(r => r.data),
  updateContent: (id: string, content: string) =>
    api.put<ManagerNote>(`/managernotes/${id}/content`, { content }).then(r => r.data),
  move: (id: string, folderId: string | null) =>
    api.post<ManagerNote>(`/managernotes/${id}/move`, { folderId }).then(r => r.data),
  togglePin: (id: string) => api.post<ManagerNote>(`/managernotes/${id}/pin`).then(r => r.data),
  delete: (id: string) => api.delete(`/managernotes/${id}`)
}

// Note folders
export const noteFoldersApi = {
  getAll: () => api.get<NoteFolder[]>('/notefolders').then(r => r.data),
  getById: (id: string) => api.get<NoteFolder>(`/notefolders/${id}`).then(r => r.data),
  create: (data: CreateNoteFolderDto) => api.post<NoteFolder>('/notefolders', data).then(r => r.data),
  update: (id: string, data: UpdateNoteFolderDto) =>
    api.put<NoteFolder>(`/notefolders/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/notefolders/${id}`)
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
  import: (data: BackupDto) => api.post<RestoreResultDto>('/backup/import', data).then(r => r.data),
  getAutomaticStatus: () => api.get<AutomaticBackupStatus>('/backup/automatic').then(r => r.data)
}

// Activity Feed
export const activityFeedApi = {
  getAll: (pageNumber = 1, pageSize = 50, search?: string) => {
    const params = new URLSearchParams()
    params.append('pageNumber', pageNumber.toString())
    params.append('pageSize', pageSize.toString())
    if (search) params.append('search', search)
    return api.get<PagedResult<Activity>>(`/activityfeed?${params.toString()}`).then(r => r.data)
  },
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

// Sentiment Analysis
export const sentimentApi = {
  getStatus: () =>
    api.get<SentimentStatus>('/sentimentanalysis/status').then(r => r.data),
  getForDirectReport: (directReportId: string) =>
    api.get<SentimentAnalysis>(`/sentimentanalysis/direct-report/${directReportId}`).then(r => r.data),
  getTeamOverview: () =>
    api.get<TeamSentimentOverview>('/sentimentanalysis/team').then(r => r.data),
  refreshForDirectReport: (directReportId: string) =>
    api.post<SentimentAnalysis>(`/sentimentanalysis/direct-report/${directReportId}/refresh`).then(r => r.data),
  validateApiKey: (apiKey: string) =>
    api.post<ApiKeyValidationResult>('/sentimentanalysis/validate-api-key', { apiKey }).then(r => r.data),
  // Blank values test the saved settings
  testOllama: (baseUrl: string, model: string) =>
    api.post<ApiKeyValidationResult>('/sentimentanalysis/test-ollama', { baseUrl, model }).then(r => r.data)
}

// Task readiness: the AI reviews one task per call, so no request waits for the whole sprint
export const taskReadinessApi = {
  get: () =>
    api.get<TaskReadinessOverview>('/task-readiness').then(r => r.data),
  reviewNext: () =>
    api.post<TaskReadinessOverview>('/task-readiness/review-next').then(r => r.data),
  reviewTask: (taskId: string) =>
    api.post<TaskReadinessOverview>(`/task-readiness/tasks/${taskId}/review`).then(r => r.data)
}

// Retro summary
export const retroSummaryApi = {
  // Made again first when a retro changed since the last summary
  get: () =>
    api.get<RetroSummary>('/retro-summary').then(r => r.data),
  refresh: () =>
    api.post<RetroSummary>('/retro-summary/refresh').then(r => r.data)
}

// Quarterly Planning
import type {
  Quarter,
  CreateQuarterDto,
  UpdateQuarterDto,
  CreateInitiativeDto,
  UpdateInitiativeDto,
  Cell,
  PlanningBoard,
  PlanDraft,
  ScenarioRequest,
  Scenario,
  PlanReview,
  Calibration,
  SaveExternalDependencyDto
} from '../types/quarterlyPlanning'

const planning = '/quarterly-planning'
type Created = { id: string }

export const quarterlyPlanningApi = {
  // Quarters
  getAllQuarters: () => api.get<Quarter[]>(`${planning}/quarters`).then(r => r.data),
  createQuarter: (data: CreateQuarterDto) => api.post<Quarter>(`${planning}/quarters`, data).then(r => r.data),
  updateQuarter: (id: string, data: UpdateQuarterDto) => api.put<Quarter>(`${planning}/quarters/${id}`, data).then(r => r.data),
  activateQuarter: (id: string) => api.post<Quarter>(`${planning}/quarters/${id}/activate`).then(r => r.data),
  completeQuarter: (id: string) => api.post<Quarter>(`${planning}/quarters/${id}/complete`).then(r => r.data),
  deleteQuarter: (id: string) => api.delete(`${planning}/quarters/${id}`),

  // The computed plan
  getBoard: (quarterId: string) => api.get<PlanningBoard>(`${planning}/quarters/${quarterId}/board`).then(r => r.data),
  draft: (quarterId: string, apply: boolean) =>
    api.post<PlanDraft>(`${planning}/quarters/${quarterId}/draft`, null, { params: { apply } }).then(r => r.data),
  runScenario: (quarterId: string, data: ScenarioRequest) =>
    api.post<Scenario>(`${planning}/quarters/${quarterId}/scenario`, data).then(r => r.data),
  commit: (quarterId: string) => api.post<Quarter>(`${planning}/quarters/${quarterId}/commit`).then(r => r.data),
  getReview: (quarterId: string) => api.get<PlanReview>(`${planning}/quarters/${quarterId}/review`).then(r => r.data),
  getCalibration: () => api.get<Calibration>(`${planning}/calibration`).then(r => r.data),
  exportToExcel: (quarterId: string) =>
    api.get<Blob>(`${planning}/quarters/${quarterId}/export`, { responseType: 'blob' }).then(r => r.data),

  // Roster, grid, sprint notes, priority order, lanes
  setRosterMember: (quarterId: string, directReportId: string, capacityPercent: number) =>
    api.put(`${planning}/quarters/${quarterId}/roster`, { directReportId, capacityPercent }),
  removeRosterMember: (quarterId: string, directReportId: string) =>
    api.delete(`${planning}/quarters/${quarterId}/roster/${directReportId}`),
  setCells: (quarterId: string, cells: Cell[]) => api.put(`${planning}/quarters/${quarterId}/cells`, { cells }),
  setSprintNotes: (quarterId: string, sprintId: string, notes: string) =>
    api.put(`${planning}/quarters/${quarterId}/sprints/${sprintId}/notes`, { notes }),
  reorder: (quarterId: string, orderedIds: string[]) =>
    api.put(`${planning}/quarters/${quarterId}/order`, { orderedIds }),
  addLane: (quarterId: string, workType: number) =>
    api.post<Created>(`${planning}/quarters/${quarterId}/lanes`, { workType }).then(r => r.data),

  // Initiatives
  createInitiative: (data: CreateInitiativeDto) => api.post<Created>(`${planning}/initiatives`, data).then(r => r.data),
  updateInitiative: (id: string, data: UpdateInitiativeDto) => api.put(`${planning}/initiatives/${id}`, data),
  changeStatus: (id: string, status: number, note?: string) =>
    api.put(`${planning}/initiatives/${id}/status`, { status, note }),
  carryOver: (id: string) => api.post<Created>(`${planning}/initiatives/${id}/carry-over`).then(r => r.data),
  deleteInitiative: (id: string) => api.delete(`${planning}/initiatives/${id}`),

  // People on initiatives
  addMember: (initiativeId: string, directReportId: string, role: number) =>
    api.post<Created>(`${planning}/initiatives/${initiativeId}/members`, { directReportId, role }).then(r => r.data),
  updateMember: (memberId: string, role: number) => api.put(`${planning}/members/${memberId}`, { role }),
  removeMember: (memberId: string) => api.delete(`${planning}/members/${memberId}`),


  // Dependencies on other teams, and between initiatives
  addExternalDependency: (initiativeId: string, data: SaveExternalDependencyDto) =>
    api.post<Created>(`${planning}/initiatives/${initiativeId}/external-dependencies`, data).then(r => r.data),
  updateExternalDependency: (id: string, data: SaveExternalDependencyDto) =>
    api.put(`${planning}/external-dependencies/${id}`, data),
  deleteExternalDependency: (id: string) => api.delete(`${planning}/external-dependencies/${id}`),
  createDependency: (dependentInitiativeId: string, dependencyInitiativeId: string) =>
    api.post<Created>(`${planning}/dependencies`, { dependentInitiativeId, dependencyInitiativeId }).then(r => r.data),
  deleteDependency: (id: string) => api.delete(`${planning}/dependencies/${id}`)
}

// Skill roadmaps and career plans
import type {
  SkillRoadmapSummary,
  SkillRoadmap,
  SaveSkillRoadmapDto,
  SaveRoadmapSkillDto,
  RoadmapLibraryItem,
  CareerPlanSummary,
  CareerPlan,
  CareerOverview,
  CreateCareerPlanDto,
  UpdateCareerPlanDto,
  AddCareerPlanSkillDto,
  SkillLevelChange
} from '../types/careerPlans'

export const skillRoadmapsApi = {
  getAll: () => api.get<SkillRoadmapSummary[]>('/skill-roadmaps').then(r => r.data),
  getById: (id: string) => api.get<SkillRoadmap>(`/skill-roadmaps/${id}`).then(r => r.data),
  create: (data: SaveSkillRoadmapDto) => api.post<SkillRoadmap>('/skill-roadmaps', data).then(r => r.data),
  update: (id: string, data: SaveSkillRoadmapDto) => api.put<SkillRoadmap>(`/skill-roadmaps/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/skill-roadmaps/${id}`),
  addSkill: (id: string, data: SaveRoadmapSkillDto) =>
    api.post<SkillRoadmap>(`/skill-roadmaps/${id}/skills`, data).then(r => r.data),
  updateSkill: (id: string, skillId: string, data: SaveRoadmapSkillDto) =>
    api.put<SkillRoadmap>(`/skill-roadmaps/${id}/skills/${skillId}`, data).then(r => r.data),
  deleteSkill: (id: string, skillId: string) =>
    api.delete<SkillRoadmap>(`/skill-roadmaps/${id}/skills/${skillId}`).then(r => r.data),
  // The built-in roadmaps, and whether each is already in Hive
  getLibrary: () => api.get<RoadmapLibraryItem[]>('/skill-roadmaps/library').then(r => r.data),
  addFromLibrary: (key: string) =>
    api.post<SkillRoadmap>(`/skill-roadmaps/library/${encodeURIComponent(key)}`).then(r => r.data)
}

export const careerPlansApi = {
  // One per direct report, by name
  getAll: () => api.get<CareerPlanSummary[]>('/career-plans').then(r => r.data),
  // Who needs the manager: no plan, nothing in focus, quiet for 60 days
  getOverview: () => api.get<CareerOverview>('/career-plans/overview').then(r => r.data),
  getById: (id: string) => api.get<CareerPlan>(`/career-plans/${id}`).then(r => r.data),
  // A direct report's plan; null when they have none yet
  getByDirectReport: (directReportId: string) => api.get<CareerPlan>(`/career-plans/by-direct-report/${directReportId}`)
    .then(r => r.data as CareerPlan | null)
    .catch(err => {
      if (axios.isAxiosError(err) && err.response?.status === 404) return null
      throw err
    }),
  create: (data: CreateCareerPlanDto) => api.post<CareerPlan>('/career-plans', data).then(r => r.data),
  update: (id: string, data: UpdateCareerPlanDto) => api.put<CareerPlan>(`/career-plans/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/career-plans/${id}`),
  // Rates a skill, answering with what the rating unlocked
  setSkillLevel: (id: string, skillId: string, level: number, evidence?: string) =>
    api.put<SkillLevelChange>(`/career-plans/${id}/skills/${skillId}/level`, { level, evidence }).then(r => r.data),
  // Marks a skill as one they are working on now, or not
  setSkillFocus: (id: string, skillId: string, isFocus: boolean) =>
    api.put<CareerPlan>(`/career-plans/${id}/skills/${skillId}/focus`, { isFocus }).then(r => r.data),
  addSkill: (id: string, data: AddCareerPlanSkillDto) =>
    api.post<CareerPlan>(`/career-plans/${id}/skills`, data).then(r => r.data),
  removeSkill: (id: string, skillId: string) =>
    api.delete<CareerPlan>(`/career-plans/${id}/skills/${skillId}`).then(r => r.data)
}

export default api
