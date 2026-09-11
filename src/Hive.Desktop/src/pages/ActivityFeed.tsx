import { useEffect, useState, useCallback } from 'react'
import {
  FileText,
  CheckSquare,
  Palmtree,
  Users,
  MessageCircle,
  StickyNote,
  FolderKanban,
  Timer,
  BarChart3,
  FileBox,
  Award,
  Star,
  ClipboardList,
  ClipboardCheck,
  Network,
  BookOpen,
  Search,
  X,
  ChevronLeft,
  ChevronRight
} from 'lucide-react'
import { activityFeedApi } from '../services/api'
import { Activity } from '../types'
import { Card, CardContent } from '../components/Card'

const ActivityFeed = () => {
  const [activities, setActivities] = useState<Activity[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')

  // Pagination state
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)

  const loadActivities = useCallback(async (page = 1, search?: string) => {
    try {
      setLoading(true)
      setError(null)
      const data = await activityFeedApi.getAll(page, pageSize, search)
      setActivities(data.items)
      setTotalCount(data.totalCount)
      setTotalPages(data.totalPages)
      setPageNumber(data.pageNumber)
    } catch (err) {
      console.error('Failed to load activities:', err)
      setError('Failed to load activity feed')
    } finally {
      setLoading(false)
    }
  }, [pageSize])

  useEffect(() => {
    loadActivities()
  }, [loadActivities])

  // Search on Enter
  const handleSearchSubmit = useCallback(() => {
    setSearchQuery(searchInput)
    setPageNumber(1)
    loadActivities(1, searchInput.trim() || undefined)
  }, [searchInput, loadActivities])

  const clearSearch = useCallback(() => {
    setSearchInput('')
    setSearchQuery('')
    setPageNumber(1)
    loadActivities(1, undefined)
  }, [loadActivities])

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      loadActivities(newPage, searchQuery.trim() || undefined)
    }
  }

  const handlePageSizeChange = async (newSize: number) => {
    setPageSize(newSize)
    setPageNumber(1)
    // Reload with new page size
    try {
      setLoading(true)
      const data = await activityFeedApi.getAll(1, newSize, searchQuery.trim() || undefined)
      setActivities(data.items)
      setTotalCount(data.totalCount)
      setTotalPages(data.totalPages)
      setPageNumber(data.pageNumber)
    } catch (err) {
      console.error('Failed to load activities:', err)
    } finally {
      setLoading(false)
    }
  }

  const getEntityIcon = (entityType: string) => {
    switch (entityType.toLowerCase()) {
      case 'review':
        return <FileText className="w-5 h-5 text-blue-500" />
      case 'task':
        return <CheckSquare className="w-5 h-5 text-green-500" />
      case 'leave':
        return <Palmtree className="w-5 h-5 text-teal-500" />
      case 'directreport':
        return <Users className="w-5 h-5 text-indigo-500" />
      case 'meeting':
        return <MessageCircle className="w-5 h-5 text-purple-500" />
      case 'meetingnote':
        return <StickyNote className="w-5 h-5 text-violet-500" />
      case 'managernote':
        return <StickyNote className="w-5 h-5 text-amber-500" />
      case 'notefolder':
        return <FolderKanban className="w-5 h-5 text-amber-600" />
      case 'project':
        return <FolderKanban className="w-5 h-5 text-orange-500" />
      case 'sprint':
        return <Timer className="w-5 h-5 text-cyan-500" />
      case 'sprintcapacity':
        return <BarChart3 className="w-5 h-5 text-sky-500" />
      case 'document':
        return <FileBox className="w-5 h-5 text-slate-500" />
      case 'skill':
        return <Award className="w-5 h-5 text-yellow-500" />
      case 'skillassessment':
        return <Star className="w-5 h-5 text-yellow-600" />
      case 'checklisttemplate':
        return <ClipboardList className="w-5 h-5 text-emerald-500" />
      case 'checklistinstance':
        return <ClipboardCheck className="w-5 h-5 text-emerald-600" />
      case 'parent':
        return <Network className="w-5 h-5 text-rose-500" />
      case 'projectknowledge':
        return <BookOpen className="w-5 h-5 text-lime-500" />
      default:
        return <FileText className="w-5 h-5 text-gray-500" />
    }
  }

  const getEntityBadgeColor = (entityType: string) => {
    switch (entityType.toLowerCase()) {
      case 'review':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
      case 'task':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
      case 'leave':
        return 'bg-teal-100 text-teal-800 dark:bg-teal-900 dark:text-teal-200'
      case 'directreport':
        return 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200'
      case 'meeting':
        return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200'
      case 'meetingnote':
        return 'bg-violet-100 text-violet-800 dark:bg-violet-900 dark:text-violet-200'
      case 'notefolder':
      case 'managernote':
        return 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200'
      case 'project':
        return 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200'
      case 'sprint':
        return 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-200'
      case 'sprintcapacity':
        return 'bg-sky-100 text-sky-800 dark:bg-sky-900 dark:text-sky-200'
      case 'document':
        return 'bg-slate-100 text-slate-800 dark:bg-slate-700 dark:text-slate-200'
      case 'skill':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200'
      case 'skillassessment':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200'
      case 'checklisttemplate':
        return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200'
      case 'checklistinstance':
        return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200'
      case 'parent':
        return 'bg-rose-100 text-rose-800 dark:bg-rose-900 dark:text-rose-200'
      case 'projectknowledge':
        return 'bg-lime-100 text-lime-800 dark:bg-lime-900 dark:text-lime-200'
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200'
    }
  }

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return 'Just now'
    if (diffMins < 60) return `${diffMins} minute${diffMins === 1 ? '' : 's'} ago`
    if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`
    if (diffDays < 7) return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`

    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    })
  }

  if (loading && activities.length === 0) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Activity Feed</h1>
            <p className="text-slate-500 dark:text-slate-400">Changes across the system</p>
          </div>
        </div>
        <div className="flex justify-center items-center h-64">
          <div className="text-slate-500">Loading activities...</div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Activity Feed</h1>
            <p className="text-slate-500 dark:text-slate-400">Changes across the system</p>
          </div>
        </div>
        <div className="flex justify-center items-center h-64">
          <div className="text-red-500">{error}</div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Activity Feed</h1>
          <p className="text-slate-500 dark:text-slate-400">Changes across the system</p>
        </div>
      </div>

      {/* Search Bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search activities... (press Enter)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') handleSearchSubmit() }}
          className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
        />
        {searchInput && (
          <button
            onClick={clearSearch}
            className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {activities.length === 0 ? (
        <Card>
          <CardContent>
            <div className="text-center py-12 text-slate-500 dark:text-slate-400">
              {searchQuery ? 'No activities found matching your search' : 'No activities found'}
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {activities.map((activity) => (
            <Card key={activity.id} className="hover:shadow-md transition-shadow">
              <CardContent>
                <div className="flex items-start gap-4">
                  <div className="flex-shrink-0 mt-1">
                    {getEntityIcon(activity.entityType)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {activity.entityName}
                      </p>
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getEntityBadgeColor(activity.entityType)}`}
                      >
                        {activity.entityTypeName}
                      </span>
                    </div>

                    <p className="text-sm text-slate-600 dark:text-slate-400 mt-1">
                      {activity.description}
                    </p>

                    <p className="text-xs text-slate-500 dark:text-slate-500 mt-2">
                      {formatTimestamp(activity.timestamp)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Pagination Controls */}
      {totalPages > 0 && (
        <div className="flex items-center justify-between px-4 py-3 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg">
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-600 dark:text-slate-400">
              Showing {((pageNumber - 1) * pageSize) + 1} - {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} activities
            </span>
            <div className="flex items-center gap-2">
              <span className="text-sm text-slate-600 dark:text-slate-400">Per page:</span>
              <select
                value={pageSize}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="px-2 py-1 text-sm border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
              >
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => handlePageChange(pageNumber - 1)}
              disabled={pageNumber <= 1}
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-700 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
              Previous
            </button>
            <span className="px-3 py-1.5 text-sm font-medium text-slate-900 dark:text-slate-100">
              Page {pageNumber} of {totalPages}
            </span>
            <button
              onClick={() => handlePageChange(pageNumber + 1)}
              disabled={pageNumber >= totalPages}
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-700 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

export default ActivityFeed
