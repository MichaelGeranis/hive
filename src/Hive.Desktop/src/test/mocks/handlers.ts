import { http, HttpResponse } from 'msw'
import type {
  DirectReport,
  ProductEngagement,
  ManagerNote,
  NoteFolder,
  OneOnOneMeeting,
  Leave,
  TeamTask,
  Project,
  Product,
  DashboardOverview,
  DashboardFocus,
  TeamLeaveOverview,
  CareerPlanSummary,
  CareerPlan,
  CareerPlanSkill,
  CareerBadge,
  CareerOverview,
  SkillRoadmap,
  SkillRoadmapSummary,
  RoadmapLibraryItem,
  Quarter,
  Initiative,
  PlanningBoard,
  PlanReview,
  RetroSummary,
  TaskReadinessOverview,
} from '../../types'

const API_BASE = 'http://localhost:5002/api'

// Off by default, as in a new install
export const createTaskReadiness = (overrides?: Partial<TaskReadinessOverview>): TaskReadinessOverview => ({
  isEnabled: false,
  isConfigured: true,
  currentSprint: null,
  nextSprint: null,
  tasks: [],
  pendingCount: 0,
  error: null,
  ...overrides,
})

// Off by default, as in a new install
export const createRetroSummary = (overrides?: Partial<RetroSummary>): RetroSummary => ({
  isEnabled: false,
  isConfigured: true,
  hasRetroFolder: false,
  retros: [],
  summary: null,
  warnings: [],
  actionItems: [],
  suggestions: [],
  model: null,
  retrosAnalyzed: 0,
  analyzedAt: null,
  isUpToDate: false,
  error: null,
  ...overrides,
})

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
  folderId: null,
  isPinned: false,
  snippet: 'Test content',
  createdAt: '2024-01-01T10:00:00Z',
  ...overrides,
})

export const createMeeting = (overrides?: Partial<OneOnOneMeeting>): OneOnOneMeeting => ({
  id: '1',
  directReportId: '1',
  directReportName: 'John Doe',
  isUnlinked: false,
  meetingDate: '2026-09-10',
  title: 'Weekly sync',
  content: 'Weekly sync\n\n- Went well',
  tags: 'johndoe',
  tagsList: ['johndoe'],
  snippet: '- Went well',
  createdAt: '2026-09-10T10:00:00Z',
  ...overrides,
})

export const createNoteFolder = (overrides?: Partial<NoteFolder>): NoteFolder => ({
  id: '1',
  name: 'Test Folder',
  parentFolderId: null,
  sortOrder: 0,
  noteCount: 0,
  isJournal: false,
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
  statusChangedAt: new Date().toISOString(),
  tags: '',
  labels: '',
  components: '',
  sprint: '',
  overriddenFields: '',
  isOverdue: false,
  isParentTask: false,
  ...overrides,
})

export const createProject = (overrides?: Partial<Project>): Project => ({
  id: 'p1',
  name: 'Test Project',
  description: 'Test project description',
  url: '',
  products: [],
  ...overrides,
})

export const createProduct = (overrides?: Partial<Product>): Product => ({
  id: 'pr1',
  name: 'Matching',
  description: '',
  jiraComponentId: '10042',
  isFromJira: true,
  totalTasks: 2,
  completedTasks: 1,
  openTasks: 1,
  parentCount: 0,
  projects: [],
  createdAt: new Date().toISOString(),
  ...overrides,
})

// Career plans and skill roadmaps
export const createCareerPlanSummary = (overrides?: Partial<CareerPlanSummary>): CareerPlanSummary => ({
  id: 'cp1',
  directReportId: '1',
  directReportName: 'John Doe',
  roadmapName: 'Backend Engineer',
  currentStage: 'Mid',
  workingOnCount: 1,
  createdAt: '2026-06-01T10:00:00Z',
  lastChangedAt: '2026-09-20T10:00:00Z',
  isQuiet: false,
  ...overrides,
})

export const createCareerPlanSkill = (overrides?: Partial<CareerPlanSkill>): CareerPlanSkill => ({
  id: 'sk1',
  section: 'APIs',
  title: 'REST design',
  description: 'Resources, verbs and status codes',
  category: 'Hard',
  isOptional: false,
  fromStage: 'Junior',
  level: 2,
  levelLabel: 'Learning',
  status: 'WorkingOn',
  isFocus: true,
  expectedLevel: 3,
  evidence: 'Reviewing the orders API',
  lastLevelUpAt: '2026-09-20T10:00:00Z',
  updatedAt: '2026-09-20T10:00:00Z',
  ...overrides,
})

export const CAREER_BADGES: CareerBadge[] = [
  { key: 'first-steps', name: 'First Steps', description: 'Rate a skill above Not started', isEarned: true },
  { key: 'got-it', name: 'Got It', description: 'Have a skill at Practising or more', isEarned: true },
  { key: 'deep-diver', name: 'Deep Diver', description: 'Reach Expert in a skill', isEarned: false },
  { key: 'on-a-roll', name: 'On a Roll', description: 'Level up three skills in 30 days', isEarned: false },
  { key: 'trailblazer', name: 'Trailblazer', description: 'Level up an optional skill', isEarned: false },
  { key: 'tech-wizard', name: 'Tech Wizard', description: 'Have five hard skills', isEarned: false },
  { key: 'people-person', name: 'People Person', description: 'Have three soft skills', isEarned: false },
  { key: 'role-ready', name: 'Role Ready', description: 'Meet their stage\'s expectations', isEarned: false },
  { key: 'ahead-of-the-curve', name: 'Ahead of the Curve', description: 'Meet the next stage\'s expectations', isEarned: false },
]

export const createCareerPlan = (overrides?: Partial<CareerPlan>): CareerPlan => {
  const rest = createCareerPlanSkill({
    id: 'sk2', title: 'Caching', level: 0, levelLabel: 'Not started', status: 'NotYet', isFocus: false, evidence: '', lastLevelUpAt: null
  })
  const sql = createCareerPlanSkill({
    id: 'sk5', title: 'SQL', level: 4, levelLabel: 'Proficient', status: 'Has', isFocus: false, evidence: 'Tuned the reporting queries'
  })
  const softSkill = createCareerPlanSkill({
    id: 'sk3', section: 'Collaboration', title: 'Code review', category: 'Soft', level: 1, levelLabel: 'Aware',
    status: 'NotYet', isFocus: false, description: 'Kind, useful reviews', evidence: ''
  })
  const mentoring = createCareerPlanSkill({
    id: 'sk4', section: 'Collaboration', title: 'Mentoring', category: 'Soft', fromStage: 'Senior', expectedLevel: null,
    level: 3, levelLabel: 'Practising', status: 'Has', isFocus: false, evidence: ''
  })
  return {
    ...createCareerPlanSummary(),
    notes: 'Wants to lead the payments rewrite',
    skillCount: 5,
    hasCount: 2,
    notYetCount: 2,
    sections: [
      { name: 'APIs', skillCount: 3, hasCount: 1, workingOnCount: 1, isCleared: false, skills: [createCareerPlanSkill(), rest, sql] },
      { name: 'Collaboration', skillCount: 2, hasCount: 1, workingOnCount: 0, isCleared: false, skills: [softSkill, mentoring] },
    ],
    ladder: {
      currentStage: 'Mid',
      skillsStage: 'Mid',
      reading: 'AtRole',
      stages: [
        { stage: 'Junior', expectedCount: 3, metCount: 3, metPercent: 100 },
        { stage: 'Mid', expectedCount: 4, metCount: 4, metPercent: 100 },
        { stage: 'Senior', expectedCount: 5, metCount: 2, metPercent: 40 },
        { stage: 'Staff', expectedCount: 5, metCount: 0, metPercent: 0 },
      ],
      strengths: [sql, mentoring],
      weaknesses: [rest, softSkill],
    },
    game: {
      xp: 60,
      playerLevel: 2,
      playerTitle: 'Explorer',
      currentLevelXp: 50,
      nextLevelXp: 150,
      skillsLevelledUpRecently: 1,
      badges: CAREER_BADGES,
    },
    ...overrides,
  }
}

export const createSkillRoadmapSummary = (overrides?: Partial<SkillRoadmapSummary>): SkillRoadmapSummary => ({
  id: 'rm1',
  name: 'Backend Engineer',
  description: 'Server-side engineering',
  libraryKey: 'backend-engineer',
  sectionCount: 2,
  skillCount: 3,
  hardSkillCount: 2,
  softSkillCount: 1,
  skillCountsByStage: { Junior: 2, Mid: 2, Senior: 3, Staff: 3 },
  createdAt: '2026-06-01T10:00:00Z',
  ...overrides,
})

export const createSkillRoadmap = (overrides?: Partial<SkillRoadmap>): SkillRoadmap => ({
  ...createSkillRoadmapSummary(),
  sections: [
    {
      name: 'APIs',
      skills: [
        { id: 'rs1', section: 'APIs', title: 'REST design', description: '', category: 'Hard', isOptional: false, sortOrder: 0, fromStage: 'Junior' },
        { id: 'rs2', section: 'APIs', title: 'GraphQL', description: '', category: 'Hard', isOptional: true, sortOrder: 1, fromStage: 'Senior' },
      ],
    },
    {
      name: 'Collaboration',
      skills: [
        { id: 'rs3', section: 'Collaboration', title: 'Code review', description: '', category: 'Soft', isOptional: false, sortOrder: 0, fromStage: 'Junior' },
      ],
    },
  ],
  ...overrides,
})

export const createRoadmapLibraryItem = (overrides?: Partial<RoadmapLibraryItem>): RoadmapLibraryItem => ({
  key: 'backend-engineer',
  name: 'Backend Engineer',
  description: 'APIs, databases, reliability and the people skills around them',
  sectionCount: 8,
  skillCount: 40,
  hardSkillCount: 32,
  softSkillCount: 8,
  isAdded: false,
  ...overrides,
})

export const createCareerOverview = (overrides?: Partial<CareerOverview>): CareerOverview => ({
  directReportsWithoutPlan: [],
  nothingInFocus: [],
  quiet: [],
  ...overrides,
})

// Default mock data
let mockDirectReports: DirectReport[] = [
  createDirectReport({ id: '1', firstName: 'John', lastName: 'Doe' }),
  createDirectReport({ id: '2', firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@example.com' }),
]

let mockMeetings: OneOnOneMeeting[] = [
  createMeeting({ id: 'm1', title: 'Weekly sync', directReportId: '1', directReportName: 'John Doe', tags: 'johndoe', tagsList: ['johndoe'] }),
  createMeeting({
    id: 'm2',
    title: 'Career chat',
    content: 'Career chat\n\n- Wants to move towards staff',
    snippet: '- Wants to move towards staff',
    meetingDate: '2026-09-03',
    directReportId: null,
    directReportName: null,
    isUnlinked: true,
    tags: '',
    tagsList: [],
  }),
]

let mockNoteFolders: NoteFolder[] = [
  createNoteFolder({ id: 'f1', name: 'Work', noteCount: 1 }),
]

let mockNotes: ManagerNote[] = [
  createManagerNote({ id: '1', title: 'First Note' }),
  createManagerNote({ id: '2', title: 'Second Note' }),
]

let mockLeaves: Leave[] = [
  createLeave({ id: '1' }),
  createLeave({ id: '2', directReportId: '2', directReportName: 'Jane Smith', type: 'Sick' }),
]

let mockTasks: TeamTask[] = [
  createTeamTask({ id: '1', title: 'Task 1' }),
  createTeamTask({ id: '2', title: 'Task 2', status: 8, statusName: 'Done' }),
]

let mockProjects: Project[] = [
  createProject({ id: 'p1', name: 'Apollo', products: [{ id: 'pr1', name: 'Matching' }] }),
  createProject({ id: 'p2', name: 'Borealis' }),
]

let mockProducts: Product[] = [
  createProduct({ id: 'pr1', name: 'Matching', projects: [{ id: 'p1', name: 'Apollo', url: '' }] }),
  createProduct({ id: 'pr2', name: 'Enrichment', jiraComponentId: '10043' }),
]

/**
 * The dashboard's focus panel: nothing due, nobody away, and no sprint. Tests override it
 * with `server.use`.
 */
export const createDashboardFocus = (overrides?: Partial<DashboardFocus>): DashboardFocus => ({
  today: '2026-09-24',
  currentSprint: null,
  oneOnOnes: { cadenceDays: 14, needsAttentionCount: 0, people: [] },
  todos: { hasJournal: false, noteId: null, noteTitle: null, items: [] },
  leave: { lookAheadDays: 14, outToday: [], upcoming: [] },
  tenureMilestones: [],
  ...overrides,
})

/**
 * The dashboard payload the page loads on mount. Tests override slices of it — usually
 * `insights` — with `server.use`.
 */
export const createDashboardOverview = (
  overrides?: Partial<DashboardOverview>
): DashboardOverview => ({
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
    tasksByPriority: [],
    tasksByLabel: [],
    tasksByComponent: [],
    supportDistribution: {
      completedStoryPoints: 0,
      completedTaskCount: 0,
      allStoryPoints: 0,
      allTaskCount: 0,
      maintenanceCompletedStoryPoints: 0,
      maintenanceCompletedTaskCount: 0,
      maintenanceAllStoryPoints: 0,
      maintenanceAllTaskCount: 0,
      byAssignee: [],
    },
    tasksByWorkCategory: [],
  },
  insights: {
    workloadWarnings: [],
    knowledgeSilos: [],
    unengagedMembers: [],
    narrowlyEngagedMembers: [],
    unworkedProducts: [],
    unmatchedTaskCount: 0,
    warnings: [],
  },
  generatedAt: new Date().toISOString(),
  ...overrides,
})

export const createProductEngagement = (overrides: Partial<ProductEngagement> = {}): ProductEngagement => ({
  months: 3,
  membersByProduct: [],
  productsByMember: [],
  knowledgeSilos: [],
  unworkedProducts: [],
  unengagedMembers: [],
  narrowlyEngagedMembers: [],
  minProductMembers: 2,
  minMemberProducts: 2,
  ...overrides,
})

export const handlers = [
  // Automatic backups of the database file (Settings, Backup & restore)
  http.get(`${API_BASE}/backup/automatic`, () => {
    return HttpResponse.json({ enabled: true, folder: '/Users/test/Library/Application Support/Hive', count: 3, lastBackupAt: '2026-09-29T08:00:00Z', intervalHours: 1 })
  }),

  // Career plans: none unless a test says otherwise
  http.get(`${API_BASE}/career-plans/overview`, () => HttpResponse.json(createCareerOverview())),
  http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json(createRetroSummary())),
  http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(createTaskReadiness())),
  http.get(`${API_BASE}/career-plans`, () => HttpResponse.json([])),
  http.get(`${API_BASE}/career-plans/:id`, ({ params }) => HttpResponse.json(createCareerPlan({ id: params.id as string }))),
  http.get(`${API_BASE}/skill-roadmaps/library`, () => HttpResponse.json([])),
  http.get(`${API_BASE}/skill-roadmaps`, () => HttpResponse.json([])),
  http.get(`${API_BASE}/skill-roadmaps/:id`, ({ params }) => HttpResponse.json(createSkillRoadmap({ id: params.id as string }))),

  // Google Calendar: not connected unless a test says otherwise
  http.get(`${API_BASE}/google-calendar/events`, () => {
    return HttpResponse.json({ isConfigured: false, isLoaded: false, events: [] })
  }),

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
    const search = url.searchParams.get('search')?.toLowerCase()
    const folderId = url.searchParams.get('folderId')

    let matching = [...mockNotes]
    if (folderId) {
      matching = matching.filter(n => n.folderId === folderId)
    }
    if (search) {
      matching = matching.filter(n =>
        n.title.toLowerCase().includes(search) || n.content.toLowerCase().includes(search)
      )
    }

    const totalCount = matching.length
    const totalPages = Math.ceil(totalCount / pageSize)
    const items = matching.slice((pageNumber - 1) * pageSize, pageNumber * pageSize)
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

  http.post(`${API_BASE}/managernotes/blank`, async ({ request }) => {
    const data = await request.json() as { folderId?: string | null }
    const newNote = createManagerNote({
      id: `note-${mockNotes.length + 1}`,
      title: 'New Note',
      content: '',
      snippet: '',
      tags: '',
      tagsList: [],
      folderId: data?.folderId ?? null,
    })
    mockNotes = [newNote, ...mockNotes]
    return HttpResponse.json(newNote, { status: 201 })
  }),

  http.put(`${API_BASE}/managernotes/:id/content`, async ({ params, request }) => {
    const { content } = await request.json() as { content: string }
    const note = mockNotes.find(n => n.id === params.id)
    if (!note) return new HttpResponse(null, { status: 404 })
    note.content = content
    note.title = content.split('\n').find(line => line.trim()) ?? 'New Note'
    note.updatedAt = new Date().toISOString()
    return HttpResponse.json(note)
  }),

  http.post(`${API_BASE}/managernotes/:id/pin`, ({ params }) => {
    const note = mockNotes.find(n => n.id === params.id)
    if (!note) return new HttpResponse(null, { status: 404 })
    note.isPinned = !note.isPinned
    return HttpResponse.json(note)
  }),

  http.post(`${API_BASE}/managernotes/:id/move`, async ({ params, request }) => {
    const { folderId } = await request.json() as { folderId: string | null }
    const note = mockNotes.find(n => n.id === params.id)
    if (!note) return new HttpResponse(null, { status: 404 })
    note.folderId = folderId
    return HttpResponse.json(note)
  }),

  http.delete(`${API_BASE}/managernotes/:id`, ({ params }) => {
    mockNotes = mockNotes.filter(n => n.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // 1:1 meetings
  http.get(`${API_BASE}/oneononemeetings`, ({ request }) => {
    const url = new URL(request.url)
    const pageNumber = parseInt(url.searchParams.get('pageNumber') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    const directReportId = url.searchParams.get('directReportId')
    const unlinked = url.searchParams.get('unlinked') === 'true'
    const search = url.searchParams.get('search')?.toLowerCase()

    let matching = [...mockMeetings]
    if (unlinked) {
      matching = matching.filter(m => m.isUnlinked)
    } else if (directReportId) {
      matching = matching.filter(m => m.directReportId === directReportId)
    }
    if (search) {
      matching = matching.filter(m =>
        m.title.toLowerCase().includes(search) || m.content.toLowerCase().includes(search)
      )
    }

    const totalCount = matching.length
    const totalPages = Math.ceil(totalCount / pageSize)
    return HttpResponse.json({
      items: matching.slice((pageNumber - 1) * pageSize, pageNumber * pageSize),
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber < totalPages
    })
  }),

  http.get(`${API_BASE}/oneononemeetings/count`, () => {
    return HttpResponse.json(mockMeetings.length)
  }),

  http.get(`${API_BASE}/oneononemeetings/counts`, () => {
    const counts = new Map<string | null, number>()
    mockMeetings.forEach(m => {
      const key = m.directReportId ?? null
      counts.set(key, (counts.get(key) ?? 0) + 1)
    })
    return HttpResponse.json(
      Array.from(counts.entries()).map(([directReportId, count]) => ({ directReportId, count }))
    )
  }),

  http.post(`${API_BASE}/oneononemeetings/blank`, async ({ request }) => {
    const data = await request.json() as { tags?: string | null }
    const tags = data?.tags ?? ''
    const linked = mockDirectReports.find(r => tags.includes(r.lastName.toLowerCase()) || tags.includes(r.firstName.toLowerCase()))
    const meeting = createMeeting({
      id: `m-${mockMeetings.length + 1}`,
      title: 'New 1:1',
      content: '',
      snippet: '',
      tags,
      tagsList: tags ? [tags] : [],
      directReportId: linked?.id ?? null,
      directReportName: linked?.fullName ?? null,
      isUnlinked: !linked,
    })
    mockMeetings = [meeting, ...mockMeetings]
    return HttpResponse.json(meeting, { status: 201 })
  }),

  http.put(`${API_BASE}/oneononemeetings/:id/content`, async ({ params, request }) => {
    const { content } = await request.json() as { content: string }
    const meeting = mockMeetings.find(m => m.id === params.id)
    if (!meeting) return new HttpResponse(null, { status: 404 })
    meeting.content = content
    meeting.title = content.split('\n').find(line => line.trim()) ?? 'New 1:1'
    return HttpResponse.json(meeting)
  }),

  http.put(`${API_BASE}/oneononemeetings/:id/tags`, async ({ params, request }) => {
    const { tags } = await request.json() as { tags: string }
    const meeting = mockMeetings.find(m => m.id === params.id)
    if (!meeting) return new HttpResponse(null, { status: 404 })
    const linked = mockDirectReports.find(r => tags.includes(r.lastName.toLowerCase()) || tags.includes(r.firstName.toLowerCase()))
    meeting.tags = tags
    meeting.tagsList = tags ? tags.split(',').map(t => t.trim()) : []
    meeting.directReportId = linked?.id ?? null
    meeting.directReportName = linked?.fullName ?? null
    meeting.isUnlinked = !linked
    return HttpResponse.json(meeting)
  }),

  http.put(`${API_BASE}/oneononemeetings/:id/date`, async ({ params, request }) => {
    const { meetingDate } = await request.json() as { meetingDate: string }
    const meeting = mockMeetings.find(m => m.id === params.id)
    if (!meeting) return new HttpResponse(null, { status: 404 })
    meeting.meetingDate = meetingDate
    return HttpResponse.json(meeting)
  }),

  http.delete(`${API_BASE}/oneononemeetings/:id`, ({ params }) => {
    mockMeetings = mockMeetings.filter(m => m.id !== params.id)
    return new HttpResponse(null, { status: 204 })
  }),

  // Note folders
  http.get(`${API_BASE}/notefolders`, () => {
    return HttpResponse.json(mockNoteFolders)
  }),

  http.post(`${API_BASE}/notefolders`, async ({ request }) => {
    const data = await request.json() as { name: string; parentFolderId?: string | null }
    const folder = createNoteFolder({
      id: `f${mockNoteFolders.length + 1}`,
      name: data.name,
      parentFolderId: data.parentFolderId ?? null,
    })
    mockNoteFolders.push(folder)
    return HttpResponse.json(folder, { status: 201 })
  }),

  http.put(`${API_BASE}/notefolders/:id`, async ({ params, request }) => {
    const data = await request.json() as { name: string }
    const folder = mockNoteFolders.find(f => f.id === params.id)
    if (!folder) return new HttpResponse(null, { status: 404 })
    folder.name = data.name
    return HttpResponse.json(folder)
  }),

  http.delete(`${API_BASE}/notefolders/:id`, ({ params }) => {
    mockNoteFolders = mockNoteFolders.filter(f => f.id !== params.id)
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

  // No sprints yet unless a test says otherwise
  http.get(`${API_BASE}/sprints/current`, () =>
    HttpResponse.json({ message: 'There are no sprints yet.' }, { status: 404 })
  ),

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

  http.get(`${API_BASE}/projects`, () => {
    return HttpResponse.json(mockProjects)
  }),

  http.get(`${API_BASE}/products`, () => {
    return HttpResponse.json(mockProducts)
  }),

  http.get(`${API_BASE}/reports/team-velocity`, () => {
    return HttpResponse.json({
      sprints: [],
      averageVelocity: 0,
      totalStoryPointsCompleted: 0,
      completionTrend: 0,
    })
  }),

  http.get(`${API_BASE}/reports/carry-over`, () => {
    return HttpResponse.json({ sprints: [], averageCarriedInPercent: 0 })
  }),

  http.get(`${API_BASE}/reports/bug-support-inflow`, () => {
    return HttpResponse.json({
      sprints: [],
      averageBugsPerSprint: 0,
      averageSupportPerSprint: 0,
      averageMaintenancePerSprint: 0,
      inflowPercent: 0,
      maintenanceLabels: [],
    })
  }),

  http.get(`${API_BASE}/reports/capacity-analysis`, () => {
    return HttpResponse.json({
      pastSprints: [],
      currentSprint: null,
      futureSprints: [],
      averageUtilization: 0,
      totalCommittedPoints: 0,
      totalCompletedPoints: 0,
    })
  }),

  // Dashboard
  http.get(`${API_BASE}/reports/dashboard`, () => {
    return HttpResponse.json(createDashboardOverview())
  }),

  http.get(`${API_BASE}/reports/focus`, () => {
    return HttpResponse.json(createDashboardFocus())
  }),

  http.get(`${API_BASE}/reports/product-engagement`, ({ request }) => {
    const months = new URL(request.url).searchParams.get('months')
    return HttpResponse.json(createProductEngagement({ months: months ? Number(months) : null }))
  }),

  // Health check
  http.get(`${API_BASE}/health`, () => {
    return HttpResponse.json({ status: 'healthy' })
  }),

  // Jira sync
  http.post(`${API_BASE}/jira-sync`, () => {
    return HttpResponse.json({
      sprintDates: {
        jiraSprintCount: 0, updated: [], unchangedCount: 0, missingDatesInJira: [],
        notFoundInJira: [], ambiguousInJira: [], errors: [], syncedAt: new Date().toISOString(),
      },
      parents: {
        jiraParentCount: 0, created: [], updated: [], unchangedCount: 0, removed: [], errors: [],
        syncedAt: new Date().toISOString(),
      },
      tasks: {
        jiraTaskCount: 0, created: [], updated: [], unchangedCount: 0, removed: [], outOfScopeCount: 0,
        sprintsCreated: [], errors: [], parents: null, syncedAt: new Date().toISOString(),
      },
    })
  }),

  http.post(`${API_BASE}/jira-sync/tasks`, () => {
    return HttpResponse.json({
      jiraTaskCount: 0, created: [], updated: [], unchangedCount: 0, removed: [], outOfScopeCount: 0,
      sprintsCreated: [], errors: [], parents: null, syncedAt: new Date().toISOString(),
    })
  }),

  http.post(`${API_BASE}/jira-sync/parents`, () => {
    return HttpResponse.json({
      jiraParentCount: 0, created: [], updated: [], unchangedCount: 0, removed: [], errors: [],
      syncedAt: new Date().toISOString(),
    })
  }),

  http.post(`${API_BASE}/jira-sync/sprint-dates`, () => {
    return HttpResponse.json({
      jiraSprintCount: 0,
      updated: [],
      unchangedCount: 0,
      missingDatesInJira: [],
      notFoundInJira: [],
      ambiguousInJira: [],
      errors: [],
      syncedAt: new Date().toISOString(),
    })
  }),
]

// Helper to reset mock data between tests
export function resetMockData() {
  mockDirectReports = [
    createDirectReport({ id: '1', firstName: 'John', lastName: 'Doe' }),
    createDirectReport({ id: '2', firstName: 'Jane', lastName: 'Smith', email: 'jane.smith@example.com' }),
  ]
  mockNotes = [
    createManagerNote({ id: '1', title: 'First Note', folderId: 'f1' }),
    createManagerNote({ id: '2', title: 'Second Note' }),
  ]
  mockNoteFolders = [
    createNoteFolder({ id: 'f1', name: 'Work', noteCount: 1 }),
  ]
  mockMeetings = [
    createMeeting({ id: 'm1', title: 'Weekly sync', directReportId: '1', directReportName: 'John Doe', tags: 'johndoe', tagsList: ['johndoe'] }),
    createMeeting({
      id: 'm2',
      title: 'Career chat',
      content: 'Career chat\n\n- Wants to move towards staff',
      snippet: '- Wants to move towards staff',
      meetingDate: '2026-09-03',
      directReportId: null,
      directReportName: null,
      isUnlinked: true,
      tags: '',
      tagsList: [],
    }),
  ]
  mockLeaves = [
    createLeave({ id: '1' }),
    createLeave({ id: '2', directReportId: '2', directReportName: 'Jane Smith', type: 'Sick' }),
  ]
  mockTasks = [
    createTeamTask({ id: '1', title: 'Task 1' }),
    createTeamTask({ id: '2', title: 'Task 2', status: 8, statusName: 'Done' }),
  ]
  mockProjects = [
    createProject({ id: 'p1', name: 'Apollo', products: [{ id: 'pr1', name: 'Matching' }] }),
    createProject({ id: 'p2', name: 'Borealis' }),
  ]
  mockProducts = [
    createProduct({ id: 'pr1', name: 'Matching', projects: [{ id: 'p1', name: 'Apollo', url: '' }] }),
    createProduct({ id: 'pr2', name: 'Enrichment', jiraComponentId: '10043' }),
  ]
}

// Quarterly planning
export const createQuarter = (overrides?: Partial<Quarter>): Quarter => ({
  id: 'q4',
  year: 2026,
  quarterNumber: 4,
  name: 'Q4 2026',
  status: 0,
  okrReference: '',
  productRoadmapPercent: 50,
  techRoadmapPercent: 20,
  maintenancePercent: 30,
  committedAt: null,
  createdAt: '2026-09-01T10:00:00Z',
  ...overrides,
})

export const createInitiative = (overrides?: Partial<Initiative>): Initiative => ({
  id: 'opt-1',
  quarterId: 'q4',
  name: 'Euterpe Integration',
  description: '',
  color: '#8B5CF6',
  tshirtSize: 'L',
  url: '',
  workType: 1,
  kind: 0,
  status: 2,
  rank: 1,
  readiness: 0,
  decisionNote: '',
  earliestSprintId: null,
  arrivedOn: null,
  dueDate: null,
  effort: 2,
  allocatedEffort: 0,
  forecast: { estimate: 2, allocated: 0, coveredPercent: 0, deliveredInQuarter: false },
  members: [],
  products: [],
  externalDependencies: [],
  suggestions: [],
  createdAt: '2026-09-01T10:00:00Z',
  ...overrides,
})

export const createPlanningBoard = (overrides?: Partial<PlanningBoard>): PlanningBoard => ({
  quarter: createQuarter(),
  sprints: [
    { id: 's1', name: 'MAR_4Q26_S1', sprintNumber: 1, startDate: '2026-10-05', endDate: '2026-10-16', workingDays: 10, notes: '' },
    { id: 's2', name: 'MAR_4Q26_S2', sprintNumber: 2, startDate: '2026-10-19', endDate: '2026-10-30', workingDays: 10, notes: '' },
  ],
  roster: [
    {
      directReportId: 'p1', name: 'Alice Smith', isDirect: true, capacityPercent: 100, totalSupply: 1.5, totalDays: 15,
      sprints: [
        { sprintId: 's1', availablePercent: 50, leaveDays: 5, supply: 0.5, allocatedPercent: 0 },
        { sprintId: 's2', availablePercent: 100, leaveDays: 0, supply: 1, allocatedPercent: 0 },
      ],
    },
  ],
  initiatives: [
    createInitiative(),
    createInitiative({ id: 'lane-m', name: 'Maintenance', kind: 2, status: 3, workType: 0, color: '#EF4444', rank: 0, effort: null, forecast: null }),
  ],
  dependencies: [],
  allocations: [],
  capacity: {
    totalSupply: 1.5,
    totalDays: 15,
    totalAllocated: 0,
    reservations: [{ workType: 0, percent: 30, effort: 0.45 }, { workType: 2, percent: 20, effort: 0.3 }],
    optionCapacity: 0.75,
    sprints: [
      { sprintId: 's1', supply: 0.5, fullSupply: 1, allocated: 0, mix: { productRoadmap: 0, techRoadmap: 0, maintenance: 0 } },
      { sprintId: 's2', supply: 1, fullSupply: 1, allocated: 0, mix: { productRoadmap: 0, techRoadmap: 0, maintenance: 0 } },
    ],
  },
  waterline: {
    capacity: 0.75,
    committedEffort: 0,
    rows: [{ initiativeId: 'opt-1', name: 'Euterpe Integration', color: '#8B5CF6', kind: 0, status: 2, rank: 1, tshirtSize: 'L', effort: 2, cumulative: 2, aboveLine: false }],
  },
  mix: {
    target: { productRoadmap: 50, techRoadmap: 20, maintenance: 30 },
    planned: { productRoadmap: 0, techRoadmap: 0, maintenance: 0 },
    hasPlan: false,
    previousQuarter: null,
    recentUnplannedPercent: 40,
    recentTaskCount: 120,
    pastWork: { productRoadmap: 40, techRoadmap: 20, maintenance: 40 },
    pastWorkStoryPoints: 250,
  },
  insights: [],
  checklist: {
    optionCount: 1, inDiscoveryCount: 0, unsizedCount: 0, notReadyCount: 0, openDependencyCount: 0,
    withoutLeadCount: 0, undecidedCount: 1, unallocatedCommittedCount: 0,
    hasSprints: true, hasRoster: true, mixOnTarget: false, committed: false,
  },
  sizeMappings: [{ size: 'M', sprints: 1, label: '' }, { size: 'L', sprints: 2, label: '' }],
  products: [],
  people: [{ id: 'p1', name: 'Alice Smith', isDirect: true }],
  knownTeams: ['CORE'],
  ...overrides,
})

export const createPlanReview = (overrides?: Partial<PlanReview>): PlanReview => ({
  committedAt: null,
  options: [],
  summary: 'Q4 2026 plan, not committed yet',
  ...overrides,
})

