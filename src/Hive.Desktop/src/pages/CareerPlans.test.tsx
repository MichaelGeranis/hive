import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, within, userEvent, waitFor } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import {
  resetMockData,
  createCareerOverview,
  createCareerPlan,
  createCareerPlanSummary,
  createRoadmapLibraryItem,
  createSkillRoadmap,
  createSkillRoadmapSummary,
  CAREER_BADGES
} from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import type { CareerAttention, CareerPlan, CareerPlanSummary, SkillLevelChange, SkillRoadmapSummary } from '../types'
import CareerPlans from './CareerPlans'

const API_BASE = 'http://localhost:5002/api'

function renderCareerPlans({
  url = '/career-plans',
  plans = [createCareerPlanSummary()],
  withoutPlan = [],
  roadmaps = [createSkillRoadmapSummary()],
  plan
}: { url?: string; plans?: CareerPlanSummary[]; withoutPlan?: CareerAttention[]; roadmaps?: SkillRoadmapSummary[]; plan?: CareerPlan } = {}) {
  server.use(
    http.get(`${API_BASE}/career-plans`, () => HttpResponse.json(plans)),
    http.get(`${API_BASE}/career-plans/overview`, () => HttpResponse.json(createCareerOverview({ directReportsWithoutPlan: withoutPlan }))),
    http.get(`${API_BASE}/skill-roadmaps`, () => HttpResponse.json(roadmaps))
  )
  if (plan) server.use(http.get(`${API_BASE}/career-plans/:id`, () => HttpResponse.json(plan)))
  return render(
    <ToastProvider>
      <CareerPlans />
    </ToastProvider>,
    { routerProps: { initialEntries: [url] } }
  )
}

const JANE: CareerAttention = { directReportId: '2', fullName: 'Jane Smith', planId: null, lastChangedAt: null }

describe('CareerPlans', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should show each plan with its stage, focus count and last change, and no scores', async () => {
    renderCareerPlans({
      plans: [
        createCareerPlanSummary({ id: 'cp1', directReportName: 'John Doe' }),
        createCareerPlanSummary({
          id: 'cp2', directReportId: '2', directReportName: 'Jane Smith', currentStage: 'Senior',
          workingOnCount: 0, lastChangedAt: '2026-06-15T10:00:00Z', isQuiet: true
        })
      ]
    })

    const john = await screen.findByRole('button', { name: "Open John Doe's plan" })
    expect(within(john).getByLabelText('Career stage Mid')).toBeInTheDocument()
    expect(within(john).getByText('Backend Engineer')).toBeInTheDocument()
    expect(within(john).getByText('1 working on')).toBeInTheDocument()
    expect(within(john).getByText('Last changed 20/09/2026')).toBeInTheDocument()
    expect(within(john).queryByText('Quiet')).not.toBeInTheDocument()

    const jane = screen.getByRole('button', { name: "Open Jane Smith's plan" })
    expect(within(jane).getByLabelText('Career stage Senior')).toBeInTheDocument()
    expect(within(jane).getByText('Quiet')).toBeInTheDocument()

    // Nothing comparable between people
    expect(screen.queryByText(/XP|Player level|%|badges/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('tablist', { name: 'Plan status' })).not.toBeInTheDocument()
  })

  it('should list who has no plan yet, each with a shortcut to create one', async () => {
    const user = userEvent.setup()
    renderCareerPlans({ withoutPlan: [JANE] })

    await user.click(await screen.findByRole('button', { name: 'Create plan for Jane Smith' }))

    expect(screen.getByLabelText('Direct report')).toHaveValue('2')
  })

  it('should guide the manager to the library when there are no roadmaps', async () => {
    renderCareerPlans({ plans: [], roadmaps: [], withoutPlan: [JANE] })

    expect(await screen.findByText('Start with a roadmap')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /New plan/ })).toBeDisabled()
  })

  it('should create a plan with the current role\'s stage, offering only people without a plan', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.post(`${API_BASE}/career-plans`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(createCareerPlan({ id: 'cp9' }), { status: 201 })
      })
    )
    renderCareerPlans({ plans: [createCareerPlanSummary()], withoutPlan: [JANE] })

    await user.click(await screen.findByRole('button', { name: /New plan/ }))
    const people = within(screen.getByLabelText('Direct report')).getAllByRole('option').map(o => o.textContent)
    expect(people).toEqual(['Choose someone...', 'Jane Smith'])
    expect(screen.getByText(/not a goal to reach/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/Target/)).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Direct report'), '2')
    await user.selectOptions(screen.getByLabelText("Their current role's stage"), 'Senior')
    await user.click(screen.getByRole('button', { name: 'Create plan' }))

    await waitFor(() => expect(body).toEqual({ directReportId: '2', roadmapId: 'rm1', currentStage: 'Senior' }))
  })

  it('should open a plan with its stage, skill map and badges, and the game on the side', async () => {
    const user = userEvent.setup()
    renderCareerPlans()

    await user.click(await screen.findByRole('button', { name: "Open John Doe's plan" }))

    const tree = await screen.findByRole('list', { name: 'Skill roadmap' })
    const apis = within(tree).getByRole('listitem', { name: 'Section APIs' })
    expect(within(apis).getByRole('button', { name: 'REST design, working on, level 2' })).toHaveTextContent('2 / exp 3')
    expect(within(apis).getByRole('button', { name: 'SQL, has, level 4' })).toBeInTheDocument()
    // A skill of a later stage says which
    expect(within(tree).getByRole('button', { name: 'Mentoring, has, level 3' })).toHaveTextContent('Senior+')

    expect(screen.getByRole('heading', { name: 'John Doe' }).parentElement).toContainElement(screen.getAllByLabelText('Career stage Mid')[0])
    expect(within(screen.getByLabelText('Game')).getByText(/Player level 2/)).toBeInTheDocument()
    const badges = screen.getByRole('list', { name: 'Badges' })
    expect(within(badges).getByRole('listitem', { name: 'Got It (earned)' })).toBeInTheDocument()
    expect(within(badges).getByRole('listitem', { name: 'Role Ready (locked)' })).toBeInTheDocument()
    expect(screen.queryByText(/target/i)).not.toBeInTheDocument()
  })

  it('should read their skills against their own role', async () => {
    renderCareerPlans({ url: '/career-plans?plan=cp1' })
    expect(await screen.findByLabelText('Ladder reading')).toHaveTextContent('Skills read at their Mid role')
    expect(screen.getByRole('listitem', { name: 'Senior: 40% of expectations met' })).toBeInTheDocument()
  })

  it('should say when their skills read above their role', async () => {
    const plan = createCareerPlan()
    renderCareerPlans({
      url: '/career-plans?plan=cp1',
      plan: { ...plan, ladder: { ...plan.ladder, reading: 'AboveRole', skillsStage: 'Senior' } }
    })
    expect(await screen.findByLabelText('Ladder reading'))
      .toHaveTextContent("Skills read above their Mid role (they meet Senior's expectations)")
  })

  it('should say when a plan is not assessed yet', async () => {
    const plan = createCareerPlan()
    renderCareerPlans({
      url: '/career-plans?plan=cp1',
      plan: { ...plan, ladder: { ...plan.ladder, reading: 'NotAssessed', skillsStage: null } }
    })
    expect(await screen.findByLabelText('Ladder reading')).toHaveTextContent('Not assessed yet')
  })

  it('should show strengths and weaknesses against what their stage expects', async () => {
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    const strengths = await screen.findByRole('list', { name: 'Strengths' })
    expect(within(strengths).getByRole('button', { name: /SQL/ })).toHaveTextContent('4 · expects 3')
    expect(within(strengths).getByRole('button', { name: /Mentoring/ })).toHaveTextContent('Senior skill')
    const weaknesses = screen.getByRole('list', { name: 'Weaknesses' })
    expect(within(weaknesses).getByRole('button', { name: /Caching/ })).toHaveTextContent('0 · expects 3')
  })

  it('should filter the skill map by has, working on and not yet', async () => {
    const user = userEvent.setup()
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    const tree = await screen.findByRole('list', { name: 'Skill roadmap' })
    expect(within(tree).getByRole('button', { name: /^Caching/ })).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Has (2)' }))
    expect(within(tree).getByRole('button', { name: /^SQL/ })).toBeInTheDocument()
    expect(within(tree).getByRole('button', { name: /^Mentoring/ })).toBeInTheDocument()
    expect(within(tree).queryByRole('button', { name: /^Caching/ })).not.toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Working on (1)' }))
    expect(within(tree).getByRole('button', { name: /^REST design/ })).toBeInTheDocument()
    expect(within(tree).queryByRole('button', { name: /^SQL/ })).not.toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Not yet (2)' }))
    expect(within(tree).getByRole('button', { name: /^Caching/ })).toBeInTheDocument()
    expect(within(tree).getByRole('button', { name: /^Code review/ })).toBeInTheDocument()
    expect(within(tree).queryByRole('listitem', { name: 'Section APIs' })).toBeInTheDocument()
  })

  it('should take a skill out of focus from Working on now', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.put(`${API_BASE}/career-plans/cp1/skills/sk1/focus`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(createCareerPlan())
      })
    )
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    const focus = await screen.findByRole('list', { name: 'Working on now' })
    expect(within(focus).getByText('Reviewing the orders API')).toBeInTheDocument()
    await user.click(within(focus).getByRole('button', { name: 'Done focusing on REST design' }))

    await waitFor(() => expect(body).toEqual({ isFocus: false }))
  })

  it('should put a skill in focus from its editor', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.put(`${API_BASE}/career-plans/cp1/skills/sk2/focus`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(createCareerPlan())
      })
    )
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    await user.click(await screen.findByRole('button', { name: 'Caching, not yet, level 0' }))
    expect(screen.getByText(/expected level/)).toHaveTextContent('3 Practising')
    await user.click(screen.getByRole('switch'))

    await waitFor(() => expect(body).toEqual({ isFocus: true }))
  })

  it('should rate a skill and celebrate the level-up and the badge it unlocked', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    const plan = createCareerPlan()
    const levelledUp: CareerPlan = {
      ...plan,
      game: { ...plan.game, xp: 150, playerLevel: 3, playerTitle: 'Adventurer', currentLevelXp: 150, nextLevelXp: 300 }
    }
    server.use(
      http.put(`${API_BASE}/career-plans/cp1/skills/sk1/level`, async ({ request }) => {
        body = await request.json()
        const change: SkillLevelChange = {
          plan: levelledUp,
          skillLevelledUp: true,
          playerLevelledUp: true,
          newBadges: [{ ...CAREER_BADGES[1], isEarned: true }]
        }
        return HttpResponse.json(change)
      })
    )
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    await user.click(await screen.findByRole('button', { name: 'REST design, working on, level 2' }))
    await user.click(screen.getByRole('radio', { name: '3 Practising' }))
    await user.clear(screen.getByLabelText('Evidence'))
    await user.type(screen.getByLabelText('Evidence'), 'Designed the payments API')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(body).toEqual({ level: 3, evidence: 'Designed the payments API' }))
    expect(await screen.findByText('Level up! John is now an Adventurer (level 3)')).toBeInTheDocument()
    expect(screen.getByText('Badge unlocked: Got It')).toBeInTheDocument()
  })

  it('should change the current role\'s stage after a promotion', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.put(`${API_BASE}/career-plans/cp1`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(createCareerPlan({ currentStage: 'Senior' }))
      })
    )
    renderCareerPlans({ url: '/career-plans?plan=cp1' })

    await user.click(await screen.findByRole('button', { name: /Edit/ }))
    await user.selectOptions(screen.getByLabelText("Their current role's stage"), 'Senior')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(body).toEqual({ currentStage: 'Senior', notes: 'Wants to lead the payments rewrite' }))
  })

  it('should add a roadmap from the library', async () => {
    const user = userEvent.setup()
    let added: string | null = null
    server.use(
      http.get(`${API_BASE}/skill-roadmaps/library`, () => HttpResponse.json([
        createRoadmapLibraryItem(),
        createRoadmapLibraryItem({ key: 'qa-engineer', name: 'QA Engineer', isAdded: true })
      ])),
      http.post(`${API_BASE}/skill-roadmaps/library/:key`, ({ params }) => {
        added = params.key as string
        return HttpResponse.json(createSkillRoadmap({ skillCount: 40 }))
      })
    )
    renderCareerPlans({ url: '/career-plans?tab=roadmaps', roadmaps: [] })

    const library = await screen.findByLabelText('Roadmap library')
    expect(await within(library).findByText('Added')).toBeInTheDocument()
    await user.click(within(library).getByRole('button', { name: 'Add Backend Engineer' }))

    await waitFor(() => expect(added).toBe('backend-engineer'))
    expect(await screen.findByText('Backend Engineer added: 40 skills to play with')).toBeInTheDocument()
  })

  it('should send the stage a roadmap skill is introduced at', async () => {
    const user = userEvent.setup()
    let body: unknown = null
    server.use(
      http.put(`${API_BASE}/skill-roadmaps/rm1/skills/rs2`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(createSkillRoadmap())
      })
    )
    renderCareerPlans({ url: '/career-plans?tab=roadmaps&roadmap=rm1' })

    await user.click(await screen.findByRole('button', { name: 'GraphQL' }))
    expect(screen.getByLabelText('Introduced at')).toHaveValue('Senior')
    await user.selectOptions(screen.getByLabelText('Introduced at'), 'Staff')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(body).toEqual({
      section: 'APIs',
      title: 'GraphQL',
      description: '',
      category: 'Hard',
      isOptional: true,
      fromStage: 'Staff'
    }))
  })

  it('should show a roadmap as a tree of its sections', async () => {
    renderCareerPlans({ url: '/career-plans?tab=roadmaps&roadmap=rm1' })

    const tree = await screen.findByRole('list', { name: 'Skill roadmap' })
    const apis = within(tree).getByRole('listitem', { name: 'Section APIs' })
    expect(within(apis).getByRole('button', { name: 'REST design' })).toBeInTheDocument()
    expect(within(apis).getByRole('button', { name: 'GraphQL' })).toBeInTheDocument()
    expect(within(tree).getByRole('button', { name: 'Add a skill to Collaboration' })).toBeInTheDocument()
    // A skill introduced after Junior says from when
    expect(within(within(apis).getByRole('button', { name: 'GraphQL' })).getByText('Senior+')).toBeInTheDocument()
    expect(within(within(apis).getByRole('button', { name: 'REST design' })).queryByText(/\+$/)).not.toBeInTheDocument()
    expect(screen.getByLabelText('Skills per stage')).toHaveTextContent('Senior 3')
  })
})
