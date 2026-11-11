import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, waitFor, userEvent, within } from '../test/test-utils'
import { server } from '../test/mocks/server'
import type { AppSettings, UpdateAppSettings } from '../types'
import Settings from './Settings'

const API_BASE = 'http://localhost:5002/api'

function settings(overrides: Partial<AppSettings> = {}): AppSettings {
  return {
    id: 'settings-1',
    tshirtSizeMappings: [],
    sentimentProvider: 'Ollama',
    hasClaudeApiKey: false,
    ollamaBaseUrl: 'http://localhost:11434',
    ollamaModel: 'llama3.1:8b',
    sentimentAnalysisDays: 90,
    sentimentAnalysisEnabled: false,
    retroSummaryEnabled: false,
    retroSummaryCount: 6,
    taskReviewEnabled: false,
    maxInProgressTasks: 2,
    maxBlockedTasks: 1,
    maxInReviewTasks: 1,
    minProductMembers: 2,
    minMemberProducts: 2,
    oneOnOneCadenceDays: 14,
    staleTaskDays: 5,
    maintenanceLabels: ['maintenance'],
    workCategoryEpicNames: { TechMaintenance: ['Tech Maintenance'], TechRoadmap: ['Tech Roadmap'], TechDebt: ['Tech Debt'], SystemImprovements: ['System Improvement'] },
    hasJiraApiToken: false,
    hasGoogleCalendarUrl: false,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

// Settings shows one section at a time; ?section= opens one
function renderSection(section: string) {
  return render(<Settings />, { routerProps: { initialEntries: [`/settings?section=${section}`] } })
}

describe('Settings: sections', () => {
  beforeEach(() => {
    server.use(http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings())))
  })

  it('opens on the dashboard settings and moves between sections', async () => {
    const user = userEvent.setup()
    render(<Settings />)

    const nav = await screen.findByRole('navigation', { name: 'Settings sections' })
    expect(within(nav).getByRole('button', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByText('Dashboard Warning Thresholds')).toBeInTheDocument()
    expect(screen.queryByText('Jira Integration')).not.toBeInTheDocument()

    await user.click(within(nav).getByRole('button', { name: 'Integrations' }))

    expect(screen.getByText('Jira Integration')).toBeInTheDocument()
    expect(screen.getByText('Google Calendar')).toBeInTheDocument()
    expect(screen.queryByText('Dashboard Warning Thresholds')).not.toBeInTheDocument()
  })
})

describe('Settings: AI model and features', () => {
  let saved: UpdateAppSettings | null
  let tested: { baseUrl: string; model: string } | null

  beforeEach(() => {
    saved = null
    tested = null
    server.use(
      http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings())),
      http.put(`${API_BASE}/settings`, async ({ request }) => {
        saved = (await request.json()) as UpdateAppSettings
        return HttpResponse.json(settings())
      }),
      http.post(`${API_BASE}/sentimentanalysis/test-ollama`, async ({ request }) => {
        tested = (await request.json()) as { baseUrl: string; model: string }
        return HttpResponse.json({ valid: false, error: 'Ollama is running, but the model mistral is not installed. Run: ollama pull mistral' })
      })
    )
  })

  it('starts on the local model, with its address and model', async () => {
    renderSection('ai')

    expect(await screen.findByRole('radio', { name: /Local \(Ollama\)/ })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByLabelText('Ollama address')).toHaveValue('http://localhost:11434')
    expect(screen.getByLabelText('Model')).toHaveValue('llama3.1:8b')
    expect(screen.queryByText('Claude API Key')).not.toBeInTheDocument()
  })

  it('lets sentiment analysis be enabled without a Claude key when using Ollama', async () => {
    renderSection('ai')

    expect(await screen.findByRole('button', { name: 'Enable Sentiment Analysis' })).toBeEnabled()
  })

  it('shows the Claude key field once Claude is chosen', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    await user.click(await screen.findByRole('radio', { name: /Claude API/ }))

    expect(screen.getByText('Claude API Key')).toBeInTheDocument()
    expect(screen.queryByLabelText('Ollama address')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Enable Sentiment Analysis' })).toBeDisabled()
  })

  it('tests the connection with what is typed, and shows why it failed', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    const model = await screen.findByLabelText('Model')
    await user.clear(model)
    await user.type(model, 'mistral')
    await user.click(screen.getByRole('button', { name: 'Test connection' }))

    expect(await screen.findByText(/ollama pull mistral/, { selector: 'div' })).toBeInTheDocument()
    expect(tested).toEqual({ baseUrl: 'http://localhost:11434', model: 'mistral' })
  })

  it('saves the chosen model with the Ollama connection, on its own', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    await user.click(await screen.findByRole('button', { name: /Save Model Settings/ }))

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved).toEqual({
      sentimentProvider: 'Ollama',
      ollamaBaseUrl: 'http://localhost:11434',
      ollamaModel: 'llama3.1:8b',
    })
  })

  it('saves the sentiment analysis settings on their own', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    await user.click(await screen.findByRole('button', { name: 'Enable Sentiment Analysis' }))
    await user.click(screen.getByRole('button', { name: /Save Sentiment Settings/ }))

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved).toEqual({ sentimentAnalysisEnabled: true, sentimentAnalysisDays: 90 })
  })

  it('saves the task review switch on its own', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    await user.click(await screen.findByRole('button', { name: 'Enable Task Review' }))
    await user.click(screen.getByRole('button', { name: /Save Task Review Settings/ }))

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved).toEqual({ taskReviewEnabled: true })
  })

  it('saves the retro summary settings on their own', async () => {
    const user = userEvent.setup()
    renderSection('ai')

    await user.click(await screen.findByRole('button', { name: 'Enable Retro Summary' }))
    await user.selectOptions(screen.getByLabelText('Retros to read'), '10')
    await user.click(screen.getByRole('button', { name: /Save Retro Settings/ }))

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved).toEqual({ retroSummaryEnabled: true, retroSummaryCount: 10 })
  })
})

describe('Settings: dashboard warning thresholds', () => {
  let saved: UpdateAppSettings | null

  beforeEach(() => {
    saved = null
    server.use(
      http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings({ minMemberProducts: 3 }))),
      http.put(`${API_BASE}/settings`, async ({ request }) => {
        saved = (await request.json()) as UpdateAppSettings
        return HttpResponse.json(settings())
      })
    )
  })

  it('shows and saves the minimum products per person', async () => {
    const user = userEvent.setup()
    render(<Settings />)

    const input = await screen.findByLabelText('Minimum Products per Person')
    await waitFor(() => expect(input).toHaveValue(3))

    await user.type(input, '4', { initialSelectionStart: 0, initialSelectionEnd: 1 })
    const save = screen.getAllByRole('button', { name: 'Save Changes' })
      .find(button => button.compareDocumentPosition(input) & Node.DOCUMENT_POSITION_PRECEDING)!
    await user.click(save)

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved).toMatchObject({ minMemberProducts: 4, minProductMembers: 2 })
  })
})

describe('Settings: T-shirt sizes', () => {
  let saved: UpdateAppSettings | null
  const sizesCard = async () =>
    within((await screen.findByText('T-Shirt Size to Sprints Mapping')).closest('.rounded-xl') as HTMLElement)

  beforeEach(() => {
    saved = null
    server.use(
      http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings({
        tshirtSizeMappings: [
          { size: 'XL', sprints: 6, label: '' },
          { size: 'XXL', sprints: 12, label: '' }
        ]
      }))),
      http.put(`${API_BASE}/settings`, async ({ request }) => {
        saved = (await request.json()) as UpdateAppSettings
        return HttpResponse.json(settings())
      })
    )
  })

  it('resets to S 1, M 2, L 4, XL 6 and XXL 12', async () => {
    renderSection('planning')

    await userEvent.click((await sizesCard()).getByRole('button', { name: 'Reset to Defaults' }))

    await waitFor(() => expect(saved?.tshirtSizeMappings?.map(m => [m.size, m.sprints])).toEqual([
      ['S', 1], ['M', 2], ['L', 4], ['XL', 6], ['XXL', 12]
    ]))
  })
})

describe('Settings: backup and restore', () => {
  beforeEach(() => {
    server.use(http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings())))
  })

  it('restores a backup file and says what came back, table by table', async () => {
    const user = userEvent.setup()
    let sent: unknown = null
    server.use(
      http.post(`${API_BASE}/backup/import`, async ({ request }) => {
        sent = await request.json()
        return HttpResponse.json({
          success: true,
          settingsRestored: true,
          restored: { TeamTask: 12, ExternalDependency: 2, ProjectKnowledge: 3 },
          skipped: {},
          errors: [],
          warnings: []
        })
      })
    )
    const { container } = renderSection('backup')
    const backup = { version: '2.0', exportedAt: '2026-09-29T10:00:00Z', tables: { TeamTask: [] } }

    const input = await waitFor(() => {
      const found = container.querySelector<HTMLInputElement>('input[type="file"]')
      if (!found) throw new Error('The restore file input is not rendered yet')
      return found
    })
    await user.upload(input, new File([JSON.stringify(backup)], 'hive-backup.json', { type: 'application/json' }))
    await user.click(within(await screen.findByRole('dialog', { name: /Restore the backup/ })).getByRole('button', { name: 'Restore' }))

    expect(await screen.findByText('Tasks')).toBeInTheDocument()
    expect(screen.getByText('12')).toBeInTheDocument()
    expect(screen.getByText('External dependencies')).toBeInTheDocument()
    expect(screen.getByText('Project knowledge')).toBeInTheDocument()
    expect(screen.getByText('✓').nextElementSibling).toHaveTextContent('Settings')
    expect(sent).toMatchObject({ version: '2.0', tables: { TeamTask: [] } })
  })
})

describe('Settings: automatic backups', () => {
  beforeEach(() => {
    server.use(http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings())))
  })

  it('says when the last automatic backup was taken and how many are kept', async () => {
    renderSection('backup')

    const last = await screen.findByTestId('last-automatic-backup')
    expect(last).toHaveTextContent(/Last backup: 29\/09\/2026 \d{2}:\d{2} · 3 kept/)
    expect(screen.getByText(/when it starts and every hour/)).toBeInTheDocument()
  })

  it('explains there is nothing to back up with the in-memory database', async () => {
    server.use(http.get(`${API_BASE}/backup/automatic`, () => HttpResponse.json({ enabled: false, count: 0, intervalHours: 1 })))
    renderSection('backup')

    expect(await screen.findByText(/keeps its data in memory/)).toBeInTheDocument()
  })
})

describe('Settings: work categories', () => {
  let saved: UpdateAppSettings | null

  beforeEach(() => {
    saved = null
    server.use(
      http.get(`${API_BASE}/settings`, () => HttpResponse.json(settings())),
      http.put(`${API_BASE}/settings`, async ({ request }) => {
        saved = (await request.json()) as UpdateAppSettings
        return HttpResponse.json(settings({ workCategoryEpicNames: saved.workCategoryEpicNames! }))
      })
    )
  })

  it('adds and removes the epic names of a category, and saves them all', async () => {
    const user = userEvent.setup()
    render(<Settings />)

    await user.type(await screen.findByLabelText('Add an epic name to Tech Maintenance'), 'KTLO{Enter}')
    await user.click(screen.getByRole('button', { name: 'Remove Tech Debt from Tech Debt' }))
    await user.click(screen.getByRole('button', { name: 'Save Work Categories' }))

    await waitFor(() => expect(saved).not.toBeNull())
    expect(saved!.workCategoryEpicNames).toEqual({
      TechMaintenance: ['Tech Maintenance', 'KTLO'],
      TechRoadmap: ['Tech Roadmap'],
      TechDebt: [],
      SystemImprovements: ['System Improvement']
    })
    expect(screen.getByText('No names: no epic counts as Tech Debt')).toBeInTheDocument()
  })

  it('shows why the server refused the names', async () => {
    const user = userEvent.setup()
    server.use(http.put(`${API_BASE}/settings`, () =>
      HttpResponse.json({ message: '"Tech Debt" names both TechDebt and TechRoadmap; an epic name can name one category.' }, { status: 400 })))
    render(<Settings />)

    await user.type(await screen.findByLabelText('Add an epic name to Tech Roadmap'), 'Tech Debt{Enter}')
    await user.click(screen.getByRole('button', { name: 'Save Work Categories' }))

    expect(await screen.findByText(/names both TechDebt and TechRoadmap/)).toBeInTheDocument()
  })
})
