import { useState, type ReactNode } from 'react'
import {
  AlertTriangle,
  CalendarOff,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  CircleDashed,
  ClipboardList,
  Coins,
  Clock,
  ExternalLink,
  Gauge,
  Hourglass,
  PartyPopper,
  Flame,
  MessageSquare,
  Package,
  PackageX,
  Snail,
  Tag,
  TrendingDown,
  TrendingUp,
  UserMinus,
  UserX,
  Users
} from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import { formatDate } from '../utils/dateUtils'
import { STATUS_COLORS, STATUS_LABELS } from '../utils/taskStatusDisplay'
import type {
  CurrentSprintFocus,
  DashboardWarning,
  DashboardWarningCategory,
  LeaveFocus,
  LeaveFocusItem,
  OneOnOneCadenceEntry,
  OneOnOneCadenceOverview,
  SprintPace,
  TenureMilestone,
  TodoFocus
} from '../types'

/**
 * The top of the dashboard: what needs the manager now. Every number here comes from
 * `/reports/focus` or the dashboard's warning list; these components only lay it out.
 */

const CATEGORY_ICONS: Record<DashboardWarningCategory, ReactNode> = {
  OneOnOnes: <MessageSquare className="w-4 h-4" />,
  OverdueTasks: <Clock className="w-4 h-4" />,
  StaleTasks: <Hourglass className="w-4 h-4" />,
  Workload: <Users className="w-4 h-4" />,
  UnengagedMembers: <UserX className="w-4 h-4" />,
  NarrowlyEngagedMembers: <UserMinus className="w-4 h-4" />,
  UnworkedProducts: <PackageX className="w-4 h-4" />,
  KnowledgeSilos: <Package className="w-4 h-4" />,
  ScopeCreep: <TrendingUp className="w-4 h-4" />,
  UnmatchedTasks: <Tag className="w-4 h-4" />,
  EstimatedSpikes: <Coins className="w-4 h-4" />,
  EstimatedSupport: <Coins className="w-4 h-4" />,
  UnestimatedTasks: <CircleDashed className="w-4 h-4" />,
  MissingAcceptanceCriteria: <ClipboardList className="w-4 h-4" />,
  LowUtilization: <Gauge className="w-4 h-4" />,
  VelocityTrend: <TrendingDown className="w-4 h-4" />,
  CareerPlans: <Snail className="w-4 h-4" />
}

// People and the manager's own commitments read amber; delivery risks read rose.
const PEOPLE_CATEGORIES: DashboardWarningCategory[] = ['OneOnOnes', 'Workload', 'UnengagedMembers', 'NarrowlyEngagedMembers', 'CareerPlans']

export function AttentionCard({
  warnings,
  onSelect
}: {
  warnings: DashboardWarning[]
  onSelect: (category: DashboardWarningCategory) => void
}) {
  return (
    <Card className="h-full flex flex-col">
      <CardHeader
        title="Needs attention"
        badge={
          <span
            className={`text-xs font-semibold px-2 py-0.5 rounded-full ${
              warnings.length === 0 ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'
            }`}
            aria-label={`${warnings.length} warnings`}
          >
            {warnings.length}
          </span>
        }
      />
      {warnings.length === 0 ? (
        <CardContent className="flex-1 flex flex-col items-center justify-center text-center gap-2 py-10">
          <CheckCircle2 className="w-8 h-8 text-green-500" />
          <p className="text-sm font-medium text-slate-700">All clear</p>
          <p className="text-xs text-slate-500">Nothing needs you right now.</p>
        </CardContent>
      ) : (
        <ul className="flex-1 divide-y divide-slate-100 overflow-y-auto max-h-[26rem]">
          {warnings.map((warning, idx) => {
            const isPeople = PEOPLE_CATEGORIES.includes(warning.category)
            return (
              <li key={`${warning.category}-${idx}`}>
                <button
                  type="button"
                  onClick={() => onSelect(warning.category)}
                  className="group w-full text-left px-6 py-3 hover:bg-slate-50 transition-colors flex items-start gap-3"
                >
                  <span
                    className={`mt-0.5 p-1.5 rounded-md flex-shrink-0 ${
                      isPeople ? 'bg-amber-50 text-amber-600' : 'bg-rose-50 text-rose-600'
                    }`}
                  >
                    {CATEGORY_ICONS[warning.category] ?? <AlertTriangle className="w-4 h-4" />}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-medium text-slate-900">{warning.title}</span>
                    {warning.detail && (
                      <span className="block text-xs text-slate-500 mt-0.5">{warning.detail}</span>
                    )}
                  </span>
                  <ChevronRight className="w-4 h-4 text-slate-300 group-hover:text-slate-500 mt-1 flex-shrink-0" />
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}

const PACE_STYLES: Record<SprintPace, { label: string; className: string }> = {
  NotStarted: { label: 'Not started', className: 'bg-slate-100 text-slate-600' },
  Ahead: { label: 'Ahead of pace', className: 'bg-green-100 text-green-700' },
  OnPace: { label: 'On pace', className: 'bg-blue-100 text-blue-700' },
  Behind: { label: 'Behind pace', className: 'bg-red-100 text-red-700' }
}

export function SprintHealthCard({
  sprint,
  onOpenTasks,
  onOpenSprints
}: {
  sprint: CurrentSprintFocus | null | undefined
  onOpenTasks: () => void
  onOpenSprints: () => void
}) {
  if (!sprint) {
    return (
      <Card className="h-full">
        <CardHeader title="Current sprint" />
        <CardContent className="flex flex-col items-center justify-center text-center gap-2 py-10">
          <p className="text-sm text-slate-500">No sprints yet. Import tasks or sync sprints from Jira.</p>
          <button type="button" onClick={onOpenSprints} className="text-sm font-medium text-amber-600 hover:text-amber-700">
            Go to Sprints
          </button>
        </CardContent>
      </Card>
    )
  }

  const pace = PACE_STYLES[sprint.pace] ?? PACE_STYLES.OnPace
  const statusTotal = sprint.byStatus.reduce((sum, s) => sum + s.taskCount, 0)

  return (
    <Card className="h-full">
      <CardHeader
        title={sprint.name}
        badge={
          sprint.isActive ? (
            <span className="text-xs px-2 py-0.5 rounded-full border border-blue-300 text-blue-600 font-normal">Current</span>
          ) : (
            <span className="text-xs px-2 py-0.5 rounded-full border border-slate-300 text-slate-500 font-normal">Next</span>
          )
        }
        subtitle={`${formatDate(sprint.startDate)} – ${formatDate(sprint.endDate)} · ${
          sprint.isActive
            ? `${sprint.workingDaysLeft} of ${sprint.workingDays} working days left`
            : `starts ${formatDate(sprint.startDate)}`
        }`}
        action={
          <button type="button" onClick={onOpenTasks} className="text-sm font-medium text-amber-600 hover:text-amber-700">
            View tasks
          </button>
        }
      />
      <CardContent className="space-y-5">
        <div className="flex flex-wrap items-end gap-x-8 gap-y-3">
          <div>
            <p className="text-xs uppercase tracking-wide text-slate-500">Story points done</p>
            <p className="text-2xl font-bold text-slate-900">
              {sprint.newCompletedPoints}
              <span className="text-base font-medium text-slate-400"> / {sprint.newPoints} SP</span>
            </p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-slate-500">Tasks done</p>
            <p className="text-2xl font-bold text-slate-900">
              {sprint.doneTasks}
              <span className="text-base font-medium text-slate-400"> / {sprint.totalTasks}</span>
            </p>
          </div>
          <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${pace.className}`}>{pace.label}</span>
        </div>

        {/* Done work against time gone: the marker is where the fill should be by now */}
        <div>
          <div
            className="relative h-3 rounded-full bg-slate-100"
            role="progressbar"
            aria-label="Sprint progress"
            aria-valuenow={sprint.completedPercent}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            <div className="h-3 rounded-full bg-blue-500" style={{ width: `${sprint.completedPercent}%` }} />
            {sprint.isActive && (
              <div
                className="absolute -top-1 h-5 w-0.5 bg-slate-700"
                style={{ left: `${sprint.elapsedPercent}%` }}
                title={`${sprint.elapsedPercent}% of the sprint has gone`}
              />
            )}
          </div>
          <div className="flex justify-between text-xs text-slate-500 mt-1.5">
            <span>{sprint.completedPercent}% of new work done</span>
            {sprint.isActive && <span>{sprint.elapsedPercent}% of time gone</span>}
          </div>
        </div>

        {statusTotal > 0 && (
          <div>
            <div className="flex h-2 rounded-full overflow-hidden bg-slate-100">
              {sprint.byStatus.map(s => (
                <div
                  key={s.statusName}
                  style={{ width: `${(s.taskCount / statusTotal) * 100}%`, backgroundColor: STATUS_COLORS[s.statusName] }}
                  title={`${STATUS_LABELS[s.statusName] ?? s.statusName}: ${s.taskCount} tasks, ${s.storyPoints} SP`}
                />
              ))}
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 mt-2">
              {sprint.byStatus.map(s => {
                // Blocked is always listed; it stands out whenever anything is blocked
                const alarming = s.statusName === 'Blocked' && s.taskCount > 0
                return (
                  <span
                    key={s.statusName}
                    className={`flex items-center gap-1.5 text-xs ${
                      alarming ? 'px-1.5 -mx-1.5 rounded bg-red-50 text-red-700 font-medium' : 'text-slate-600'
                    }`}
                  >
                    <span className="w-2 h-2 rounded-full" style={{ backgroundColor: STATUS_COLORS[s.statusName] }} />
                    {STATUS_LABELS[s.statusName] ?? s.statusName}
                    <span className={`font-semibold ${alarming ? 'text-red-700' : 'text-slate-900'}`}>{s.taskCount}</span>
                  </span>
                )
              })}
            </div>
          </div>
        )}

        <div className="flex flex-wrap gap-2 text-xs">
          {sprint.committedPoints > 0 && (
            <span className="px-2 py-1 rounded-md bg-slate-100 text-slate-600">{sprint.committedPoints} SP committed</span>
          )}
          {sprint.scopeCreepPoints > 0 && (
            <span className="px-2 py-1 rounded-md bg-amber-50 text-amber-700">+{sprint.scopeCreepPoints} SP scope creep</span>
          )}
          {sprint.carriedOverPoints > 0 && (
            <span className="px-2 py-1 rounded-md bg-blue-50 text-blue-700">{sprint.carriedOverPoints} SP carried over</span>
          )}
        </div>

        {sprint.blockedTasks.length > 0 && (
          <div className="rounded-lg border border-red-100 bg-red-50/50">
            <p className="px-3 pt-2.5 text-xs font-semibold text-red-700">
              {sprint.blockedTasks.length} blocked
            </p>
            <ul className="divide-y divide-red-100">
              {sprint.blockedTasks.slice(0, 5).map(task => (
                <li key={task.id} className="px-3 py-2 flex items-center gap-2 text-sm">
                  <span className="min-w-0 flex-1 truncate text-slate-800" title={task.title}>{task.title}</span>
                  {task.assigneeName && <span className="text-xs text-slate-500 flex-shrink-0">{task.assigneeName}</span>}
                  {task.storyPoints != null && (
                    <span className="text-xs text-slate-500 flex-shrink-0">{task.storyPoints} SP</span>
                  )}
                  {task.jiraUrl && (
                    <a
                      href={task.jiraUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="flex items-center gap-1 text-xs text-blue-600 hover:underline flex-shrink-0"
                    >
                      {task.jiraKey}
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function cadenceLabel(person: OneOnOneCadenceEntry): { text: string; className: string } {
  const days = person.daysUntilDue
  switch (person.status) {
    case 'NeverMet':
      return { text: 'No 1:1 yet', className: 'bg-red-100 text-red-700' }
    case 'Overdue':
      return { text: `Overdue ${Math.abs(days ?? 0)}d`, className: 'bg-red-100 text-red-700' }
    case 'DueSoon':
      return {
        text: days === 0 ? 'Due today' : `Due in ${days}d`,
        className: 'bg-amber-100 text-amber-700'
      }
    default:
      return { text: `In ${days}d`, className: 'bg-slate-100 text-slate-600' }
  }
}

export function OneOnOnesCard({
  oneOnOnes,
  onOpenPerson
}: {
  oneOnOnes: OneOnOneCadenceOverview
  onOpenPerson: (directReportId: string) => void
}) {
  const [showOnTrack, setShowOnTrack] = useState(false)
  const due = oneOnOnes.people.filter(p => p.status !== 'OnTrack')
  const onTrack = oneOnOnes.people.filter(p => p.status === 'OnTrack')

  const row = (person: OneOnOneCadenceEntry) => {
    const label = cadenceLabel(person)
    return (
      <li key={person.directReportId}>
        <button
          type="button"
          onClick={() => onOpenPerson(person.directReportId)}
          className="w-full text-left px-6 py-2.5 hover:bg-slate-50 transition-colors flex items-center gap-3"
        >
          <span className="min-w-0 flex-1">
            <span className="block text-sm font-medium text-slate-900 truncate">{person.fullName}</span>
            <span className="block text-xs text-slate-500">
              {person.lastMeetingDate ? `Last ${formatDate(person.lastMeetingDate)}` : 'Never met'}
              {person.isOnLeave && ' · on leave'}
            </span>
          </span>
          <span className={`text-xs font-medium px-2 py-0.5 rounded-full flex-shrink-0 ${label.className}`}>
            {label.text}
          </span>
        </button>
      </li>
    )
  }

  return (
    <Card className="h-full flex flex-col">
      <CardHeader title="1:1s" subtitle={`Every ${oneOnOnes.cadenceDays} days`} />
      {oneOnOnes.people.length === 0 ? (
        <CardContent className="text-sm text-slate-500">No direct reports yet.</CardContent>
      ) : (
        <div className="flex-1">
          {due.length === 0 && (
            <p className="px-6 pt-4 pb-2 text-sm text-slate-600 flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4 text-green-500" />
              Everyone is on track.
            </p>
          )}
          <ul className="divide-y divide-slate-100">{due.map(row)}</ul>
          {onTrack.length > 0 && (
            <>
              <button
                type="button"
                onClick={() => setShowOnTrack(v => !v)}
                aria-expanded={showOnTrack}
                className="w-full px-6 py-2.5 text-left text-xs font-medium text-slate-500 hover:text-slate-700 flex items-center gap-1 border-t border-slate-100"
              >
                {onTrack.length} on track
                <ChevronDown className={`w-3 h-3 transition-transform ${showOnTrack ? 'rotate-180' : ''}`} />
              </button>
              {showOnTrack && <ul className="divide-y divide-slate-100">{onTrack.map(row)}</ul>}
            </>
          )}
        </div>
      )}
    </Card>
  )
}

/**
 * The to-dos are the "hot" bullets of the latest journal entry, exactly as written there.
 * The note is the place to change them; every row opens it.
 */
export function TodosCard({ todos, onOpen }: { todos: TodoFocus; onOpen: (noteId?: string) => void }) {
  const noteId = todos.noteId ?? undefined
  const empty = !todos.hasJournal
    ? 'Add a Journal folder in Notes. The hot items of its latest note show here.'
    : !noteId
      ? 'No notes in the Journal folder yet.'
      : 'Nothing hot in the latest journal entry.'

  return (
    <Card className="h-full flex flex-col">
      <CardHeader
        title="To-dos"
        subtitle={todos.noteTitle ? `Hot in ${todos.noteTitle}` : undefined}
        action={
          <button type="button" onClick={() => onOpen(noteId)} className="text-sm font-medium text-amber-600 hover:text-amber-700">
            {noteId ? 'Open note' : 'Notes'}
          </button>
        }
      />
      {todos.items.length === 0 ? (
        <CardContent className="text-sm text-slate-600 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-green-500 flex-shrink-0" />
          {empty}
        </CardContent>
      ) : (
        <ul className="flex-1 divide-y divide-slate-100">
          {todos.items.map((item, index) => (
            <li key={index}>
              <button
                type="button"
                onClick={() => onOpen(noteId)}
                className="w-full text-left px-6 py-2.5 hover:bg-slate-50 transition-colors flex items-start gap-3"
              >
                <Flame className="w-4 h-4 mt-0.5 text-orange-500 flex-shrink-0" />
                <span className="min-w-0 flex-1 text-sm text-slate-900">{item}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

function LeaveRow({ leave }: { leave: LeaveFocusItem }) {
  const sameDay = leave.startDate === leave.endDate
  return (
    <li className="px-6 py-2.5 flex items-center gap-3">
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium text-slate-900 truncate">{leave.fullName}</span>
        <span className="block text-xs text-slate-500">
          {sameDay ? formatDate(leave.startDate) : `${formatDate(leave.startDate)} – ${formatDate(leave.endDate)}`}
        </span>
      </span>
      <span className="text-xs text-slate-500 flex-shrink-0">
        {leave.type === 'PublicHoliday' ? 'Public holiday' : leave.type} · {leave.businessDays}d
      </span>
    </li>
  )
}

export function WhosOutCard({ leave, onOpen }: { leave: LeaveFocus; onOpen: () => void }) {
  const nobody = leave.outToday.length === 0 && leave.upcoming.length === 0
  return (
    <Card className="h-full flex flex-col">
      <CardHeader
        title="Who's out"
        subtitle={`Today and the next ${leave.lookAheadDays} days`}
        action={
          <button type="button" onClick={onOpen} className="text-sm font-medium text-amber-600 hover:text-amber-700">
            Leave
          </button>
        }
      />
      {nobody ? (
        <CardContent className="text-sm text-slate-600 flex items-center gap-2">
          <CalendarOff className="w-4 h-4 text-slate-400" />
          No leave booked.
        </CardContent>
      ) : (
        <div className="flex-1">
          <p className="px-6 pt-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Out today</p>
          {leave.outToday.length === 0 ? (
            <p className="px-6 py-2 text-sm text-slate-500">Everyone is in.</p>
          ) : (
            <ul className="divide-y divide-slate-100">
              {leave.outToday.map(l => <LeaveRow key={l.leaveId} leave={l} />)}
            </ul>
          )}
          {leave.upcoming.length > 0 && (
            <>
              <p className="px-6 pt-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Coming up</p>
              <ul className="divide-y divide-slate-100">
                {leave.upcoming.map(l => <LeaveRow key={l.leaveId} leave={l} />)}
              </ul>
            </>
          )}
        </div>
      )}
    </Card>
  )
}

function milestoneLabel(m: TenureMilestone) {
  if (m.kind === 'SixMonths') return '6 months'
  return m.years === 1 ? '1 year' : `${m.years} years`
}

function whenLabel(daysUntil: number) {
  if (daysUntil === 0) return 'Today'
  if (daysUntil === 1) return 'Tomorrow'
  return `In ${daysUntil} days`
}

/**
 * Six months for newcomers, then every full year, in the week leading up to each. The caller
 * hides the card when nobody has one coming.
 */
export function TenureMilestonesCard({
  milestones,
  onOpenPerson
}: {
  milestones: TenureMilestone[]
  onOpenPerson: (directReportId: string) => void
}) {
  return (
    <Card className="h-full flex flex-col">
      <CardHeader title="Milestones" subtitle="Time at the company, this week" />
      <ul className="flex-1 divide-y divide-slate-100">
        {milestones.map(m => (
          <li key={m.directReportId}>
            <button
              type="button"
              onClick={() => onOpenPerson(m.directReportId)}
              className="w-full px-6 py-2.5 flex items-center gap-3 text-left hover:bg-slate-50"
            >
              <PartyPopper className="w-4 h-4 text-amber-500 flex-shrink-0" />
              <span className="min-w-0 flex-1">
                <span className="block text-sm font-medium text-slate-900 truncate">{m.fullName}</span>
                <span className="block text-xs text-slate-500">
                  {milestoneLabel(m)} on {formatDate(m.date)} · joined {formatDate(m.hireDate)}
                </span>
              </span>
              <span className={`text-xs flex-shrink-0 ${m.daysUntil === 0 ? 'font-semibold text-amber-600' : 'text-slate-500'}`}>
                {whenLabel(m.daysUntil)}
              </span>
            </button>
          </li>
        ))}
      </ul>
    </Card>
  )
}
