import { useEffect, useState, useCallback } from 'react'
import { Plus, Star, MoreVertical, Edit, Trash2, Search, X, Users } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { reviewsApi, directReportsApi } from '../services/api'
import type { PerformanceReview, DirectReport } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

// Map rating enum values to star counts:
// NotRated (0) → 0 stars, NeedsImprovement (1) → 2 stars, MeetsExpectations (2) → 3 stars,
// ExceedsExpectations (3) → 4 stars, Outstanding (4) → 5 stars
const getRatingStarCount = (rating: number): number => {
  if (rating === 0) return 0
  return rating + 1
}

const ratingStars = (rating: number) => {
  const starCount = getRatingStarCount(rating)
  return Array.from({ length: 5 }, (_, i) => (
    <Star
      key={i}
      className={`w-4 h-4 ${i < starCount ? 'text-amber-400 fill-amber-400' : 'text-slate-200'}`}
    />
  ))
}

export default function Reviews() {
  const { showError } = useToast()
  const [reviews, setReviews] = useState<PerformanceReview[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    directReportId: '',
    reviewPeriod: '',
    rating: '0',
    strengths: '',
    areasForImprovement: '',
    managerNotes: ''
  })

  const resetForm = () => {
    setFormData({
      directReportId: '',
      reviewPeriod: '',
      rating: '0',
      strengths: '',
      areasForImprovement: '',
      managerNotes: ''
    })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    setError(null)
    resetForm()
  }, [])

  useEscapeKey(closeModal, showForm)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [reviewsData, drData] = await Promise.all([
        reviewsApi.getAll(),
        directReportsApi.getAll()
      ])
      setReviews(reviewsData)
      setDirectReports(drData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const loadReviews = async () => {
    try {
      const data = await reviewsApi.getAll()
      setReviews(data)
    } catch (err) {
      console.error(err)
    }
  }

  const [searchQuery, setSearchQuery] = useState('')
  const [selectedDirectReportId, setSelectedDirectReportId] = useState<string | null>(null)

  const clearFilters = () => {
    setSearchQuery('')
    setSelectedDirectReportId(null)
  }

  const filteredReviews = (() => {
    let result = reviews

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(r =>
        r.directReportName.toLowerCase().includes(query) ||
        r.reviewPeriod.toLowerCase().includes(query) ||
        r.strengths?.toLowerCase().includes(query) ||
        r.areasForImprovement?.toLowerCase().includes(query)
      )
    }

    // Apply team member filter
    if (selectedDirectReportId) {
      result = result.filter(r => r.directReportId === selectedDirectReportId)
    }

    return result
  })()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      if (editingId) {
        // For updates, only send content fields (not directReportId or reviewPeriod)
        const updateData = {
          strengths: formData.strengths,
          areasForImprovement: formData.areasForImprovement,
          managerNotes: formData.managerNotes,
          rating: parseInt(formData.rating)
        }
        await reviewsApi.update(editingId, updateData)
      } else {
        const reviewData = {
          directReportId: formData.directReportId,
          reviewPeriod: formData.reviewPeriod,
          reviewDate: new Date().toISOString(),
          rating: parseInt(formData.rating),
          strengths: formData.strengths,
          areasForImprovement: formData.areasForImprovement,
          managerNotes: formData.managerNotes
        }
        await reviewsApi.create(reviewData)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadReviews()
    } catch (err: any) {
      const message = err.response?.data?.message || 'An error occurred while saving the review.'
      setError(message)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (review: PerformanceReview) => {
    setError(null)
    setFormData({
      directReportId: review.directReportId,
      reviewPeriod: review.reviewPeriod,
      rating: review.rating.toString(),
      strengths: review.strengths || '',
      areasForImprovement: review.areasForImprovement || '',
      managerNotes: review.managerNotes || ''
    })
    setEditingId(review.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this review?')) {
      try {
        await reviewsApi.delete(id)
        loadReviews()
      } catch (err) {
        console.error(err)
        showError(getErrorMessage(err))
      }
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Performance Reviews</h1>
          <p className="text-slate-500 dark:text-slate-400">Track performance reviews</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New Review
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4 max-h-[90vh] overflow-y-auto">
            <CardHeader title={editingId ? 'Edit Review' : 'New Review'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Team Member</label>
                  <select
                    value={formData.directReportId}
                    onChange={(e) => setFormData({ ...formData, directReportId: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 disabled:bg-slate-100 dark:disabled:bg-slate-700 disabled:cursor-not-allowed"
                    required={!editingId}
                    disabled={!!editingId}
                  >
                    <option value="">Select team member</option>
                    {directReports.map(dr => (
                      <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                    ))}
                  </select>
                  {editingId && <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">Team member cannot be changed when editing</p>}
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Review Period</label>
                    <input
                      type="text"
                      value={formData.reviewPeriod}
                      onChange={(e) => setFormData({ ...formData, reviewPeriod: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 disabled:bg-slate-100 dark:disabled:bg-slate-700 disabled:cursor-not-allowed"
                      placeholder="e.g., 2024 Q1"
                      required={!editingId}
                      disabled={!!editingId}
                    />
                    {editingId && <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">Period cannot be changed</p>}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Rating</label>
                    <select
                      value={formData.rating}
                      onChange={(e) => setFormData({ ...formData, rating: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    >
                      <option value="0">Not Rated</option>
                      <option value="1">Needs Improvement</option>
                      <option value="2">Meets Expectations</option>
                      <option value="3">Exceeds Expectations</option>
                      <option value="4">Outstanding</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Strengths</label>
                  <textarea
                    value={formData.strengths}
                    onChange={(e) => setFormData({ ...formData, strengths: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    rows={2}
                    placeholder="Key strengths and achievements..."
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Areas for Improvement</label>
                  <textarea
                    value={formData.areasForImprovement}
                    onChange={(e) => setFormData({ ...formData, areasForImprovement: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    rows={2}
                    placeholder="Areas to develop..."
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Manager Notes</label>
                  <textarea
                    value={formData.managerNotes}
                    onChange={(e) => setFormData({ ...formData, managerNotes: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    rows={2}
                    placeholder="Additional notes..."
                  />
                </div>
                {error && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                    {error}
                  </div>
                )}
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={() => { setShowForm(false); setEditingId(null); setError(null); resetForm() }}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
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
            </CardContent>
          </Card>
        </div>
      )}

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search reviews..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
          />
          {(searchQuery || selectedDirectReportId) && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Team Members */}
        {directReports.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Users className="w-4 h-4 text-slate-400" />
            {directReports.map((dr) => (
              <button
                key={dr.id}
                onClick={() => setSelectedDirectReportId(selectedDirectReportId === dr.id ? null : dr.id)}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedDirectReportId === dr.id
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                {dr.fullName}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Reviews List */}
      {filteredReviews.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500">No reviews found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {filteredReviews.map((review) => (
            <Card key={review.id}>
              <CardContent>
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 bg-amber-100 dark:bg-amber-900/30 rounded-full flex items-center justify-center text-amber-700 dark:text-amber-400 font-semibold">
                      {review.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-900 dark:text-slate-100">{review.directReportName}</h3>
                      <p className="text-sm text-slate-500 dark:text-slate-400">{review.reviewPeriod}</p>
                      <div className="flex items-center gap-1 mt-2">
                        {ratingStars(review.rating)}
                        <span className="ml-2 text-sm text-slate-500 dark:text-slate-400">{review.ratingDescription}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="relative group">
                      <button className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded">
                        <MoreVertical className="w-5 h-5 text-slate-400" />
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => handleEdit(review)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
                        >
                          <Edit className="w-4 h-4" />
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(review.id)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                        >
                          <Trash2 className="w-4 h-4" />
                          Delete
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
                {(review.strengths || review.areasForImprovement) && (
                  <div className="mt-4 pt-4 border-t dark:border-slate-700 grid grid-cols-2 gap-4">
                    {review.strengths && (
                      <div>
                        <p className="text-sm font-medium text-slate-700 dark:text-slate-300">Strengths</p>
                        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">{review.strengths}</p>
                      </div>
                    )}
                    {review.areasForImprovement && (
                      <div>
                        <p className="text-sm font-medium text-slate-700 dark:text-slate-300">Areas for Improvement</p>
                        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">{review.areasForImprovement}</p>
                      </div>
                    )}
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
