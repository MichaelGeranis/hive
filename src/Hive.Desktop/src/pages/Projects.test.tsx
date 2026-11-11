import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, within, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData, createProject, createProduct } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import Projects from './Projects'

const API_BASE = 'http://localhost:5002/api'

function renderProjects() {
  return render(
    <ToastProvider>
      <Projects />
    </ToastProvider>
  )
}

describe('Projects', () => {
  beforeEach(() => {
    resetMockData()
    server.use(
      http.get(`${API_BASE}/projects`, () => HttpResponse.json([
        createProject({ id: 'p1', name: 'matching-api', products: [{ id: 'pr1', name: 'Matching' }, { id: 'pr2', name: 'IPI' }] }),
        createProject({ id: 'p2', name: 'enrichment-jobs', products: [{ id: 'pr3', name: 'Enrichment' }] }),
        createProject({ id: 'p3', name: 'scratch', products: [] })
      ])),
      http.get(`${API_BASE}/products`, () => HttpResponse.json([
        createProduct({ id: 'pr1', name: 'Matching' }),
        createProduct({ id: 'pr2', name: 'IPI' }),
        createProduct({ id: 'pr3', name: 'Enrichment' })
      ]))
    )
  })

  it('should show the products each project serves', async () => {
    renderProjects()

    const products = await screen.findByRole('group', { name: 'Products of matching-api' })
    expect(within(products).getByText('Matching')).toBeInTheDocument()
    expect(within(products).getByText('IPI')).toBeInTheDocument()
    expect(within(screen.getByRole('group', { name: 'Products of scratch' })).getByText('No products')).toBeInTheDocument()
  })

  it('should filter projects by product', async () => {
    const user = userEvent.setup()
    renderProjects()

    const filter = await screen.findByRole('group', { name: 'Filter by product' })
    await user.click(within(filter).getByRole('button', { name: 'Enrichment' }))

    expect(screen.getByText('enrichment-jobs')).toBeInTheDocument()
    expect(screen.queryByText('matching-api')).not.toBeInTheDocument()
    expect(screen.queryByText('scratch')).not.toBeInTheDocument()
  })

  it('should send the chosen products when creating a project', async () => {
    const user = userEvent.setup()
    let posted: unknown = null
    server.use(
      http.post(`${API_BASE}/projects`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json(createProject({ id: 'p9', name: 'new-repo' }), { status: 201 })
      })
    )
    renderProjects()

    await user.click(await screen.findByRole('button', { name: /New Project/ }))
    await user.type(screen.getByLabelText('Name'), 'new-repo')
    const picker = screen.getByRole('group', { name: 'Products it serves' })
    await user.click(within(picker).getByRole('button', { name: 'IPI' }))
    await user.click(within(picker).getByRole('button', { name: 'Matching' }))
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(posted).toEqual({ name: 'new-repo', description: '', url: '', productIds: ['pr2', 'pr1'] })
  })
})
