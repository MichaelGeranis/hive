import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, waitFor, within, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import type { Parent } from '../types'
import Parents from './Parents'

const API_BASE = 'http://localhost:5002/api'

function parent(overrides: Partial<Parent>): Parent {
  return {
    id: 'p1',
    name: 'Payments revamp',
    labels: '',
    totalTasks: 3,
    completedTasks: 1,
    openTasks: 2,
    isDone: false,
    totalStoryPoints: 8,
    completedStoryPoints: 3,
    createdAt: new Date().toISOString(),
    ...overrides
  }
}

function renderParents(parents: Parent[][], jiraBrowseUrl: string | null = 'https://acme.atlassian.net/browse') {
  let load = 0
  server.use(
    // Each load returns the next list, so a reload after syncing shows the synced parents
    http.get(`${API_BASE}/parents`, () => HttpResponse.json(parents[Math.min(load++, parents.length - 1)])),
    http.get(`${API_BASE}/settings`, () => HttpResponse.json({ jiraBrowseUrl, hasJiraApiToken: true }))
  )
  return render(
    <ToastProvider>
      <Parents />
    </ToastProvider>
  )
}

describe('Parents', () => {
  beforeEach(() => {
    resetMockData()
    localStorage.clear()
  })

  it('should hide done parents and show them again', async () => {
    const user = userEvent.setup()
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp' }),
      parent({ id: 'p2', name: 'Old migration', completedTasks: 3, openTasks: 0, isDone: true })
    ]])

    await user.click(await screen.findByRole('button', { name: 'Hide done (1)' }))

    expect(screen.getByText('Payments revamp')).toBeInTheDocument()
    expect(screen.queryByText('Old migration')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Show done (1)' }))

    expect(screen.getByText('Old migration')).toBeInTheDocument()
  })

  it('should remember that done parents are hidden', async () => {
    localStorage.setItem('hive-parents-hide-done', 'true')
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp' }),
      parent({ id: 'p2', name: 'Old migration', completedTasks: 3, openTasks: 0, isDone: true })
    ]])

    expect(await screen.findByText('Payments revamp')).toBeInTheDocument()
    expect(screen.queryByText('Old migration')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Show done (1)' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('should not offer the toggle when no parent is done', async () => {
    renderParents([[parent({ id: 'p1', name: 'Payments revamp' })]])

    expect(await screen.findByText('Payments revamp')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /done \(/ })).not.toBeInTheDocument()
  })

  it('should show the products a parent belongs to', async () => {
    renderParents([[parent({ name: 'Payments revamp', components: 'Matching,IPI' })]])

    const products = await screen.findByRole('group', { name: 'Products of Payments revamp' })
    expect(within(products).getByText('Matching')).toBeInTheDocument()
    expect(within(products).getByText('IPI')).toBeInTheDocument()
  })

  it('should link a parent synced from Jira to its epic', async () => {
    renderParents([[parent({ jiraKey: 'MAR-128' })]])

    const link = await screen.findByRole('link', { name: 'MAR-128' })
    expect(link).toHaveAttribute('href', 'https://acme.atlassian.net/browse/MAR-128')
  })

  it('should show the key without a link when no Jira base URL is set', async () => {
    renderParents([[parent({ jiraKey: 'MAR-128' })]], null)

    expect(await screen.findByText('MAR-128')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'MAR-128' })).not.toBeInTheDocument()
  })

  it('should badge every parent with its Jira issue type', async () => {
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp', jiraKey: 'MAR-1', jiraIssueType: 'Epic' }),
      parent({ id: 'p2', name: 'Backfill created_at', jiraKey: 'MAR-111', jiraIssueType: 'Story' }),
      parent({ id: 'p3', name: 'Hand-made group' })
    ]])

    const epicCard = (await screen.findByText('Payments revamp')).closest('div')!
    expect(epicCard).toHaveTextContent('Epic')
    expect(screen.getByText('Backfill created_at').closest('div')).toHaveTextContent('Story')
    expect(screen.getByText('Hand-made group').closest('div')).not.toHaveTextContent(/Epic|Story/)
  })

  it('should show only epics when the Epics filter is on', async () => {
    const user = userEvent.setup()
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp', jiraKey: 'MAR-1', jiraIssueType: 'Epic' }),
      parent({ id: 'p2', name: 'Backfill created_at', jiraKey: 'MAR-111', jiraIssueType: 'Story' }),
      parent({ id: 'p3', name: 'Service Maintainer', jiraKey: 'MAR-125', jiraIssueType: 'Support' })
    ]])
    const typeFilter = await screen.findByRole('group', { name: 'Filter by type' })
    // Epics come first among the types
    expect(within(typeFilter).getAllByRole('button').map(b => b.textContent)).toEqual(['Epics', 'Story', 'Support'])

    await user.click(within(typeFilter).getByRole('button', { name: 'Epics' }))

    expect(screen.getByText('Payments revamp')).toBeInTheDocument()
    expect(screen.queryByText('Backfill created_at')).not.toBeInTheDocument()
    expect(screen.queryByText('Service Maintainer')).not.toBeInTheDocument()

    await user.click(within(typeFilter).getByRole('button', { name: 'Epics' }))

    expect(screen.getByText('Backfill created_at')).toBeInTheDocument()
  })

  it('should total done versus total per type, then tasks and story points, without hours', async () => {
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp', jiraIssueType: 'Epic', isDone: true, totalTasks: 4, completedTasks: 4, totalStoryPoints: 10, completedStoryPoints: 10 }),
      parent({ id: 'p2', name: 'Search rework', jiraIssueType: 'Epic', totalTasks: 6, completedTasks: 1, totalStoryPoints: 15, completedStoryPoints: 2 }),
      parent({ id: 'p3', name: 'Backfill created_at', jiraIssueType: 'Story', isDone: true, totalTasks: 2, completedTasks: 2, totalStoryPoints: 3, completedStoryPoints: 3 }),
      parent({ id: 'p4', name: 'Service Maintainer', jiraIssueType: 'Support', totalTasks: 8, completedTasks: 7, totalStoryPoints: 0, completedStoryPoints: 0 })
    ]])

    const bar = await screen.findByLabelText('Totals')

    expect(bar).toHaveTextContent('Epics1/2')
    expect(bar).toHaveTextContent('Stories1/1')
    expect(bar).toHaveTextContent('Support0/1')
    expect(bar).toHaveTextContent('14/20 tasks')
    expect(bar).toHaveTextContent('15/28 SP')
    expect(bar).not.toHaveTextContent(/logged|\dh|\dm\b/)
    // Epics come first
    expect(bar.textContent!.indexOf('Epics')).toBeLessThan(bar.textContent!.indexOf('Stories'))
  })

  it('should total only the parents the filters show', async () => {
    const user = userEvent.setup()
    renderParents([[
      parent({ id: 'p1', name: 'Payments revamp', jiraIssueType: 'Epic', totalTasks: 4, completedTasks: 2, totalStoryPoints: 10, completedStoryPoints: 5 }),
      parent({ id: 'p2', name: 'Backfill created_at', jiraIssueType: 'Story', totalTasks: 2, completedTasks: 1, totalStoryPoints: 3, completedStoryPoints: 1 })
    ]])
    const typeFilter = await screen.findByRole('group', { name: 'Filter by type' })

    await user.click(within(typeFilter).getByRole('button', { name: 'Epics' }))

    const bar = screen.getByLabelText('Totals')
    expect(bar).toHaveTextContent('Epics0/1')
    expect(bar).not.toHaveTextContent('Stories')
    expect(bar).toHaveTextContent('2/4 tasks')
    expect(bar).toHaveTextContent('5/10 SP')
  })

  it('should sync only parents and show the result', async () => {
    let parentsSynced = false
    server.use(
      http.post(`${API_BASE}/jira-sync/parents`, () => {
        parentsSynced = true
        return HttpResponse.json({
          jiraParentCount: 46,
          created: [{ parentId: 'p2', jiraKey: 'MAR-2', name: 'Onboarding' }],
          updated: [],
          unchangedCount: 45,
          errors: ['MAR-3: another parent is already called \'Maintenance\' (MAR-1).'],
          syncedAt: new Date().toISOString()
        })
      }),
      http.post(`${API_BASE}/jira-sync`, () => HttpResponse.json({ message: 'should not sync everything' }, { status: 500 }))
    )
    const user = userEvent.setup()
    renderParents([
      [parent({ jiraKey: 'MAR-1' })],
      [parent({ jiraKey: 'MAR-1' }), parent({ id: 'p2', name: 'Onboarding', jiraKey: 'MAR-2', totalTasks: 0 })]
    ])

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Parents synced from Jira: 1 added (46 in Jira)')).toBeInTheDocument()
    expect(screen.getByText("1 Jira issue not applied: MAR-3: another parent is already called 'Maintenance' (MAR-1).")).toBeInTheDocument()
    expect(parentsSynced).toBe(true)
    // The list reloads with the new parent
    await waitFor(() => expect(screen.getByText('Onboarding')).toBeInTheDocument())
  })

  it('should show why the sync failed', async () => {
    server.use(
      http.post(`${API_BASE}/jira-sync/parents`, () =>
        HttpResponse.json({ message: 'Jira sync is not configured. Add the Jira space key in Settings.' }, { status: 400 })
      )
    )
    const user = userEvent.setup()
    renderParents([[parent({})]])

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(
      await screen.findByText('Jira sync is not configured. Add the Jira space key in Settings.')
    ).toBeInTheDocument()
  })
})
