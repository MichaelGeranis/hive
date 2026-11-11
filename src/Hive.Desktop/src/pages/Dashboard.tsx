import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Eye, EyeOff, Settings2 } from 'lucide-react'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingState } from '../components/PageState'
import { SentimentInsights } from '../components/SentimentInsights'
import { JiraSyncButton } from '../components/JiraSyncButton'
import {
  AttentionCard,
  OneOnOnesCard,
  SprintHealthCard,
  TenureMilestonesCard,
  TodosCard,
  WhosOutCard
} from '../components/DashboardFocus'
import {
  ALL_TIME_BADGE,
  BugSupportInflowCard,
  CapacityCard,
  CareerPlansCard,
  CarryOverCard,
  ProductEngagementCard,
  RetroSummaryCard,
  TaskReadinessCard,
  VelocityCard,
  WidgetLoadError,
  WidgetLoading,
  WorkloadCard,
  WorkMixCard,
  type TaskDrillDown
} from '../components/DashboardAnalytics'
import { reportsApi, productsApi, jiraSyncApi, careerPlansApi, retroSummaryApi, taskReadinessApi } from '../services/api'
import type { DashboardFocus, DashboardOverview, DashboardWarningCategory } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useHiveStatus } from '../contexts/HiveStatusContext'
import { formatDate } from '../utils/dateUtils'
import { describeParentSync, describeProductSync, describeSprintDateSync, describeTaskSync, type SyncMessages } from '../utils/jiraSync'

// A new key: the widget set changed, and old preferences name widgets that no longer exist
const DASHBOARD_WIDGETS_KEY = 'hive-dashboard-widgets-v2'

interface WidgetVisibility {
  attention: boolean
  currentSprint: boolean
  taskReadiness: boolean
  oneOnOnes: boolean
  todos: boolean
  whosOut: boolean
  tenure: boolean
  velocity: boolean
  capacity: boolean
  carryOver: boolean
  bugSupportInflow: boolean
  workload: boolean
  workMix: boolean
  productEngagement: boolean
  teamSentiment: boolean
  retroSummary: boolean
  careerPlans: boolean
}

const DEFAULT_WIDGETS: WidgetVisibility = {
  attention: true,
  currentSprint: true,
  taskReadiness: true,
  oneOnOnes: true,
  todos: true,
  whosOut: true,
  tenure: true,
  velocity: true,
  capacity: true,
  carryOver: true,
  bugSupportInflow: true,
  workload: true,
  workMix: true,
  productEngagement: true,
  teamSentiment: true,
  retroSummary: true,
  careerPlans: true
}

const WIDGET_LABELS: Record<keyof WidgetVisibility, string> = {
  attention: 'Needs attention',
  currentSprint: 'Current sprint',
  taskReadiness: 'Task readiness',
  oneOnOnes: '1:1s',
  todos: 'To-dos',
  whosOut: "Who's out",
  tenure: 'Milestones',
  velocity: 'Velocity',
  capacity: 'Capacity',
  carryOver: 'Carry-over trend',
  bugSupportInflow: 'Bug & support inflow',
  workload: 'Workload',
  workMix: 'Work mix',
  productEngagement: 'Product engagement',
  teamSentiment: 'Team sentiment',
  retroSummary: 'Retro summary',
  careerPlans: 'Career plans'
}

// Where each warning category sends the manager when clicked.
const WARNING_ROUTES: Record<DashboardWarningCategory, string> = {
  OneOnOnes: '/meetings',
  OverdueTasks: '/tasks?filter=overdue',
  StaleTasks: '/tasks?filter=stale',
  Workload: '/tasks',
  UnengagedMembers: '/team',
  NarrowlyEngagedMembers: '/team',
  UnworkedProducts: '/products',
  KnowledgeSilos: '/products',
  ScopeCreep: '/sprints',
  // Parents excluded, as the warning's count excludes them
  UnmatchedTasks: '/tasks?unattributed=true&excludeParents=true',
  EstimatedSpikes: '/tasks?type=Spike',
  EstimatedSupport: '/tasks?type=Support',
  UnestimatedTasks: '/tasks?unestimated=true',
  MissingAcceptanceCriteria: '/tasks?noAcceptanceCriteria=true',
  LowUtilization: '/sprints',
  VelocityTrend: '/sprints',
  CareerPlans: '/career-plans'
}

const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

function loadWidgets(): WidgetVisibility {
  try {
    const stored = localStorage.getItem(DASHBOARD_WIDGETS_KEY)
    if (!stored) return DEFAULT_WIDGETS
    const parsed = JSON.parse(stored) as Partial<Record<string, boolean>>
    // Only known widgets, so a stale key never shows up in Customize
    return Object.fromEntries(
      (Object.keys(DEFAULT_WIDGETS) as (keyof WidgetVisibility)[]).map(key => [
        key,
        typeof parsed[key] === 'boolean' ? parsed[key] : DEFAULT_WIDGETS[key]
      ])
    ) as unknown as WidgetVisibility
  } catch {
    return DEFAULT_WIDGETS
  }
}

function saveWidgets(widgets: WidgetVisibility) {
  try {
    localStorage.setItem(DASHBOARD_WIDGETS_KEY, JSON.stringify(widgets))
  } catch {
    // The preference is a convenience; the page works without it
  }
}

// The Tasks page URL for a widget's drill-down
function tasksUrl(drillDown: TaskDrillDown) {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(drillDown)) {
    if (value) params.append(key, value)
  }
  return `/tasks?${params.toString()}`
}

function SectionHeading({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div>
      <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
      <p className="text-sm text-slate-500">{subtitle}</p>
    </div>
  )
}

/**
 * The manager's start page, action first: what needs attention, the sprint in progress, the
 * 1:1s, to-dos and leave coming up, and only then how delivery has been going.
 */
export default function Dashboard() {
  const navigate = useNavigate()
  const { showError, showSuccess, showWarning } = useToast()
  const { reportAttention } = useHiveStatus()
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [focus, setFocus] = useState<DashboardFocus | null>(null)
  const [focusError, setFocusError] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCustomize, setShowCustomize] = useState(false)
  const [widgets, setWidgets] = useState<WidgetVisibility>(loadWidgets)

  const toggleWidget = (widget: keyof WidgetVisibility) => {
    const next = { ...widgets, [widget]: !widgets[widget] }
    setWidgets(next)
    saveWidgets(next)
  }

  const resetWidgets = () => {
    setWidgets(DEFAULT_WIDGETS)
    saveWidgets(DEFAULT_WIDGETS)
  }

  const closeCustomizeModal = useCallback(() => setShowCustomize(false), [])

  // The focus panel is about now
  const loadFocus = async () => {
    try {
      setFocusError(false)
      setFocus(await reportsApi.getFocus())
    } catch (err) {
      console.error('Failed to load the focus panel:', err)
      setFocusError(true)
    }
  }

  useEffect(() => {
    loadFocus()
  }, [])

  useEffect(() => {
    loadCoreData()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- loads once on opening; a Jira sync reloads it itself
  }, [])

  // A quiet reload keeps the current dashboard on screen and reports a failure as a toast,
  // instead of swapping the page for the loading and error screens.
  const loadCoreData = async ({ quiet = false }: { quiet?: boolean } = {}) => {
    try {
      if (!quiet) setLoading(true)
      const overview = await reportsApi.getDashboard()
      setDashboard(overview)
      // Hive's mark lights up from any page while there is something here to deal with
      reportAttention(overview.insights.warnings.length)
    } catch (err) {
      console.error(err)
      if (quiet) {
        showError('Could not refresh the dashboard. Reload the page to see the latest data.')
      } else {
        setError('Make sure the API is running.')
      }
    } finally {
      if (!quiet) setLoading(false)
    }
  }

  const openTasks = (drillDown: TaskDrillDown) => navigate(tasksUrl(drillDown))

  // Pulls everything Hive reads from Jira, then reloads the dashboard in place: sprint dates
  // decide which sprint is current, how sprints are ordered, and how much leave falls in each.
  // The widgets that fetch their own data refetch when the reloaded dashboard's generatedAt changes.
  const handleJiraSync = async () => {
    try {
      const result = await jiraSyncApi.syncAll()
      report(result.sprintDates ? describeSprintDateSync(result.sprintDates) : null, 'Sprint dates', result.sprintDatesError)
      report(result.products ? describeProductSync(result.products) : null, 'Products', result.productsError)
      report(result.parents ? describeParentSync(result.parents) : null, 'Parents', result.parentsError)
      report(result.tasks ? describeTaskSync(result.tasks) : null, 'Tasks', result.tasksError)
      await Promise.all([loadCoreData({ quiet: true }), loadFocus()])
    } catch (err) {
      console.error('Failed to sync from Jira:', err)
      showError(getErrorMessage(err))
    }
  }

  const report = (messages: SyncMessages | null, part: string, error?: string | null) => {
    if (error) {
      showError(`${part}: ${error}`)
      return
    }
    if (!messages) return
    showSuccess(messages.success)
    if (messages.warning) showWarning(messages.warning)
  }

  if (loading) {
    return <LoadingState fill />
  }

  if (error) {
    return (
      <ErrorState
        what="the dashboard"
        detail={error}
        onRetry={() => {
          setError(null)
          loadCoreData()
        }}
      />
    )
  }

  if (!dashboard) return null

  const now = new Date()
  const showToday = widgets.attention || widgets.currentSprint
  // Milestones only show in the week before one; otherwise the tile is hidden, not empty
  const milestones = widgets.tenure && focus ? focus.tenureMilestones : []
  const showPeople = widgets.oneOnOnes || widgets.todos || widgets.whosOut || milestones.length > 0
  const showDelivery = widgets.taskReadiness || widgets.velocity || widgets.capacity || widgets.carryOver || widgets.bugSupportInflow ||
    widgets.workload || widgets.workMix || widgets.productEngagement
  const showTeamHealth = widgets.teamSentiment || widgets.retroSummary || widgets.careerPlans

  const focusWidget = (title: string, render: (focus: DashboardFocus) => React.ReactNode) =>
    focusError ? <WidgetLoadError title={title} onRetry={loadFocus} />
      : focus ? render(focus)
      : <WidgetLoading title={title} />

  return (
    <div className="space-y-8">
      <PageHeader
        title="Dashboard"
        subtitle={`${WEEKDAYS[now.getDay()]}, ${formatDate(now)}`}
        actions={
          <>
            <JiraSyncButton
              onSync={handleJiraSync}
              title="Pull everything from Jira: sprint dates, products, parents and tasks"
            />
            <button
              onClick={() => setShowCustomize(true)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
            >
              <Settings2 className="w-4 h-4" />
              Customize
            </button>
          </>
        }
      />

      {/* Today: what needs the manager, and the sprint in progress */}
      {showToday && (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          {widgets.attention && (
            <div className={widgets.currentSprint ? 'xl:col-span-1' : 'xl:col-span-3'}>
              <AttentionCard
                warnings={dashboard.insights.warnings}
                onSelect={category => navigate(WARNING_ROUTES[category])}
              />
            </div>
          )}
          {widgets.currentSprint && (
            <div className={widgets.attention ? 'xl:col-span-2' : 'xl:col-span-3'}>
              {focusWidget('Current sprint', f => (
                <SprintHealthCard
                  sprint={f.currentSprint}
                  onOpenTasks={() => navigate('/tasks?sprint=current')}
                  onOpenSprints={() => navigate('/sprints')}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* People: 1:1s, the manager's own to-dos, and who is away */}
      {showPeople && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {widgets.oneOnOnes && focusWidget('1:1s', f => (
            <OneOnOnesCard
              oneOnOnes={f.oneOnOnes}
              onOpenPerson={id => navigate(`/meetings?person=${encodeURIComponent(id)}`)}
            />
          ))}
          {widgets.todos && focusWidget('To-dos', f => (
            <TodosCard
              todos={f.todos}
              onOpen={noteId => navigate(noteId ? `/notes?note=${noteId}` : '/notes')}
            />
          ))}
          {widgets.whosOut && focusWidget("Who's out", f => (
            <WhosOutCard leave={f.leave} onOpen={() => navigate('/leaves')} />
          ))}
          {milestones.length > 0 && (
            <TenureMilestonesCard
              milestones={milestones}
              onOpenPerson={id => navigate(`/meetings?person=${encodeURIComponent(id)}`)}
            />
          )}
        </div>
      )}

      {/* Delivery: history. Sprint-by-sprint widgets choose their own sprints; the rest cover all */}
      {showDelivery && (
        <section className="space-y-6">
          <SectionHeading title="Delivery" subtitle="How the team has been delivering" />

          {widgets.taskReadiness && (
            <TaskReadinessCard
              load={taskReadinessApi.get}
              reviewNext={taskReadinessApi.reviewNext}
              reviewTask={taskReadinessApi.reviewTask}
              reloadKey={dashboard.generatedAt}
              onOpenTask={task => navigate(`/tasks?search=${encodeURIComponent(task.jiraKey ?? task.title)}`)}
              onOpenMissingCriteria={() => navigate('/tasks?noAcceptanceCriteria=true')}
              onOpenSettings={() => navigate('/settings?section=ai')}
            />
          )}

          {(widgets.velocity || widgets.capacity) && (
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
              {widgets.velocity && (
                <VelocityCard load={reportsApi.getTeamVelocity} reloadKey={dashboard.generatedAt} onOpenTasks={openTasks} />
              )}
              {widgets.capacity && (
                <CapacityCard load={reportsApi.getCapacityAnalysis} reloadKey={dashboard.generatedAt} onOpenTasks={openTasks} />
              )}
            </div>
          )}

          {(widgets.carryOver || widgets.bugSupportInflow) && (
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
              {widgets.carryOver && (
                <CarryOverCard load={reportsApi.getCarryOverTrend} reloadKey={dashboard.generatedAt} onOpenTasks={openTasks} />
              )}
              {widgets.bugSupportInflow && (
                <BugSupportInflowCard load={reportsApi.getBugSupportInflow} reloadKey={dashboard.generatedAt} onOpenTasks={openTasks} />
              )}
            </div>
          )}

          {widgets.workload && <WorkloadCard dashboard={dashboard} onOpenTasks={openTasks} />}

          {(widgets.workMix || widgets.productEngagement) && (
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
              {widgets.workMix && <WorkMixCard tasks={dashboard.tasks} onOpenTasks={openTasks} />}
              {widgets.productEngagement && (
                <ProductEngagementCard
                  load={reportsApi.getProductEngagement}
                  loadProducts={productsApi.getAll}
                  reloadKey={dashboard.generatedAt}
                />
              )}
            </div>
          )}
        </section>
      )}

      {/* Team health: how people are doing */}
      {showTeamHealth && (
        <section className="space-y-6">
          <SectionHeading title="Team health" subtitle="How people are doing" />

          {widgets.teamSentiment && <SentimentInsights showTeamOverview={true} badge={ALL_TIME_BADGE} />}
          {widgets.retroSummary && (
            <RetroSummaryCard
              load={retroSummaryApi.get}
              refresh={retroSummaryApi.refresh}
              reloadKey={dashboard.generatedAt}
              onOpenNote={id => navigate(`/notes?note=${encodeURIComponent(id)}`)}
              onOpenNotes={() => navigate('/notes')}
              onOpenSettings={() => navigate('/settings?section=ai')}
            />
          )}
          {widgets.careerPlans && (
            <CareerPlansCard
              load={careerPlansApi.getOverview}
              reloadKey={dashboard.generatedAt}
              onOpenPlan={id => navigate(`/career-plans?plan=${encodeURIComponent(id)}`)}
              onOpenPlans={() => navigate('/career-plans')}
            />
          )}
        </section>
      )}

      {/* Customize Dashboard Modal */}
      {showCustomize && (
        <Modal
          title="Customize Dashboard"
          subtitle="Choose which widgets to display"
          onClose={closeCustomizeModal}
          size="sm"
          footer={
            <>
              <button
                onClick={resetWidgets}
                className="px-4 py-2 text-sm border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
              >
                Reset to Default
              </button>
              <button
                onClick={closeCustomizeModal}
                className="px-4 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
              >
                Done
              </button>
            </>
          }
        >
          <div className="space-y-2">
            {(Object.keys(DEFAULT_WIDGETS) as Array<keyof WidgetVisibility>).map((widget) => (
              <button
                key={widget}
                onClick={() => toggleWidget(widget)}
                aria-pressed={widgets[widget]}
                className="flex items-center justify-between w-full p-3 bg-slate-50 rounded-lg hover:bg-slate-100 transition-colors"
              >
                <span className="text-sm font-medium text-slate-700">{WIDGET_LABELS[widget]}</span>
                {widgets[widget] ? (
                  <Eye className="w-5 h-5 text-green-500" />
                ) : (
                  <EyeOff className="w-5 h-5 text-slate-400" />
                )}
              </button>
            ))}
          </div>
        </Modal>
      )}
    </div>
  )
}
