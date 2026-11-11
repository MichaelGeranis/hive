import { describe, it, expect, vi, beforeEach } from 'vitest'
import { act, fireEvent, render, screen, waitFor, within } from '../test/test-utils'
import Logs from './Logs'
import { clearLogs, getLogs, getUnseenErrorCount, recordLog } from '../services/logStore'

describe('Logs', () => {
  const writeText = vi.fn()

  beforeEach(() => {
    clearLogs()
    writeText.mockReset().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
  })

  it('shows errors and warnings by default, and hides info', () => {
    recordLog('error', 'API GET /reports - Failed (500)', 'api')
    recordLog('warn', 'Slow response', 'renderer')
    recordLog('info', 'Loaded dashboard', 'renderer')

    render(<Logs />)

    expect(screen.getByText('API GET /reports - Failed (500)')).toBeInTheDocument()
    expect(screen.getByText('Slow response')).toBeInTheDocument()
    expect(screen.queryByText('Loaded dashboard')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Info 1/ })).toHaveAttribute('aria-pressed', 'false')
  })

  it('shows info once its chip is turned on', () => {
    recordLog('info', 'Loaded dashboard', 'renderer')
    recordLog('error', 'Broke', 'renderer')

    render(<Logs />)
    fireEvent.click(screen.getByRole('button', { name: /Info 1/ }))

    expect(screen.getByText('Loaded dashboard')).toBeInTheDocument()
  })

  it('shows repeated entries as one row with a count', () => {
    for (let i = 0; i < 6; i++) recordLog('error', 'Health check failed: TypeError: Failed to fetch', 'renderer')

    render(<Logs />)

    expect(screen.getAllByText('Health check failed: TypeError: Failed to fetch')).toHaveLength(1)
    expect(screen.getByText('×6')).toBeInTheDocument()
  })

  it('shows the full message and details when a row is expanded', () => {
    recordLog('error', 'Broke', 'renderer', 'at Dashboard.load (Dashboard.tsx:42)')

    render(<Logs />)
    expect(screen.queryByText(/Dashboard.tsx:42/)).not.toBeInTheDocument()

    fireEvent.click(screen.getByText('Broke'))

    expect(screen.getByText(/Dashboard.tsx:42/)).toBeInTheDocument()
  })

  it('filters by search text', () => {
    recordLog('error', 'Jira sync failed', 'api')
    recordLog('error', 'Calendar sync failed', 'api')

    render(<Logs />)
    fireEvent.change(screen.getByLabelText('Search logs'), { target: { value: 'jira' } })

    expect(screen.getByText('Jira sync failed')).toBeInTheDocument()
    expect(screen.queryByText('Calendar sync failed')).not.toBeInTheDocument()
  })

  it('filters by source', () => {
    recordLog('error', 'From the API', 'api')
    recordLog('error', 'From the page', 'renderer')

    render(<Logs />)
    fireEvent.click(screen.getByRole('button', { name: 'API' }))

    expect(screen.queryByText('From the API')).not.toBeInTheDocument()
    expect(screen.getByText('From the page')).toBeInTheDocument()
  })

  it('says all is well when there are no errors or warnings', () => {
    recordLog('info', 'Loaded dashboard', 'renderer')

    render(<Logs />)

    expect(screen.getByText(/No errors or warnings since/)).toBeInTheDocument()
  })

  it('copies a report of the errors and marks them reported', async () => {
    recordLog('info', 'Opened Tasks', 'renderer')
    recordLog('error', 'API GET /tasks - Failed (500)', 'api', 'Response: boom')

    render(<Logs />)
    fireEvent.click(screen.getByRole('button', { name: /Copy report/ }))

    await waitFor(() => expect(writeText).toHaveBeenCalled())
    const report = writeText.mock.calls[0][0] as string
    expect(report).toContain('Hive error report')
    expect(report).toContain('API GET /tasks - Failed (500)')
    expect(report).toContain('Response: boom')
    expect(report).toContain('Opened Tasks')
    expect(await screen.findByText('Reported')).toBeInTheDocument()
  })

  it('reports only the selected problems when some are selected', async () => {
    recordLog('error', 'First problem', 'api')
    recordLog('error', 'Second problem', 'api')

    render(<Logs />)
    fireEvent.click(screen.getByLabelText('Select First problem'))
    fireEvent.click(screen.getByRole('button', { name: 'Copy report (1)' }))

    await waitFor(() => expect(writeText).toHaveBeenCalled())
    const report = writeText.mock.calls[0][0] as string
    expect(report).toContain('First problem')
    expect(report).not.toContain('Second problem')
  })

  it('asks before clearing the logs', async () => {
    recordLog('error', 'Broke', 'renderer')

    render(<Logs />)
    fireEvent.click(screen.getByRole('button', { name: 'Clear logs' }))
    const dialog = screen.getByRole('dialog', { name: 'Clear logs' })

    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(getLogs()).toHaveLength(1)

    fireEvent.click(screen.getByRole('button', { name: 'Clear logs' }))
    fireEvent.click(within(screen.getByRole('dialog', { name: 'Clear logs' })).getByRole('button', { name: 'Clear' }))
    // The answer comes back from Hive's confirm dialog a moment after the click
    await waitFor(() => expect(getLogs()).toHaveLength(0))
  })

  it('counts errors as seen while the page is open', () => {
    recordLog('error', 'Broke', 'renderer')
    expect(getUnseenErrorCount()).toBe(1)

    render(<Logs />)
    expect(getUnseenErrorCount()).toBe(0)

    act(() => recordLog('error', 'Broke again', 'renderer'))
    expect(getUnseenErrorCount()).toBe(0)
  })
})
