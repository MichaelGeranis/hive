import { describe, it, expect, beforeEach } from 'vitest'
import { http, HttpResponse } from 'msw'
import { render, screen, within, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData, createProduct } from '../test/mocks/handlers'
import { server } from '../test/mocks/server'
import type { Product } from '../types'
import Products from './Products'

const API_BASE = 'http://localhost:5002/api'

function renderProducts(products: Product[][]) {
  let load = 0
  server.use(
    // Each load returns the next list, so a reload after syncing shows the synced products
    http.get(`${API_BASE}/products`, () => HttpResponse.json(products[Math.min(load++, products.length - 1)]))
  )
  return render(
    <ToastProvider>
      <Products />
    </ToastProvider>
  )
}

describe('Products', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should list the projects serving each product and its task progress', async () => {
    renderProducts([[
      createProduct({
        name: 'Matching',
        totalTasks: 4,
        completedTasks: 3,
        projects: [
          { id: 'p1', name: 'matching-api', url: 'https://github.com/acme/matching-api' },
          { id: 'p2', name: 'matching-ui', url: '' }
        ]
      })
    ]])

    const list = await screen.findByRole('list', { name: 'Projects serving Matching' })
    expect(within(list).getByRole('link', { name: 'matching-api' })).toHaveAttribute('href', 'https://github.com/acme/matching-api')
    expect(within(list).getByText('matching-ui')).toBeInTheDocument()
    expect(screen.getByText('3/4 tasks')).toBeInTheDocument()
  })

  it('should say when a product has no projects yet', async () => {
    renderProducts([[createProduct({ name: 'IPI', projects: [] })]])

    expect(await screen.findByText(/No projects yet/)).toBeInTheDocument()
  })

  it('should offer edit and delete only for products made by hand', async () => {
    renderProducts([[
      createProduct({ id: 'pr1', name: 'Matching', isFromJira: true }),
      createProduct({ id: 'pr2', name: 'Internal tooling', isFromJira: false, jiraComponentId: null })
    ]])

    expect(await screen.findByRole('button', { name: 'Actions for Internal tooling' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Actions for Matching' })).not.toBeInTheDocument()
  })

  it('should sync products from Jira and show the result', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(`${API_BASE}/jira-sync/products`, () => HttpResponse.json({
        jiraComponentCount: 2,
        created: [{ productId: 'pr2', jiraComponentId: '10043', name: 'IPI' }],
        updated: [],
        unchangedCount: 1,
        removed: [],
        errors: [],
        syncedAt: new Date().toISOString()
      }))
    )
    renderProducts([
      [createProduct({ id: 'pr1', name: 'Matching' })],
      [createProduct({ id: 'pr1', name: 'Matching' }), createProduct({ id: 'pr2', name: 'IPI', jiraComponentId: '10043' })]
    ])

    await user.click(await screen.findByRole('button', { name: /Sync Jira/ }))

    expect(await screen.findByText('Products synced from Jira: 1 added (2 components in Jira)')).toBeInTheDocument()
    expect(await screen.findByText('IPI')).toBeInTheDocument()
  })

  it('should create a product by hand', async () => {
    const user = userEvent.setup()
    let posted: unknown = null
    server.use(
      http.post(`${API_BASE}/products`, async ({ request }) => {
        posted = await request.json()
        return HttpResponse.json(createProduct({ id: 'pr3', name: 'Internal tooling' }), { status: 201 })
      })
    )
    renderProducts([[]])

    await user.click(await screen.findByRole('button', { name: /New Product/ }))
    await user.type(screen.getByLabelText('Name'), 'Internal tooling')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(posted).toEqual({ name: 'Internal tooling', description: '' })
  })
})
