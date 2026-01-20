import { useState, useEffect } from 'react'
import { RefreshCw, AlertCircle, Brain, TrendingUp, TrendingDown, Minus, Loader2 } from 'lucide-react'
import { PieChart, Pie, Cell, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts'
import { Card, CardHeader, CardContent } from './Card'
import { sentimentApi } from '../services/api'
import type { SentimentAnalysis, TeamSentimentOverview, SentimentStatus } from '../types'

const SENTIMENT_COLORS = {
  positive: '#22c55e', // green-500
  neutral: '#64748b',  // slate-500
  negative: '#ef4444'  // red-500
}

interface SentimentInsightsProps {
  directReportId?: string
  directReportName?: string
  showTeamOverview?: boolean
}

export function SentimentInsights({ directReportId, directReportName, showTeamOverview = false }: SentimentInsightsProps) {
  const [status, setStatus] = useState<SentimentStatus | null>(null)
  const [analysis, setAnalysis] = useState<SentimentAnalysis | null>(null)
  const [teamOverview, setTeamOverview] = useState<TeamSentimentOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadData()
  }, [directReportId, showTeamOverview])

  const loadData = async () => {
    try {
      setLoading(true)
      setError(null)

      const statusData = await sentimentApi.getStatus()
      setStatus(statusData)

      if (!statusData.isConfigured || !statusData.isEnabled) {
        return
      }

      if (showTeamOverview) {
        const overview = await sentimentApi.getTeamOverview()
        setTeamOverview(overview)
      } else if (directReportId) {
        const analysisData = await sentimentApi.getForDirectReport(directReportId)
        setAnalysis(analysisData)
      }
    } catch (err: any) {
      console.error('Failed to load sentiment data', err)
      setError(err.response?.data?.message || 'Failed to load sentiment analysis')
    } finally {
      setLoading(false)
    }
  }

  const handleRefresh = async () => {
    if (!directReportId || refreshing) return

    try {
      setRefreshing(true)
      setError(null)
      const analysisData = await sentimentApi.refreshForDirectReport(directReportId)
      setAnalysis(analysisData)
    } catch (err: any) {
      console.error('Failed to refresh sentiment', err)
      setError(err.response?.data?.message || 'Failed to refresh sentiment analysis')
    } finally {
      setRefreshing(false)
    }
  }

  if (loading) {
    return (
      <Card>
        <CardContent>
          <div className="flex items-center justify-center h-48">
            <Loader2 className="w-8 h-8 animate-spin text-purple-500" />
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!status?.isConfigured) {
    return (
      <Card>
        <CardHeader title="Sentiment Analysis" subtitle="AI-powered team morale insights" />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <Brain className="w-12 h-12 text-slate-300 dark:text-slate-600 mb-3" />
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Sentiment Analysis Not Configured
            </h4>
            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm">
              Configure your Claude API key in Settings to enable AI-powered sentiment analysis of meeting notes.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!status?.isEnabled) {
    return (
      <Card>
        <CardHeader title="Sentiment Analysis" subtitle="AI-powered team morale insights" />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <Brain className="w-12 h-12 text-slate-300 dark:text-slate-600 mb-3" />
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Sentiment Analysis Disabled
            </h4>
            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm">
              Enable sentiment analysis in Settings to view AI-powered insights about team morale.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardHeader title="Sentiment Analysis" subtitle="AI-powered team morale insights" />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <AlertCircle className="w-12 h-12 text-red-400 mb-3" />
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
            <button
              onClick={loadData}
              className="mt-4 px-4 py-2 text-sm bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors"
            >
              Try Again
            </button>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Team Overview Mode
  if (showTeamOverview && teamOverview) {
    return <TeamSentimentWidget overview={teamOverview} />
  }

  // Individual Direct Report Mode
  if (analysis) {
    return (
      <IndividualSentimentWidget
        analysis={analysis}
        directReportName={directReportName}
        onRefresh={handleRefresh}
        refreshing={refreshing}
      />
    )
  }

  return null
}

interface TeamSentimentWidgetProps {
  overview: TeamSentimentOverview
}

function TeamSentimentWidget({ overview }: TeamSentimentWidgetProps) {
  const pieData = [
    { name: 'Positive', value: overview.averagePositive, color: SENTIMENT_COLORS.positive },
    { name: 'Neutral', value: overview.averageNeutral, color: SENTIMENT_COLORS.neutral },
    { name: 'Negative', value: overview.averageNegative, color: SENTIMENT_COLORS.negative }
  ]

  const getSentimentColor = (sentiment: string) => {
    switch (sentiment.toLowerCase()) {
      case 'positive': return 'text-green-600 dark:text-green-400'
      case 'negative': return 'text-red-600 dark:text-red-400'
      case 'mixed': return 'text-amber-600 dark:text-amber-400'
      default: return 'text-slate-600 dark:text-slate-400'
    }
  }

  const getTrendIcon = (direction: string) => {
    switch (direction.toLowerCase()) {
      case 'improving': return <TrendingUp className="w-4 h-4 text-green-500" />
      case 'declining': return <TrendingDown className="w-4 h-4 text-red-500" />
      default: return <Minus className="w-4 h-4 text-slate-400" />
    }
  }

  if (overview.directReportsAnalyzed === 0) {
    return (
      <Card>
        <CardHeader title="Team Sentiment" subtitle="AI-powered team morale insights" />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <Brain className="w-12 h-12 text-slate-300 dark:text-slate-600 mb-3" />
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              No Data Available
            </h4>
            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm">
              Add meeting notes for your direct reports to see sentiment analysis.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader
        title="Team Sentiment"
        subtitle={`Based on ${overview.totalNotesAnalyzed} notes from ${overview.directReportsAnalyzed} team members`}
        action={
          <div className={`px-3 py-1 rounded-full text-sm font-medium ${
            overview.overallTeamSentiment === 'Positive' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
            overview.overallTeamSentiment === 'Negative' ? 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400' :
            overview.overallTeamSentiment === 'Mixed' ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400' :
            'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-400'
          }`}>
            {overview.overallTeamSentiment}
          </div>
        }
      />
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Pie Chart */}
          <div className="h-48">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  innerRadius={40}
                  outerRadius={70}
                  paddingAngle={2}
                  dataKey="value"
                >
                  {pieData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => `${value.toFixed(1)}%`} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>

          {/* By Direct Report */}
          <div className="space-y-2">
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">By Team Member</h4>
            <div className="space-y-2 max-h-40 overflow-y-auto">
              {overview.byDirectReport.map(dr => (
                <div key={dr.directReportId} className="flex items-center justify-between text-sm p-2 bg-slate-50 dark:bg-slate-700/50 rounded">
                  <span className="text-slate-700 dark:text-slate-300 truncate flex-1">{dr.directReportName}</span>
                  <div className="flex items-center gap-2">
                    <span className={getSentimentColor(dr.overallSentiment)}>{dr.overallSentiment}</span>
                    {getTrendIcon(dr.trendDirection)}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Common Themes */}
        {overview.commonThemes.length > 0 && (
          <div className="mt-4 pt-4 border-t dark:border-slate-700">
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Common Themes</h4>
            <div className="flex flex-wrap gap-2">
              {overview.commonThemes.map((theme, idx) => (
                <span
                  key={idx}
                  className="px-2 py-1 bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded text-xs"
                >
                  {theme}
                </span>
              ))}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

interface IndividualSentimentWidgetProps {
  analysis: SentimentAnalysis
  directReportName?: string
  onRefresh: () => void
  refreshing: boolean
}

function IndividualSentimentWidget({ analysis, directReportName, onRefresh, refreshing }: IndividualSentimentWidgetProps) {
  const pieData = [
    { name: 'Positive', value: analysis.score.positive, color: SENTIMENT_COLORS.positive },
    { name: 'Neutral', value: analysis.score.neutral, color: SENTIMENT_COLORS.neutral },
    { name: 'Negative', value: analysis.score.negative, color: SENTIMENT_COLORS.negative }
  ]

  const trendData = analysis.trend.map(t => ({
    name: t.monthName.substring(0, 3),
    positive: t.positiveScore,
    neutral: t.neutralScore,
    negative: t.negativeScore,
    notes: t.notesCount
  }))

  if (analysis.notesAnalyzed === 0) {
    return (
      <Card>
        <CardHeader
          title={`Sentiment Analysis${directReportName ? ` - ${directReportName}` : ''}`}
          subtitle="AI-powered morale insights"
        />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <Brain className="w-12 h-12 text-slate-300 dark:text-slate-600 mb-3" />
            <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              No Meeting Notes
            </h4>
            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm">
              Add meeting notes to see sentiment analysis for this team member.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader
        title={`Sentiment Analysis${directReportName ? ` - ${directReportName}` : ''}`}
        subtitle={`Based on ${analysis.notesAnalyzed} notes from the last ${analysis.daysAnalyzed} days`}
        action={
          <button
            onClick={onRefresh}
            disabled={refreshing}
            className="p-2 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors disabled:opacity-50"
            title="Refresh analysis"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
          </button>
        }
      />
      <CardContent>
        <div className="space-y-6">
          {/* Overall Sentiment */}
          <div className="flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
            <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Overall Sentiment</span>
            <span className={`px-3 py-1 rounded-full text-sm font-medium ${
              analysis.score.overallSentiment === 'Positive' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
              analysis.score.overallSentiment === 'Negative' ? 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400' :
              analysis.score.overallSentiment === 'Mixed' ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400' :
              'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-400'
            }`}>
              {analysis.score.overallSentiment}
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Pie Chart */}
            <div className="h-48">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieData}
                    cx="50%"
                    cy="50%"
                    innerRadius={40}
                    outerRadius={70}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {pieData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => `${value.toFixed(1)}%`} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </div>

            {/* Key Themes */}
            <div>
              <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">Key Themes</h4>
              {analysis.keyThemes.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {analysis.keyThemes.map((theme, idx) => (
                    <span
                      key={idx}
                      className="px-2 py-1 bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded text-xs"
                    >
                      {theme}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500 dark:text-slate-400">No key themes identified</p>
              )}
            </div>
          </div>

          {/* Trend Chart */}
          {trendData.length > 1 && (
            <div>
              <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">Monthly Trend</h4>
              <div className="h-48">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={trendData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                    <YAxis domain={[0, 100]} tick={{ fontSize: 12 }} />
                    <Tooltip formatter={(value: number) => `${value.toFixed(1)}%`} />
                    <Legend />
                    <Line type="monotone" dataKey="positive" stroke={SENTIMENT_COLORS.positive} strokeWidth={2} dot={{ r: 3 }} name="Positive" />
                    <Line type="monotone" dataKey="neutral" stroke={SENTIMENT_COLORS.neutral} strokeWidth={2} dot={{ r: 3 }} name="Neutral" />
                    <Line type="monotone" dataKey="negative" stroke={SENTIMENT_COLORS.negative} strokeWidth={2} dot={{ r: 3 }} name="Negative" />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>
          )}

          {/* Analysis Timestamp */}
          <div className="text-xs text-slate-500 dark:text-slate-400 text-right">
            Last analyzed: {new Date(analysis.analyzedAt).toLocaleString()}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
