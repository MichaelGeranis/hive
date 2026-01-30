import { http, HttpResponse } from 'msw'
import type {
  DirectReport,
  ManagerNote,
  Leave,
  TeamTask,
  DashboardOverview,
  TeamLeaveOverview,
} from '../../types'

const API_BASE = 'http://localhost:5002/api'

// Sample data factories
export const createDirectReport = (overrides?: Partial<DirectReport>): DirectReport => ({
  id: '1',
  firstName: 'John',
  lastName: 'Doe',
  fullName: 'John Doe',
  email: 'john.doe@example.com',
  jobTitle: 'Software Engineer',
  department: 'Engineering',
  hireDate: '2023-01-15',
  isDirect: true,
  createdAt: '2023-01-15T10:00:00Z',
  ...overrides,
})

export const createManagerNote = (overrides?: Partial<ManagerNote>): ManagerNote => ({
  id: '1',
  title: 'Test Note',
  content: 'Test content',
  tags: 'test,sample',
  tagsList: ['test', 'sample'],
  priority: 1,
  priorityName: 'Normal',
  isCompleted: false,
  isOverdue: false,
  createdAt: '2024-01-01T10:00:00Z',
  ...overrides,
})

export const createLeave = (overrides?: Partial<Leave>): Leave => ({
  id: '1',
  directReportId: '1',
  directReportName: 'John Doe',
  type: 'Vacation',
  startDate: '2024-01-15',
  endDate: '2024-01-19',
  daysCount: 5,
  businessDaysCount: 5,
  createdAt: '2024-01-01T10:00:00Z',
  ...overrides,
})

export const createTeamTask = (overrides?: Partial<TeamTask>): TeamTask => ({
  id: '1',
  title: 'Test Task',
  description: 'Test description',
  type: 0,
  typeName: 'Task',
  priority: 1,
  priorityName: 'Medium',
  status: 1,
  statusName: 'Todo',
  matchedProjectNames: [],
  tags: '',
  labels: '',
  sprint: '',
  isOverdue: false,
  ...overrides,
})

// Default mock data
let mockDirectReports: DirectReport[] = [
  createDirectReport({ id: '1', firstName: 'John', lastName: 'Doe' }),
  createDirectReport({ id: '2', firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@example.com' }),
]

let mockNotes: ManagerNote[] = [
  createManagerNote({ id: '1', title: 'First Note', priority: 2, priorityName: 'High' }),
  createManagerNote({ id: '2', title: 'Second Note', isCompleted: true }),
]

let mockLeaves: Leave[] = [
  createLeave({ id: '1' }),
  createLeave({ id: '2', directReportId: '2', directReportName: 'Jane Smith', type: 'Sick' }),
]

let mockTasks: TeamTask[] = [
  createTeamTask({ id: '1', title: 'Task 1' }),
  createTeamTask({ id: '2', title: 'Task 2', status: 8, statusName: 'Done' }),
]

export const handlers = [
  // Direct Reports
  http.get(`${API_BASE}/directreports`, () => {
    return HttpResponse.json(mockDirectReports)
  }),

  http.get(`${API_BASE}/directreports/:id`, ({ params }) => {
    const report = mockDirectReports.find(r => r.id === params.id)
    if (!report) {
      return new HttpResponse(null, { status: 404 })
    }
    return HttpResponse.json(report)
  }),

  http.post(`${API_BASE}/directreports`, async ({ request }) => {
    const data = await request.json() as Partial<DirectReport>
    const newReport = createDirectReport({
      ...data,
      id: String(mockDirectReports.length + 1),
      fullName: `${data.firstName} ${data.lastName}`,
    })
    mockDirectReports.push(newReport)
    return HttpResponse.json(newReport, { status: 201 })
  }),

  http.delete(`${API_BASE}/directreports/:id`, ({ params }) => {
    mockDirectReports = mockDirectReports.filter(r => r.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // Manager Notes
  http.get(`${API_BASE}/managernotes`, ({ request }) => {
    const url = new URL(request.url)
    const pageNumber = parseInt(url.searchParams.get('pageNumber') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    const totalCount = mockNotes.length
    const totalPages = Math.ceil(totalCount / pageSize)
    const items = mockNotes.slice((pageNumber - 1) * pageSize, pageNumber * pageSize)
    return HttpResponse.json({
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber < totalPages
    })
  }),

  http.get(`${API_BASE}/managernotes/pending`, () => {
    return HttpResponse.json(mockNotes.filter(n => !n.isCompleted))
  }),

  http.get(`${API_BASE}/managernotes/completed`, () => {
    return HttpResponse.json(mockNotes.filter(n => n.isCompleted))
  }),

  http.get(`${API_BASE}/managernotes/overdue`, () => {
    return HttpResponse.json(mockNotes.filter(n => n.isOverdue))
  }),

  http.get(`${API_BASE}/managernotes/search`, ({ request }) => {
    const url = new URL(request.url)
    const q = url.searchParams.get('q')?.toLowerCase()
    const tag = url.searchParams.get('tag')?.toLowerCase()

    let results = [...mockNotes]
    if (q) {
      results = results.filter(n =>
        n.title.toLowerCase().includes(q) ||
        n.content.toLowerCase().includes(q)
      )
    }
    if (tag) {
      results = results.filter(n =>
        n.tagsList.some(t => t.toLowerCase().includes(tag))
      )
    }
    return HttpResponse.json(results)
  }),

  http.get(`${API_BASE}/managernotes/tags`, () => {
    const allTags = mockNotes.flatMap(n => n.tagsList)
    return HttpResponse.json([...new Set(allTags)])
  }),

  http.post(`${API_BASE}/managernotes`, async ({ request }) => {
    const data = await request.json() as Partial<ManagerNote>
    const newNote = createManagerNote({
      ...data,
      id: String(mockNotes.length + 1),
    })
    mockNotes.push(newNote)
    return HttpResponse.json(newNote, { status: 201 })
  }),

  http.post(`${API_BASE}/managernotes/:id/toggle`, ({ params }) => {
    const note = mockNotes.find(n => n.id === params.id)
    if (note) {
      note.isCompleted = !note.isCompleted
      return HttpResponse.json(note)
    }
    return new HttpResponse(null, { status: 404 })
  }),

  http.delete(`${API_BASE}/managernotes/:id`, ({ params }) => {
    mockNotes = mockNotes.filter(n => n.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // Leaves
  http.get(`${API_BASE}/leaves`, () => {
    return HttpResponse.json(mockLeaves)
  }),

  http.get(`${API_BASE}/leaves/overview`, () => {
    const overview: TeamLeaveOverview = {
      totalLeaveRecords: mockLeaves.length,
      teamMembersOnLeaveToday: 0,
      teamMembersOnLeaveThisWeek: 1,
      upcomingLeaves: mockLeaves.slice(0, 2),
      currentLeaves: [],
      monthlyTrend: [],
    }
    return HttpResponse.json(overview)
  }),

  http.post(`${API_BASE}/leaves`, async ({ request }) => {
    const data = await request.json() as Partial<Leave>
    const newLeave = createLeave({
      ...data,
      id: String(mockLeaves.length + 1),
    })
    mockLeaves.push(newLeave)
    return HttpResponse.json(newLeave, { status: 201 })
  }),

  http.delete(`${API_BASE}/leaves/:id`, ({ params }) => {
    mockLeaves = mockLeaves.filter(l => l.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // Tasks
  http.get(`${API_BASE}/teamtasks`, ({ request }) => {
    const url = new URL(request.url)
    const pageNumber = parseInt(url.searchParams.get('pageNumber') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    const totalCount = mockTasks.length
    const totalPages = Math.ceil(totalCount / pageSize)
    const items = mockTasks.slice((pageNumber - 1) * pageSize, pageNumber * pageSize)
    return HttpResponse.json({
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber < totalPages
    })
  }),

  http.get(`${API_BASE}/teamtasks/overdue`, () => {
    return HttpResponse.json(mockTasks.filter(t => t.isOverdue))
  }),

  http.post(`${API_BASE}/teamtasks`, async ({ request }) => {
    const data = await request.json() as Partial<TeamTask>
    const newTask = createTeamTask({
      ...data,
      id: String(mockTasks.length + 1),
    })
    mockTasks.push(newTask)
    return HttpResponse.json(newTask, { status: 201 })
  }),

  http.delete(`${API_BASE}/teamtasks/:id`, ({ params }) => {
    mockTasks = mockTasks.filter(t => t.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // Dashboard
  http.get(`${API_BASE}/reports/dashboard`, () => {
    const dashboard: DashboardOverview = {
      team: {
        totalReports: mockDirectReports.length,
        totalDirectReports: mockDirectReports.filter(r => r.isDirect).length,
        directReports: mockDirectReports.map(r => ({
          id: r.id,
          fullName: r.fullName,
          jobTitle: r.jobTitle,
          department: r.department,
          hireDate: r.hireDate,
          tenureMonths: 12,
        })),
      },
      reviews: {
        totalReviews: 0,
        draftReviews: 0,
        submittedReviews: 0,
        acknowledgedReviews: 0,
        completedReviews: 0,
        completionRate: 0,
        ratingDistribution: [],
        reviewsByPeriod: [],
      },
      oneOnOnes: {
        totalMeetings: 0,
        completedMeetings: 0,
        scheduledMeetings: 0,
        cancelledMeetings: 0,
        rescheduledMeetings: 0,
        completionRate: 0,
        totalMeetingMinutes: 0,
        averageMeetingDuration: 0,
        frequencyByDirectReport: [],
        actionItemsSummary: [],
      },
      tasks: {
        projects: {
          totalProjects: 0,
          planningProjects: 0,
          activeProjects: 0,
          onHoldProjects: 0,
          completedProjects: 0,
          cancelledProjects: 0,
          completionRate: 0,
        },
        tasks: {
          totalTasks: mockTasks.length,
          backlogTasks: 0,
          todoTasks: 1,
          inProgressTasks: 0,
          inReviewTasks: 0,
          doneTasks: 1,
          cancelledTasks: 0,
          overdueTasks: 0,
          unassignedTasks: 0,
          completionRate: 50,
        },
        tasksByAssignee: [],
        tasksByType: [],
        tasksByTypeSP: [],
        tasksByTypeHours: [],
        tasksByPriority: [],
        supportDistribution: {
          completedHours: 0,
          completedTaskCount: 0,
          allHours: 0,
          allTaskCount: 0,
          byAssignee: [],
        },
        productivity: {
          totalEstimatedHours: 0,
          totalActualHours: 0,
          estimationAccuracy: 0,
        },
      },
      insights: {
        workloadWarnings: [],
        knowledgeSilos: [],
        unengagedMembers: [],
      },
      generatedAt: new Date().toISOString(),
    }
    return HttpResponse.json(dashboard)
  }),

  // Health check
  http.get(`${API_BASE}/health`, () => {
    return HttpResponse.json({ status: 'healthy' })
  }),
]

// Helper to reset mock data between tests
export function resetMockData() {
  mockDirectReports = [
    createDirectReport({ id: '1', firstName: 'John', lastName: 'Doe' }),
    createDirectReport({ id: '2', firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@example.com' }),
  ]
  mockNotes = [
    createManagerNote({ id: '1', title: 'First Note', priority: 2, priorityName: 'High' }),
    createManagerNote({ id: '2', title: 'Second Note', isCompleted: true }),
  ]
  mockLeaves = [
    createLeave({ id: '1' }),
    createLeave({ id: '2', directReportId: '2', directReportName: 'Jane Smith', type: 'Sick' }),
  ]
  mockTasks = [
    createTeamTask({ id: '1', title: 'Task 1' }),
    createTeamTask({ id: '2', title: 'Task 2', status: 8, statusName: 'Done' }),
  ]
}
