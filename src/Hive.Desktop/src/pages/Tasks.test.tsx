import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, userEvent, waitFor } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData, createTeamTask } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import Tasks from './Tasks'

const API_BASE = 'http://localhost:5002/api'

const EMPTY_SUMMARY = {
  totalTasks: 0, backlogTasks: 0, todoTasks: 0, blockedTasks: 0, inProgressTasks: 0, inReviewTasks: 0,
  inTestTasks: 0, poAcceptanceTasks: 0, readyToReleaseTasks: 0, doneTasks: 0, cancelledTasks: 0,
  overdueTasks: 0, staleTasks: 0, staleTaskDays: 5, unassignedTasks: 0, allLabels: [], allSprints: []
}

const NO_PARENT_CHANGES = {
  jiraParentCount: 60, created: [], updated: [], unchangedCount: 60, removed: [], errors: [],
  syncedAt: new Date().toISOString()
}

function renderTasks() {
  server.use(
    http.get(`${API_BASE}/teamtasks/summary`, () => HttpResponse.json(EMPTY_SUMMARY)),
    http.get(`${API_BASE}/settings`, () => HttpResponse.json({ jiraBrowseUrl: null, hasJiraApiToken: true }))
  )
  return render(
    <ToastProvider>
      <Tasks />
    </ToastProvider>
  )
}

describe('Tasks: syncing from Jira', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should sync tasks and report what changed', async () => {
    let everythingSynced = false
    server.use(
      http.post(`${API_BASE}/jira-sync/tasks`, () =>
        HttpResponse.json({
          jiraTaskCount: 291,
          created: [{ taskId: 't1', jiraKey: 'MAR-600', title: 'New work' }],
          updated: [{ taskId: 't2', jiraKey: 'MAR-10', title: 'Changed work' }],
          unchangedCount: 288,
          removed: [{ taskId: 't3', jiraKey: 'MAR-436', title: 'Moved to backlog' }],
          outOfScopeCount: 61,
          sprintsCreated: [],
          errors: [],
          parents: NO_PARENT_CHANGES,
          syncedAt: new Date().toISOString()
        })
      ),
      http.post(`${API_BASE}/jira-sync`, () => {
        everythingSynced = true
        return HttpResponse.json({})
      })
    )
    const user = userEvent.setup()
    renderTasks()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Tasks synced from Jira: 1 added, 1 updated, 1 removed (291 in sprints)')).toBeInTheDocument()
    // Parents did not change, so they are not mentioned
    expect(screen.queryByText(/^Parents/)).not.toBeInTheDocument()
    expect(everythingSynced).toBe(false)
  })

  it('should mention parents when the sync changed them', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync/tasks`, () =>
        HttpResponse.json({
          jiraTaskCount: 291, created: [], updated: [], unchangedCount: 291, removed: [], outOfScopeCount: 61,
          sprintsCreated: [], errors: [],
          parents: { ...NO_PARENT_CHANGES, created: [{ parentId: 'p1', jiraKey: 'MAR-600', name: 'New epic' }], unchangedCount: 59 },
          syncedAt: new Date().toISOString()
        })
      )
    )
    const user = userEvent.setup()
    renderTasks()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Tasks already match Jira (291 in sprints)')).toBeInTheDocument()
    expect(screen.getByText('Parents synced from Jira: 1 added (60 in Jira)')).toBeInTheDocument()
  })

  it('should keep the CSV import, labelled as such', async () => {
    renderTasks()

    expect(await screen.findByRole('button', { name: /Import CSV/ })).toBeInTheDocument()
  })
})

describe('Tasks: current sprint filter', () => {
  beforeEach(() => {
    resetMockData()
  })

  function withSprints(requested: (string | null)[]) {
    server.use(
      http.get(`${API_BASE}/teamtasks/summary`, () =>
        HttpResponse.json({ ...EMPTY_SUMMARY, allSprints: ['MAR_3Q26_S5', 'MAR_3Q26_S6'] })
      ),
      http.get(`${API_BASE}/settings`, () => HttpResponse.json({ jiraBrowseUrl: null, hasJiraApiToken: true })),
      http.get(`${API_BASE}/sprints/current`, () => HttpResponse.json({ id: 's6', name: 'MAR_3Q26_S6' })),
      http.get(`${API_BASE}/teamtasks`, ({ request }) => {
        requested.push(new URL(request.url).searchParams.get('sprint'))
        return HttpResponse.json({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0, totalStoryPoints: 0 })
      })
    )
  }

  it('should filter to the sprint the team is in today', async () => {
    const requested: (string | null)[] = []
    withSprints(requested)
    const user = userEvent.setup()
    render(<ToastProvider><Tasks /></ToastProvider>)

    const chip = await screen.findByRole('button', { name: 'Current sprint' })
    expect(chip).toHaveAttribute('aria-pressed', 'false')
    await user.click(chip)

    expect(chip).toHaveAttribute('aria-pressed', 'true')
    await waitFor(() => expect(requested.at(-1)).toBe('MAR_3Q26_S6'))
  })

  it('should open on the current sprint when the link asks for it', async () => {
    const requested: (string | null)[] = []
    withSprints(requested)
    render(
      <ToastProvider><Tasks /></ToastProvider>,
      { routerProps: { initialEntries: ['/tasks?sprint=current'] } }
    )

    expect(await screen.findByRole('button', { name: 'Current sprint' })).toHaveAttribute('aria-pressed', 'true')
    expect(requested).toEqual(['MAR_3Q26_S6'])
  })
})

describe('Tasks: dashboard drill-downs', () => {
  beforeEach(() => {
    resetMockData()
  })

  function recordRequests() {
    const requested: URLSearchParams[] = []
    server.use(
      http.get(`${API_BASE}/teamtasks/summary`, () => HttpResponse.json({ ...EMPTY_SUMMARY, staleTasks: 2 })),
      http.get(`${API_BASE}/settings`, () => HttpResponse.json({ jiraBrowseUrl: null, hasJiraApiToken: true })),
      http.get(`${API_BASE}/teamtasks`, ({ request }) => {
        requested.push(new URL(request.url).searchParams)
        return HttpResponse.json({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0, totalStoryPoints: 0 })
      })
    )
    return requested
  }

  it('should open on the filters the dashboard link carries, and clear one', async () => {
    const requested = recordRequests()
    const user = userEvent.setup()
    render(
      <ToastProvider><Tasks /></ToastProvider>,
      { routerProps: { initialEntries: ['/tasks?assignee=unassigned&type=Bug&component=Matching'] } }
    )

    await waitFor(() => expect(requested.length).toBeGreaterThan(0))
    expect(requested[0].get('assignee')).toBe('unassigned')
    expect(requested[0].get('type')).toBe('Bug')
    expect(requested[0].get('component')).toBe('Matching')
    expect(screen.getByText('Unassigned')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Clear Type: Bug' }))

    await waitFor(() => expect(requested.at(-1)!.get('type')).toBeNull())
    expect(requested.at(-1)!.get('assignee')).toBe('unassigned')
  })

  it('should open on the stale filter and say what stale means', async () => {
    const requested = recordRequests()
    render(
      <ToastProvider><Tasks /></ToastProvider>,
      { routerProps: { initialEntries: ['/tasks?filter=stale'] } }
    )

    await waitFor(() => expect(requested.length).toBeGreaterThan(0))
    expect(requested[0].get('filter')).toBe('stale')
    expect(await screen.findByRole('button', { name: 'Stale (2)' })).toHaveClass('bg-amber-500')
    expect(screen.getByText(/no status change for 5\+ days/)).toBeInTheDocument()
  })
})

describe('Tasks: task readiness', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should open on the upcoming tasks without acceptance criteria, and on a search', async () => {
    const requested: URLSearchParams[] = []
    server.use(
      http.get(`${API_BASE}/teamtasks/summary`, () => HttpResponse.json(EMPTY_SUMMARY)),
      http.get(`${API_BASE}/settings`, () => HttpResponse.json({ jiraBrowseUrl: null, hasJiraApiToken: true })),
      http.get(`${API_BASE}/teamtasks`, ({ request }) => {
        requested.push(new URL(request.url).searchParams)
        return HttpResponse.json({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0, totalStoryPoints: 0 })
      })
    )
    render(
      <ToastProvider><Tasks /></ToastProvider>,
      { routerProps: { initialEntries: ['/tasks?noAcceptanceCriteria=true&search=MAR-12'] } }
    )

    await waitFor(() => expect(requested.length).toBeGreaterThan(0))
    expect(requested[0].get('noAcceptanceCriteria')).toBe('true')
    expect(requested[0].get('search')).toBe('MAR-12')
    expect(screen.getByText('Upcoming, no acceptance criteria')).toBeInTheDocument()
  })

  it('should mark a task the AI has reviewed with the bee badge', async () => {
    server.use(
      http.get(`${API_BASE}/teamtasks`, () => HttpResponse.json({
        items: [
          createTeamTask({
            id: 't1',
            title: 'Save drafts',
            aiReview: {
              verdict: 'NeedsWork', summary: 'Keeps drafts.', missing: ['Acceptance criteria'],
              model: 'llama3.1:8b', analyzedAt: '2026-09-30T08:00:00Z', isUpToDate: true
            }
          }),
          createTeamTask({ id: 't2', title: 'Never reviewed' })
        ],
        totalCount: 2, pageNumber: 1, pageSize: 20, totalPages: 1, totalStoryPoints: 0
      }))
    )
    renderTasks()

    const badge = await screen.findByLabelText('AI review: Needs work')
    expect(badge).toHaveAttribute('title', expect.stringContaining('Missing: Acceptance criteria'))
    expect(screen.getAllByLabelText(/AI review/)).toHaveLength(1)
  })
})
