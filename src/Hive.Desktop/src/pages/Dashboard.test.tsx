import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { Route, Routes, useLocation } from 'react-router-dom'
import { render, screen, waitFor, within, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData, createDashboardOverview, createDashboardFocus, createProductEngagement, createCareerOverview, createRetroSummary, createTaskReadiness } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import type { CurrentSprintFocus, DashboardFocus, DashboardInsights, DashboardWarning, ProductEngagement, TasksByAssignee } from '../types'
import { TaskStatus } from '../types'
import Dashboard from './Dashboard'

const API_BASE = 'http://localhost:5002/api'

const WIDGETS_KEY = 'hive-dashboard-widgets-v2'

const ALL_WIDGETS = [
  'attention', 'currentSprint', 'taskReadiness', 'oneOnOnes', 'todos', 'whosOut', 'tenure',
  'velocity', 'capacity', 'carryOver', 'bugSupportInflow', 'workload', 'workMix', 'productEngagement', 'teamSentiment', 'retroSummary', 'careerPlans'
]

/**
 * Several widgets are charts that measure nothing under jsdom. These tests seed the
 * widget-visibility preference so only the widgets under test are mounted.
 */
function showOnly(...visible: string[]) {
  const widgets = Object.fromEntries(ALL_WIDGETS.map(w => [w, visible.includes(w)]))
  localStorage.setItem(WIDGETS_KEY, JSON.stringify(widgets))
}

/** Overrides the insights block of the dashboard payload, leaving the rest of it alone. */
function withInsights(insights: Partial<DashboardInsights>) {
  const dashboard = createDashboardOverview()
  server.use(
    http.get(`${API_BASE}/reports/dashboard`, () =>
      HttpResponse.json({
        ...dashboard,
        insights: { ...dashboard.insights, ...insights }
      })
    )
  )
}

/** Serves the product engagement widget, recording the period of every request it makes. */
function withEngagement(engagement: Partial<ProductEngagement>) {
  const requestedMonths: (string | null)[] = []
  server.use(
    http.get(`${API_BASE}/reports/product-engagement`, ({ request }) => {
      requestedMonths.push(new URL(request.url).searchParams.get('months'))
      return HttpResponse.json(createProductEngagement(engagement))
    })
  )
  return requestedMonths
}

function withFocus(focus: Partial<DashboardFocus>) {
  server.use(http.get(`${API_BASE}/reports/focus`, () => HttpResponse.json(createDashboardFocus(focus))))
}

/** Shows the path the dashboard navigated to, so a click can be followed. */
function Destination() {
  const location = useLocation()
  return <p>Now at {location.pathname}{location.search}</p>
}

function renderDashboard() {
  return render(
    <ToastProvider>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="*" element={<Destination />} />
      </Routes>
    </ToastProvider>
  )
}

const TWO_WARNINGS: DashboardWarning[] = [
  { category: 'OneOnOnes', title: '1 overdue 1:1', detail: 'John Doe' },
  { category: 'KnowledgeSilos', title: '1 product is a knowledge silo', detail: 'Matching' }
]

const SPRINT: CurrentSprintFocus = {
  sprintId: 's1',
  name: 'LP_3Q26_S5',
  startDate: '2026-09-14',
  endDate: '2026-09-25',
  isActive: true,
  workingDays: 10,
  workingDaysLeft: 2,
  elapsedPercent: 80,
  committedPoints: 20,
  newPoints: 24,
  newCompletedPoints: 9,
  carriedOverPoints: 3,
  completedPercent: 38,
  pace: 'Behind',
  scopeCreepPoints: 4,
  totalTasks: 8,
  doneTasks: 3,
  byStatus: [
    { status: TaskStatus.Blocked, statusName: 'Blocked', taskCount: 1, storyPoints: 5 },
    { status: TaskStatus.Done, statusName: 'Done', taskCount: 3, storyPoints: 9 }
  ],
  blockedTasks: [
    { id: 't1', title: 'Payments API timeout', jiraKey: 'MAR-7', jiraUrl: 'https://team.atlassian.net/browse/MAR-7', assigneeName: 'John Doe', storyPoints: 5 }
  ]
}

describe('Dashboard needs attention', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should list every warning the backend reported', async () => {
    showOnly('attention')
    withInsights({ warnings: TWO_WARNINGS })

    renderDashboard()

    expect(await screen.findByText('1 overdue 1:1')).toBeInTheDocument()
    expect(screen.getByText('1 product is a knowledge silo')).toBeInTheDocument()
    expect(screen.getByLabelText('2 warnings')).toBeInTheDocument()
  })

  it('should count the same warnings whether or not the delivery widgets are visible', async () => {
    // Velocity and capacity are the widgets whose data the page used to derive warnings
    // from, so hiding them used to lower the count.
    showOnly('attention')
    withInsights({ warnings: TWO_WARNINGS })
    const hidden = renderDashboard()
    expect(await screen.findByLabelText('2 warnings')).toBeInTheDocument()
    hidden.unmount()

    showOnly('attention', 'velocity', 'capacity')
    withInsights({ warnings: TWO_WARNINGS })
    renderDashboard()

    expect(await screen.findByLabelText('2 warnings')).toBeInTheDocument()
  })

  it('should open the page that deals with a warning', async () => {
    const user = userEvent.setup()
    showOnly('attention')
    withInsights({ warnings: TWO_WARNINGS })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /1 overdue 1:1/ }))

    expect(await screen.findByText('Now at /meetings')).toBeInTheDocument()
  })

  it('should send overdue tasks to the overdue filter on Tasks', async () => {
    const user = userEvent.setup()
    showOnly('attention')
    withInsights({ warnings: [{ category: 'OverdueTasks', title: '1 overdue task', detail: 'Fix login (due 20/09/2026)' }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /1 overdue task/ }))

    expect(await screen.findByText('Now at /tasks?filter=overdue')).toBeInTheDocument()
  })

  it('should send tasks without story points to that filter on Tasks', async () => {
    const user = userEvent.setup()
    showOnly('attention')
    withInsights({ warnings: [{ category: 'UnestimatedTasks', title: '2 tasks have no story points', detail: 'Fix login (Story)' }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /2 tasks have no story points/ }))

    expect(await screen.findByText('Now at /tasks?unestimated=true')).toBeInTheDocument()
  })

  it('should say all is clear when there are no warnings', async () => {
    showOnly('attention')
    withInsights({ warnings: [] })

    renderDashboard()

    expect(await screen.findByText('All clear')).toBeInTheDocument()
    expect(screen.getByLabelText('0 warnings')).toBeInTheDocument()
  })
})

describe('Dashboard current sprint', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should show progress against time, scope and blocked work', async () => {
    showOnly('currentSprint')
    withFocus({ currentSprint: SPRINT })

    renderDashboard()

    expect(await screen.findByText('LP_3Q26_S5')).toBeInTheDocument()
    expect(screen.getByText('14/09/2026 – 25/09/2026 · 2 of 10 working days left')).toBeInTheDocument()
    expect(screen.getByText('Behind pace')).toBeInTheDocument()
    expect(screen.getByRole('progressbar', { name: 'Sprint progress' })).toHaveAttribute('aria-valuenow', '38')
    expect(screen.getByText('+4 SP scope creep')).toBeInTheDocument()
    expect(screen.getByText('3 SP carried over')).toBeInTheDocument()
    expect(screen.getByText('Payments API timeout')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /MAR-7/ })).toHaveAttribute('href', 'https://team.atlassian.net/browse/MAR-7')
  })

  it('should always count blocked work, even when nothing is blocked', async () => {
    showOnly('currentSprint')
    withFocus({
      currentSprint: {
        ...SPRINT,
        byStatus: [
          { status: TaskStatus.Blocked, statusName: 'Blocked', taskCount: 0, storyPoints: 0 },
          { status: TaskStatus.Done, statusName: 'Done', taskCount: 3, storyPoints: 9 }
        ],
        blockedTasks: []
      }
    })

    renderDashboard()

    const blocked = (await screen.findByText('Blocked')).closest('span')!
    expect(within(blocked).getByText('0')).toBeInTheDocument()
  })

  it('should open the current sprint\'s tasks', async () => {
    const user = userEvent.setup()
    showOnly('currentSprint')
    withFocus({ currentSprint: SPRINT })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: 'View tasks' }))

    expect(await screen.findByText('Now at /tasks?sprint=current')).toBeInTheDocument()
  })

  it('should point to Sprints when there is no sprint yet', async () => {
    showOnly('currentSprint')
    withFocus({ currentSprint: null })

    renderDashboard()

    expect(await screen.findByText(/No sprints yet/)).toBeInTheDocument()
  })

  it('should offer a retry when the focus panel fails to load', async () => {
    showOnly('currentSprint')
    server.use(http.get(`${API_BASE}/reports/focus`, () => HttpResponse.json({}, { status: 500 })))

    renderDashboard()

    expect(await screen.findByText('Could not load current sprint.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Retry/ })).toBeInTheDocument()
  })
})

describe('Dashboard people', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should list 1:1s that are due and fold away the ones on track', async () => {
    const user = userEvent.setup()
    showOnly('oneOnOnes')
    withFocus({
      oneOnOnes: {
        cadenceDays: 14,
        needsAttentionCount: 1,
        people: [
          { directReportId: '2', fullName: 'Jane Smith', lastMeetingDate: '2026-09-01', daysSinceLastMeeting: 23, daysUntilDue: -9, status: 'Overdue', isOnLeave: false },
          { directReportId: '1', fullName: 'John Doe', lastMeetingDate: '2026-09-20', daysSinceLastMeeting: 4, daysUntilDue: 10, status: 'OnTrack', isOnLeave: false }
        ]
      }
    })

    renderDashboard()

    expect(await screen.findByText('Every 14 days')).toBeInTheDocument()
    expect(screen.getByText('Overdue 9d')).toBeInTheDocument()
    expect(screen.getByText('Last 01/09/2026')).toBeInTheDocument()
    expect(screen.queryByText('John Doe')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /1 on track/ }))
    expect(screen.getByText('John Doe')).toBeInTheDocument()
  })

  it('should open a person\'s 1:1s', async () => {
    const user = userEvent.setup()
    showOnly('oneOnOnes')
    withFocus({
      oneOnOnes: {
        cadenceDays: 14,
        needsAttentionCount: 1,
        people: [
          { directReportId: '2', fullName: 'Jane Smith', lastMeetingDate: null, daysSinceLastMeeting: null, daysUntilDue: null, status: 'NeverMet', isOnLeave: false }
        ]
      }
    })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Jane Smith/ }))

    expect(await screen.findByText('Now at /meetings?person=2')).toBeInTheDocument()
  })

  it('should list the hot items of the latest journal entry, each opening the note', async () => {
    const user = userEvent.setup()
    showOnly('todos')
    withFocus({
      todos: {
        hasJournal: true,
        noteId: 'j1',
        noteTitle: '2026-09-28',
        items: ['Write a downtime document', 'Check the review template']
      }
    })

    renderDashboard()

    expect(await screen.findByText('Hot in 2026-09-28')).toBeInTheDocument()
    expect(screen.getByText('Check the review template')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Write a downtime document' }))

    expect(await screen.findByText('Now at /notes?note=j1')).toBeInTheDocument()
  })

  it('should say when there is no Journal folder to read to-dos from', async () => {
    showOnly('todos')
    withFocus({ todos: { hasJournal: false, noteId: null, noteTitle: null, items: [] } })

    renderDashboard()

    expect(await screen.findByText(/Add a Journal folder in Notes/)).toBeInTheDocument()
  })

  it('should show who is out today and who is going away', async () => {
    showOnly('whosOut')
    withFocus({
      leave: {
        lookAheadDays: 14,
        outToday: [
          { leaveId: 'l1', directReportId: '1', fullName: 'John Doe', type: 'Sick', startDate: '2026-09-24', endDate: '2026-09-24', businessDays: 1 }
        ],
        upcoming: [
          { leaveId: 'l2', directReportId: '2', fullName: 'Jane Smith', type: 'Vacation', startDate: '2026-09-29', endDate: '2026-10-02', businessDays: 4 }
        ]
      }
    })

    renderDashboard()

    expect(await screen.findByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('Sick · 1d')).toBeInTheDocument()
    expect(screen.getByText('29/09/2026 – 02/10/2026')).toBeInTheDocument()
  })
})

describe('Dashboard product engagement', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should show product engagement under Delivery, not Team health', async () => {
    showOnly('productEngagement')
    renderDashboard()

    expect(await screen.findByText('Delivery')).toBeInTheDocument()
    expect(screen.queryByText('Team health')).not.toBeInTheDocument()
  })

  it('should render member engagement from the backend distribution', async () => {
    const user = userEvent.setup()
    showOnly('productEngagement')
    withEngagement({
      productsByMember: [
        { directReportId: '1', memberName: 'John Doe', productCount: 3, productIds: ['pr1', 'pr2'], isNarrow: false },
        { directReportId: '2', memberName: 'Jane Smith', productCount: 1, productIds: ['pr1'], isNarrow: true }
      ]
    })
    renderDashboard()

    await user.click(await screen.findByRole('tab', { name: 'By person' }))

    // The counts come from the backend, not from a page of tasks fetched by the client.
    const john = (await screen.findByText('John Doe')).closest('li')!
    expect(within(john).getByText('3')).toBeInTheDocument()
    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
  })

  it('should describe silos using the configured minimum member count', async () => {
    showOnly('productEngagement')
    withEngagement({
      minProductMembers: 3,
      knowledgeSilos: [
        { productId: 'pr1', productName: 'Matching', memberCount: 1, memberNames: ['John Doe'] }
      ],
      membersByProduct: [
        { productId: 'pr1', productName: 'Matching', memberCount: 1, memberNames: ['John Doe'], isSilo: true }
      ]
    })

    renderDashboard()

    expect(
      await screen.findByText(/1 product has fewer than 3 members/)
    ).toBeInTheDocument()
    expect(screen.getByText('Matching')).toBeInTheDocument()
  })

  it('should name the products nobody has worked on', async () => {
    showOnly('productEngagement')
    withEngagement({
      unworkedProducts: [{ productId: 'pr2', productName: 'Orphan' }],
      membersByProduct: [
        { productId: 'pr2', productName: 'Orphan', memberCount: 0, memberNames: [], isSilo: true }
      ]
    })

    renderDashboard()

    expect(await screen.findByText('Nobody has worked on: Orphan')).toBeInTheDocument()
    expect(screen.getByText('nobody')).toBeInTheDocument()
  })

  it('should list the people who have worked on a product when it is clicked', async () => {
    const user = userEvent.setup()
    showOnly('productEngagement')
    withEngagement({
      membersByProduct: [
        { productId: 'pr1', productName: 'Matching', memberCount: 2, memberNames: ['Rena Tsagka', 'Iraklis Tzinos'], isSilo: false }
      ]
    })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Matching/ }))

    const dialog = screen.getByText('Who has worked on Matching').closest('div.fixed') as HTMLElement
    expect(within(dialog).getByText('2 people')).toBeInTheDocument()
    expect(within(dialog).getAllByRole('listitem').map(li => li.textContent)).toEqual(['Iraklis Tzinos', 'Rena Tsagka'])
  })

  it('should name the people who have worked on fewer products than the configured minimum', async () => {
    const user = userEvent.setup()
    showOnly('productEngagement')
    withEngagement({
      minMemberProducts: 3,
      productsByMember: [
        { directReportId: '2', memberName: 'Jane Smith', productCount: 1, productIds: ['pr1'], isNarrow: true }
      ],
      narrowlyEngagedMembers: [{ directReportId: '2', fullName: 'Jane Smith', productCount: 1 }]
    })
    renderDashboard()

    await user.click(await screen.findByRole('tab', { name: 'By person' }))

    expect(await screen.findByText('Worked on fewer than 3 products: Jane Smith')).toBeInTheDocument()
  })

  it('should look back 3 months by default and let the manager choose 6 months or all time', async () => {
    const user = userEvent.setup()
    showOnly('productEngagement')
    const requestedMonths = withEngagement({})
    renderDashboard()

    const period = await screen.findByRole('tablist', { name: 'Engagement period' })
    expect(within(period).getByRole('tab', { name: '3 months' })).toHaveAttribute('aria-selected', 'true')
    await waitFor(() => expect(requestedMonths).toEqual(['3']))

    await user.click(within(period).getByRole('tab', { name: '6 months' }))
    await waitFor(() => expect(requestedMonths).toEqual(['3', '6']))

    await user.click(within(period).getByRole('tab', { name: 'All time' }))
    await waitFor(() => expect(requestedMonths).toEqual(['3', '6', null]))
  })
})

describe('Dashboard capacity', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should predict the next sprint even when Hive does not have it yet', async () => {
    showOnly('capacity')
    server.use(
      http.get(`${API_BASE}/reports/capacity-analysis`, () =>
        HttpResponse.json({
          pastSprints: [],
          currentSprint: {
            sprintId: 's6', sprintName: 'MAR_3Q26_S6', year: 2026, quarter: 3, sprintNumber: 6,
            committedPoints: 0, completedPoints: 12, newCompletedPoints: 12, carriedOverPoints: 0,
            utilizationPercentage: 0, totalStoryPoints: 30, totalTasks: 10, doneTasks: 4, status: 'Current'
          },
          futureSprints: [],
          nextSprint: {
            sprintId: null, sprintName: null, isProjected: true,
            startDate: '2026-09-30', endDate: '2026-10-13',
            teamSize: 5, availableMembers: 4.5, predictedPoints: 27
          },
          averageUtilization: 0,
          totalCommittedPoints: 0,
          totalCompletedPoints: 12
        })
      )
    )

    renderDashboard()

    expect(await screen.findByText('27 SP')).toBeInTheDocument()
    expect(screen.getByText('Projected 30/09/2026 – 13/10/2026 · 4.5 of 5 available')).toBeInTheDocument()
  })
})

describe('Dashboard customize', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should only offer widgets that exist, whatever was stored', async () => {
    const user = userEvent.setup()
    localStorage.setItem(WIDGETS_KEY, JSON.stringify({ attention: true, topStats: true }))
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Customize/ }))

    expect(screen.getByRole('button', { name: 'Needs attention' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.queryByText('Top Stats')).not.toBeInTheDocument()
  })
})

const NO_SPRINT_CHANGES = {
  jiraSprintCount: 0, updated: [], unchangedCount: 0, missingDatesInJira: [],
  notFoundInJira: [], ambiguousInJira: [], errors: [], syncedAt: new Date().toISOString()
}

const NO_PARENT_CHANGES = {
  jiraParentCount: 46, created: [], updated: [], unchangedCount: 46, errors: [], syncedAt: new Date().toISOString()
}

describe('Dashboard Jira sync', () => {
  beforeEach(() => {
    resetMockData()
    showOnly('attention')
  })

  it('should sync everything and report each part', async () => {
    let dashboardLoads = 0
    const dashboard = createDashboardOverview()
    server.use(
      http.get(`${API_BASE}/reports/dashboard`, () => {
        dashboardLoads++
        return HttpResponse.json(dashboard)
      }),
      http.post(`${API_BASE}/jira-sync`, () =>
        HttpResponse.json({
          sprintDates: {
            ...NO_SPRINT_CHANGES,
            jiraSprintCount: 3,
            updated: [
              { sprintId: 's1', name: 'LP_4Q25_S1', startDate: '2025-10-06T00:00:00', endDate: '2025-10-17T00:00:00' },
              { sprintId: 's2', name: 'LP_4Q25_S2', startDate: '2025-10-20T00:00:00', endDate: '2025-10-31T00:00:00' }
            ]
          },
          products: {
            jiraComponentCount: 9, created: [{ productId: 'pr9', jiraComponentId: '10099', name: 'IPI' }],
            updated: [], unchangedCount: 8, removed: [], errors: [], syncedAt: new Date().toISOString()
          },
          parents: {
            ...NO_PARENT_CHANGES,
            created: [{ parentId: 'p1', jiraKey: 'MAR-1', name: 'Payments' }],
            unchangedCount: 45
          }
        })
      )
    )
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Updated dates for 2 sprints from Jira')).toBeInTheDocument()
    expect(screen.getByText('Products synced from Jira: 1 added (9 components in Jira)')).toBeInTheDocument()
    expect(screen.getByText('Parents synced from Jira: 1 added (46 in Jira)')).toBeInTheDocument()
    // The dashboard is refreshed in place, since sprint dates decide which sprint is current
    await waitFor(() => expect(dashboardLoads).toBe(2))
    expect(screen.getByRole('button', { name: /Sync Jira/ })).toBeInTheDocument()
  })

  it('should report upcoming sprints it added from Jira', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync`, () =>
        HttpResponse.json({
          sprintDates: { ...NO_SPRINT_CHANGES, jiraSprintCount: 8, created: ['MAR_4Q26_S1', 'MAR_4Q26_S2'] },
          parents: NO_PARENT_CHANGES
        })
      )
    )
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Sprints synced from Jira: added 2 upcoming sprints')).toBeInTheDocument()
  })

  it('should warn about sprints it could not match', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync`, () =>
        HttpResponse.json({
          sprintDates: {
            ...NO_SPRINT_CHANGES,
            jiraSprintCount: 1,
            missingDatesInJira: ['LP_1Q26_S1'],
            notFoundInJira: ['LP_4Q25_S9', 'LP_4Q25_S10']
          },
          parents: NO_PARENT_CHANGES
        })
      )
    )
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Sprint dates already match Jira')).toBeInTheDocument()
    expect(
      screen.getByText('Sprints skipped: 2 not on the Jira board, 1 without dates in Jira')
    ).toBeInTheDocument()
    expect(screen.getByText('Parents already match Jira (46 in Jira)')).toBeInTheDocument()
  })

  it('should report a part that failed without hiding the others', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync`, () =>
        HttpResponse.json({
          sprintDates: NO_SPRINT_CHANGES,
          parentsError: 'Jira sync is not configured. Add the Jira space key in Settings.'
        })
      )
    )
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(
      await screen.findByText('Parents: Jira sync is not configured. Add the Jira space key in Settings.')
    ).toBeInTheDocument()
    expect(screen.getByText('Sprint dates already match Jira')).toBeInTheDocument()
  })

  it('should show why the sync failed outright', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync`, () =>
        HttpResponse.json({ message: 'Something went wrong' }, { status: 500 })
      )
    )
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Something went wrong')).toBeInTheDocument()
  })
})

describe('Dashboard stale tasks', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should send stale tasks to the stale filter on Tasks', async () => {
    const user = userEvent.setup()
    showOnly('attention')
    withInsights({ warnings: [{ category: 'StaleTasks', title: '2 tasks stuck for 5+ days', detail: 'Fix login (InProgress, 8 days)' }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /2 tasks stuck/ }))

    expect(await screen.findByText('Now at /tasks?filter=stale')).toBeInTheDocument()
  })
})

describe('Dashboard milestones', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should show milestones coming up this week', async () => {
    showOnly('tenure')
    withFocus({
      tenureMilestones: [
        { directReportId: '1', fullName: 'John Doe', hireDate: '2021-09-30', kind: 'Anniversary', years: 5, date: '2026-09-30', daysUntil: 2 },
        { directReportId: '2', fullName: 'Jane Smith', hireDate: '2026-03-28', kind: 'SixMonths', years: 0, date: '2026-09-28', daysUntil: 0 }
      ]
    })

    renderDashboard()

    expect(await screen.findByText('5 years on 30/09/2026 · joined 30/09/2021')).toBeInTheDocument()
    expect(screen.getByText('6 months on 28/09/2026 · joined 28/03/2026')).toBeInTheDocument()
    expect(screen.getByText('Today')).toBeInTheDocument()
    expect(screen.getByText('In 2 days')).toBeInTheDocument()
  })

  it('should hide the tile when nobody has a milestone this week', async () => {
    showOnly('tenure', 'whosOut')
    withFocus({ tenureMilestones: [] })

    renderDashboard()

    expect(await screen.findByText("Who's out")).toBeInTheDocument()
    expect(screen.queryByText('Milestones')).not.toBeInTheDocument()
  })
})

describe('Dashboard sprint trends', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should summarise how much work each sprint inherited', async () => {
    showOnly('carryOver')
    server.use(
      http.get(`${API_BASE}/reports/carry-over`, () => HttpResponse.json({
        averageCarriedInPercent: 22.5,
        sprints: [
          { sprintName: 'LP_3Q26_S4', totalTasks: 10, totalPoints: 20, carriedInTasks: 1, carriedInPoints: 2, newPoints: 18, carriedInPercent: 10, carriedOutTasks: 2, carriedOutPoints: 5 },
          { sprintName: 'LP_3Q26_S5', totalTasks: 12, totalPoints: 25, carriedInTasks: 2, carriedInPoints: 8, newPoints: 17, carriedInPercent: 32, carriedOutTasks: 3, carriedOutPoints: 6 }
        ]
      }))
    )

    renderDashboard()

    expect(await screen.findByText('22.5%')).toBeInTheDocument()
    expect(screen.getByText('32%')).toBeInTheDocument()
    expect(screen.getByText('Carried into LP_3Q26_S5')).toBeInTheDocument()
    expect(screen.getByText('3 tasks')).toBeInTheDocument()
  })

  it('should count bug, support and maintenance tickets without story points', async () => {
    showOnly('bugSupportInflow')
    server.use(
      http.get(`${API_BASE}/reports/bug-support-inflow`, () => HttpResponse.json({
        averageBugsPerSprint: 2.5,
        averageSupportPerSprint: 1.5,
        averageMaintenancePerSprint: 0.5,
        inflowPercent: 40,
        maintenanceLabels: ['maintenance'],
        sprints: [{ sprintName: 'LP_3Q26_S5', newTasks: 10, bugTasks: 2, supportTasks: 1, maintenanceTasks: 1, inflowPercent: 40 }]
      }))
    )

    renderDashboard()

    expect(await screen.findByText('Maintenance / sprint')).toBeInTheDocument()
    expect(screen.getByText('2.5')).toBeInTheDocument()
    expect(screen.getByText('40%')).toBeInTheDocument()
    expect(screen.queryByText(/SP/)).not.toBeInTheDocument()
  })
})

/** Records the sprintCount of every request made to a report endpoint. */
function recordSprintCounts(path: string, body: object) {
  const requested: (string | null)[] = []
  server.use(
    http.get(`${API_BASE}/reports/${path}`, ({ request }) => {
      requested.push(new URL(request.url).searchParams.get('sprintCount'))
      return HttpResponse.json(body)
    })
  )
  return requested
}

const CARRY_OVER = {
  averageCarriedInPercent: 10,
  sprints: [
    { sprintName: 'LP_3Q26_S5', totalTasks: 10, totalPoints: 20, carriedInTasks: 1, carriedInPoints: 2, newPoints: 18, carriedInPercent: 10, carriedOutTasks: 2, carriedOutPoints: 5 }
  ]
}

const INFLOW = {
  averageBugsPerSprint: 1, averageSupportPerSprint: 1, averageMaintenancePerSprint: 0, inflowPercent: 20,
  maintenanceLabels: [],
  sprints: [{ sprintName: 'LP_3Q26_S5', newTasks: 10, bugTasks: 1, supportTasks: 1, maintenanceTasks: 0, inflowPercent: 20 }]
}

describe('Dashboard sprint windows', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should have no page-wide sprint history filter', async () => {
    showOnly('carryOver', 'workload')
    renderDashboard()

    expect(await screen.findByText('Delivery')).toBeInTheDocument()
    expect(screen.queryByText('Sprint history')).not.toBeInTheDocument()
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
  })

  it('should let a sprint-by-sprint widget choose its own sprints, all by default', async () => {
    const user = userEvent.setup()
    showOnly('carryOver')
    const requested = recordSprintCounts('carry-over', CARRY_OVER)
    renderDashboard()

    const sprints = await screen.findByRole('tablist', { name: 'Carry-over trend sprints' })
    expect(within(sprints).getByRole('tab', { name: 'All' })).toHaveAttribute('aria-selected', 'true')
    await waitFor(() => expect(requested).toEqual([null]))

    await user.click(within(sprints).getByRole('tab', { name: 'Last 3' }))
    await waitFor(() => expect(requested).toEqual([null, '3']))

    await user.click(within(sprints).getByRole('tab', { name: 'Current' }))
    await waitFor(() => expect(requested).toEqual([null, '3', '1']))
  })

  it('should change one widget\'s sprints without touching another\'s', async () => {
    const user = userEvent.setup()
    showOnly('carryOver', 'bugSupportInflow')
    const carryOver = recordSprintCounts('carry-over', CARRY_OVER)
    const inflow = recordSprintCounts('bug-support-inflow', INFLOW)
    renderDashboard()

    const sprints = await screen.findByRole('tablist', { name: 'Carry-over trend sprints' })
    await waitFor(() => expect(inflow).toEqual([null]))
    await user.click(within(sprints).getByRole('tab', { name: 'Last 6' }))

    await waitFor(() => expect(carryOver).toEqual([null, '6']))
    expect(inflow).toEqual([null])
    const inflowSprints = screen.getByRole('tablist', { name: 'Bug & support inflow sprints' })
    expect(within(inflowSprints).getByRole('tab', { name: 'All' })).toHaveAttribute('aria-selected', 'true')
  })

  it('should keep the sprints chosen and offer a retry when a widget fails to load', async () => {
    showOnly('carryOver')
    server.use(http.get(`${API_BASE}/reports/carry-over`, () => HttpResponse.json({}, { status: 500 })))
    renderDashboard()

    expect(await screen.findByText('Could not load carry-over trend.')).toBeInTheDocument()
    expect(screen.getByRole('tablist', { name: 'Carry-over trend sprints' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Retry/ })).toBeInTheDocument()
  })

  it('should show workload and work mix over all sprints, and say so', async () => {
    showOnly('workload', 'workMix')
    const requested = recordSprintCounts('dashboard', createDashboardOverview())
    renderDashboard()

    expect(await screen.findAllByText('All sprints')).toHaveLength(2)
    expect(requested).toEqual([null])
  })
})

describe('Dashboard drill-downs', () => {
  beforeEach(() => {
    resetMockData()
  })

  const PERSON: TasksByAssignee = {
    assigneeId: 'dr-1', assigneeName: 'John Doe', totalTasks: 3, backlogTasks: 0, todoTasks: 1, blockedTasks: 0,
    inProgressTasks: 2, inReviewTasks: 0, inTestTasks: 0, poAcceptanceTasks: 0, readyToReleaseTasks: 0,
    completedTasks: 0, cancelledTasks: 0, overdueTasks: 0, completionRate: 0, totalStoryPoints: 5, completedStoryPoints: 0
  }

  function withTasks(tasks: Partial<ReturnType<typeof createDashboardOverview>['tasks']>) {
    const dashboard = createDashboardOverview()
    server.use(
      http.get(`${API_BASE}/reports/dashboard`, () =>
        HttpResponse.json({ ...dashboard, tasks: { ...dashboard.tasks, ...tasks } })
      )
    )
  }

  it('should open a person\'s tasks from the workload', async () => {
    const user = userEvent.setup()
    showOnly('workload')
    withTasks({ tasksByAssignee: [PERSON, { ...PERSON, assigneeId: undefined, assigneeName: 'Unassigned' }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /John Doe/ }))

    expect(await screen.findByText('Now at /tasks?assignee=dr-1')).toBeInTheDocument()
  })

  it('should open the unassigned tasks from the workload', async () => {
    const user = userEvent.setup()
    showOnly('workload')
    withTasks({ tasksByAssignee: [{ ...PERSON, assigneeId: undefined, assigneeName: 'Unassigned' }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Unassigned/ }))

    expect(await screen.findByText('Now at /tasks?assignee=unassigned')).toBeInTheDocument()
  })

  it('should open the tasks of a type from the work mix', async () => {
    const user = userEvent.setup()
    showOnly('workMix')
    withTasks({ tasksByTypeSP: [{ type: 4, typeName: 'Bug', totalStoryPoints: 3, taskCount: 2 }] })
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /Bug/ }))

    expect(await screen.findByText('Now at /tasks?type=Bug')).toBeInTheDocument()
  })

  it('should split the work mix by work category in issues and story points', async () => {
    const user = userEvent.setup()
    showOnly('workMix')
    const share = (categoryName: string, category: number, taskCount: number, storyPoints: number, percentageOfTasks: number, percentageOfStoryPoints: number) =>
      ({ category, categoryName, taskCount, storyPoints, percentageOfTasks, percentageOfStoryPoints })
    withTasks({
      tasksByWorkCategory: [
        share('Productisation', 0, 3, 10, 60, 100),
        share('Tech Maintenance', 1, 2, 0, 40, 0),
        share('Tech Roadmap', 2, 0, 0, 0, 0),
        share('Tech Debt', 3, 0, 0, 0, 0),
        share('System Improvements', 4, 0, 0, 0, 0)
      ]
    })
    renderDashboard()

    await user.click(await screen.findByRole('tab', { name: 'Work category' }))

    // Each measure reads count, bar, percentage: Tech Maintenance has 2 issues (40%) and 0 SP (0%)
    const cells = within(screen.getByRole('row', { name: /Tech Maintenance/ })).getAllByRole('cell').map(c => c.textContent)
    expect(cells).toEqual(['Tech Maintenance', '2', '', '40%', '0', '', '0%'])
    expect(screen.getByRole('row', { name: /Total/ })).toHaveTextContent('Total510')
    expect(screen.queryByRole('tablist', { name: 'Measure' })).not.toBeInTheDocument()
  })

  it('should break the work mix down by type, label, component and work category only', async () => {
    showOnly('workMix')
    renderDashboard()

    expect(await screen.findByRole('tab', { name: 'Type' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Work category' })).toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: 'Support & maintenance' })).not.toBeInTheDocument()
  })

  it('should count support and maintenance per person in tasks, not points, in the workload', async () => {
    const user = userEvent.setup()
    showOnly('workload')
    const dashboard = createDashboardOverview()
    withTasks({
      supportDistribution: {
        ...dashboard.tasks.supportDistribution,
        allTaskCount: 3,
        maintenanceAllTaskCount: 1,
        byAssignee: [{
          assigneeId: 'dr-1', assigneeName: 'John Doe',
          completedStoryPoints: 0, completedTaskCount: 1, allStoryPoints: 0, allTaskCount: 3,
          maintenanceCompletedStoryPoints: 0, maintenanceCompletedTaskCount: 0, maintenanceAllStoryPoints: 0, maintenanceAllTaskCount: 1
        }]
      }
    })
    renderDashboard()

    await user.click(await screen.findByRole('tab', { name: 'Support & maintenance' }))

    expect(screen.getByText('4 tasks')).toBeInTheDocument()
    expect(screen.getByText('3 support · 1 maint.')).toBeInTheDocument()
    expect(screen.queryByText(/SP/)).not.toBeInTheDocument()
  })
})

describe('Dashboard career plans', () => {
  beforeEach(() => {
    resetMockData()
    showOnly('careerPlans')
  })

  it('should list who needs the manager: no plan, nothing in focus, quiet', async () => {
    const user = userEvent.setup()
    server.use(
      http.get(`${API_BASE}/career-plans/overview`, () => HttpResponse.json(createCareerOverview({
        directReportsWithoutPlan: [{ directReportId: '2', fullName: 'Jane Smith', planId: null, lastChangedAt: null }],
        nothingInFocus: [{ directReportId: '1', fullName: 'John Doe', planId: 'cp1', lastChangedAt: '2026-09-01T10:00:00Z' }],
        quiet: [{ directReportId: '3', fullName: 'Ada Lovelace', planId: 'cp3', lastChangedAt: '2026-06-15T10:00:00Z' }]
      })))
    )
    renderDashboard()

    const noPlan = await screen.findByRole('list', { name: 'No plan yet' })
    expect(within(noPlan).getByRole('button', { name: 'Jane Smith' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Nothing in focus' })).getByRole('button', { name: 'John Doe' })).toBeInTheDocument()
    const quiet = screen.getByRole('list', { name: 'Quiet for 60 days' })
    expect(within(quiet).getByRole('button', { name: /Ada Lovelace/ })).toHaveTextContent('15/06/2026')
    // Nothing that compares people
    expect(screen.queryByText(/Player level|XP/)).not.toBeInTheDocument()

    await user.click(within(quiet).getByRole('button', { name: /Ada Lovelace/ }))
    expect(await screen.findByText('Now at /career-plans?plan=cp3')).toBeInTheDocument()
  })

  it('should send someone without a plan to Career Plans', async () => {
    const user = userEvent.setup()
    server.use(
      http.get(`${API_BASE}/career-plans/overview`, () => HttpResponse.json(createCareerOverview({
        directReportsWithoutPlan: [{ directReportId: '2', fullName: 'Jane Smith', planId: null, lastChangedAt: null }]
      })))
    )
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: 'Jane Smith' }))

    expect(await screen.findByText('Now at /career-plans')).toBeInTheDocument()
  })

  it('should say all is clear when nobody needs the manager', async () => {
    renderDashboard()

    expect(await screen.findByText(/All clear/)).toBeInTheDocument()
  })

  it('should send quiet career plans to Career Plans', async () => {
    const user = userEvent.setup()
    showOnly('attention')
    withInsights({ warnings: [{ category: 'CareerPlans', title: '1 career plan has gone quiet', detail: 'John Doe' }] })
    renderDashboard()

    await user.click(await screen.findByText('1 career plan has gone quiet'))

    expect(await screen.findByText('Now at /career-plans')).toBeInTheDocument()
  })
})

describe('Dashboard retro summary', () => {
  beforeEach(() => {
    resetMockData()
    showOnly('retroSummary')
  })

  const summarised = createRetroSummary({
    isEnabled: true,
    hasRetroFolder: true,
    retros: [
      { noteId: 'n2', title: '2026-09-28', retroDate: '2026-09-28' },
      { noteId: 'n1', title: '2026-09-14', retroDate: '2026-09-14' }
    ],
    summary: 'The team kept raising flaky tests.',
    warnings: ['Flaky tests raised in two retros in a row'],
    actionItems: [{ text: 'Quarantine flaky tests', retroDate: '2026-09-28', owner: 'Alice' }],
    suggestions: ['Give the flaky test work an owner'],
    model: 'llama3.1:8b',
    retrosAnalyzed: 2,
    analyzedAt: '2026-09-29T08:00:00Z',
    isUpToDate: true
  })

  it('should show the warnings first, then the summary, action items and suggestions', async () => {
    server.use(http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json(summarised)))
    renderDashboard()

    const warnings = await screen.findByRole('list', { name: 'Retro warnings' })
    expect(warnings).toHaveTextContent('Flaky tests raised in two retros in a row')
    const summary = screen.getByText('The team kept raising flaky tests.')
    expect(warnings.compareDocumentPosition(summary) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(within(screen.getByRole('list', { name: 'Action items' })).getByText('Quarantine flaky tests')).toBeInTheDocument()
    expect(screen.getByText('Alice · agreed 28/09/2026')).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Suggestions' })).getByText('Give the flaky test work an owner')).toBeInTheDocument()
    expect(screen.getByText('Latest 2 retros')).toBeInTheDocument()
    expect(screen.getByText(/by llama3\.1:8b/)).toBeInTheDocument()
  })

  it('should open a retro note from the summary', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json(summarised)))
    renderDashboard()

    const retros = await screen.findByRole('list', { name: 'Retros read' })
    await user.click(within(retros).getByRole('button', { name: '14/09/2026' }))

    expect(await screen.findByText('Now at /notes?note=n1')).toBeInTheDocument()
  })

  it('should keep the last summary and say why it could not be updated', async () => {
    server.use(http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json({
      ...summarised,
      isUpToDate: false,
      error: 'Could not reach Ollama at http://localhost:11434. Check that Ollama is running.'
    })))
    renderDashboard()

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not reach Ollama')
    expect(screen.getByRole('alert')).toHaveTextContent('Showing the one from 29/09/2026')
    expect(screen.getByText('The team kept raising flaky tests.')).toBeInTheDocument()
    expect(screen.getByText(/out of date/)).toBeInTheDocument()
  })

  it('should refresh on request', async () => {
    const user = userEvent.setup()
    let refreshed = false
    server.use(
      http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json(summarised)),
      http.post(`${API_BASE}/retro-summary/refresh`, () => {
        refreshed = true
        return HttpResponse.json({ ...summarised, summary: 'A fresh reading.' })
      })
    )
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: 'Refresh' }))

    expect(await screen.findByText('A fresh reading.')).toBeInTheDocument()
    expect(refreshed).toBe(true)
  })

  it('should send the manager to Settings while it is turned off', async () => {
    const user = userEvent.setup()
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: 'Turn it on in Settings' }))

    expect(await screen.findByText('Now at /settings?section=ai')).toBeInTheDocument()
  })

  it('should explain the Retro folder when there is none', async () => {
    server.use(http.get(`${API_BASE}/retro-summary`, () => HttpResponse.json(createRetroSummary({ isEnabled: true }))))
    renderDashboard()

    expect(await screen.findByText(/Create a folder named Retro in Notes/)).toBeInTheDocument()
  })
})

describe('Dashboard task readiness', () => {
  beforeEach(() => {
    resetMockData()
    showOnly('taskReadiness')
  })

  const needsWork = {
    verdict: 'NeedsWork' as const, summary: 'Lets a user save a draft.', missing: ['Acceptance criteria', 'Offline behaviour'],
    model: 'llama3.1:8b', analyzedAt: '2026-09-30T08:00:00Z', isUpToDate: true
  }
  const ready = { ...needsWork, verdict: 'Ready' as const, summary: 'Clear.', missing: [] }

  const item = (id: string, title: string, overrides = {}) => ({
    taskId: id, title, typeName: 'Story', statusName: 'Todo', sprint: 'MAR_3Q26_S6', jiraKey: `MAR-${id}`,
    needsAcceptanceCriteria: true, hasAcceptanceCriteria: true, review: null, ...overrides
  })

  const reviewed = createTaskReadiness({
    isEnabled: true,
    currentSprint: 'MAR_3Q26_S6',
    nextSprint: 'MAR_4Q26_S1',
    tasks: [
      item('1', 'Save drafts', { hasAcceptanceCriteria: false, review: needsWork }),
      item('2', 'Export CSV', { review: ready })
    ]
  })

  it('should show the fact, the review and what is missing, and hide ready work until asked', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(reviewed)))
    renderDashboard()

    expect(await screen.findByText('Unstarted work in MAR_3Q26_S6 and MAR_4Q26_S1')).toBeInTheDocument()
    expect(screen.getByText('No acceptance criteria')).toBeInTheDocument()
    expect(screen.getByLabelText('AI review: Needs work')).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'Missing from Save drafts' })).getByText('Offline behaviour')).toBeInTheDocument()
    expect(screen.queryByText('Export CSV')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Show 1 ready task too' }))

    expect(screen.getByText('Export CSV')).toBeInTheDocument()
    expect(screen.getByLabelText('AI review: Ready')).toBeInTheDocument()
  })

  it('should review the pending tasks one at a time', async () => {
    let reviews = 0
    server.use(
      http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(createTaskReadiness({
        isEnabled: true, currentSprint: 'MAR_3Q26_S6',
        tasks: [item('1', 'Save drafts'), item('2', 'Export CSV')], pendingCount: 2
      }))),
      http.post(`${API_BASE}/task-readiness/review-next`, () => {
        reviews++
        return HttpResponse.json(createTaskReadiness({
          isEnabled: true, currentSprint: 'MAR_3Q26_S6',
          tasks: [
            item('1', 'Save drafts', { review: needsWork }),
            item('2', 'Export CSV', reviews > 1 ? { review: needsWork } : {})
          ],
          pendingCount: 2 - reviews
        }))
      })
    )
    renderDashboard()

    await waitFor(() => expect(screen.getAllByLabelText('AI review: Needs work')).toHaveLength(2))
    expect(reviews).toBe(2)
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('should stop and say why when the model fails, and try again on request', async () => {
    const user = userEvent.setup()
    let attempts = 0
    const pending = createTaskReadiness({
      isEnabled: true, currentSprint: 'MAR_3Q26_S6', tasks: [item('1', 'Save drafts')], pendingCount: 1
    })
    server.use(
      http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(pending)),
      http.post(`${API_BASE}/task-readiness/review-next`, () => {
        attempts++
        return attempts === 1
          ? HttpResponse.json({ ...pending, error: 'Could not reach Ollama at http://localhost:11434.' })
          : HttpResponse.json({ ...pending, tasks: [item('1', 'Save drafts', { review: needsWork })], pendingCount: 0 })
      })
    )
    renderDashboard()

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not reach Ollama')
    expect(attempts).toBe(1)

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByLabelText('AI review: Needs work')).toBeInTheDocument()
    expect(attempts).toBe(2)
  })

  it('should show the facts while the AI review is off, and send the manager to Settings', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(createTaskReadiness({
      currentSprint: 'MAR_3Q26_S6', tasks: [item('1', 'Save drafts', { hasAcceptanceCriteria: false })], pendingCount: 0
    }))))
    renderDashboard()

    expect(await screen.findByText('No acceptance criteria')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Turn it on in Settings' }))

    expect(await screen.findByText('Now at /settings?section=ai')).toBeInTheDocument()
  })

  it('should open a task, and the tasks without acceptance criteria, on the Tasks page', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(reviewed)))
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /1 story or task has no acceptance criteria/ }))
    expect(await screen.findByText('Now at /tasks?noAcceptanceCriteria=true')).toBeInTheDocument()
  })

  it('should open one task by its Jira key', async () => {
    const user = userEvent.setup()
    server.use(http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(reviewed)))
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: /^MAR-1/ }))
    expect(await screen.findByText('Now at /tasks?search=MAR-1')).toBeInTheDocument()
  })

  it('should review one task again on request', async () => {
    const user = userEvent.setup()
    server.use(
      http.get(`${API_BASE}/task-readiness`, () => HttpResponse.json(reviewed)),
      http.post(`${API_BASE}/task-readiness/tasks/1/review`, () => HttpResponse.json({
        ...reviewed, tasks: [item('1', 'Save drafts', { review: { ...needsWork, summary: 'A fresh reading.' } }), reviewed.tasks[1]]
      }))
    )
    renderDashboard()

    await user.click(await screen.findByRole('button', { name: 'Review Save drafts again' }))

    expect(await screen.findByText('A fresh reading.')).toBeInTheDocument()
  })
})
