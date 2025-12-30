import { useState, useEffect } from 'react'
import {
  Calendar,
  Plus,
  Check,
  X,
  Clock,
  User,
  Filter,
  Palmtree,
  Thermometer,
  Coffee
} from 'lucide-react'
import { leavesApi, directReportsApi } from '../services/api'
import type { Leave, DirectReport, CreateLeaveDto, TeamLeaveOverview, MonthlyLeaveSummary } from '../types'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from 'recharts'

const leaveTypes = [
  'PTO',
  'Vacation',
  'SickLeave',
  'PersonalLeave',
  'FamilyLeave',
  'BereavementLeave',
  'JuryDuty',
  'PublicHoliday',
  'Unpaid',
  'Other'
]

const leaveTypeLabels: Record<string, string> = {
  PTO: 'PTO',
  Vacation: 'Vacation',
  SickLeave: 'Sick Leave',
  PersonalLeave: 'Personal Leave',
  FamilyLeave: 'Family Leave',
  BereavementLeave: 'Bereavement',
  JuryDuty: 'Jury Duty',
  PublicHoliday: 'Public Holiday',
  Unpaid: 'Unpaid Leave',
  Other: 'Other'
}

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-400',
  Approved: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400',
  Rejected: 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-400',
  Cancelled: 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
}

const typeIcons: Record<string, typeof Palmtree> = {
  Vacation: Palmtree,
  SickLeave: Thermometer,
  PTO: Coffee
}

export default function Leaves() {
  const [leaves, setLeaves] = useState<Leave[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [overview, setOverview] = useState<TeamLeaveOverview | null>(null)
  const [monthlyTrend, setMonthlyTrend] = useState<MonthlyLeaveSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [formData, setFormData] = useState<CreateLeaveDto>({
    directReportId: '',
    type: 'PTO',
    startDate: '',
    endDate: '',
    reason: ''
  })

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [leavesData, drData, overviewData, trendData] = await Promise.all([
        leavesApi.getAll(),
        directReportsApi.getAll(),
        leavesApi.getOverview(),
        leavesApi.getMonthlyTrend(6)
      ])
      setLeaves(leavesData)
      setDirectReports(drData)
      setOverview(overviewData)
      setMonthlyTrend(trendData)
    } catch (error) {
      console.error('Failed to load leaves:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await leavesApi.create(formData)
      setShowForm(false)
      setFormData({ directReportId: '', type: 'PTO', startDate: '', endDate: '', reason: '' })
      loadData()
    } catch (error) {
      console.error('Failed to create leave:', error)
    }
  }

  const handleApprove = async (id: string) => {
    try {
      await leavesApi.approve(id, 'Manager')
      loadData()
    } catch (error) {
      console.error('Failed to approve leave:', error)
    }
  }

  const handleReject = async (id: string) => {
    try {
      await leavesApi.reject(id)
      loadData()
    } catch (error) {
      console.error('Failed to reject leave:', error)
    }
  }

  const handleCancel = async (id: string) => {
    try {
      await leavesApi.cancel(id)
      loadData()
    } catch (error) {
      console.error('Failed to cancel leave:', error)
    }
  }

  const filteredLeaves = statusFilter === 'all'
    ? leaves
    : leaves.filter(l => l.status === statusFilter)

  const formatDate = (date: string) => new Date(date).toLocaleDateString()

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
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Leave Management</h1>
          <p className="text-slate-500 dark:text-slate-400">Track team PTO, vacation, and sick leave</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Request Leave
        </button>
      </div>

      {/* Overview Cards */}
      {overview && (
        <div className="grid grid-cols-4 gap-4">
          <div className="bg-white dark:bg-slate-800 rounded-xl p-4 shadow-sm border border-slate-200 dark:border-slate-700">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                <Calendar className="w-5 h-5 text-blue-600 dark:text-blue-400" />
              </div>
              <div>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Requests</p>
                <p className="text-xl font-bold text-slate-900 dark:text-slate-100">{overview.totalLeaveRequests}</p>
              </div>
            </div>
          </div>
          <div className="bg-white dark:bg-slate-800 rounded-xl p-4 shadow-sm border border-slate-200 dark:border-slate-700">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-yellow-100 dark:bg-yellow-900/30 rounded-lg">
                <Clock className="w-5 h-5 text-yellow-600 dark:text-yellow-400" />
              </div>
              <div>
                <p className="text-sm text-slate-500 dark:text-slate-400">Pending</p>
                <p className="text-xl font-bold text-slate-900 dark:text-slate-100">{overview.pendingRequests}</p>
              </div>
            </div>
          </div>
          <div className="bg-white dark:bg-slate-800 rounded-xl p-4 shadow-sm border border-slate-200 dark:border-slate-700">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
                <Check className="w-5 h-5 text-green-600 dark:text-green-400" />
              </div>
              <div>
                <p className="text-sm text-slate-500 dark:text-slate-400">Approved</p>
                <p className="text-xl font-bold text-slate-900 dark:text-slate-100">{overview.approvedRequests}</p>
              </div>
            </div>
          </div>
          <div className="bg-white dark:bg-slate-800 rounded-xl p-4 shadow-sm border border-slate-200 dark:border-slate-700">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
                <User className="w-5 h-5 text-purple-600 dark:text-purple-400" />
              </div>
              <div>
                <p className="text-sm text-slate-500 dark:text-slate-400">On Leave Today</p>
                <p className="text-xl font-bold text-slate-900 dark:text-slate-100">{overview.teamMembersOnLeaveToday}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Monthly Trend Chart */}
      {monthlyTrend.length > 0 && (
        <div className="bg-white dark:bg-slate-800 rounded-xl p-6 shadow-sm border border-slate-200 dark:border-slate-700">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">Leave Days by Month</h2>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={monthlyTrend}>
              <XAxis dataKey="monthName" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Bar dataKey="totalBusinessDays" name="Business Days" fill="#f59e0b" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Filter */}
      <div className="flex items-center gap-2">
        <Filter className="w-5 h-5 text-slate-400" />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2 text-sm"
        >
          <option value="all">All Status</option>
          <option value="Pending">Pending</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </div>

      {/* Leave List */}
      <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-200 dark:border-slate-700 overflow-hidden">
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-700/50 border-b border-slate-200 dark:border-slate-700">
            <tr>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Team Member</th>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Type</th>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Dates</th>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Days</th>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Status</th>
              <th className="text-left px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Reason</th>
              <th className="text-right px-6 py-3 text-sm font-medium text-slate-500 dark:text-slate-400">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
            {filteredLeaves.map((leave) => {
              const TypeIcon = typeIcons[leave.type] || Calendar
              return (
                <tr key={leave.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/50">
                  <td className="px-6 py-4">
                    <div className="font-medium text-slate-900 dark:text-slate-100">{leave.directReportName}</div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <TypeIcon className="w-4 h-4 text-slate-400" />
                      <span className="text-slate-700 dark:text-slate-300">{leaveTypeLabels[leave.type] || leave.type}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-slate-600 dark:text-slate-400">
                    {formatDate(leave.startDate)} - {formatDate(leave.endDate)}
                  </td>
                  <td className="px-6 py-4 text-slate-600 dark:text-slate-400">
                    {leave.businessDaysCount} days
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${statusColors[leave.status]}`}>
                      {leave.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-slate-600 dark:text-slate-400 max-w-xs truncate">
                    {leave.reason || '-'}
                  </td>
                  <td className="px-6 py-4 text-right">
                    {leave.status === 'Pending' && (
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleApprove(leave.id)}
                          className="p-1 text-green-600 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20 rounded"
                          title="Approve"
                        >
                          <Check className="w-5 h-5" />
                        </button>
                        <button
                          onClick={() => handleReject(leave.id)}
                          className="p-1 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded"
                          title="Reject"
                        >
                          <X className="w-5 h-5" />
                        </button>
                      </div>
                    )}
                    {leave.status === 'Approved' && (
                      <button
                        onClick={() => handleCancel(leave.id)}
                        className="text-sm text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
                      >
                        Cancel
                      </button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
        {filteredLeaves.length === 0 && (
          <div className="text-center py-12 text-slate-500 dark:text-slate-400">
            No leave requests found
          </div>
        )}
      </div>

      {/* Create Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-xl p-6 w-full max-w-md">
            <h2 className="text-xl font-bold text-slate-900 dark:text-slate-100 mb-4">Request Leave</h2>
            <form onSubmit={handleCreate} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Team Member
                </label>
                <select
                  value={formData.directReportId}
                  onChange={(e) => setFormData({ ...formData, directReportId: e.target.value })}
                  className="w-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2"
                  required
                >
                  <option value="">Select team member</option>
                  {directReports.map((dr) => (
                    <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Leave Type
                </label>
                <select
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                  className="w-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2"
                  required
                >
                  {leaveTypes.map((type) => (
                    <option key={type} value={type}>{leaveTypeLabels[type]}</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Start Date
                  </label>
                  <input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    End Date
                  </label>
                  <input
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2"
                    required
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Reason (optional)
                </label>
                <textarea
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  className="w-full border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 rounded-lg px-3 py-2"
                  rows={3}
                />
              </div>
              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowForm(false)}
                  className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                >
                  Submit Request
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
