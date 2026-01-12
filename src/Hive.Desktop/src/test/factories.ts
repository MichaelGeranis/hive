import type {
  DirectReport,
  ManagerNote,
  Leave,
  TeamTask,
  OneOnOneMeeting,
  MeetingNote,
  Project,
  PerformanceReview,
  Sprint,
  SprintCapacity,
  NotePriority,
  TaskPriority,
  TaskStatus,
  TaskType,
  PerformanceRating,
  ReviewStatus,
  NoteCategory,
  ActionItemStatus,
} from '../types'

let idCounter = 0

function generateId(): string {
  return `test-${++idCounter}`
}

export function resetIdCounter(): void {
  idCounter = 0
}

// Direct Report Factory
export function createDirectReport(overrides?: Partial<DirectReport>): DirectReport {
  const id = overrides?.id ?? generateId()
  const firstName = overrides?.firstName ?? 'John'
  const lastName = overrides?.lastName ?? 'Doe'

  return {
    id,
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    email: `${firstName.toLowerCase()}.${lastName.toLowerCase()}@example.com`,
    jobTitle: 'Software Engineer',
    department: 'Engineering',
    hireDate: '2023-01-15',
    isDirect: true,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Manager Note Factory
export function createManagerNote(overrides?: Partial<ManagerNote>): ManagerNote {
  const tags = overrides?.tags ?? 'test'
  return {
    id: overrides?.id ?? generateId(),
    title: 'Test Note',
    content: 'Test content',
    tags,
    tagsList: tags.split(',').map(t => t.trim()).filter(Boolean),
    priority: 1 as NotePriority,
    priorityName: 'Normal',
    isCompleted: false,
    isOverdue: false,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Leave Factory
export function createLeave(overrides?: Partial<Leave>): Leave {
  return {
    id: overrides?.id ?? generateId(),
    directReportId: '1',
    directReportName: 'John Doe',
    type: 'Vacation',
    startDate: '2024-01-15',
    endDate: '2024-01-19',
    daysCount: 5,
    businessDaysCount: 5,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Task Factory
export function createTeamTask(overrides?: Partial<TeamTask>): TeamTask {
  return {
    id: overrides?.id ?? generateId(),
    title: 'Test Task',
    description: 'Test task description',
    type: 0 as TaskType,
    typeName: 'Task',
    priority: 1 as TaskPriority,
    priorityName: 'Medium',
    status: 1 as TaskStatus,
    statusName: 'Todo',
    tags: '',
    labels: '',
    sprint: '',
    isOverdue: false,
    ...overrides,
  }
}

// Project Factory
export function createProject(overrides?: Partial<Project>): Project {
  return {
    id: overrides?.id ?? generateId(),
    name: 'Test Project',
    description: 'Test project description',
    labels: '',
    url: '',
    totalTasks: 0,
    completedTasks: 0,
    openTasks: 0,
    parentCount: 0,
    ...overrides,
  }
}

// Meeting Factory
export function createMeeting(overrides?: Partial<OneOnOneMeeting>): OneOnOneMeeting {
  return {
    id: overrides?.id ?? generateId(),
    directReportId: '1',
    directReportName: 'John Doe',
    meetingDate: new Date().toISOString(),
    durationMinutes: 30,
    location: 'Office',
    agenda: 'Weekly sync',
    noteCount: 0,
    openActionItemCount: 0,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Meeting Note Factory
export function createMeetingNote(overrides?: Partial<MeetingNote>): MeetingNote {
  return {
    id: overrides?.id ?? generateId(),
    meetingId: '1',
    meetingDate: new Date().toISOString(),
    directReportName: 'John Doe',
    content: 'Test note content',
    category: 0 as NoteCategory,
    categoryName: 'Discussion',
    isPrivate: false,
    isOverdue: false,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Performance Review Factory
export function createPerformanceReview(overrides?: Partial<PerformanceReview>): PerformanceReview {
  return {
    id: overrides?.id ?? generateId(),
    directReportId: '1',
    directReportName: 'John Doe',
    reviewPeriod: '2024',
    reviewDate: new Date().toISOString(),
    rating: 2 as PerformanceRating,
    ratingDescription: 'Meets Expectations',
    status: 0 as ReviewStatus,
    statusDescription: 'Draft',
    strengths: 'Great teamwork',
    areasForImprovement: 'Communication',
    goalsForNextPeriod: 'Lead a project',
    managerNotes: '',
    employeeSelfAssessment: '',
    ...overrides,
  }
}

// Sprint Factory
export function createSprint(overrides?: Partial<Sprint>): Sprint {
  return {
    id: overrides?.id ?? generateId(),
    name: 'LP_1Q24_S1',
    teamName: 'LP',
    quarter: 1,
    year: 2024,
    sprintNumber: 1,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Sprint Capacity Factory
export function createSprintCapacity(overrides?: Partial<SprintCapacity>): SprintCapacity {
  return {
    id: overrides?.id ?? generateId(),
    sprintId: '1',
    sprintName: 'LP_1Q24_S1',
    totalCapacityPoints: 50,
    availableMembers: 5,
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

// Action Item Factory (for meeting notes with action items)
export function createActionItem(overrides?: Partial<MeetingNote>): MeetingNote {
  return createMeetingNote({
    category: 1 as NoteCategory,
    categoryName: 'ActionItem',
    actionStatus: 0 as ActionItemStatus,
    actionStatusName: 'Open',
    actionDueDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    ...overrides,
  })
}

// Helper to create multiple items
export function createMany<T>(
  factory: (overrides?: Partial<T>) => T,
  count: number,
  overrides?: Partial<T>[]
): T[] {
  return Array.from({ length: count }, (_, i) =>
    factory(overrides?.[i])
  )
}

// Date helpers for tests
export function getToday(): string {
  return new Date().toISOString().split('T')[0]
}

export function getDaysFromNow(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString().split('T')[0]
}

export function getDaysAgo(days: number): string {
  return getDaysFromNow(-days)
}
