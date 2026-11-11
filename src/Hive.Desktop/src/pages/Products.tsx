import { useEffect, useState, useCallback } from 'react'
import { Plus, Package, ExternalLink, MoreVertical, Edit, Trash2, Search, Layers, FolderKanban, X } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import { productsApi, jiraSyncApi } from '../services/api'
import type { Product } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, LoadingState } from '../components/PageState'
import { JiraSyncButton } from '../components/JiraSyncButton'
import { describeProductSync } from '../utils/jiraSync'

const EMPTY_FORM = { name: '', description: '' }

export default function Products() {
  const { showError, showSuccess, showWarning } = useToast()
  const confirm = useConfirm()
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState(EMPTY_FORM)

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    setFormData(EMPTY_FORM)
  }, [])

  useEffect(() => {
    loadProducts()
  }, [])

  // A quiet reload refreshes the list without swapping the page for the loading state
  const loadProducts = async ({ quiet = false }: { quiet?: boolean } = {}) => {
    try {
      if (!quiet) setLoading(true)
      setProducts(await productsApi.getAll())
    } catch (err) {
      console.error(err)
    } finally {
      if (!quiet) setLoading(false)
    }
  }

  const handleJiraSync = async () => {
    try {
      const messages = describeProductSync(await jiraSyncApi.syncProducts())
      showSuccess(messages.success)
      if (messages.warning) showWarning(messages.warning)
      await loadProducts({ quiet: true })
    } catch (err) {
      console.error('Failed to sync products from Jira:', err)
      showError(getErrorMessage(err))
    }
  }

  const filteredProducts = () => {
    if (!searchQuery.trim()) return products

    const query = searchQuery.toLowerCase()
    return products.filter(p =>
      p.name.toLowerCase().includes(query) ||
      p.description?.toLowerCase().includes(query) ||
      p.projects.some(project => project.name.toLowerCase().includes(query))
    )
  }

  const clearSearch = () => {
    setSearchInput('')
    setSearchQuery('')
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editingId) {
        await productsApi.update(editingId, formData)
      } else {
        await productsApi.create(formData)
      }
      closeModal()
      loadProducts()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (product: Product) => {
    setFormData({ name: product.name, description: product.description || '' })
    setEditingId(product.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    const confirmed = await confirm({
      title: 'Delete this product?',
      message: 'Projects stop serving it. Tasks keep their components.',
      confirmLabel: 'Delete'
    })
    if (!confirmed) return
    try {
      await productsApi.delete(id)
      loadProducts()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  if (loading) {
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Products"
        subtitle="Jira components, the work that names them, and the projects that serve them"
        actions={
          <>
            <JiraSyncButton onSync={handleJiraSync} title="Add and update a product for every component in the Jira space" />
            <button
              onClick={() => setShowForm(true)}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
            >
              <Plus className="w-5 h-5" />
              New Product
            </button>
          </>
        }
      />

      {/* Form Modal */}
      {showForm && (
        <Modal title={editingId ? 'Edit Product' : 'Create Product'} onClose={closeModal}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <p className="text-sm text-slate-500">
              Use the product's Jira component name: tasks and parents are linked by their components.
            </p>
            <div>
              <label htmlFor="product-name" className="block text-sm font-medium text-slate-700 mb-1">Name</label>
              <input
                id="product-name"
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                required
              />
            </div>
            <div>
              <label htmlFor="product-description" className="block text-sm font-medium text-slate-700 mb-1">Description</label>
              <textarea
                id="product-description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                rows={3}
              />
            </div>
            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={closeModal}
                className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
              >
                {editingId ? 'Update' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search products or projects... (press Enter)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
          className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
        />
        {searchInput && (
          <button
            onClick={clearSearch}
            aria-label="Clear search"
            className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Products Grid */}
      {filteredProducts().length === 0 ? (
        <Card>
          <EmptyState
            icon={<Package />}
            title={products.length === 0
              ? 'No products yet. Sync them from Jira, or create one by hand.'
              : 'No products found'}
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProducts().map((product) => (
            <Card key={product.id}>
              <CardContent>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <div className="p-2 bg-amber-100 rounded-lg">
                      <Package className="w-5 h-5 text-amber-600" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="font-semibold text-slate-900 truncate">{product.name}</h3>
                      <p className="text-xs text-slate-400">{product.isFromJira ? 'Jira component' : 'Made by hand'}</p>
                    </div>
                  </div>
                  {/* A product from Jira is edited in Jira and goes when its component does */}
                  {!product.isFromJira && (
                    <div className="relative group">
                      <button className="p-1 hover:bg-slate-100 rounded" aria-label={`Actions for ${product.name}`}>
                        <MoreVertical className="w-5 h-5 text-slate-400" />
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => handleEdit(product)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                        >
                          <Edit className="w-4 h-4" />
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(product.id)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                        >
                          <Trash2 className="w-4 h-4" />
                          Delete
                        </button>
                      </div>
                    </div>
                  )}
                </div>

                {product.description && (
                  <p className="text-sm text-slate-500 mb-3 line-clamp-2">{product.description}</p>
                )}

                {/* Projects serving the product */}
                <div className="mb-3">
                  <div className="flex items-center gap-2 text-xs font-medium text-slate-500 mb-1">
                    <FolderKanban className="w-4 h-4 text-slate-400" />
                    Projects
                  </div>
                  {product.projects.length > 0 ? (
                    <ul className="space-y-1" aria-label={`Projects serving ${product.name}`}>
                      {product.projects.map(project => (
                        <li key={project.id} className="flex items-center gap-2 text-sm text-slate-700 min-w-0">
                          <span className="w-1.5 h-1.5 rounded-full bg-purple-400 flex-shrink-0" />
                          {project.url ? (
                            <a
                              href={project.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-amber-600 hover:underline truncate flex items-center gap-1"
                            >
                              {project.name}
                              <ExternalLink className="w-3 h-3 flex-shrink-0" />
                            </a>
                          ) : (
                            <span className="truncate">{project.name}</span>
                          )}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="text-sm text-slate-400">No projects yet. Link them on the Projects page.</p>
                  )}
                </div>

                {product.parentCount > 0 && (
                  <div className="flex items-center gap-2 text-sm text-slate-600">
                    <Layers className="w-4 h-4 text-indigo-500" />
                    <span>{product.parentCount} parent{product.parentCount !== 1 ? 's' : ''}</span>
                  </div>
                )}

                {/* Progress */}
                <div className="mt-4">
                  <div className="flex justify-between text-sm mb-1">
                    <span className="text-slate-500">Progress</span>
                    <span className="font-medium text-slate-900">{product.completedTasks}/{product.totalTasks} tasks</span>
                  </div>
                  <div className="w-full bg-slate-100 rounded-full h-2">
                    <div
                      className="bg-amber-500 h-2 rounded-full transition-all"
                      style={{
                        width: product.totalTasks > 0
                          ? `${(product.completedTasks / product.totalTasks) * 100}%`
                          : '0%'
                      }}
                    />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
