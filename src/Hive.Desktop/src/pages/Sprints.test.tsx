import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import { createSprint } from '../test/factories'
import Sprints from './Sprints'

const API_BASE = 'http://localhost:5002/api'

describe('Sprints: syncing from Jira', () => {
  beforeEach(() => {
    resetMockData()
    server.use(
      http.get(`${API_BASE}/sprints`, () => HttpResponse.json([createSprint({ name: 'LP_4Q25_S1' })])),
      http.get(`${API_BASE}/sprint-capacity`, () => HttpResponse.json([])),
      http.get(`${API_BASE}/settings`, () => HttpResponse.json({ sprintTeamFilter: null, hasJiraApiToken: true }))
    )
  })

  it('should sync only sprint dates and show the result', async () => {
    let everythingSynced = false
    server.use(
      http.post(`${API_BASE}/jira-sync/sprint-dates`, () =>
        HttpResponse.json({
          jiraSprintCount: 31,
          updated: [{ sprintId: 's1', name: 'LP_4Q25_S1', startDate: '2025-10-06T00:00:00', endDate: '2025-10-17T00:00:00' }],
          unchangedCount: 16,
          missingDatesInJira: [],
          notFoundInJira: [],
          ambiguousInJira: [],
          errors: [],
          syncedAt: new Date().toISOString()
        })
      ),
      http.post(`${API_BASE}/jira-sync`, () => {
        everythingSynced = true
        return HttpResponse.json({})
      })
    )
    const user = userEvent.setup()
    render(
      <ToastProvider>
        <Sprints />
      </ToastProvider>
    )

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Updated dates for 1 sprint from Jira')).toBeInTheDocument()
    expect(everythingSynced).toBe(false)
  })
})
