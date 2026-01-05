import { useState, useEffect } from 'react'
import { Plus, Search, Edit, Trash2, ExternalLink, ChevronDown, ChevronUp } from 'lucide-react'
import { documentsApi } from '../services/api'
import type { Document } from '../types'

export default function Documents() {
  const [documents, setDocuments] = useState<Document[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingDocument, setEditingDocument] = useState<Document | null>(null)
  const [expandedDocuments, setExpandedDocuments] = useState<Set<string>>(new Set())
  const [formData, setFormData] = useState({
    title: '',
    content: '',
    url: '',
    tags: ''
  })

  useEffect(() => {
    loadDocuments()
  }, [])

  const loadDocuments = async () => {
    try {
      const documents = await documentsApi.getAll()
      setDocuments(documents)
    } catch (error) {
      console.error('Failed to load documents:', error)
      setDocuments([]) // Ensure documents is always an array
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editingDocument) {
        await documentsApi.update(editingDocument.id, formData)
      } else {
        await documentsApi.create(formData)
      }
      setShowForm(false)
      setEditingDocument(null)
      setFormData({ title: '', content: '', url: '', tags: '' })
      loadDocuments()
    } catch (error) {
      console.error('Failed to save document:', error)
    }
  }

  const handleEdit = (document: Document) => {
    setEditingDocument(document)
    setFormData({
      title: document.title,
      content: document.content,
      url: document.url || '',
      tags: document.tags
    })
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this document?')) {
      try {
        await documentsApi.delete(id)
        loadDocuments()
      } catch (error) {
        console.error('Failed to delete document:', error)
      }
    }
  }

  const toggleDocumentExpansion = (documentId: string) => {
    setExpandedDocuments(prev => {
      const newSet = new Set(prev)
      if (newSet.has(documentId)) {
        newSet.delete(documentId)
      } else {
        newSet.add(documentId)
      }
      return newSet
    })
  }

  const filteredDocuments = (documents || []).filter(doc =>
    doc.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
    doc.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
    doc.tags.toLowerCase().includes(searchTerm.toLowerCase())
  )

  if (loading) {
    return <div className="text-center py-8">Loading documents...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Documents</h1>
        <button
          onClick={() => setShowForm(true)}
          className="bg-amber-500 hover:bg-amber-600 text-white px-4 py-2 rounded-lg flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          Add Document
        </button>
      </div>

      <div className="flex gap-4">
        <div className="flex-1 relative">
          <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
          <input
            type="text"
            placeholder="Search documents..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
          />
        </div>
      </div>

      {showForm && (
        <div className="bg-white dark:bg-slate-800 rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold mb-4">
            {editingDocument ? 'Edit Document' : 'Add New Document'}
          </h2>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Title</label>
              <input
                type="text"
                required
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Content</label>
              <textarea
                rows={4}
                value={formData.content}
                onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">URL (optional)</label>
              <input
                type="url"
                value={formData.url}
                onChange={(e) => setFormData({ ...formData, url: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Tags (comma-separated)</label>
              <input
                type="text"
                value={formData.tags}
                onChange={(e) => setFormData({ ...formData, tags: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-transparent"
              />
            </div>
            <div className="flex gap-2">
              <button
                type="submit"
                className="bg-amber-500 hover:bg-amber-600 text-white px-4 py-2 rounded-lg"
              >
                {editingDocument ? 'Update' : 'Create'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowForm(false)
                  setEditingDocument(null)
                  setFormData({ title: '', content: '', url: '', tags: '' })
                }}
                className="bg-slate-300 hover:bg-slate-400 text-slate-700 px-4 py-2 rounded-lg"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="grid gap-4">
        {filteredDocuments.map((document) => (
          <div key={document.id} className="bg-white dark:bg-slate-800 rounded-lg shadow p-6">
            <div className="flex justify-between items-start mb-4">
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
                  {document.title}
                </h3>
                {document.content && (
                  <button
                    onClick={() => toggleDocumentExpansion(document.id)}
                    className="text-amber-500 hover:text-amber-600 text-sm font-medium flex items-center gap-1"
                  >
                    {expandedDocuments.has(document.id) ? (
                      <>
                        <ChevronUp className="w-4 h-4" />
                        Show less
                      </>
                    ) : (
                      <>
                        <ChevronDown className="w-4 h-4" />
                        Show more
                      </>
                    )}
                  </button>
                )}
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => handleEdit(document)}
                  className="text-slate-400 hover:text-amber-500"
                >
                  <Edit className="w-4 h-4" />
                </button>
                <button
                  onClick={() => handleDelete(document.id)}
                  className="text-slate-400 hover:text-red-500"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
            {expandedDocuments.has(document.id) && (
              <>
                {document.content && (
                  <p className="text-slate-600 dark:text-slate-300 mb-4 whitespace-pre-wrap">
                    {document.content}
                  </p>
                )}
                {document.url && (
                  <a
                    href={document.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-amber-500 hover:text-amber-600 flex items-center gap-1 mb-4"
                  >
                    <ExternalLink className="w-4 h-4" />
                    {document.url}
                  </a>
                )}
                {document.tags && (
                  <div className="flex flex-wrap gap-2 mb-4">
                    {document.tags.split(',').map((tag, index) => (
                      <span
                        key={index}
                        className="bg-amber-100 dark:bg-amber-900 text-amber-800 dark:text-amber-200 px-2 py-1 rounded text-sm"
                      >
                        {tag.trim()}
                      </span>
                    ))}
                  </div>
                )}
                <div className="text-sm text-slate-500 dark:text-slate-400">
                  Created: {new Date(document.createdAt).toLocaleDateString()}
                  {document.updatedAt && (
                    <> • Updated: {new Date(document.updatedAt).toLocaleDateString()}</>
                  )}
                </div>
              </>
            )}
          </div>
        ))}
      </div>

      {filteredDocuments.length === 0 && (
        <div className="text-center py-8 text-slate-500">
          {searchTerm ? 'No documents found matching your search.' : 'No documents yet. Add your first document!'}
        </div>
      )}
    </div>
  )
}