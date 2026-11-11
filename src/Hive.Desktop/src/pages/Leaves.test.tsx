import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, fireEvent, waitFor, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import Leaves from './Leaves'

const API_BASE = 'http://localhost:5002/api'

function renderLeaves() {
  return render(
    <ToastProvider>
      <Leaves />
    </ToastProvider>
  )
}

describe('Leaves: adding a public holiday', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should create it for the whole team through the public holiday endpoint', async () => {
    let publicHolidayBody: unknown = null
    let singleLeavePosted = false
    server.use(
      http.post(`${API_BASE}/leaves/public-holiday`, async ({ request }) => {
        publicHolidayBody = await request.json()
        return HttpResponse.json(
          { totalCreated: 3, teamMembersAffected: ['A', 'B', 'C'], skippedMembers: [], createdLeaves: [] },
          { status: 201 }
        )
      }),
      http.post(`${API_BASE}/leaves`, () => {
        singleLeavePosted = true
        return HttpResponse.json({ message: 'should not be called' }, { status: 400 })
      })
    )
    const user = userEvent.setup()
    renderLeaves()

    await user.click(await screen.findByRole('button', { name: /Add Leave/ }))
    const typeSelect = screen.getByRole('option', { name: 'Public Holiday' }).closest('select')!
    await user.selectOptions(typeSelect, 'PublicHoliday')
    await user.type(screen.getByPlaceholderText(/Christmas Day/), 'Malakasa')
    // The form is a dialog, rendered over the page rather than inside it
    const dialog = screen.getByRole('dialog', { name: 'Add Leave' })
    const [start, end] = Array.from(dialog.querySelectorAll<HTMLInputElement>('input[type="date"]'))
    fireEvent.change(start, { target: { value: '2026-09-25' } })
    fireEvent.change(end, { target: { value: '2026-09-25' } })
    await user.click(dialog.querySelector<HTMLButtonElement>('button[type="submit"]')!)

    expect(await screen.findByText('Public holiday added for 3 team members')).toBeInTheDocument()
    expect(publicHolidayBody).toEqual({
      startDate: '2026-09-25',
      endDate: '2026-09-25',
      name: 'Malakasa',
      notes: ''
    })
    expect(singleLeavePosted).toBe(false)
  })

  it('should say who was skipped because they already had leave booked', async () => {
    server.use(
      http.post(`${API_BASE}/leaves/public-holiday`, () =>
        HttpResponse.json(
          { totalCreated: 2, teamMembersAffected: ['A', 'B'], skippedMembers: ['Jane Smith'], createdLeaves: [] },
          { status: 201 }
        )
      )
    )
    const user = userEvent.setup()
    renderLeaves()

    await user.click(await screen.findByRole('button', { name: /Add Leave/ }))
    await user.selectOptions(screen.getByRole('option', { name: 'Public Holiday' }).closest('select')!, 'PublicHoliday')
    await user.type(screen.getByPlaceholderText(/Christmas Day/), 'Christmas Day')
    // The form is a dialog, rendered over the page rather than inside it
    const dialog = screen.getByRole('dialog', { name: 'Add Leave' })
    const [start, end] = Array.from(dialog.querySelectorAll<HTMLInputElement>('input[type="date"]'))
    fireEvent.change(start, { target: { value: '2026-12-25' } })
    fireEvent.change(end, { target: { value: '2026-12-25' } })
    await user.click(dialog.querySelector<HTMLButtonElement>('button[type="submit"]')!)

    await waitFor(() =>
      expect(
        screen.getByText(
          'Public holiday added for 2 team members. Skipped 1 with leave already booked: Jane Smith'
        )
      ).toBeInTheDocument()
    )
  })
})
