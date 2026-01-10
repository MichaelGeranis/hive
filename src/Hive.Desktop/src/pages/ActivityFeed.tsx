import { useEffect, useState } from 'react'
import { FileText, CheckSquare, Palmtree } from 'lucide-react'
import { activityFeedApi } from '../services/api'
import { Activity } from '../types'
import { Card, CardContent, CardHeader } from '../components/Card'

const ActivityFeed = () => {
  const [activities, setActivities] = useState<Activity[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadActivities()
  }, [])

  const loadActivities = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await activityFeedApi.getRecent(7)
      setActivities(data)
    } catch (err) {
      console.error('Failed to load activities:', err)
      setError('Failed to load activity feed')
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

  if (loading) {
    return (
      <div className="p-6">
        <h1 className="text-2xl font-bold mb-6">Activity Feed</h1>
        <div className="flex justify-center items-center h-64">
          <div className="text-slate-500">Loading activities...</div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-6">
        <h1 className="text-2xl font-bold mb-6">Activity Feed</h1>
        <div className="flex justify-center items-center h-64">
          <div className="text-red-500">{error}</div>
        </div>
      </div>
    )
  }

  return (
    <div className="p-6">
      <CardHeader
        title="Activity Feed"
        subtitle="Recent changes across the system from the past week"
      />

      {activities.length === 0 ? (
        <Card>
          <CardContent>
            <div className="text-center py-12 text-slate-500 dark:text-slate-400">
              No recent activities found
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
    </div>
  )
}

export default ActivityFeed
