import { useState, useEffect, useCallback, ReactNode } from 'react'
import { formatDateTime } from '../utils/dateUtils'
import { apiErrorMessage } from '../utils/apiErrors'
import { RefreshCw, AlertCircle, TrendingUp, TrendingDown, Minus, Loader2 } from 'lucide-react'
import { PieChart, Pie, Cell, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts'
import { Card, CardHeader, CardContent } from './Card'
import { Bee } from './AiBeeBadge'
import { sentimentApi } from '../services/api'
import type { DirectReportSentimentSummary, SentimentAnalysis, TeamSentimentOverview, SentimentStatus } from '../types'

const SENTIMENT_COLORS = {
  positive: '#22c55e', // green-500
  neutral: '#64748b',  // slate-500
  negative: '#ef4444'  // red-500
}

interface SentimentInsightsProps {
  directReportId?: string
  directReportName?: string
  showTeamOverview?: boolean
  badge?: ReactNode
}

export function SentimentInsights({ directReportId, directReportName, showTeamOverview = false, badge }: SentimentInsightsProps) {
  const [status, setStatus] = useState<SentimentStatus | null>(null)
  const [analysis, setAnalysis] = useState<SentimentAnalysis | null>(null)
  const [teamOverview, setTeamOverview] = useState<TeamSentimentOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
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
    } catch (err) {
      console.error('Failed to load sentiment data', err)
      setError(apiErrorMessage(err) ?? 'Failed to load sentiment analysis')
    } finally {
      setLoading(false)
    }
  }, [directReportId, showTeamOverview])

  // Loads again when the person or the view changes
  useEffect(() => {
    loadData()
  }, [loadData])

  const handleRefresh = async () => {
    if (!directReportId || refreshing) return

    try {
      setRefreshing(true)
      setError(null)
      const analysisData = await sentimentApi.refreshForDirectReport(directReportId)
      setAnalysis(analysisData)
    } catch (err) {
      console.error('Failed to refresh sentiment', err)
      setError(apiErrorMessage(err) ?? 'Failed to refresh sentiment analysis')
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
        <CardHeader ai title="Sentiment Analysis" subtitle="AI-powered team morale insights" badge={badge} />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <div className="mb-3 opacity-60"><Bee size={48} /></div>
            <h4 className="text-sm font-medium text-slate-700 mb-1">
              Sentiment Analysis Not Configured
            </h4>
            <p className="text-sm text-slate-500 max-w-sm">
              Choose a model in Settings: a local Ollama model, or Claude with an API key.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!status?.isEnabled) {
    return (
      <Card>
        <CardHeader ai title="Sentiment Analysis" subtitle="AI-powered team morale insights" badge={badge} />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <div className="mb-3 opacity-60"><Bee size={48} /></div>
            <h4 className="text-sm font-medium text-slate-700 mb-1">
              Sentiment Analysis Disabled
            </h4>
            <p className="text-sm text-slate-500 max-w-sm">
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
        <CardHeader ai title="Sentiment Analysis" subtitle="AI-powered team morale insights" badge={badge} />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <AlertCircle className="w-12 h-12 text-red-400 mb-3" />
            <p className="text-sm text-red-600">{error}</p>
            <button
              onClick={loadData}
              className="mt-4 px-4 py-2 text-sm bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors"
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
    return <TeamSentimentWidget overview={teamOverview} badge={badge} />
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
  badge?: ReactNode
}

const REASONING_TOOLTIP_ID = 'sentiment-reasoning'
const REASONING_WIDTH = 320

function TeamSentimentWidget({ overview, badge }: TeamSentimentWidgetProps) {
  // Fixed to the viewport, so the list's scrolling box cannot clip it, and always above the
  // row: the widget sits at the bottom of the page, where there is no room below.
  const [reasoningFor, setReasoningFor] = useState<{ id: string; name: string; text: string; notes: number; bottom: number; left: number } | null>(null)

  const showReasoning = (dr: DirectReportSentimentSummary, row: HTMLElement) => {
    const rect = row.getBoundingClientRect()
    setReasoningFor({
      id: dr.directReportId,
      name: dr.directReportName,
      text: dr.reasoning,
      notes: dr.notesCount,
      bottom: window.innerHeight - rect.top + 6,
      left: Math.max(8, Math.min(rect.left, window.innerWidth - REASONING_WIDTH - 8))
    })
  }
  const hideReasoning = () => setReasoningFor(null)

  const pieData = [
    { name: 'Positive', value: overview.averagePositive, color: SENTIMENT_COLORS.positive },
    { name: 'Neutral', value: overview.averageNeutral, color: SENTIMENT_COLORS.neutral },
    { name: 'Negative', value: overview.averageNegative, color: SENTIMENT_COLORS.negative }
  ]

  const getSentimentColor = (sentiment: string) => {
    switch (sentiment.toLowerCase()) {
      case 'positive': return 'text-green-600'
      case 'negative': return 'text-red-600'
      case 'mixed': return 'text-amber-600'
      default: return 'text-slate-600'
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
        <CardHeader ai title="Team Sentiment" subtitle="AI-powered team morale insights" badge={badge} />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <div className="mb-3 opacity-60"><Bee size={48} /></div>
            <h4 className="text-sm font-medium text-slate-700 mb-1">
              No Data Available
            </h4>
            <p className="text-sm text-slate-500 max-w-sm">
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
        ai
        title="Team Sentiment"
        badge={badge}
        subtitle={`Based on ${overview.totalNotesAnalyzed} notes from ${overview.directReportsAnalyzed} team members`}
        action={
          <div className={`px-3 py-1 rounded-full text-sm font-medium ${
            overview.overallTeamSentiment === 'Positive' ? 'bg-green-100 text-green-700' :
            overview.overallTeamSentiment === 'Negative' ? 'bg-red-100 text-red-700' :
            overview.overallTeamSentiment === 'Mixed' ? 'bg-amber-100 text-amber-700' :
            'bg-slate-100 text-slate-700'
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
                <Tooltip formatter={(value) => `${Number(value).toFixed(1)}%`} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>

          {/* By Direct Report */}
          <div className="space-y-2">
            <h4 className="text-sm font-medium text-slate-700 mb-2">By Team Member</h4>
            <div className="space-y-2 max-h-40 overflow-y-auto">
              {overview.byDirectReport.map(dr => (
                <div
                  key={dr.directReportId}
                  tabIndex={0}
                  aria-describedby={reasoningFor?.id === dr.directReportId ? REASONING_TOOLTIP_ID : undefined}
                  onMouseEnter={e => showReasoning(dr, e.currentTarget)}
                  onMouseLeave={hideReasoning}
                  onFocus={e => showReasoning(dr, e.currentTarget)}
                  onBlur={hideReasoning}
                  className="flex items-center justify-between text-sm p-2 bg-slate-50 rounded-sm cursor-help hover:bg-slate-100 focus:outline-hidden focus:ring-2 focus:ring-purple-300"
                >
                  <span className="text-slate-700 truncate flex-1">{dr.directReportName}</span>
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
          <div className="mt-4 pt-4 border-t">
            <h4 className="text-sm font-medium text-slate-700 mb-2">Common Themes</h4>
            <div className="flex flex-wrap gap-2">
              {overview.commonThemes.map((theme, idx) => (
                <span
                  key={idx}
                  className="px-2 py-1 bg-purple-50 text-purple-700 rounded-sm text-xs"
                >
                  {theme}
                </span>
              ))}
            </div>
          </div>
        )}
      </CardContent>

      {reasoningFor && (
        <div
          id={REASONING_TOOLTIP_ID}
          role="tooltip"
          className="fixed z-50 p-3 text-xs text-slate-700 bg-white border border-slate-200 rounded-lg shadow-lg pointer-events-none"
          style={{ bottom: reasoningFor.bottom, left: reasoningFor.left, width: REASONING_WIDTH }}
        >
          <p className="font-medium text-slate-900 mb-1">
            Why {reasoningFor.name}: {reasoningFor.notes} {reasoningFor.notes === 1 ? '1:1' : '1:1s'}
          </p>
          <p className="leading-relaxed">
            {reasoningFor.text || 'No reasoning yet. It is added the next time this person is analysed.'}
          </p>
        </div>
      )}
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
          ai
          title={`Sentiment Analysis${directReportName ? ` - ${directReportName}` : ''}`}
          subtitle="AI-powered morale insights"
        />
        <CardContent>
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <div className="mb-3 opacity-60"><Bee size={48} /></div>
            <h4 className="text-sm font-medium text-slate-700 mb-1">
              No Meeting Notes
            </h4>
            <p className="text-sm text-slate-500 max-w-sm">
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
        ai
        title={`Sentiment Analysis${directReportName ? ` - ${directReportName}` : ''}`}
        subtitle={`Based on ${analysis.notesAnalyzed} notes from the last ${analysis.daysAnalyzed} days`}
        action={
          <button
            onClick={onRefresh}
            disabled={refreshing}
            className="p-2 text-slate-500 hover:text-slate-700 rounded-lg hover:bg-slate-100 transition-colors disabled:opacity-50"
            title="Refresh analysis"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
          </button>
        }
      />
      <CardContent>
        <div className="space-y-6">
          {/* Overall Sentiment */}
          <div className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
            <span className="text-sm font-medium text-slate-700">Overall Sentiment</span>
            <span className={`px-3 py-1 rounded-full text-sm font-medium ${
              analysis.score.overallSentiment === 'Positive' ? 'bg-green-100 text-green-700' :
              analysis.score.overallSentiment === 'Negative' ? 'bg-red-100 text-red-700' :
              analysis.score.overallSentiment === 'Mixed' ? 'bg-amber-100 text-amber-700' :
              'bg-slate-100 text-slate-700'
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
                  <Tooltip formatter={(value) => `${Number(value).toFixed(1)}%`} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </div>

            {/* Key Themes */}
            <div>
              {analysis.reasoning && (
                <>
                  <h4 className="text-sm font-medium text-slate-700 mb-2">Why</h4>
                  <p className="text-sm text-slate-600 leading-relaxed mb-4">{analysis.reasoning}</p>
                </>
              )}
              <h4 className="text-sm font-medium text-slate-700 mb-3">Key Themes</h4>
              {analysis.keyThemes.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {analysis.keyThemes.map((theme, idx) => (
                    <span
                      key={idx}
                      className="px-2 py-1 bg-purple-50 text-purple-700 rounded-sm text-xs"
                    >
                      {theme}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500">No key themes identified</p>
              )}
            </div>
          </div>

          {/* Trend Chart */}
          {trendData.length > 1 && (
            <div>
              <h4 className="text-sm font-medium text-slate-700 mb-3">Monthly Trend</h4>
              <div className="h-48">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={trendData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                    <YAxis domain={[0, 100]} tick={{ fontSize: 12 }} />
                    <Tooltip formatter={(value) => `${Number(value).toFixed(1)}%`} />
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
          <div className="text-xs text-slate-500 text-right">
            Last analyzed: {formatDateTime(analysis.analyzedAt)}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
