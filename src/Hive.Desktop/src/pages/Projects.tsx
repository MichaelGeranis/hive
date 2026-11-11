import { useEffect, useState, useCallback, useMemo } from 'react'
import { Plus, FolderKanban, ExternalLink, MoreVertical, Edit, Trash2, Search, Package, X } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import CollapsibleTags from '../components/CollapsibleTags'
import { projectsApi, productsApi } from '../services/api'
import type { Product, Project } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, LoadingState } from '../components/PageState'

const EMPTY_FORM = { name: '', description: '', url: '', productIds: [] as string[] }

export default function Projects() {
  const { showError } = useToast()
  const confirm = useConfirm()
  const [projects, setProjects] = useState<Project[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState(EMPTY_FORM)

  const resetForm = () => {
    setFormData(EMPTY_FORM)
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  useEffect(() => {
    loadProjects()
  }, [])

  const loadProjects = async () => {
    try {
      setLoading(true)
      const [projectData, productData] = await Promise.all([projectsApi.getAll(), productsApi.getAll()])
      setProjects(projectData)
      setProducts(productData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const [selectedProduct, setSelectedProduct] = useState<string | null>(null)

  // The products at least one project serves, for the filter
  const servedProducts = useMemo(() => {
    const names = new Set<string>()
    projects.forEach(p => p.products.forEach(product => names.add(product.name)))
    return Array.from(names).sort((a, b) => a.localeCompare(b))
  }, [projects])

  const clearFilters = () => {
    setSearchInput('')
    setSearchQuery('')
    setSelectedProduct(null)
  }

  const filteredProjects = () => {
    let result = projects

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(p =>
        p.name.toLowerCase().includes(query) ||
        p.description?.toLowerCase().includes(query) ||
        p.products.some(product => product.name.toLowerCase().includes(query))
      )
    }

    // Apply product filter
    if (selectedProduct) {
      result = result.filter(p => p.products.some(product => product.name === selectedProduct))
    }

    return result
  }

  const toggleProduct = (productId: string) => {
    setFormData(current => ({
      ...current,
      productIds: current.productIds.includes(productId)
        ? current.productIds.filter(id => id !== productId)
        : [...current.productIds, productId]
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editingId) {
        await projectsApi.update(editingId, formData)
      } else {
        await projectsApi.create(formData)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadProjects()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (project: Project) => {
    setFormData({
      name: project.name,
      description: project.description || '',
      url: project.url || '',
      productIds: project.products.map(p => p.id)
    })
    setEditingId(project.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    const confirmed = await confirm({
      title: 'Delete this project?',
      message: 'Its knowledge levels and the products it serves go with it.',
      confirmLabel: 'Delete'
    })
    if (!confirmed) return
    try {
      await projectsApi.delete(id)
      loadProjects()
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
        title="Projects"
        subtitle="Code repositories and the products they serve"
        actions={
          <button
            onClick={() => setShowForm(true)}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            <Plus className="w-5 h-5" />
            New Project
          </button>
        }
      />

      {/* Form Modal */}
      {showForm && (
        <Modal title={editingId ? 'Edit Project' : 'Create Project'} onClose={closeModal}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="project-name" className="block text-sm font-medium text-slate-700 mb-1">Name</label>
              <input
                id="project-name"
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                required
              />
            </div>
            <div>
              <label htmlFor="project-description" className="block text-sm font-medium text-slate-700 mb-1">Description</label>
              <textarea
                id="project-description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                rows={3}
              />
            </div>
            <div>
              <label htmlFor="project-url" className="block text-sm font-medium text-slate-700 mb-1">URL</label>
              <input
                id="project-url"
                type="url"
                value={formData.url}
                onChange={(e) => setFormData({ ...formData, url: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                placeholder="https://github.com/org/project"
              />
            </div>
            <fieldset>
              <legend className="block text-sm font-medium text-slate-700 mb-1">Products it serves</legend>
              {products.length === 0 ? (
                <p className="text-sm text-slate-500">No products yet. Sync them from Jira on the Products page.</p>
              ) : (
                <div className="flex flex-wrap gap-2 max-h-40 overflow-y-auto">
                  {products.map(product => {
                    const selected = formData.productIds.includes(product.id)
                    return (
                      <button
                        key={product.id}
                        type="button"
                        onClick={() => toggleProduct(product.id)}
                        aria-pressed={selected}
                        className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                          selected
                            ? 'bg-amber-500 text-white'
                            : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                        }`}
                      >
                        {product.name}
                      </button>
                    )
                  })}
                </div>
              )}
            </fieldset>
            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={() => { setShowForm(false); setEditingId(null); resetForm() }}
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

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search projects... (press Enter)"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
          />
          {(searchInput || selectedProduct) && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Products */}
        {servedProducts.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap" role="group" aria-label="Filter by product">
            <Package className="w-4 h-4 text-slate-400" />
            <CollapsibleTags
              items={servedProducts}
              alwaysShow={(name) => name === selectedProduct}
              renderItem={(name) => (
                <button
                  key={name}
                  onClick={() => setSelectedProduct(selectedProduct === name ? null : name)}
                  aria-pressed={selectedProduct === name}
                  className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                    selectedProduct === name
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {name}
                </button>
              )}
            />
          </div>
        )}
      </div>

      {/* Projects Grid */}
      {filteredProjects().length === 0 ? (
        <Card>
          <EmptyState icon={<FolderKanban />} title="No projects found" />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProjects().map((project) => (
            <Card key={project.id}>
              <CardContent>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3 flex-1">
                    <div className="p-2 bg-purple-100 rounded-lg">
                      <FolderKanban className="w-5 h-5 text-purple-600" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-slate-900">{project.name}</h3>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 rounded-sm" aria-label={`Actions for ${project.name}`}>
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(project)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(project.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>

                {project.description && (
                  <p className="text-sm text-slate-500 mb-3 line-clamp-2">{project.description}</p>
                )}

                <div className="flex items-center gap-2 mb-3" role="group" aria-label={`Products of ${project.name}`}>
                  <Package className="w-4 h-4 text-slate-400 shrink-0" />
                  {project.products.length > 0 ? (
                    <div className="flex flex-wrap gap-1">
                      <CollapsibleTags
                        items={project.products}
                        renderItem={(product) => (
                          <span
                            key={product.id}
                            className="px-2 py-0.5 text-xs bg-amber-50 text-amber-700 rounded-full"
                          >
                            {product.name}
                          </span>
                        )}
                      />
                    </div>
                  ) : (
                    <span className="text-xs text-slate-400">No products</span>
                  )}
                </div>

                {project.url && (
                  <div className="flex items-center gap-2 text-sm text-slate-600">
                    <ExternalLink className="w-4 h-4 text-slate-400" />
                    <a
                      href={project.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-amber-600 hover:underline truncate"
                    >
                      {project.url.replace(/^https?:\/\//, '')}
                    </a>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
