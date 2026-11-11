import { useEffect, useRef, useState, type ReactNode } from 'react'
import { AlertTriangle, Lightbulb, ListChecks, Loader2, Package, RefreshCw, User } from 'lucide-react'
import { AiBeeBadge, Bee } from './AiBeeBadge'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
  Legend,
  ReferenceLine,
  ComposedChart,
  PieChart,
  Pie,
  Cell
} from 'recharts'
import { Card, CardHeader, CardContent } from './Card'
import CollapsibleTags from './CollapsibleTags'
import { STATUS_COLORS, STATUS_LABELS } from '../utils/taskStatusDisplay'
import { Modal } from './Modal'
import { ErrorState, LoadingState } from './PageState'
import { formatDate, formatDateTime } from '../utils/dateUtils'
import type { BugSupportInflow, CapacityAnalysis, CareerAttention, CareerOverview, CarryOverTrend, DashboardOverview, Product, ProductEngagement, RetroSummary, SupportDistribution, TaskReadinessItem, TaskReadinessOverview, TeamVelocity, WorkCategoryShare } from '../types'
import { apiErrorMessage } from '../utils/apiErrors'

/**
 * The lower half of the dashboard: how delivery has been going. Widgets that chart sprint by
 * sprint pick their own sprint window; the rest cover all sprints and carry `ALL_TIME_BADGE`.
 */

/** What the Tasks page should filter on when the manager clicks through from a widget. */
export interface TaskDrillDown {
  sprint?: string
  type?: string
  label?: string
  component?: string
  // A direct report's id, or 'unassigned'
  assignee?: string
}

type OpenTasks = (drillDown: TaskDrillDown) => void

/** The sprint name of a clicked Recharts bar or point, when there is one. */
function clickedSprint(state: unknown, key = 'sprintName'): string | undefined {
  const payload = (state as { activePayload?: { payload?: Record<string, unknown> }[] } | null)?.activePayload?.[0]?.payload
  const name = payload?.[key]
  return typeof name === 'string' ? name : undefined
}

export const ALL_TIME_BADGE = (
  <span className="text-xs px-2 py-0.5 rounded-full border border-slate-300 text-slate-500 font-normal">
    All sprints
  </span>
)

/**
 * Shown in place of a lazily-loaded widget whose fetch failed, so a failure is visible and
 * recoverable rather than an empty gap on the page.
 */
export function WidgetLoadError({ title, onRetry, ai = false }: { title: string; onRetry: () => void; ai?: boolean }) {
  return (
    <Card>
      <CardHeader ai={ai} title={title} />
      <CardContent>
        <ErrorState what={title.toLowerCase()} onRetry={onRetry} />
      </CardContent>
    </Card>
  )
}

export function WidgetLoading({ title, ai = false }: { title: string; ai?: boolean }) {
  return (
    <Card>
      <CardHeader ai={ai} title={title} />
      <CardContent>
        <LoadingState />
      </CardContent>
    </Card>
  )
}

function Tabs<T extends string>({
  tabs,
  value,
  onChange,
  label
}: {
  tabs: { key: T; label: string }[]
  value: T
  onChange: (key: T) => void
  label: string
}) {
  return (
    <div role="tablist" aria-label={label} className="inline-flex rounded-lg bg-slate-100 p-0.5">
      {tabs.map(tab => (
        <button
          key={tab.key}
          type="button"
          role="tab"
          aria-selected={value === tab.key}
          onClick={() => onChange(tab.key)}
          className={`px-2.5 py-1 text-xs font-medium rounded-md transition-colors ${
            value === tab.key ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}

type SprintWindow = '1' | '3' | '6' | '12' | 'all'

const SPRINT_WINDOWS: { key: SprintWindow; label: string }[] = [
  { key: '1', label: 'Current' },
  { key: '3', label: 'Last 3' },
  { key: '6', label: 'Last 6' },
  { key: '12', label: 'Last 12' },
  { key: 'all', label: 'All' }
]

type LoadSprintHistory<T> = (sprintCount?: number) => Promise<T>

/**
 * A delivery widget that charts sprint by sprint, with its own choice of how many recent
 * sprints to show. It fetches its own data; `reloadKey` changes when the dashboard reloads
 * (after a Jira sync) and it fetches again.
 */
function SprintHistoryCard<T>({
  title,
  subtitle,
  load,
  reloadKey,
  isEmpty,
  empty,
  children
}: {
  title: string
  subtitle: string
  load: LoadSprintHistory<T>
  reloadKey?: string
  isEmpty: (data: T) => boolean
  empty: string
  children: (data: T) => ReactNode
}) {
  const [sprintWindow, setSprintWindow] = useState<SprintWindow>('all')
  const [data, setData] = useState<T | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let cancelled = false
    setFailed(false)
    load(sprintWindow === 'all' ? undefined : Number(sprintWindow))
      .then(result => { if (!cancelled) setData(result) })
      .catch(err => {
        console.error(`Failed to load ${title.toLowerCase()}:`, err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [load, title, sprintWindow, reloadKey, attempt])

  return (
    <Card className="h-full">
      <CardHeader
        title={title}
        subtitle={subtitle}
        action={<Tabs label={`${title} sprints`} value={sprintWindow} onChange={setSprintWindow} tabs={SPRINT_WINDOWS} />}
      />
      <CardContent>
        {failed ? (
          <ErrorState what={title.toLowerCase()} onRetry={() => setAttempt(a => a + 1)} />
        ) : !data ? (
          <LoadingState />
        ) : isEmpty(data) ? (
          <p className="flex items-center justify-center h-48 text-sm text-slate-500">{empty}</p>
        ) : (
          children(data)
        )}
      </CardContent>
    </Card>
  )
}

interface BarRow {
  key: string
  label: string
  value: number
  display: string
  note?: string
  color?: string
  onClick?: () => void
}

/** A ranked list of horizontal bars; easier to compare than a pie and readable at any length. */
function BarList({ rows, empty }: { rows: BarRow[]; empty: string }) {
  if (rows.length === 0) {
    return <p className="flex items-center justify-center h-40 text-sm text-slate-500">{empty}</p>
  }
  const max = Math.max(...rows.map(r => r.value), 1)
  return (
    <ul className="space-y-2.5 max-h-80 overflow-y-auto pr-1">
      {rows.map(row => {
        const content = (
          <>
            <div className="flex items-baseline justify-between gap-3 text-sm">
              <span className="truncate text-slate-700" title={row.label}>{row.label}</span>
              <span className="flex-shrink-0">
                <span className="font-medium text-slate-900">{row.display}</span>
                {row.note && <span className="ml-1.5 text-xs text-slate-400">{row.note}</span>}
              </span>
            </div>
            <div className="mt-1 h-2 rounded-full bg-slate-100">
              <div
                className="h-2 rounded-full"
                style={{ width: `${(row.value / max) * 100}%`, backgroundColor: row.color ?? '#3b82f6' }}
              />
            </div>
          </>
        )
        return (
          <li key={row.key}>
            {row.onClick ? (
              <button type="button" onClick={row.onClick} className="w-full text-left rounded hover:bg-slate-50">
                {content}
              </button>
            ) : (
              content
            )}
          </li>
        )
      })}
    </ul>
  )
}

const TASK_TYPE_COLORS: Record<string, string> = {
  Spike: '#ef4444',
  Task: '#3b82f6',
  Support: '#f59e0b',
  Story: '#10b981',
  SubTask: '#06b6d4',
  Bug: '#8b5cf6',
  Epic: '#ec4899'
}

// Fixed order, the order the backend lists the categories in; validated for colour-blind
// separation between neighbouring slices, including the last and the first
const WORK_CATEGORY_COLORS: Record<string, string> = {
  Productisation: '#2a78d6',
  'Tech Maintenance': '#eb6834',
  'Tech Roadmap': '#1baf7a',
  'Tech Debt': '#eda100',
  'System Improvements': '#e87ba4'
}

type WorkCategorySlice = { name: string; value: number; percent: number }

// The hovered slice shows in the middle of the donut, in place of the total
function WorkCategoryPie({ title, data, total, unit }: { title: string; data: WorkCategorySlice[]; total: number; unit: string }) {
  const [active, setActive] = useState<WorkCategorySlice | null>(null)
  const hasData = data.some(d => d.value > 0)
  return (
    <figure className="flex flex-col items-center">
      <div className="relative h-40 w-40">
        {hasData ? (
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={data}
                dataKey="value"
                nameKey="name"
                innerRadius={48}
                outerRadius={72}
                startAngle={90}
                endAngle={-270}
                stroke="#ffffff"
                strokeWidth={2}
                isAnimationActive={false}
                onMouseEnter={(_, index) => setActive(data[index])}
                onMouseLeave={() => setActive(null)}
              >
                {data.map(d => (
                  <Cell
                    key={d.name}
                    fill={WORK_CATEGORY_COLORS[d.name]}
                    fillOpacity={active && active.name !== d.name ? 0.35 : 1}
                    className="cursor-default outline-none"
                  />
                ))}
              </Pie>
            </PieChart>
          </ResponsiveContainer>
        ) : (
          <div className="h-full w-full rounded-full border-[24px] border-slate-100" />
        )}
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center px-12 text-center">
          {active ? (
            <>
              <span className="text-[10px] leading-tight text-slate-500">{active.name}</span>
              <span className="text-base font-semibold text-slate-900">{active.value} {unit}</span>
              <span className="text-xs text-slate-500">{Math.round(active.percent)}%</span>
            </>
          ) : (
            <>
              <span className="text-lg font-semibold text-slate-900">{total}</span>
              <span className="text-xs text-slate-500">{unit}</span>
            </>
          )}
        </div>
      </div>
      <figcaption className="mt-1 text-xs font-medium text-slate-600">{title}</figcaption>
    </figure>
  )
}

// One measure in a row, read left to right: the count, its share as a bar, the percentage
function ShareCells({ value, percent, color, divider }: { value: number; percent: number; color: string; divider?: boolean }) {
  return (
    <>
      <td className={`py-1.5 pr-2 text-right tabular-nums font-medium text-slate-900 ${divider ? 'border-l border-slate-200 pl-4' : 'pl-2'}`}>
        {value}
      </td>
      <td className="py-1.5">
        <div className="h-1.5 w-full rounded-full bg-slate-100">
          <div className="h-1.5 rounded-full" style={{ width: `${Math.min(percent, 100)}%`, backgroundColor: color }} />
        </div>
      </td>
      <td className="py-1.5 pl-2 pr-2 text-right tabular-nums text-xs text-slate-500">{Math.round(percent)}%</td>
    </>
  )
}

// Issues and story points side by side: unestimated work shows in one and not the other
function WorkCategoryBreakdown({ categories }: { categories: WorkCategoryShare[] }) {
  const totalTasks = categories.reduce((sum, c) => sum + c.taskCount, 0)
  const totalPoints = categories.reduce((sum, c) => sum + c.storyPoints, 0)
  if (totalTasks === 0) {
    return <p className="flex items-center justify-center h-32 text-sm text-slate-500">No tasks found</p>
  }

  // A fixed layout splits the space left of the category column evenly between the two
  // measures, and each pie heads its own measure's columns
  const measureColumns = (
    <>
      <col className="w-16" />
      <col />
      <col className="w-12" />
    </>
  )
  return (
    <table className="w-full table-fixed text-sm">
      <colgroup>
        <col className="w-44" />
        {measureColumns}
        {measureColumns}
      </colgroup>
      <thead>
        <tr>
          <td />
          <td colSpan={3} className="pb-3">
            <WorkCategoryPie
              title="Issues"
              total={totalTasks}
              unit="issues"
              data={categories.map(c => ({ name: c.categoryName, value: c.taskCount, percent: c.percentageOfTasks }))}
            />
          </td>
          <td colSpan={3} className="pb-3 border-l border-slate-200">
            <WorkCategoryPie
              title="Story points"
              total={totalPoints}
              unit="SP"
              data={categories.map(c => ({ name: c.categoryName, value: c.storyPoints, percent: c.percentageOfStoryPoints }))}
            />
          </td>
        </tr>
        <tr className="text-xs text-slate-500">
          <th className="text-left font-medium pb-2 pr-3 whitespace-nowrap">Work category</th>
          <th className="text-left font-medium pb-2 pl-2" colSpan={3}>Issues</th>
          <th className="text-left font-medium pb-2 pl-4 border-l border-slate-200" colSpan={3}>Story points</th>
        </tr>
      </thead>
      <tbody>
        {categories.map(c => {
          const color = WORK_CATEGORY_COLORS[c.categoryName]
          return (
            <tr key={c.categoryName} className="border-t border-slate-100">
              <td className="py-1.5 pr-3 text-slate-700 whitespace-nowrap">
                <span className="inline-block w-2.5 h-2.5 rounded-sm mr-2 align-middle" style={{ backgroundColor: color }} />
                {c.categoryName}
              </td>
              <ShareCells value={c.taskCount} percent={c.percentageOfTasks} color={color} />
              <ShareCells value={c.storyPoints} percent={c.percentageOfStoryPoints} color={color} divider />
            </tr>
          )
        })}
        <tr className="border-t border-slate-200 font-medium text-slate-900">
          <td className="py-1.5 pr-3">Total</td>
          <td className="py-1.5 pl-2 pr-2 text-right tabular-nums">{totalTasks}</td>
          <td colSpan={2} />
          <td className="py-1.5 pl-4 pr-2 text-right tabular-nums border-l border-slate-200">{totalPoints}</td>
          <td colSpan={2} />
        </tr>
      </tbody>
    </table>
  )
}

type WorkMixTab = 'type' | 'label' | 'component' | 'category'

export function WorkMixCard({ tasks, onOpenTasks }: { tasks: DashboardOverview['tasks']; onOpenTasks: OpenTasks }) {
  const [tab, setTab] = useState<WorkMixTab>('type')
  const [byPoints, setByPoints] = useState(true)

  let rows: BarRow[] = []
  let empty = 'No tasks found'
  if (tab === 'type') {
    rows = byPoints
      ? tasks.tasksByTypeSP.map(t => ({
          key: t.typeName, label: t.typeName, value: t.totalStoryPoints,
          display: `${t.totalStoryPoints} SP`, note: `${t.taskCount} tasks`, color: TASK_TYPE_COLORS[t.typeName]
        }))
      : tasks.tasksByType.map(t => ({
          key: t.typeName, label: t.typeName, value: t.totalTasks,
          display: `${t.totalTasks}`, note: `${t.completedTasks} done`, color: TASK_TYPE_COLORS[t.typeName]
        }))
    rows.sort((a, b) => b.value - a.value)
    empty = byPoints ? 'No story points assigned' : 'No tasks found'
  } else if (tab === 'label') {
    rows = tasks.tasksByLabel.map(l => byPoints
      ? { key: l.label, label: l.label, value: l.totalStoryPoints, display: `${l.totalStoryPoints} SP`, note: `${l.totalTasks} tasks` }
      : { key: l.label, label: l.label, value: l.totalTasks, display: `${l.totalTasks}`, note: `${l.percentageOfTotal}%` })
    rows.sort((a, b) => b.value - a.value)
    empty = 'No labels assigned'
  } else if (tab === 'component') {
    rows = tasks.tasksByComponent.map(c => byPoints
      ? { key: c.component, label: c.component, value: c.totalStoryPoints, display: `${c.totalStoryPoints} SP`, note: `${c.percentageOfTotalStoryPoints}%` }
      : { key: c.component, label: c.component, value: c.totalTasks, display: `${c.totalTasks}`, note: `${c.percentageOfTotal}%` })
    rows.sort((a, b) => b.value - a.value)
    empty = 'No components assigned'
  }

  // Each bar opens the tasks behind it
  const drillDown = (key: string): TaskDrillDown =>
    tab === 'type' ? { type: key }
      : tab === 'label' ? { label: key }
      : { component: key }
  rows = rows.map(row => ({ ...row, onClick: () => onOpenTasks(drillDown(row.key)) }))

  return (
    <Card className="h-full">
      <CardHeader
        title="Work mix"
        subtitle="Where the team's effort goes"
        badge={ALL_TIME_BADGE}
        action={
          tab !== 'category' && (
            <Tabs
              label="Measure"
              value={byPoints ? 'sp' : 'count'}
              onChange={v => setByPoints(v === 'sp')}
              tabs={[{ key: 'sp', label: 'SP' }, { key: 'count', label: 'Tasks' }]}
            />
          )
        }
      />
      <CardContent className="space-y-4">
        <Tabs
          label="Break down by"
          value={tab}
          onChange={setTab}
          tabs={[
            { key: 'type', label: 'Type' },
            { key: 'label', label: 'Label' },
            { key: 'component', label: 'Component' },
            { key: 'category', label: 'Work category' }
          ]}
        />
        {tab === 'category'
          ? <WorkCategoryBreakdown categories={tasks.tasksByWorkCategory} />
          : <BarList rows={rows} empty={empty} />}
      </CardContent>
    </Card>
  )
}

type EngagementTab = 'product' | 'person'
type EngagementPeriod = '3' | '6' | 'all'

const ENGAGEMENT_PERIODS: { key: EngagementPeriod; label: string }[] = [
  { key: '3', label: '3 months' },
  { key: '6', label: '6 months' },
  { key: 'all', label: 'All time' }
]

/**
 * Who has worked on which product, over its own period in months, so it fetches its own data;
 * `reloadKey` changes when the dashboard reloads (after a Jira sync) and it fetches again.
 */
export function ProductEngagementCard({
  load,
  loadProducts,
  reloadKey
}: {
  load: (months?: number) => Promise<ProductEngagement>
  loadProducts: () => Promise<Product[]>
  reloadKey?: string
}) {
  const [tab, setTab] = useState<EngagementTab>('product')
  const [period, setPeriod] = useState<EngagementPeriod>('3')
  const [engagement, setEngagement] = useState<ProductEngagement | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  // A person's products, or a product's people
  const [selected, setSelected] = useState<
    { kind: 'member'; name: string; products: Product[] } | { kind: 'product'; name: string; members: string[] } | null
  >(null)

  useEffect(() => {
    let cancelled = false
    setFailed(false)
    load(period === 'all' ? undefined : Number(period))
      .then(result => { if (!cancelled) setEngagement(result) })
      .catch(err => {
        console.error('Failed to load product engagement:', err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [load, period, reloadKey, attempt])

  // Attribution comes from the backend, which sees every task; the client only resolves the
  // ids it names to full products for the detail list.
  const openMember = async (memberName: string, productIds: string[]) => {
    const ids = new Set(productIds)
    const products = await loadProducts()
    setSelected({ kind: 'member', name: memberName, products: products.filter(p => ids.has(p.id)) })
  }

  const unworked = engagement?.unworkedProducts ?? []
  const siloCount = engagement?.knowledgeSilos.length ?? 0
  const rows: BarRow[] = !engagement ? [] : tab === 'product'
    ? engagement.membersByProduct.map(p => ({
        key: p.productId,
        label: p.productName,
        value: p.memberCount,
        display: `${p.memberCount}`,
        note: p.memberCount === 0 ? 'nobody' : p.memberCount === 1 ? 'person' : 'people',
        color: p.isSilo ? '#ef4444' : '#10b981',
        onClick: () => setSelected({ kind: 'product', name: p.productName, members: [...p.memberNames].sort() })
      }))
    : engagement.productsByMember
        .filter(m => m.productCount > 0)
        .map(m => ({
          key: m.directReportId ?? m.memberName,
          label: m.memberName,
          value: m.productCount,
          display: `${m.productCount}`,
          note: m.productCount === 1 ? 'product' : 'products',
          color: m.isNarrow ? '#f59e0b' : '#8b5cf6',
          onClick: () => openMember(m.memberName, m.productIds)
        }))

  return (
    <Card className="h-full">
      <CardHeader
        title="Product engagement"
        subtitle="Which direct reports have worked on which product, epics aside"
        action={
          <Tabs label="Engagement period" value={period} onChange={setPeriod} tabs={ENGAGEMENT_PERIODS} />
        }
      />
      <CardContent className="space-y-4">
        <Tabs
          label="Engagement view"
          value={tab}
          onChange={setTab}
          tabs={[{ key: 'product', label: 'By product' }, { key: 'person', label: 'By person' }]}
        />
        {failed ? (
          <ErrorState what="product engagement" onRetry={() => setAttempt(a => a + 1)} />
        ) : !engagement ? (
          <LoadingState />
        ) : (
          <>
            {tab === 'product' && unworked.length > 0 && (
              <p className="text-xs text-red-700 bg-red-50 rounded-md px-3 py-2">
                Nobody has worked on: {unworked.map(p => p.productName).join(', ')}
              </p>
            )}
            {tab === 'product' && siloCount > 0 && (
              <p className="text-xs text-red-700 bg-red-50 rounded-md px-3 py-2">
                {siloCount} product{siloCount !== 1 ? 's have' : ' has'} fewer than {engagement.minProductMembers} members
                who have worked on it.
              </p>
            )}
            {tab === 'person' && engagement.unengagedMembers.length > 0 && (
              <p className="text-xs text-amber-700 bg-amber-50 rounded-md px-3 py-2">
                Not on any product: {engagement.unengagedMembers.map(u => u.fullName).join(', ')}
              </p>
            )}
            {tab === 'person' && engagement.narrowlyEngagedMembers.length > 0 && (
              <p className="text-xs text-amber-700 bg-amber-50 rounded-md px-3 py-2">
                Worked on fewer than {engagement.minMemberProducts} products:{' '}
                {engagement.narrowlyEngagedMembers.map(m => m.fullName).join(', ')}
              </p>
            )}
            <BarList rows={rows} empty="No product work found" />
          </>
        )}
      </CardContent>

      {selected && (
        <Modal
          title={selected.kind === 'member' ? `${selected.name}'s products` : `Who has worked on ${selected.name}`}
          subtitle={selected.kind === 'member'
            ? `${selected.products.length} product${selected.products.length !== 1 ? 's' : ''}`
            : `${selected.members.length} ${selected.members.length === 1 ? 'person' : 'people'}`}
          onClose={() => setSelected(null)}
        >
          {selected.kind === 'product' ? (
            selected.members.length > 0 ? (
              <ul className="space-y-2 max-h-80 overflow-y-auto">
                {selected.members.map(member => (
                  <li key={member} className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg">
                    <User className="w-5 h-5 text-emerald-600" />
                    <span className="font-medium text-slate-900">{member}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-slate-500 text-center py-4">Nobody has worked on this product in this period</p>
            )
          ) : selected.products.length > 0 ? (
            <div className="space-y-3 max-h-80 overflow-y-auto">
              {selected.products.map(product => (
                <div key={product.id} className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg">
                  <Package className="w-5 h-5 text-amber-500" />
                  <div className="flex-1">
                    <p className="font-medium text-slate-900">{product.name}</p>
                    {product.description && (
                      <p className="text-sm text-slate-500 line-clamp-1">{product.description}</p>
                    )}
                    {product.projects.length > 0 && (
                      <div className="flex gap-1 mt-1 flex-wrap">
                        <CollapsibleTags
                          items={product.projects}
                          renderItem={project => (
                            <span key={project.id} className="px-2 py-0.5 bg-purple-100 text-purple-700 rounded text-xs">
                              {project.name}
                            </span>
                          )}
                        />
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-slate-500 text-center py-4">No products found for this member</p>
          )}
        </Modal>
      )}
    </Card>
  )
}

const WORKLOAD_STATUSES = [
  ['blockedTasks', 'Blocked'],
  ['inProgressTasks', 'InProgress'],
  ['inReviewTasks', 'InReview'],
  ['inTestTasks', 'InTest'],
  ['poAcceptanceTasks', 'POAcceptance'],
  ['readyToReleaseTasks', 'ReadyToRelease'],
  ['todoTasks', 'Todo'],
  ['backlogTasks', 'Backlog'],
  ['completedTasks', 'Done'],
  ['cancelledTasks', 'Cancelled']
] as const

// Support tickets (Jira issue type Support) and maintenance (labelled) per person
function SupportMaintenance({ support, onOpenTasks }: { support: SupportDistribution; onOpenTasks: OpenTasks }) {
  // Counted in tasks, not points: support and maintenance tickets are often not estimated
  const rows: BarRow[] = support.byAssignee
    .map(a => ({
      key: a.assigneeId ?? a.assigneeName,
      label: a.assigneeName,
      value: a.allTaskCount + a.maintenanceAllTaskCount,
      display: `${a.allTaskCount + a.maintenanceAllTaskCount} tasks`,
      note: `${a.allTaskCount} support · ${a.maintenanceAllTaskCount} maint.`,
      color: '#f59e0b',
      onClick: () => onOpenTasks({ assignee: a.assigneeId ?? 'unassigned' })
    }))
    .sort((a, b) => b.value - a.value)

  return (
    <>
      {(support.allTaskCount > 0 || support.maintenanceAllTaskCount > 0) && (
        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-lg bg-amber-50 px-3 py-2">
            <p className="text-xs text-amber-700">Support</p>
            <p className="text-lg font-semibold text-slate-900">
              {support.completedTaskCount}
              <span className="text-sm font-normal text-slate-500"> / {support.allTaskCount} tasks done</span>
            </p>
          </div>
          <div className="rounded-lg bg-violet-50 px-3 py-2">
            <p className="text-xs text-violet-700">Maintenance</p>
            <p className="text-lg font-semibold text-slate-900">
              {support.maintenanceCompletedTaskCount}
              <span className="text-sm font-normal text-slate-500"> / {support.maintenanceAllTaskCount} tasks done</span>
            </p>
          </div>
        </div>
      )}
      <BarList rows={rows} empty="No support or maintenance work" />
    </>
  )
}

type WorkloadTab = 'status' | 'support'

export function WorkloadCard({ dashboard, onOpenTasks }: { dashboard: DashboardOverview; onOpenTasks: OpenTasks }) {
  const [tab, setTab] = useState<WorkloadTab>('status')
  const people = dashboard.tasks.tasksByAssignee
  const flagged = new Map(dashboard.insights.workloadWarnings.map(w => [w.assigneeName, w.issues]))
  const max = Math.max(...people.map(p => p.totalTasks), 1)

  return (
    <Card>
      <CardHeader title="Workload" subtitle="Tasks per person, by status and support load" badge={ALL_TIME_BADGE} />
      <CardContent className="space-y-4">
        <Tabs
          label="Show"
          value={tab}
          onChange={setTab}
          tabs={[
            { key: 'status', label: 'By status' },
            { key: 'support', label: 'Support & maintenance' }
          ]}
        />
        {tab === 'support' ? (
          <SupportMaintenance support={dashboard.tasks.supportDistribution} onOpenTasks={onOpenTasks} />
        ) : (
          <>
            {people.length === 0 ? (
              <p className="flex items-center justify-center h-32 text-sm text-slate-500">No tasks found</p>
            ) : (
              <ul className="space-y-3">
                {people.map(person => {
                  const issues = flagged.get(person.assigneeName)
                  return (
                    <li key={person.assigneeId ?? person.assigneeName} className="grid grid-cols-[10rem_1fr_5rem] items-center gap-3">
                      <button
                        type="button"
                        onClick={() => onOpenTasks({ assignee: person.assigneeId ?? 'unassigned' })}
                        className="truncate text-left text-sm text-slate-700 hover:text-amber-700 hover:underline"
                        title={`Open ${person.assigneeName}'s tasks`}
                      >
                        {issues && <AlertTriangle className="inline w-3.5 h-3.5 text-amber-500 mr-1 -mt-0.5" />}
                        {person.assigneeName}
                      </button>
                      <div
                        className="flex h-4 rounded overflow-hidden bg-slate-50"
                        style={{ width: `${(person.totalTasks / max) * 100}%` }}
                        title={issues ? issues.join(', ') : undefined}
                      >
                        {WORKLOAD_STATUSES.map(([field, status]) => {
                          const count = person[field]
                          return count > 0 ? (
                            <div
                              key={status}
                              style={{ flexGrow: count, backgroundColor: STATUS_COLORS[status] }}
                              title={`${STATUS_LABELS[status]}: ${count}`}
                            />
                          ) : null
                        })}
                      </div>
                      <span className="text-xs text-slate-500 text-right">
                        {person.completedStoryPoints}/{person.totalStoryPoints} SP
                      </span>
                    </li>
                  )
                })}
              </ul>
            )}
            <div className="flex flex-wrap gap-x-4 gap-y-1 pt-2 border-t border-slate-100">
              {WORKLOAD_STATUSES.map(([, status]) => (
                <span key={status} className="flex items-center gap-1.5 text-xs text-slate-500">
                  <span className="w-2 h-2 rounded-sm" style={{ backgroundColor: STATUS_COLORS[status] }} />
                  {STATUS_LABELS[status]}
                </span>
              ))}
            </div>
          </>
        )}
      </CardContent>
    </Card>
  )
}

function Stat({ label, value, tone = 'text-slate-900' }: { label: string; value: ReactNode; tone?: string }) {
  return (
    <div>
      <p className={`text-xl font-bold ${tone}`}>{value}</p>
      <p className="text-xs text-slate-500">{label}</p>
    </div>
  )
}

export function VelocityCard({ load, reloadKey, onOpenTasks }: { load: LoadSprintHistory<TeamVelocity>; reloadKey?: string; onOpenTasks: OpenTasks }) {
  return (
    <SprintHistoryCard
      title="Velocity"
      subtitle="Completed story points per sprint"
      load={load}
      reloadKey={reloadKey}
      isEmpty={velocity => velocity.sprints.length === 0}
      empty="No completed story points yet"
    >
      {velocity => (
        <>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <Stat label="Avg velocity (SP)" value={velocity.averageVelocity} />
            <Stat label="Total SP" value={velocity.totalStoryPointsCompleted} />
            <Stat
              label="vs previous sprint"
              value={`${velocity.completionTrend > 0 ? '+' : ''}${velocity.completionTrend}%`}
              tone={velocity.completionTrend >= 0 ? 'text-green-600' : 'text-red-600'}
            />
          </div>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart
              data={velocity.sprints}
              onClick={state => { const sprint = clickedSprint(state); if (sprint) onOpenTasks({ sprint }) }}
              className="cursor-pointer"
            >
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="sprintName" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip />
              <Legend />
              <ReferenceLine y={velocity.averageVelocity} stroke="#f59e0b" strokeDasharray="5 5" />
              <Bar dataKey="storyPointsCompleted" fill="#3b82f6" name="Completed SP" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </>
      )}
    </SprintHistoryCard>
  )
}

export function CapacityCard({ load, reloadKey, onOpenTasks }: { load: LoadSprintHistory<CapacityAnalysis>; reloadKey?: string; onOpenTasks: OpenTasks }) {
  return (
    <SprintHistoryCard
      title="Capacity"
      subtitle="Committed against completed, and what is predicted next"
      load={load}
      reloadKey={reloadKey}
      isEmpty={capacity => capacity.pastSprints.length === 0 && !capacity.currentSprint && capacity.futureSprints.length === 0}
      empty="No sprints yet"
    >
      {capacity => <CapacityChart capacity={capacity} onOpenTasks={onOpenTasks} />}
    </SprintHistoryCard>
  )
}

function CapacityChart({ capacity, onOpenTasks }: { capacity: CapacityAnalysis; onOpenTasks: OpenTasks }) {
  // Predictions come from the backend. The next sprint is added to the chart when the
  // sprint window hides it or Hive does not have it yet.
  const next = capacity.nextSprint
  const nextLabel = next ? (next.sprintName ?? 'Next (projected)') : null
  const chartData = [
    ...capacity.pastSprints,
    ...(capacity.currentSprint ? [capacity.currentSprint] : []),
    ...capacity.futureSprints
  ].map(s => ({ ...s, name: s.sprintName } as { name: string; committedPoints?: number; completedPoints?: number; predictedPoints?: number | null }))
  if (next && !capacity.futureSprints.some(s => s.sprintId === next.sprintId)) {
    chartData.push({ name: nextLabel!, predictedPoints: next.predictedPoints })
  }

  return (
    <>
      <div className="grid grid-cols-3 gap-4 mb-4">
        <Stat
          label="Avg utilization"
          value={`${capacity.averageUtilization}%`}
          tone={capacity.averageUtilization < 75 ? 'text-amber-600' : 'text-slate-900'}
        />
        <Stat label="Completed / committed" value={`${capacity.totalCompletedPoints}/${capacity.totalCommittedPoints}`} />
        <div>
          <p className="text-xl font-bold text-violet-600">{next ? `${next.predictedPoints} SP` : '—'}</p>
          <p className="text-xs text-slate-500">Next sprint (predicted)</p>
          {next && (
            <p className="text-xs text-slate-400 mt-0.5">
              {next.isProjected
                ? `Projected ${formatDate(next.startDate)} – ${formatDate(next.endDate)}`
                : next.sprintName}
              {' · '}
              {next.availableMembers} of {next.teamSize} available
            </p>
          )}
        </div>
      </div>
      <ResponsiveContainer width="100%" height={240}>
        <LineChart
          data={chartData}
          // A projected sprint has no sprintName, and no tasks to open
          onClick={state => { const sprint = clickedSprint(state); if (sprint) onOpenTasks({ sprint }) }}
          className="cursor-pointer"
        >
          <CartesianGrid strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="name" tick={{ fontSize: 11 }} />
          <YAxis tick={{ fontSize: 11 }} />
          <Tooltip labelFormatter={label => `Sprint: ${label}`} />
          <Legend />
          <Line type="monotone" dataKey="committedPoints" stroke="#3b82f6" strokeWidth={2} name="Committed SP" dot={{ fill: '#3b82f6' }} />
          <Line type="monotone" dataKey="completedPoints" stroke="#10b981" strokeWidth={2} name="Completed SP" dot={{ fill: '#10b981' }} />
          <Line type="monotone" dataKey="predictedPoints" stroke="#8b5cf6" strokeWidth={2} strokeDasharray="5 5" name="Predicted SP" dot={{ fill: '#8b5cf6' }} connectNulls={false} />
        </LineChart>
      </ResponsiveContainer>
    </>
  )
}

export function CarryOverCard({ load, reloadKey, onOpenTasks }: { load: LoadSprintHistory<CarryOverTrend>; reloadKey?: string; onOpenTasks: OpenTasks }) {
  return (
    <SprintHistoryCard
      title="Carry-over trend"
      subtitle="Work each sprint inherited from an earlier one"
      load={load}
      reloadKey={reloadKey}
      isEmpty={carryOver => carryOver.sprints.length === 0}
      empty="No sprints with work yet"
    >
      {carryOver => {
        const latest = carryOver.sprints[carryOver.sprints.length - 1]
        return (
          <>
            <div className="grid grid-cols-3 gap-4 mb-4">
              <Stat label="Avg carried in" value={`${carryOver.averageCarriedInPercent}%`} />
              <Stat
                label={`Carried into ${latest.sprintName}`}
                value={`${latest.carriedInPercent}%`}
                tone={latest.carriedInPercent > carryOver.averageCarriedInPercent ? 'text-amber-600' : 'text-slate-900'}
              />
              <Stat label={`Moved on from ${latest.sprintName}`} value={`${latest.carriedOutTasks} tasks`} />
            </div>
            <ResponsiveContainer width="100%" height={240}>
              <ComposedChart
                data={carryOver.sprints}
                onClick={state => { const sprint = clickedSprint(state); if (sprint) onOpenTasks({ sprint }) }}
                className="cursor-pointer"
              >
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="sprintName" tick={{ fontSize: 11 }} />
                <YAxis yAxisId="sp" tick={{ fontSize: 11 }} />
                <YAxis yAxisId="pct" orientation="right" unit="%" domain={[0, 100]} tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                <Bar yAxisId="sp" dataKey="newPoints" stackId="sp" fill="#3b82f6" name="New SP" />
                <Bar yAxisId="sp" dataKey="carriedInPoints" stackId="sp" fill="#94a3b8" name="Carried-in SP" radius={[4, 4, 0, 0]} />
                <Line yAxisId="pct" type="monotone" dataKey="carriedInPercent" stroke="#f59e0b" strokeWidth={2} name="Carried in %" dot={{ fill: '#f59e0b' }} />
              </ComposedChart>
            </ResponsiveContainer>
          </>
        )
      }}
    </SprintHistoryCard>
  )
}

export function BugSupportInflowCard({ load, reloadKey, onOpenTasks }: { load: LoadSprintHistory<BugSupportInflow>; reloadKey?: string; onOpenTasks: OpenTasks }) {
  return (
    <SprintHistoryCard
      title="Bug & support inflow"
      subtitle="New bug, support and maintenance tickets per sprint"
      load={load}
      reloadKey={reloadKey}
      isEmpty={inflow => !inflow.sprints.some(s => s.bugTasks > 0 || s.supportTasks > 0 || s.maintenanceTasks > 0)}
      empty="No bugs, support or maintenance tickets in these sprints"
    >
      {inflow => {
        // A bar opens that sprint's tickets of its kind. Maintenance is a label, so it can only be
        // narrowed to one when a single maintenance label is configured.
        const open = (drillDown: Omit<TaskDrillDown, 'sprint'>) => (bar: unknown) => {
          const sprint = (bar as { sprintName?: string } | null)?.sprintName
          if (sprint) onOpenTasks({ sprint, ...drillDown })
        }
        const maintenanceLabel = inflow.maintenanceLabels.length === 1 ? inflow.maintenanceLabels[0] : undefined
        return (
          <>
            <div className="grid grid-cols-4 gap-4 mb-4">
              <Stat label="Bugs / sprint" value={inflow.averageBugsPerSprint} />
              <Stat label="Support / sprint" value={inflow.averageSupportPerSprint} />
              <Stat label="Maintenance / sprint" value={inflow.averageMaintenancePerSprint} />
              <Stat label="Share of new tasks" value={`${inflow.inflowPercent}%`} />
            </div>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={inflow.sprints}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="sprintName" tick={{ fontSize: 11 }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                {/* Side by side, not stacked: a bug labelled maintenance is in both columns */}
                <Bar dataKey="bugTasks" fill={TASK_TYPE_COLORS.Bug} name="Bugs" radius={[4, 4, 0, 0]} className="cursor-pointer" onClick={open({ type: 'Bug' })} />
                <Bar dataKey="supportTasks" fill={TASK_TYPE_COLORS.Support} name="Support" radius={[4, 4, 0, 0]} className="cursor-pointer" onClick={open({ type: 'Support' })} />
                <Bar dataKey="maintenanceTasks" fill="#64748b" name="Maintenance" radius={[4, 4, 0, 0]} className="cursor-pointer" onClick={open({ label: maintenanceLabel })} />
              </BarChart>
            </ResponsiveContainer>
          </>
        )
      }}
    </SprintHistoryCard>
  )
}

/**
 * What in the career plans needs the manager: who has no plan, whose plan has nothing in
 * focus, and whose plan has gone quiet. No stages, levels or scores: nothing that compares
 * people. It fetches its own data while visible and again when the dashboard reloads.
 */
export function CareerPlansCard({
  load,
  reloadKey,
  onOpenPlan,
  onOpenPlans
}: {
  load: () => Promise<CareerOverview>
  reloadKey?: string
  onOpenPlan: (planId: string) => void
  onOpenPlans: () => void
}) {
  const [overview, setOverview] = useState<CareerOverview | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let cancelled = false
    setFailed(false)
    load()
      .then(result => { if (!cancelled) setOverview(result) })
      .catch(err => {
        console.error('Failed to load career plans:', err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [load, reloadKey, attempt])

  if (failed) return <WidgetLoadError title="Career plans" onRetry={() => setAttempt(a => a + 1)} />
  if (!overview) return <WidgetLoading title="Career plans" />

  const { directReportsWithoutPlan, nothingInFocus, quiet } = overview
  const allClear = directReportsWithoutPlan.length === 0 && nothingInFocus.length === 0 && quiet.length === 0

  const open = (person: CareerAttention) => person.planId ? onOpenPlan(person.planId) : onOpenPlans()

  const group = (title: string, people: CareerAttention[], showLastChanged = false) => people.length > 0 && (
    <div>
      <h4 className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1.5">{title}</h4>
      <ul className="flex flex-wrap gap-2" aria-label={title}>
        {people.map(person => (
          <li key={person.directReportId}>
            <button
              type="button"
              onClick={() => open(person)}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border border-slate-200 text-sm text-slate-700 hover:border-amber-400 hover:text-amber-700"
            >
              {person.fullName}
              {showLastChanged && person.lastChangedAt && (
                <span className="text-xs text-slate-400">{formatDate(person.lastChangedAt)}</span>
              )}
            </button>
          </li>
        ))}
      </ul>
    </div>
  )

  return (
    <Card>
      <CardHeader
        title="Career plans"
        subtitle="What needs you"
        action={
          <button type="button" onClick={onOpenPlans} className="text-sm text-amber-600 hover:underline">
            All plans
          </button>
        }
      />
      <CardContent className="space-y-4">
        {allClear ? (
          <p className="text-sm text-slate-500">All clear: everyone has a plan, something in focus, and recent progress.</p>
        ) : (
          <>
            {group('No plan yet', directReportsWithoutPlan)}
            {group('Nothing in focus', nothingInFocus)}
            {group('Quiet for 60 days', quiet, true)}
          </>
        )}
      </CardContent>
    </Card>
  )
}

const RETRO_SUMMARY_TITLE = 'Retro summary'

/**
 * The AI reading of the team's latest retros: warnings first, then the summary, the action
 * items and the suggestions. The server makes it again when a retro changed; a failure keeps
 * the last summary and says why.
 */
export function RetroSummaryCard({
  load,
  refresh,
  reloadKey,
  onOpenNote,
  onOpenNotes,
  onOpenSettings
}: {
  load: () => Promise<RetroSummary>
  refresh: () => Promise<RetroSummary>
  reloadKey?: string
  onOpenNote: (noteId: string) => void
  onOpenNotes: () => void
  onOpenSettings: () => void
}) {
  const [data, setData] = useState<RetroSummary | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [refreshing, setRefreshing] = useState(false)
  const [refreshError, setRefreshError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setFailed(false)
    load()
      .then(result => { if (!cancelled) setData(result) })
      .catch(err => {
        console.error('Failed to load the retro summary:', err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [load, reloadKey, attempt])

  const handleRefresh = async () => {
    setRefreshing(true)
    setRefreshError(null)
    try {
      setData(await refresh())
    } catch (err) {
      console.error('Failed to refresh the retro summary:', err)
      setRefreshError(apiErrorMessage(err) ?? 'The retro summary could not be refreshed.')
    } finally {
      setRefreshing(false)
    }
  }

  if (failed) return <WidgetLoadError ai title={RETRO_SUMMARY_TITLE} onRetry={() => setAttempt(a => a + 1)} />
  if (!data) {
    return (
      <Card>
        <CardHeader ai title={RETRO_SUMMARY_TITLE} />
        <CardContent>
          <LoadingState label="Reading the retros… a local model can take a minute or more." />
        </CardContent>
      </Card>
    )
  }

  const setupMessage = (message: string, action: string, onAction: () => void) => (
    <Card>
      <CardHeader ai title={RETRO_SUMMARY_TITLE} subtitle="From the notes in your Retro folder" />
      <CardContent>
        <p className="text-sm text-slate-500">
          {message}{' '}
          <button type="button" onClick={onAction} className="text-amber-600 hover:underline">{action}</button>
        </p>
      </CardContent>
    </Card>
  )

  if (!data.isEnabled) return setupMessage('The retro summary is turned off.', 'Turn it on in Settings', onOpenSettings)
  if (!data.isConfigured) return setupMessage('The AI model needs a Claude API key.', 'Add it in Settings', onOpenSettings)
  if (!data.hasRetroFolder) return setupMessage('Create a folder named Retro in Notes, with one note per retro titled by its date.', 'Open Notes', onOpenNotes)
  if (data.retros.length === 0) return setupMessage('The Retro folder has no retros yet.', 'Open Notes', onOpenNotes)

  const error = refreshError ?? data.error
  const retroCount = data.retros.length

  return (
    <Card>
      <CardHeader
        ai
        title={RETRO_SUMMARY_TITLE}
        subtitle="What the team keeps raising, and what to do about it"
        badge={
          <span className="text-xs px-2 py-0.5 rounded-full border border-slate-300 text-slate-500 font-normal">
            {retroCount === 1 ? 'Latest retro' : `Latest ${retroCount} retros`}
          </span>
        }
        action={
          <button
            type="button"
            onClick={handleRefresh}
            disabled={refreshing}
            className="inline-flex items-center gap-1.5 text-sm text-amber-600 hover:underline disabled:opacity-50 disabled:no-underline"
          >
            {refreshing ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
            {refreshing ? 'Reading…' : 'Refresh'}
          </button>
        }
      />
      <CardContent className="space-y-5">
        {error && (
          <div role="alert" className="p-3 rounded-lg border border-red-200 bg-red-50 text-sm text-red-700">
            Could not update the summary: {error}
            {data.summary && data.analyzedAt && ` Showing the one from ${formatDate(data.analyzedAt)}.`}
          </div>
        )}

        {data.warnings.length > 0 && (
          <ul aria-label="Retro warnings" className="space-y-2">
            {data.warnings.map((warning, i) => (
              <li key={i} className="flex items-start gap-2 p-3 rounded-lg border border-amber-200 bg-amber-50 text-sm text-amber-800">
                <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                {warning}
              </li>
            ))}
          </ul>
        )}

        {data.summary ? (
          <>
            <p className="text-sm text-slate-700 leading-relaxed whitespace-pre-line">{data.summary}</p>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-slate-500 mb-2">
                  <ListChecks className="w-4 h-4" /> Action items
                </h4>
                {data.actionItems.length === 0 ? (
                  <p className="text-sm text-slate-400">No open action items.</p>
                ) : (
                  <ul aria-label="Action items" className="space-y-2">
                    {data.actionItems.map((item, i) => (
                      <li key={i} className="text-sm text-slate-700">
                        {item.text}
                        <span className="block text-xs text-slate-400">
                          {[item.owner, item.retroDate && `agreed ${formatDate(item.retroDate)}`].filter(Boolean).join(' · ')}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div>
                <h4 className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-slate-500 mb-2">
                  <Lightbulb className="w-4 h-4" /> Suggestions
                </h4>
                {data.suggestions.length === 0 ? (
                  <p className="text-sm text-slate-400">No suggestions.</p>
                ) : (
                  <ul aria-label="Suggestions" className="list-disc pl-5 space-y-2">
                    {data.suggestions.map((suggestion, i) => (
                      <li key={i} className="text-sm text-slate-700">{suggestion}</li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          </>
        ) : (
          !error && <p className="text-sm text-slate-500">No summary yet. Press Refresh to read the retros.</p>
        )}

        <div className="pt-3 border-t border-slate-100 text-xs text-slate-500 space-y-1.5">
          <ul aria-label="Retros read" className="flex flex-wrap gap-2">
            {data.retros.map(retro => (
              <li key={retro.noteId}>
                <button
                  type="button"
                  onClick={() => onOpenNote(retro.noteId)}
                  className="px-2 py-0.5 rounded-full border border-slate-200 text-slate-600 hover:border-amber-400 hover:text-amber-700"
                >
                  {formatDate(retro.retroDate)}
                </button>
              </li>
            ))}
          </ul>
          {data.analyzedAt && (
            <p>
              Summarised {formatDateTime(data.analyzedAt)}{data.model && ` by ${data.model}`}
              {!data.isUpToDate && ' · out of date'}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

const TASK_READINESS_TITLE = 'Task readiness'

const CURRENT_AND_NEXT_BADGE = (
  <span className="text-xs px-2 py-0.5 rounded-full border border-slate-300 text-slate-500 font-normal">
    Current &amp; next sprint
  </span>
)

/**
 * Whether the unstarted work of the current and next sprint is ready to build. Hive's own
 * fact (no acceptance criteria) shows on every task; the AI reviews the tasks one call at a
 * time while the card is open, and each review carries the bee badge.
 */
export function TaskReadinessCard({
  load,
  reviewNext,
  reviewTask,
  reloadKey,
  onOpenTask,
  onOpenMissingCriteria,
  onOpenSettings
}: {
  load: () => Promise<TaskReadinessOverview>
  reviewNext: () => Promise<TaskReadinessOverview>
  reviewTask: (taskId: string) => Promise<TaskReadinessOverview>
  reloadKey?: string
  onOpenTask: (task: TaskReadinessItem) => void
  onOpenMissingCriteria: () => void
  onOpenSettings: () => void
}) {
  const [data, setData] = useState<TaskReadinessOverview | null>(null)
  const [failed, setFailed] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [stopped, setStopped] = useState(false)
  const [reviewing, setReviewing] = useState(false)
  const [reviewingTask, setReviewingTask] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [showAll, setShowAll] = useState(false)

  useEffect(() => {
    let cancelled = false
    setFailed(false)
    load()
      .then(result => { if (!cancelled) setData(result) })
      .catch(err => {
        console.error('Failed to load task readiness:', err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [load, reloadKey, attempt])

  // Review the pending tasks one at a time while the card is on screen. A ref, not state,
  // guards the request in flight, so a re-render never sends the same task twice.
  const inFlight = useRef(false)
  const mounted = useRef(true)
  // Changes whenever a review ends, so the next one starts even when nothing else on screen did
  const [finished, setFinished] = useState(0)
  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  const pending = data && data.isEnabled && data.isConfigured ? data.pendingCount : 0
  useEffect(() => {
    if (pending === 0 || stopped || error || reviewingTask || inFlight.current) return
    inFlight.current = true
    setReviewing(true)
    reviewNext()
      .then(result => {
        if (!mounted.current) return
        setData(result)
        if (result.error) setError(result.error)
      })
      .catch(err => {
        console.error('Failed to review a task:', err)
        if (mounted.current) setError(apiErrorMessage(err) ?? 'A task could not be reviewed.')
      })
      .finally(() => {
        inFlight.current = false
        if (mounted.current) {
          setReviewing(false)
          setFinished(n => n + 1)
        }
      })
  }, [pending, stopped, error, reviewingTask, finished, reviewNext])

  const handleReviewAgain = async (taskId: string) => {
    setReviewingTask(taskId)
    setError(null)
    try {
      const result = await reviewTask(taskId)
      setData(result)
      if (result.error) setError(result.error)
    } catch (err) {
      console.error('Failed to review the task:', err)
      setError(apiErrorMessage(err) ?? 'The task could not be reviewed.')
    } finally {
      setReviewingTask(null)
    }
  }

  const resume = () => {
    setError(null)
    setStopped(false)
  }

  if (failed) return <WidgetLoadError ai title={TASK_READINESS_TITLE} onRetry={() => setAttempt(a => a + 1)} />
  if (!data) return <WidgetLoading ai title={TASK_READINESS_TITLE} />

  const missingCriteria = data.tasks.filter(t => t.needsAcceptanceCriteria && !t.hasAcceptanceCriteria)
  const needsAttention = (t: TaskReadinessItem) =>
    (t.needsAcceptanceCriteria && !t.hasAcceptanceCriteria) || !t.review || t.review.verdict !== 'Ready'
  const shown = showAll ? data.tasks : data.tasks.filter(needsAttention)
  const readyCount = data.tasks.length - data.tasks.filter(needsAttention).length
  const sprints = [data.currentSprint, data.nextSprint].filter(Boolean).join(' and ')

  return (
    <Card>
      <CardHeader
        ai
        title={TASK_READINESS_TITLE}
        subtitle={sprints ? `Unstarted work in ${sprints}` : 'Unstarted work in the current and next sprint'}
        badge={CURRENT_AND_NEXT_BADGE}
        action={
          pending > 0 && !error && (
            stopped ? (
              <button type="button" onClick={resume} className="text-sm text-amber-600 hover:underline">
                Resume review
              </button>
            ) : (
              <button type="button" onClick={() => setStopped(true)} className="text-sm text-slate-500 hover:underline">
                Stop
              </button>
            )
          )
        }
      />
      <CardContent className="space-y-4">
        {!data.currentSprint ? (
          <p className="text-sm text-slate-500">Hive has no current sprint yet. Sync the sprints from Jira to check the upcoming work.</p>
        ) : data.tasks.length === 0 ? (
          <p className="text-sm text-slate-500">No unstarted work in these sprints.</p>
        ) : (
          <>
            {!data.isEnabled && (
              <p className="text-sm text-slate-500">
                The AI review is turned off; only Hive&apos;s own checks are shown.{' '}
                <button type="button" onClick={onOpenSettings} className="text-amber-600 hover:underline">Turn it on in Settings</button>
              </p>
            )}
            {data.isEnabled && !data.isConfigured && (
              <p className="text-sm text-slate-500">
                The AI model needs a Claude API key.{' '}
                <button type="button" onClick={onOpenSettings} className="text-amber-600 hover:underline">Add it in Settings</button>
              </p>
            )}

            {pending > 0 && !error && (
              <div role="status" className="flex items-center gap-2 text-sm text-slate-600">
                {reviewing && !stopped ? <Loader2 className="w-4 h-4 animate-spin text-amber-500" /> : <Bee size={16} />}
                {stopped
                  ? `Review paused · ${pending} ${pending === 1 ? 'task' : 'tasks'} left`
                  : `Reviewing with AI · ${pending} ${pending === 1 ? 'task' : 'tasks'} left`}
              </div>
            )}

            {error && (
              <div role="alert" className="flex items-start justify-between gap-3 p-3 rounded-lg border border-red-200 bg-red-50 text-sm text-red-700">
                <span>Could not review a task: {error}</span>
                <button type="button" onClick={resume} className="flex-shrink-0 text-red-700 underline">Try again</button>
              </div>
            )}

            {missingCriteria.length > 0 && (
              <button
                type="button"
                onClick={onOpenMissingCriteria}
                className="flex items-center gap-2 text-sm text-red-700 hover:underline"
              >
                <AlertTriangle className="w-4 h-4" />
                {missingCriteria.length === 1
                  ? '1 story or task has no acceptance criteria'
                  : `${missingCriteria.length} stories and tasks have no acceptance criteria`}
              </button>
            )}

            <ul aria-label="Upcoming tasks" className="divide-y divide-slate-100">
              {shown.map(task => (
                <li key={task.taskId} className="py-3 first:pt-0">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <button
                          type="button"
                          onClick={() => onOpenTask(task)}
                          className="text-sm font-medium text-slate-900 hover:text-amber-700 text-left"
                        >
                          {task.jiraKey && <span className="text-slate-400 font-normal mr-1.5">{task.jiraKey}</span>}
                          {task.title}
                        </button>
                        <span className="text-xs text-slate-400">{task.typeName}</span>
                        {task.needsAcceptanceCriteria && !task.hasAcceptanceCriteria && (
                          <span className="text-xs px-1.5 py-0.5 rounded bg-red-50 text-red-700 border border-red-200">
                            No acceptance criteria
                          </span>
                        )}
                        {task.review && <AiBeeBadge review={task.review} />}
                      </div>
                      {task.review && task.review.verdict !== 'Ready' && (
                        <>
                          <p className="text-sm text-slate-600">{task.review.summary}</p>
                          {task.review.missing.length > 0 && (
                            <ul aria-label={`Missing from ${task.title}`} className="list-disc pl-5 text-sm text-slate-600">
                              {task.review.missing.map((item, i) => <li key={i}>{item}</li>)}
                            </ul>
                          )}
                        </>
                      )}
                    </div>
                    {data.isEnabled && data.isConfigured && task.review && (
                      <button
                        type="button"
                        onClick={() => handleReviewAgain(task.taskId)}
                        disabled={reviewingTask !== null || reviewing}
                        aria-label={`Review ${task.title} again`}
                        title="Review again"
                        className="flex-shrink-0 p-1 text-slate-400 hover:text-amber-600 disabled:opacity-40"
                      >
                        {reviewingTask === task.taskId ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>

            {shown.length === 0 && (
              <p className="text-sm text-slate-500">Everything upcoming reads as ready to build.</p>
            )}

            {readyCount > 0 && (
              <button type="button" onClick={() => setShowAll(!showAll)} className="text-sm text-amber-600 hover:underline">
                {showAll ? 'Show only what needs attention' : `Show ${readyCount} ready ${readyCount === 1 ? 'task' : 'tasks'} too`}
              </button>
            )}
          </>
        )}
      </CardContent>
    </Card>
  )
}
