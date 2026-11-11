import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, fireEvent, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import Calendar from './Calendar'

const API_BASE = 'http://localhost:5002/api'

const emptyPage = {
  items: [], totalCount: 0, pageNumber: 1, pageSize: 100, totalPages: 0, hasPreviousPage: false, hasNextPage: false
}

const syncedEvents = {
  isConfigured: true,
  isLoaded: true,
  syncedAt: new Date(2026, 8, 16, 11, 45).toISOString(),
  events: [
    {
      id: 'standup',
      title: 'Team standup',
      isAllDay: false,
      startsAt: new Date(2026, 8, 16, 9, 30).toISOString(),
      endsAt: new Date(2026, 8, 16, 9, 45).toISOString(),
    },
    {
      id: 'offsite',
      title: 'Offsite',
      location: 'Athens',
      isAllDay: true,
      startDate: '2026-09-21',
      endDate: '2026-09-22',
    },
  ],
}

const notSynced = { isConfigured: true, isLoaded: false, events: [] }

function renderCalendar() {
  return render(
    <ToastProvider>
      <Calendar />
    </ToastProvider>
  )
}

describe('Calendar: Google Calendar events', () => {
  beforeEach(() => {
    resetMockData()
    // Only the clock is faked, so requests and rendering run as normal. Wednesday 16/09/2026
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 8, 16, 12, 0, 0))
    server.use(
      http.get(`${API_BASE}/managernotes`, () => HttpResponse.json(emptyPage)),
      http.get(`${API_BASE}/oneononemeetings`, () => HttpResponse.json(emptyPage))
    )
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('should not sync on opening, and load the visible month when Sync is pressed', async () => {
    let synced: URLSearchParams | null = null
    server.use(
      http.get(`${API_BASE}/google-calendar/events`, () => HttpResponse.json(notSynced)),
      http.post(`${API_BASE}/google-calendar/sync`, ({ request }) => {
        synced = new URL(request.url).searchParams
        return HttpResponse.json(syncedEvents)
      })
    )
    const user = userEvent.setup()

    renderCalendar()

    expect(await screen.findByText(/not synced/)).toBeInTheDocument()
    expect(screen.queryByText('Team standup')).not.toBeInTheDocument()
    expect(synced).toBeNull()

    await user.click(screen.getByRole('button', { name: /Sync Google Calendar/ }))

    expect(await screen.findByText('Team standup')).toBeInTheDocument()
    expect(screen.getByText('09:30')).toBeInTheDocument()
    // An all-day event shows on every day it covers
    expect(screen.getAllByText('Offsite')).toHaveLength(2)
    expect(screen.getByText('synced 16/09/2026 11:45')).toBeInTheDocument()
    expect(synced!.get('from')).toBe('2026-09-01')
    expect(synced!.get('to')).toBe('2026-09-30')

    fireEvent.click(screen.getByText('16'))
    expect(await screen.findByText('16/09/2026 · 09:30–09:45')).toBeInTheDocument()
  })

  it('should show the last sync straight away when the page is opened again', async () => {
    let syncCalled = false
    server.use(
      http.get(`${API_BASE}/google-calendar/events`, () => HttpResponse.json(syncedEvents)),
      http.post(`${API_BASE}/google-calendar/sync`, () => {
        syncCalled = true
        return HttpResponse.json(syncedEvents)
      })
    )

    renderCalendar()

    expect(await screen.findByText('Team standup')).toBeInTheDocument()
    expect(syncCalled).toBe(false)
  })

  it('should show why a sync failed without hiding the rest of the calendar', async () => {
    server.use(
      http.get(`${API_BASE}/google-calendar/events`, () => HttpResponse.json(notSynced)),
      http.post(`${API_BASE}/google-calendar/sync`, () =>
        HttpResponse.json({ message: 'Google Calendar does not recognise this address.' }, { status: 502 })
      )
    )
    const user = userEvent.setup()

    renderCalendar()
    await user.click(await screen.findByRole('button', { name: /Sync Google Calendar/ }))

    expect(await screen.findByText(/Google Calendar does not recognise this address/)).toBeInTheDocument()
    expect(screen.getByText('Daily Planner')).toBeInTheDocument()
  })

  it('should offer no sync when no calendar is connected', async () => {
    renderCalendar()

    expect(await screen.findByText('Daily Planner')).toBeInTheDocument()
    expect(screen.queryByText('Google Calendar')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Sync Google Calendar/ })).not.toBeInTheDocument()
  })
})
