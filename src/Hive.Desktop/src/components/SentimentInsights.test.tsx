import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, userEvent } from '../test/test-utils'
import { server } from '../test/mocks/server'
import { SentimentInsights } from './SentimentInsights'
import type { DirectReportSentimentSummary, TeamSentimentOverview } from '../types'

const API_BASE = 'http://localhost:5002/api'

function person(overrides: Partial<DirectReportSentimentSummary>): DirectReportSentimentSummary {
  return {
    directReportId: '1',
    directReportName: 'John Doe',
    overallSentiment: 'Positive',
    positiveScore: 70,
    notesCount: 4,
    trendDirection: 'Stable',
    reasoning: '',
    ...overrides
  }
}

function withTeam(byDirectReport: DirectReportSentimentSummary[]) {
  const overview: TeamSentimentOverview = {
    averagePositive: 70,
    averageNeutral: 20,
    averageNegative: 10,
    overallTeamSentiment: 'Positive',
    byDirectReport,
    commonThemes: [],
    totalNotesAnalyzed: 4,
    directReportsAnalyzed: byDirectReport.length
  }
  server.use(
    http.get(`${API_BASE}/sentimentanalysis/status`, () =>
      HttpResponse.json({ isEnabled: true, isConfigured: true, analysisDays: 90 })
    ),
    http.get(`${API_BASE}/sentimentanalysis/team`, () => HttpResponse.json(overview))
  )
}

describe('SentimentInsights team overview', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  it('should show the reasoning for a direct report on hover', async () => {
    const user = userEvent.setup()
    withTeam([person({ reasoning: 'They shipped on time and want to lead the next project.' })])
    render(<SentimentInsights showTeamOverview={true} />)

    await user.hover(await screen.findByText('John Doe'))

    const tooltip = screen.getByRole('tooltip')
    expect(tooltip).toHaveTextContent('Why John Doe: 4 1:1s')
    expect(tooltip).toHaveTextContent('They shipped on time and want to lead the next project.')

    await user.unhover(screen.getByText('John Doe'))
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
  })

  it('should say when a result has no reasoning yet', async () => {
    const user = userEvent.setup()
    withTeam([person({ reasoning: '' })])
    render(<SentimentInsights showTeamOverview={true} />)

    await user.hover(await screen.findByText('John Doe'))

    expect(screen.getByRole('tooltip')).toHaveTextContent('No reasoning yet')
  })
})
