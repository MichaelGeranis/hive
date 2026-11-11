import { useState, useEffect, useCallback } from 'react'
import { formatDate, formatMonthYear, formatWeekday } from '../utils/dateUtils'
import {
  Calendar as CalendarIcon,
  Plus,
  User,
  Palmtree,
  Thermometer,
  Briefcase,
  Users,
  ChevronLeft,
  ChevronRight,
  Trash2,
  Globe
} from 'lucide-react'
import { leavesApi, directReportsApi } from '../services/api'
import type { Leave, DirectReport, CreateLeaveDto, UpdateLeaveDto, TeamLeaveOverview, CreatePublicHolidayLeaveDto } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { LoadingState } from '../components/PageState'
import { LineChart, Line, XAxis, YAxis, Tooltip as RechartsTooltip, ResponsiveContainer, CartesianGrid } from 'recharts'

const leaveTypes = ['Vacation', 'Sick', 'Other', 'PublicHoliday']

const leaveTypeLabels: Record<string, string> = {
  Vacation: 'Vacation',
  Sick: 'Sick Leave',
  Other: 'Other',
  PublicHoliday: 'Public Holiday'
}

const typeIcons: Record<string, typeof Palmtree> = {
  Vacation: Palmtree,
  Sick: Thermometer,
  Other: Briefcase,
  PublicHoliday: Globe
}

const typeColors: Record<string, string> = {
  Vacation: 'bg-green-500',
  Sick: 'bg-orange-500',
  Other: 'bg-blue-500',
  PublicHoliday: 'bg-purple-500'
}

interface DayHoverState {
  date: Date | null
  timeoutId: number | null
}

export default function Leaves() {
  const { showError, showSuccess } = useToast()
  const confirm = useConfirm()
  const [leaves, setLeaves] = useState<Leave[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [overview, setOverview] = useState<TeamLeaveOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingLeave, setEditingLeave] = useState<Leave | null>(null)
  const [currentMonth, setCurrentMonth] = useState(new Date())
  const [hoveredDay, setHoveredDay] = useState<DayHoverState>({ date: null, timeoutId: null })
  const [tooltipPosition, setTooltipPosition] = useState({ x: 0, y: 0 })
  const [formData, setFormData] = useState<CreateLeaveDto & { name?: string }>({
    directReportId: '',
    type: 'Vacation',
    startDate: '',
    endDate: '',
    notes: '',
    name: ''
  })

  const resetForm = useCallback(() => {
    setFormData({
      directReportId: '',
      type: 'Vacation',
      startDate: '',
      endDate: '',
      notes: '',
      name: ''
    })
    setEditingLeave(null)
  }, [])

  const closeModal = useCallback(() => {
    setShowForm(false)
    resetForm()
  }, [resetForm])

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [leavesData, drData, overviewData] = await Promise.all([
        leavesApi.getAll(),
        directReportsApi.getAll(),
        leavesApi.getOverview()
      ])
      setLeaves(leavesData)
      setDirectReports(drData)
      setOverview(overviewData)
    } catch (error) {
      console.error('Failed to load leaves:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      // A public holiday belongs to nobody in particular: it goes to its own endpoint,
      // which records it for every team member
      if (formData.type === 'PublicHoliday') {
        const publicHoliday: CreatePublicHolidayLeaveDto = {
          startDate: formData.startDate,
          endDate: formData.endDate,
          name: formData.name || 'Public Holiday',
          notes: formData.notes
        }
        const result = await leavesApi.createPublicHoliday(publicHoliday)
        let message = `Public holiday added for ${result.totalCreated} team member${result.totalCreated === 1 ? '' : 's'}`
        if (result.skippedMembers.length > 0) {
          message += `. Skipped ${result.skippedMembers.length} with leave already booked: ${result.skippedMembers.join(', ')}`
        }
        showSuccess(message)
      } else {
        await leavesApi.create(formData)
      }
      closeModal()
      loadData()
    } catch (error) {
      console.error('Failed to create leave:', error)
      showError(getErrorMessage(error))
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingLeave) return
    try {
      const updateData: UpdateLeaveDto = {
        type: formData.type,
        startDate: formData.startDate,
        endDate: formData.endDate,
        notes: formData.notes
      }
      await leavesApi.update(editingLeave.id, updateData)
      closeModal()
      loadData()
    } catch (error) {
      console.error('Failed to update leave:', error)
      showError(getErrorMessage(error))
    }
  }

  const handleDelete = async (id: string) => {
    const confirmed = await confirm({
      title: 'Delete this leave?',
      message: 'It is removed from the calendar and from capacity.',
      confirmLabel: 'Delete'
    })
    if (!confirmed) return
    try {
      await leavesApi.delete(id)
      closeModal()
      loadData()
    } catch (error) {
      console.error('Failed to delete leave:', error)
      showError(getErrorMessage(error))
    }
  }

  const openEditForm = (leave: Leave) => {
    setEditingLeave(leave)
    setFormData({
      directReportId: leave.directReportId,
      type: leave.type,
      startDate: leave.startDate.split('T')[0],
      endDate: leave.endDate.split('T')[0],
      notes: leave.notes || ''
    })
    setShowForm(true)
  }

  // Calendar functions
  const getDaysInMonth = (date: Date) => {
    const year = date.getFullYear()
    const month = date.getMonth()
    return new Date(year, month + 1, 0).getDate()
  }

  const getFirstDayOfMonth = (date: Date) => {
    return new Date(date.getFullYear(), date.getMonth(), 1).getDay()
  }

  const getMonthLabel = (date: Date) => {
    return formatMonthYear(date)
  }

  const isDateInRange = (date: Date, start: string, end: string) => {
    const checkDate = new Date(date.getFullYear(), date.getMonth(), date.getDate())
    const startDate = new Date(start)
    const endDate = new Date(end)
    return checkDate >= startDate && checkDate <= endDate
  }

  const getLeavesForDate = (date: Date) => {
    return leaves.filter(leave =>
      isDateInRange(date, leave.startDate, leave.endDate)
    )
  }

  const isToday = (date: Date) => {
    const today = new Date()
    return date.getDate() === today.getDate() &&
           date.getMonth() === today.getMonth() &&
           date.getFullYear() === today.getFullYear()
  }

  const isWeekend = (date: Date) => {
    const day = date.getDay()
    return day === 0 || day === 6
  }

  const nextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1))
  }

  const prevMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1))
  }

  const goToToday = () => {
    setCurrentMonth(new Date())
  }

  // Hover functionality
  const handleDayMouseEnter = (date: Date, event: React.MouseEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect()
    setTooltipPosition({
      x: rect.left + rect.width / 2,
      y: rect.top
    })

    const timeoutId = window.setTimeout(() => {
      setHoveredDay({ date, timeoutId: null })
    }, 1000)

    setHoveredDay({ date: null, timeoutId })
  }

  const handleDayMouseLeave = () => {
    if (hoveredDay.timeoutId) {
      clearTimeout(hoveredDay.timeoutId)
    }
    setHoveredDay({ date: null, timeoutId: null })
  }

  // Generate chart data for the current month
  const generateChartData = () => {
    const daysInMonth = getDaysInMonth(currentMonth)
    const data = []

    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day)
      const dayLeaves = getLeavesForDate(date)
      data.push({
        day: day.toString(),
        people: dayLeaves.length,
        date: formatDate(date)
      })
    }

    return data
  }

  const chartData = generateChartData()

  // Render calendar for current month
  const renderCalendar = () => {
    const daysInMonth = getDaysInMonth(currentMonth)
    const firstDay = getFirstDayOfMonth(currentMonth)
    const days = []

    // Empty cells for days before month starts
    for (let i = 0; i < firstDay; i++) {
      days.push(<div key={`empty-${i}`} className="h-28 bg-slate-100" />)
    }

    // Actual days
    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day)
      const dayLeaves = getLeavesForDate(date)
      const isCurrentDay = isToday(date)
      const isWeekendDay = isWeekend(date)

      days.push(
        <div
          key={day}
          className={`h-28 border border-slate-200 p-1.5 overflow-hidden relative ${
            isCurrentDay ? 'bg-amber-50 border-amber-500' :
            isWeekendDay ? 'bg-slate-50' : 'bg-white'
          }`}
          onMouseEnter={(e) => handleDayMouseEnter(date, e)}
          onMouseLeave={handleDayMouseLeave}
        >
          <div className={`text-sm font-semibold mb-1 ${
            isCurrentDay ? 'text-amber-600' :
            isWeekendDay ? 'text-slate-400' : 'text-slate-600'
          }`}>
            {day}
          </div>
          <div className="space-y-0.5">
            {dayLeaves.slice(0, 4).map(leave => {
              const TypeIcon = typeIcons[leave.type]
              return (
                <div
                  key={leave.id}
                  className={`text-[10px] ${typeColors[leave.type]} text-white rounded px-1 py-0.5 truncate cursor-pointer hover:opacity-80`}
                  title={`${leave.directReportName} - ${leave.type}`}
                  onClick={() => openEditForm(leave)}
                >
                  <div className="flex items-center gap-0.5">
                    <TypeIcon className="w-2.5 h-2.5 flex-shrink-0" />
                    <span className="truncate">{leave.directReportName.split(' ')[0]}</span>
                  </div>
                </div>
              )
            })}
            {dayLeaves.length > 4 && (
              <div className="text-[10px] text-slate-500 px-1 font-medium">
                +{dayLeaves.length - 4} more
              </div>
            )}
          </div>
        </div>
      )
    }

    return days
  }

  if (loading) {
    return <LoadingState fill />
  }

  const hoveredDayLeaves = hoveredDay.date ? getLeavesForDate(hoveredDay.date) : []

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Leave Calendar"
        subtitle="Visual leave tracking and capacity planning"
        actions={
          <button
            onClick={() => setShowForm(true)}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            <Plus className="w-5 h-5" />
            Add Leave
          </button>
        }
      />

      {/* Overview Cards */}
      {overview && (
        <div className="grid grid-cols-4 gap-4">
          <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <CalendarIcon className="w-5 h-5 text-green-600" />
              </div>
              <div>
                <p className="text-sm text-slate-500">Total Records</p>
                <p className="text-xl font-bold text-slate-900">{overview.totalLeaveRecords}</p>
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-amber-100 rounded-lg">
                <User className="w-5 h-5 text-amber-600" />
              </div>
              <div>
                <p className="text-sm text-slate-500">On Leave Today</p>
                <p className="text-xl font-bold text-slate-900">{overview.teamMembersOnLeaveToday}</p>
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Users className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <p className="text-sm text-slate-500">This Week</p>
                <p className="text-xl font-bold text-slate-900">{overview.teamMembersOnLeaveThisWeek}</p>
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <Palmtree className="w-5 h-5 text-purple-600" />
              </div>
              <div>
                <p className="text-sm text-slate-500">Upcoming</p>
                <p className="text-xl font-bold text-slate-900">{overview.upcomingLeaves.length}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Leave Trend Chart */}
      <div className="bg-white rounded-xl p-6 shadow-sm border border-slate-200">
        <h2 className="text-lg font-semibold text-slate-900 mb-4">
          People on Leave per Day - {getMonthLabel(currentMonth)}
        </h2>
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-slate-200" />
            <XAxis
              dataKey="day"
              className="text-xs"
              tick={{ fill: 'currentColor', className: 'text-slate-600' }}
            />
            <YAxis
              className="text-xs"
              tick={{ fill: 'currentColor', className: 'text-slate-600' }}
              allowDecimals={false}
            />
            <RechartsTooltip
              contentStyle={{
                backgroundColor: 'rgb(30 41 59)',
                border: '1px solid rgb(51 65 85)',
                borderRadius: '0.5rem',
                color: 'white'
              }}
              labelFormatter={(value) => {
                const day = parseInt(value)
                const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day)
                return `${formatWeekday(date, 'short')} ${formatDate(date)}`
              }}
            />
            <Line
              type="monotone"
              dataKey="people"
              stroke="#f59e0b"
              strokeWidth={2}
              dot={{ fill: '#f59e0b', r: 4 }}
              activeDot={{ r: 6 }}
              name="People on Leave"
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Calendar Navigation */}
      <div className="flex items-center justify-between bg-white rounded-xl p-4 shadow-sm border border-slate-200">
        <button
          onClick={prevMonth}
          className="p-2 hover:bg-slate-100 rounded-lg transition-colors"
        >
          <ChevronLeft className="w-5 h-5 text-slate-600" />
        </button>
        <div className="flex items-center gap-3">
          <h2 className="text-xl font-semibold text-slate-900">
            {getMonthLabel(currentMonth)}
          </h2>
          <button
            onClick={goToToday}
            className="px-3 py-1.5 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            Today
          </button>
        </div>
        <button
          onClick={nextMonth}
          className="p-2 hover:bg-slate-100 rounded-lg transition-colors"
        >
          <ChevronRight className="w-5 h-5 text-slate-600" />
        </button>
      </div>

      {/* Calendar Grid */}
      <div className="bg-white rounded-xl p-6 shadow-sm border border-slate-200">
        <div className="grid grid-cols-7 gap-px bg-slate-300 rounded-lg overflow-hidden">
          {['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].map(day => (
            <div key={day} className="bg-slate-100 px-2 py-3 text-center text-sm font-semibold text-slate-600">
              {day}
            </div>
          ))}
          {renderCalendar()}
        </div>
      </div>

      {/* Hover Tooltip */}
      {hoveredDay.date && hoveredDayLeaves.length > 0 && (
        <div
          className="fixed z-50 pointer-events-none"
          style={{
            left: `${tooltipPosition.x}px`,
            top: `${tooltipPosition.y - 10}px`,
            transform: 'translate(-50%, -100%)'
          }}
        >
          <div className="bg-slate-900 text-white rounded-lg shadow-2xl border border-slate-700 p-4 max-w-sm">
            <div className="text-sm font-semibold mb-2 text-amber-400">
              {formatWeekday(hoveredDay.date)} {formatDate(hoveredDay.date)}
            </div>
            <div className="space-y-2">
              {hoveredDayLeaves.map(leave => {
                const TypeIcon = typeIcons[leave.type]
                return (
                  <div key={leave.id} className="flex items-start gap-2 text-sm">
                    <TypeIcon className="w-4 h-4 mt-0.5 flex-shrink-0 text-slate-400" />
                    <div className="flex-1 min-w-0">
                      <div className="font-medium">{leave.directReportName}</div>
                      <div className="text-xs text-slate-400">{leaveTypeLabels[leave.type]}</div>
                      {leave.notes && (
                        <div className="text-xs text-slate-500 mt-1 italic">{leave.notes}</div>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
            <div className="mt-3 pt-2 border-t border-slate-700 text-xs text-slate-400">
              {hoveredDayLeaves.length} {hoveredDayLeaves.length === 1 ? 'person' : 'people'} on leave
            </div>
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-200">
        <h3 className="text-sm font-semibold text-slate-900 mb-3">Legend</h3>
        <div className="flex gap-6 flex-wrap">
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-green-500 rounded"></div>
            <span className="text-sm text-slate-600">Vacation</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-orange-500 rounded"></div>
            <span className="text-sm text-slate-600">Sick Leave</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-blue-500 rounded"></div>
            <span className="text-sm text-slate-600">Other</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-purple-500 rounded"></div>
            <span className="text-sm text-slate-600">Public Holiday</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 border-2 border-amber-500 bg-amber-50 rounded"></div>
            <span className="text-sm text-slate-600">Today</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-sm text-slate-500 italic">💡 Hover over a day for 1 second to see all leaves</span>
          </div>
        </div>
      </div>

      {/* Create/Edit Form Modal */}
      {showForm && (
        <Modal title={editingLeave ? 'Edit Leave' : 'Add Leave'} onClose={closeModal} size="sm" closeOnBackdrop>
          <form onSubmit={editingLeave ? handleUpdate : handleCreate} className="space-y-4">
            {!editingLeave && formData.type !== 'PublicHoliday' && (
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Team Member
                </label>
                <select
                  value={formData.directReportId}
                  onChange={(e) => setFormData({ ...formData, directReportId: e.target.value })}
                  className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                  required
                >
                  <option value="">Select team member</option>
                  {directReports.map((dr) => (
                    <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                  ))}
                </select>
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Leave Type
              </label>
              <select
                value={formData.type}
                onChange={(e) => {
                  const newType = e.target.value
                  setFormData({
                    ...formData,
                    type: newType,
                    directReportId: newType === 'PublicHoliday' ? '' : formData.directReportId
                  })
                }}
                className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                required
              >
                {leaveTypes.map((type) => (
                  <option key={type} value={type}>{leaveTypeLabels[type]}</option>
                ))}
              </select>
            </div>
            {!editingLeave && formData.type === 'PublicHoliday' && (
              <>
                <div className="bg-purple-50 border border-purple-200 rounded-lg p-3">
                  <p className="text-sm text-purple-800">
                    This leave will be created for all active team members.
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">
                    Holiday Name
                  </label>
                  <input
                    type="text"
                    value={formData.name || ''}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                    placeholder="e.g., Christmas Day, New Year's Day"
                    required
                  />
                </div>
              </>
            )}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Start Date
                </label>
                <input
                  type="date"
                  value={formData.startDate}
                  onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                  className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  End Date
                </label>
                <input
                  type="date"
                  value={formData.endDate}
                  onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                  className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                  required
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Notes (optional)
              </label>
              <textarea
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                className="w-full border border-slate-300 bg-white text-slate-900 rounded-lg px-3 py-2"
                rows={3}
                placeholder="Optional notes about the leave"
              />
            </div>
            <div className="flex gap-3 pt-4">
              {editingLeave && (
                <button
                  type="button"
                  onClick={() => handleDelete(editingLeave.id)}
                  className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 flex items-center gap-2"
                >
                  <Trash2 className="w-4 h-4" />
                  Delete
                </button>
              )}
              <div className="flex-1" />
              <button
                type="button"
                onClick={closeModal}
                className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
              >
                {editingLeave ? 'Save Changes' : 'Add Leave'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
