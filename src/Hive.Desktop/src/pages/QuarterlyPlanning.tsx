import { useEffect, useState } from 'react'
import {
  Calendar,
  Plus,
  ChevronLeft,
  ChevronRight,
  Target,
  AlertCircle,
  Loader2,
  RefreshCw,
  PanelLeftClose,
  PanelRightClose,
  Check
} from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import { quarterlyPlanningApi } from '../services/api'
import type {
  Quarter,
  PlanningBoard,
  PlanningInsights
} from '../types'
import PlanningMatrix from '../components/PlanningMatrix'
import InitiativesPanel from '../components/InitiativesPanel'
import InsightsSidebar from '../components/InsightsSidebar'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

export default function QuarterlyPlanning() {
  const { showError } = useToast()

  // Data state
  const [quarters, setQuarters] = useState<Quarter[]>([])
  const [selectedQuarterId, setSelectedQuarterId] = useState<string | null>(null)
  const [planningBoard, setPlanningBoard] = useState<PlanningBoard | null>(null)
  const [insights, setInsights] = useState<PlanningInsights | null>(null)

  // UI state
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [leftPanelOpen, setLeftPanelOpen] = useState(true)
  const [rightPanelOpen, setRightPanelOpen] = useState(true)

  // Create quarter modal
  const [showCreateQuarter, setShowCreateQuarter] = useState(false)
  const [newQuarterYear, setNewQuarterYear] = useState(new Date().getFullYear())
  const [newQuarterNumber, setNewQuarterNumber] = useState(Math.ceil((new Date().getMonth() + 1) / 3))
  const [newQuarterOkr, setNewQuarterOkr] = useState('')
  const [creating, setCreating] = useState(false)

  
  // Load quarters on mount
  useEffect(() => {
    loadQuarters()
  }, [])

  // Load board data when quarter is selected
  useEffect(() => {
    if (selectedQuarterId) {
      loadBoardData(selectedQuarterId)
    }
  }, [selectedQuarterId])

  const loadQuarters = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await quarterlyPlanningApi.getAllQuarters()
      setQuarters(data)

      // Auto-select first quarter or active quarter
      if (data.length > 0) {
        const active = data.find(q => q.status === 1) // Active status
        setSelectedQuarterId(active?.id || data[0].id)
      }
    } catch (err: any) {
      console.error('Failed to load quarters', err)
      setError('Failed to load quarters. Please ensure sprints exist for the quarter.')
    } finally {
      setLoading(false)
    }
  }

  const loadBoardData = async (quarterId: string) => {
    try {
      setLoading(true)
      setError(null)
      const [boardData, insightsData] = await Promise.all([
        quarterlyPlanningApi.getPlanningBoard(quarterId),
        quarterlyPlanningApi.getInsights(quarterId)
      ])
      setPlanningBoard(boardData)
      setInsights(insightsData)
    } catch (err: any) {
      console.error('Failed to load board data', err)
      setError(err.response?.data?.message || 'Failed to load planning board data.')
    } finally {
      setLoading(false)
    }
  }

  const handleCreateQuarter = async () => {
    try {
      setCreating(true)
      setError(null)
      const created = await quarterlyPlanningApi.createQuarter({
        year: newQuarterYear,
        quarterNumber: newQuarterNumber,
        okrReference: newQuarterOkr || undefined
      })
      setQuarters(prev => [created, ...prev])
      setSelectedQuarterId(created.id)
      setShowCreateQuarter(false)
      setNewQuarterOkr('')
    } catch (err: any) {
      console.error('Failed to create quarter', err)
      showError(getErrorMessage(err))
      setError(err.response?.data?.message || 'Failed to create quarter.')
    } finally {
      setCreating(false)
    }
  }

  const handleRefresh = () => {
    if (selectedQuarterId) {
      loadBoardData(selectedQuarterId)
    }
  }

  const handleAllocationCreated = () => {
    if (selectedQuarterId) {
      loadBoardData(selectedQuarterId)
    }
  }

  const handleInitiativeCreated = () => {
    if (selectedQuarterId) {
      loadBoardData(selectedQuarterId)
    }
  }

  const navigateQuarter = (direction: 'prev' | 'next') => {
    const currentIndex = quarters.findIndex(q => q.id === selectedQuarterId)
    if (direction === 'prev' && currentIndex > 0) {
      setSelectedQuarterId(quarters[currentIndex - 1].id)
    } else if (direction === 'next' && currentIndex < quarters.length - 1) {
      setSelectedQuarterId(quarters[currentIndex + 1].id)
    }
  }

  if (loading && !planningBoard) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="w-8 h-8 animate-spin text-amber-500" />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-[calc(100vh-8rem)]">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Calendar className="w-6 h-6 text-amber-500" />
            <h1 className="text-2xl font-bold text-slate-800 dark:text-white">
              Quarterly Planning
            </h1>
          </div>

          {/* Quarter Selector */}
          {quarters.length > 0 && (
            <div className="flex items-center gap-2 ml-4">
              <button
                onClick={() => navigateQuarter('prev')}
                disabled={quarters.findIndex(q => q.id === selectedQuarterId) === 0}
                className="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronLeft className="w-5 h-5" />
              </button>
              <select
                value={selectedQuarterId || ''}
                onChange={(e) => setSelectedQuarterId(e.target.value)}
                className="px-3 py-1 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-800 dark:text-white"
              >
                {quarters.map(q => (
                  <option key={q.id} value={q.id}>
                    {q.name} {q.status === 1 ? '(Active)' : q.status === 2 ? '(Completed)' : ''}
                  </option>
                ))}
              </select>
              <button
                onClick={() => navigateQuarter('next')}
                disabled={quarters.findIndex(q => q.id === selectedQuarterId) === quarters.length - 1}
                className="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronRight className="w-5 h-5" />
              </button>
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleRefresh}
            disabled={loading}
            className="p-2 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 disabled:opacity-50"
            title="Refresh"
          >
            <RefreshCw className={`w-5 h-5 ${loading ? 'animate-spin' : ''}`} />
          </button>
          <button
            onClick={() => setLeftPanelOpen(!leftPanelOpen)}
            className={`p-2 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 ${!leftPanelOpen ? 'bg-slate-200 dark:bg-slate-700' : ''}`}
            title="Toggle Initiatives Panel"
          >
            <PanelLeftClose className="w-5 h-5" />
          </button>
          <button
            onClick={() => setRightPanelOpen(!rightPanelOpen)}
            className={`p-2 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 ${!rightPanelOpen ? 'bg-slate-200 dark:bg-slate-700' : ''}`}
            title="Toggle Insights Panel"
          >
            <PanelRightClose className="w-5 h-5" />
          </button>
          <button
            onClick={() => setShowCreateQuarter(true)}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            <Plus className="w-4 h-4" />
            New Quarter
          </button>
        </div>
      </div>

      {/* Error Banner */}
      {error && (
        <div className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg flex items-center gap-2 text-red-700 dark:text-red-400">
          <AlertCircle className="w-5 h-5" />
          {error}
          <button onClick={() => setError(null)} className="ml-auto text-red-500 hover:text-red-700">
            &times;
          </button>
        </div>
      )}

      {/* Main Content */}
      {quarters.length === 0 ? (
        <Card className="flex-1">
          <CardContent>
            <div className="flex flex-col items-center justify-center h-64 text-slate-500 dark:text-slate-400">
              <Target className="w-12 h-12 mb-4 opacity-50" />
              <p className="text-lg font-medium mb-2">No quarters created yet</p>
              <p className="text-sm mb-4">Create your first quarter to start planning</p>
              <button
                onClick={() => setShowCreateQuarter(true)}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
              >
                <Plus className="w-4 h-4" />
                Create Quarter
              </button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="flex flex-1 gap-4 overflow-hidden">
          {/* Left Panel - Initiatives */}
          {leftPanelOpen && (
            <div className="w-72 flex-shrink-0 overflow-auto">
              <InitiativesPanel
                initiatives={planningBoard?.initiatives || []}
                quarterId={selectedQuarterId!}
                onInitiativeCreated={handleInitiativeCreated}
                onDragStart={() => {
                  // Handle drag start for allocations
                }}
              />
            </div>
          )}

          {/* Center - Planning Board + Insights */}
          <div className="flex-1 overflow-auto">
            <PlanningMatrix
              sprints={planningBoard?.sprints || []}
              teamMembers={planningBoard?.teamMembers || []}
              allocations={planningBoard?.allocations || []}
              initiatives={planningBoard?.initiatives || []}
              leaves={planningBoard?.leaves || []}
              sprintGoals={planningBoard?.sprintGoals || []}
              quarterId={selectedQuarterId!}
              onAllocationCreated={handleAllocationCreated}
            />
            {rightPanelOpen && (
              <div className="mt-4">
                <InsightsSidebar insights={insights} />
              </div>
            )}
          </div>
        </div>
      )}

      {/* Create Quarter Modal */}
      {showCreateQuarter && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl p-6 w-96">
            <h2 className="text-xl font-bold mb-4 text-slate-800 dark:text-white">Create New Quarter</h2>

            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Year
                  </label>
                  <input
                    type="number"
                    value={newQuarterYear}
                    onChange={(e) => setNewQuarterYear(parseInt(e.target.value))}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Quarter
                  </label>
                  <select
                    value={newQuarterNumber}
                    onChange={(e) => setNewQuarterNumber(parseInt(e.target.value))}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                  >
                    <option value={1}>Q1</option>
                    <option value={2}>Q2</option>
                    <option value={3}>Q3</option>
                    <option value={4}>Q4</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  OKR Reference (optional)
                </label>
                <input
                  type="text"
                  value={newQuarterOkr}
                  onChange={(e) => setNewQuarterOkr(e.target.value)}
                  placeholder="Link to company OKRs..."
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-800 dark:text-white"
                />
              </div>
            </div>

            <div className="flex justify-end gap-3 mt-6">
              <button
                onClick={() => setShowCreateQuarter(false)}
                className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleCreateQuarter}
                disabled={creating}
                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50"
              >
                {creating ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                Create
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
