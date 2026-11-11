import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, within, userEvent, waitFor } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import {
  createInitiative,
  createPlanningBoard,
  createPlanReview,
  createQuarter,
  resetMockData
} from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import { paintCell } from '../utils/planningDisplay'
import type { PlanningBoard } from '../types'
import QuarterlyPlanning from './QuarterlyPlanning'

const API_BASE = 'http://localhost:5002/api/quarterly-planning'

function renderPlanning(board: PlanningBoard = createPlanningBoard(), url = '/quarterly-planning') {
  server.use(
    http.get(`${API_BASE}/quarters`, () => HttpResponse.json([board.quarter])),
    http.get(`${API_BASE}/quarters/:id/board`, () => HttpResponse.json(board)),
    http.get(`${API_BASE}/quarters/:id/review`, () => HttpResponse.json(createPlanReview())),
    http.get(`${API_BASE}/calibration`, () => HttpResponse.json({ sizes: [], deliveredCount: 0 }))
  )
  return render(
    <ToastProvider>
      <QuarterlyPlanning />
    </ToastProvider>,
    { routerProps: { initialEntries: [url] } }
  )
}

describe('QuarterlyPlanning', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should show the steps with what each still needs', async () => {
    renderPlanning()

    const steps = await screen.findByRole('navigation', { name: 'Planning steps' })
    expect(within(steps).getByText('Options')).toBeInTheDocument()
    expect(within(steps).getByText('1 option ready')).toBeInTheDocument()
    expect(within(steps).getByText('Nothing planned yet')).toBeInTheDocument()
  })

  it('should list options with priority, due date and dependencies', async () => {
    const board = createPlanningBoard({
      initiatives: [
        createInitiative({
          dueDate: '2026-11-20',
          externalDependencies: [{ id: 'd1', initiativeId: 'opt-1', team: 'CORE', need: 'Release plan', status: 0 }]
        }),
        createInitiative({ id: 'left', name: 'Live Matching', kind: 1, rank: 0 })
      ]
    })
    renderPlanning(board)

    const row = (await screen.findByText('Euterpe Integration')).closest('tr')!
    expect(within(row).getByText('1')).toBeInTheDocument()
    expect(within(row).getByText('20/11/2026')).toBeInTheDocument()
    expect(within(row).getByText('CORE')).toBeInTheDocument()
    const leftover = screen.getByText('Live Matching').closest('tr')!
    expect(within(leftover).getByText('0')).toBeInTheDocument()
  })

  it('should add an option with the status it starts in', async () => {
    let posted: unknown = null
    server.use(
      http.post(`${API_BASE}/initiatives`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json({ id: 'new' }, { status: 201 })
      })
    )
    renderPlanning()

    await userEvent.type(await screen.findByLabelText('New option name'), 'WH off-cycle')
    expect(screen.queryByLabelText('New option size')).not.toBeInTheDocument()
    await userEvent.selectOptions(screen.getByLabelText('New option status'), 'In Solution Design')
    await userEvent.click(screen.getByRole('button', { name: /Add$/ }))

    await waitFor(() => expect(posted).toMatchObject({ quarterId: 'q4', name: 'WH off-cycle', status: 1, kind: 0 }))
  })

  it('should show the waterline and capacity on the capacity step', async () => {
    renderPlanning(createPlanningBoard(), '/quarterly-planning?step=capacity')

    expect(await screen.findByText('Waterline')).toBeInTheDocument()
    expect(screen.getByText('For options')).toBeInTheDocument()
    expect(screen.getByText(/40% of 120 tickets were bugs, support or maintenance/)).toBeInTheDocument()
    expect(screen.getByText('50%', { selector: 'span' })).toBeInTheDocument()
  })

  it("should fill the mix target from the team's past work and save it", async () => {
    let saved: unknown = null
    server.use(
      http.put(`${API_BASE}/quarters/:id`, async ({ request }) => {
        saved = await request.json()
        return HttpResponse.json(createQuarter())
      })
    )
    renderPlanning(createPlanningBoard(), '/quarterly-planning?step=capacity')

    await userEvent.click(await screen.findByRole('button', { name: /Use the team's average/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Save target' }))

    await waitFor(() => expect(saved).toMatchObject({ productRoadmapPercent: 40, techRoadmapPercent: 20, maintenancePercent: 40 }))
  })

  it('should leave out the what-if scenario', async () => {
    renderPlanning(createPlanningBoard(), '/quarterly-planning?step=capacity')

    expect(await screen.findByText('Waterline')).toBeInTheDocument()
    expect(screen.queryByText('What if…')).not.toBeInTheDocument()
  })

  it('should not offer stretch, a start sprint, value increments or option dependencies on an option', async () => {
    renderPlanning(createPlanningBoard(), '/quarterly-planning?option=opt-1')

    const drawer = await screen.findByRole('dialog', { name: 'Euterpe Integration' })
    expect(within(drawer).getByRole('button', { name: 'Committed' })).toBeInTheDocument()
    expect(within(drawer).queryByRole('button', { name: 'Stretch' })).not.toBeInTheDocument()
    expect(within(drawer).queryByText('Can start from')).not.toBeInTheDocument()
    expect(within(drawer).queryByText('Value increments')).not.toBeInTheDocument()
    expect(within(drawer).queryByText('Needs another option first')).not.toBeInTheDocument()
  })

  it('should open an option from the URL and name someone suggested as lead', async () => {
    let member: unknown = null
    server.use(
      http.post(`${API_BASE}/initiatives/opt-1/members`, async ({ request }) => {
        member = await request.json()
        return HttpResponse.json({ id: 'm1' }, { status: 201 })
      })
    )
    const board = createPlanningBoard({
      initiatives: [
        createInitiative({
          suggestions: [{ directReportId: 'p1', name: 'Alice Smith', knowledgeLevel: 4, taskCount: 12, freeEffort: 1.5, isMember: false }]
        })
      ]
    })
    renderPlanning(board, '/quarterly-planning?option=opt-1')

    const drawer = await screen.findByRole('dialog', { name: 'Euterpe Integration' })
    expect(within(drawer).getByText(/knowledge 4\/5/)).toBeInTheDocument()
    await userEvent.click(within(drawer).getByRole('button', { name: 'Lead' }))

    await waitFor(() => expect(member).toEqual({ directReportId: 'p1', role: 1 }))
  })

  it('should commit the plan from the review step', async () => {
    let committed = false
    server.use(
      http.post(`${API_BASE}/quarters/q4/commit`, () => {
        committed = true
        return HttpResponse.json(createQuarter({ committedAt: '2026-09-28T10:00:00Z' }))
      })
    )
    renderPlanning(createPlanningBoard(), '/quarterly-planning?step=review')

    expect(await screen.findByText('Q4 2026 plan, not committed yet')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /Commit the plan/ }))

    await waitFor(() => expect(committed).toBe(true))
  })

  it('should not show an insights panel next to the steps', async () => {
    const board = createPlanningBoard({
      insights: [{ type: 'NoLead', severity: 1, title: 'Euterpe Integration has no lead', message: 'Name who leads it.', initiativeId: 'opt-1' }]
    })
    renderPlanning(board)

    expect(await screen.findByText('Euterpe Integration')).toBeInTheDocument()
    expect(screen.queryByText('Euterpe Integration has no lead')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /insights/i })).not.toBeInTheDocument()
  })

  it('should invite creating a quarter when there is none', async () => {
    server.use(http.get(`${API_BASE}/quarters`, () => HttpResponse.json([])))
    render(
      <ToastProvider>
        <QuarterlyPlanning />
      </ToastProvider>
    )

    expect(await screen.findByText('No quarters yet')).toBeInTheDocument()
  })
})

describe('sizing', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should size only options ready for commitment', async () => {
    const board = createPlanningBoard({
      initiatives: [
        createInitiative(),
        createInitiative({ id: 'disc', name: 'Live Matching', status: 0, tshirtSize: null, effort: null, rank: 2 })
      ]
    })
    renderPlanning(board)

    const ready = (await screen.findByText('Euterpe Integration')).closest('tr')!
    expect(within(ready).getByLabelText('Size of Euterpe Integration')).toHaveValue('L')
    const discovery = screen.getByText('Live Matching').closest('tr')!
    expect(within(discovery).queryByLabelText('Size of Live Matching')).not.toBeInTheDocument()
    expect(within(discovery).getByTitle('Sized once it is ready for commitment')).toBeInTheDocument()
  })
})

describe('paintCell', () => {
  const a = { initiativeId: 'a', percent: 100 }

  it('should replace the cell with the brush', () => {
    expect(paintCell([a], 'b')).toEqual([{ initiativeId: 'b', percent: 100 }])
  })

  it('should clear the cell with the eraser', () => {
    expect(paintCell([a], 'erase')).toEqual([])
  })
})
