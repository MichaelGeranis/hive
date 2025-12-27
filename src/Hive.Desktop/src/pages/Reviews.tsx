import { useEffect, useState } from 'react'
import { Plus, Star, Clock, CheckCircle, Send } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { reviewsApi, directReportsApi } from '../services/api'
import type { PerformanceReview, DirectReport, ReviewStatus } from '../types'

const statusColors: Record<ReviewStatus, string> = {
  [ReviewStatus.Draft]: 'bg-slate-100 text-slate-700',
  [ReviewStatus.Submitted]: 'bg-blue-100 text-blue-700',
  [ReviewStatus.Acknowledged]: 'bg-purple-100 text-purple-700',
  [ReviewStatus.Completed]: 'bg-green-100 text-green-700',
}

const ratingStars = (rating: number) => {
  return Array.from({ length: 5 }, (_, i) => (
    <Star
      key={i}
      className={`w-4 h-4 ${i < rating ? 'text-amber-400 fill-amber-400' : 'text-slate-200'}`}
    />
  ))
}

export default function Reviews() {
  const [reviews, setReviews] = useState<PerformanceReview[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | ReviewStatus>('all')

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

  const filteredReviews = filter === 'all'
    ? reviews
    : reviews.filter(r => r.status === filter)

  const handleAction = async (id: string, action: 'submit' | 'acknowledge' | 'complete') => {
    try {
      if (action === 'submit') await reviewsApi.submit(id)
      else if (action === 'acknowledge') await reviewsApi.acknowledge(id)
      else if (action === 'complete') await reviewsApi.complete(id)
      loadData()
    } catch (err) {
      console.error(err)
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
          <h1 className="text-2xl font-bold text-slate-900">Performance Reviews</h1>
          <p className="text-slate-500 mt-1">Track and manage performance reviews</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors">
          <Plus className="w-5 h-5" />
          New Review
        </button>
      </div>

      {/* Filters */}
      <div className="flex gap-2">
        {[
          { value: 'all', label: 'All' },
          { value: ReviewStatus.Draft, label: 'Draft' },
          { value: ReviewStatus.Submitted, label: 'Submitted' },
          { value: ReviewStatus.Acknowledged, label: 'Acknowledged' },
          { value: ReviewStatus.Completed, label: 'Completed' },
        ].map((f) => (
          <button
            key={f.value}
            onClick={() => setFilter(f.value as any)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
              filter === f.value
                ? 'bg-amber-500 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            {f.label}
          </button>
        ))}
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
                    <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center text-amber-700 font-semibold">
                      {review.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-900">{review.directReportName}</h3>
                      <p className="text-sm text-slate-500">{review.reviewPeriod}</p>
                      <div className="flex items-center gap-1 mt-2">
                        {ratingStars(review.rating)}
                        <span className="ml-2 text-sm text-slate-500">{review.ratingName}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[review.status]}`}>
                      {review.statusName}
                    </span>
                    {review.status === ReviewStatus.Draft && (
                      <button
                        onClick={() => handleAction(review.id, 'submit')}
                        className="flex items-center gap-1 px-3 py-1 text-sm bg-blue-500 text-white rounded-lg hover:bg-blue-600"
                      >
                        <Send className="w-4 h-4" />
                        Submit
                      </button>
                    )}
                    {review.status === ReviewStatus.Submitted && (
                      <button
                        onClick={() => handleAction(review.id, 'acknowledge')}
                        className="flex items-center gap-1 px-3 py-1 text-sm bg-purple-500 text-white rounded-lg hover:bg-purple-600"
                      >
                        <CheckCircle className="w-4 h-4" />
                        Acknowledge
                      </button>
                    )}
                    {review.status === ReviewStatus.Acknowledged && (
                      <button
                        onClick={() => handleAction(review.id, 'complete')}
                        className="flex items-center gap-1 px-3 py-1 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600"
                      >
                        <CheckCircle className="w-4 h-4" />
                        Complete
                      </button>
                    )}
                  </div>
                </div>
                {(review.strengths || review.areasForImprovement) && (
                  <div className="mt-4 pt-4 border-t grid grid-cols-2 gap-4">
                    {review.strengths && (
                      <div>
                        <p className="text-sm font-medium text-slate-700">Strengths</p>
                        <p className="text-sm text-slate-500 mt-1">{review.strengths}</p>
                      </div>
                    )}
                    {review.areasForImprovement && (
                      <div>
                        <p className="text-sm font-medium text-slate-700">Areas for Improvement</p>
                        <p className="text-sm text-slate-500 mt-1">{review.areasForImprovement}</p>
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
