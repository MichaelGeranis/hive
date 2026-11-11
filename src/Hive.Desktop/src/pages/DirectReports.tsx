import { useEffect, useState, useCallback, useMemo } from 'react'
import { Plus, Mail, Building2, Calendar, MoreVertical, Trash2, Edit, Search, Upload, FileText, CheckCircle, XCircle, AlertCircle, Download, Users, UserMinus, X, Filter } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import { directReportsApi } from '../services/api'
import type { DirectReport, CreateDirectReportDto, BulkImportResultDto } from '../types'
import { useToast } from '../contexts/ToastContext'
import { apiErrorMessage, getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, LoadingState } from '../components/PageState'

export default function DirectReports() {
  const { showError } = useToast()
  const confirm = useConfirm()
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState<CreateDirectReportDto>({
    firstName: '',
    lastName: '',
    email: '',
    jobTitle: '',
    department: '',
    hireDate: new Date().toISOString().split('T')[0],
    isDirect: true
  })

  // Bulk import state
  const [showImport, setShowImport] = useState(false)
  const [csvContent, setCsvContent] = useState('')
  const [skipDuplicates, setSkipDuplicates] = useState(true)
  const [importLoading, setImportLoading] = useState(false)
  const [importResult, setImportResult] = useState<BulkImportResultDto | null>(null)
  const [importError, setImportError] = useState<string | null>(null)

  const resetForm = useCallback(() => {
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      jobTitle: '',
      department: '',
      hireDate: new Date().toISOString().split('T')[0],
      isDirect: true
    })
  }, [])

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [resetForm])

  const closeImportModal = useCallback(() => {
    setShowImport(false)
    setCsvContent('')
    setImportResult(null)
    setImportError(null)
  }, [])

  useEffect(() => {
    loadDirectReports()
  }, [])

  const loadDirectReports = async () => {
    try {
      setLoading(true)
      const data = await directReportsApi.getAll()
      setDirectReports(data)
    } catch (err) {
      setError('Failed to load team members')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editingId) {
        await directReportsApi.update(editingId, formData)
      } else {
        await directReportsApi.create(formData)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadDirectReports()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (dr: DirectReport) => {
    setFormData({
      firstName: dr.firstName,
      lastName: dr.lastName,
      email: dr.email,
      jobTitle: dr.jobTitle,
      department: dr.department,
      hireDate: dr.hireDate.split('T')[0],
      isDirect: dr.isDirect
    })
    setEditingId(dr.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    const confirmed = await confirm({
      title: 'Remove this team member?',
      message: 'They leave the Team page, with their 1:1s, leave and career plan.',
      confirmLabel: 'Remove'
    })
    if (!confirmed) return
    try {
      await directReportsApi.delete(id)
      loadDirectReports()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  // Bulk import handlers
  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (e) => {
      const content = e.target?.result as string
      setCsvContent(content)
      setImportResult(null)
      setImportError(null)
    }
    reader.onerror = () => {
      setImportError('Failed to read file')
    }
    reader.readAsText(file)
  }

  const handleImport = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    try {
      setImportLoading(true)
      setImportError(null)
      const result = await directReportsApi.bulkImport({
        csvContent,
        skipDuplicates
      })
      setImportResult(result)
      if (result.successCount > 0) {
        loadDirectReports()
      }
    } catch (err) {
      console.error('Failed to import', err)
      setImportError(apiErrorMessage(err) || (err instanceof Error ? err.message : '') || 'Failed to import CSV')
    } finally {
      setImportLoading(false)
    }
  }

  const downloadSampleCsv = () => {
    const sample = `FirstName,LastName,Email,JobTitle,Department,HireDate,IsDirect
John,Doe,john.doe@example.com,Software Engineer,Engineering,2023-01-15,true
Jane,Smith,jane.smith@example.com,Product Manager,Product,2022-06-01,true
Bob,Johnson,bob.johnson@example.com,Designer,Design,2024-03-10,false`

    const blob = new Blob([sample], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'team_members_template.csv'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }

  const calculateTenure = (hireDate: string) => {
    const hire = new Date(hireDate)
    const now = new Date()
    const months = (now.getFullYear() - hire.getFullYear()) * 12 + (now.getMonth() - hire.getMonth())
    if (months < 12) return `${months} months`
    const years = Math.floor(months / 12)
    const remainingMonths = months % 12
    return remainingMonths > 0 ? `${years}y ${remainingMonths}m` : `${years} years`
  }

  const [selectedDepartment, setSelectedDepartment] = useState<string | null>(null)
  const [reportFilter, setReportFilter] = useState<'all' | 'direct' | 'indirect'>('all')

  // Get all unique departments
  const allDepartments = useMemo(() => {
    const deptSet = new Set<string>()
    directReports.forEach(dr => {
      if (dr.department) deptSet.add(dr.department.trim())
    })
    return Array.from(deptSet).sort()
  }, [directReports])

  // Calculate report type counts
  const reportCounts = useMemo(() => ({
    all: directReports.length,
    direct: directReports.filter(dr => dr.isDirect).length,
    indirect: directReports.filter(dr => !dr.isDirect).length,
  }), [directReports])

  const clearFilters = () => {
    setSearchInput('')
    setSearchQuery('')
    setSelectedDepartment(null)
    setReportFilter('all')
  }

  const filteredDirectReports = () => {
    let result = directReports

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(dr =>
        dr.fullName.toLowerCase().includes(query) ||
        dr.firstName.toLowerCase().includes(query) ||
        dr.lastName.toLowerCase().includes(query) ||
        dr.email.toLowerCase().includes(query) ||
        dr.jobTitle?.toLowerCase().includes(query) ||
        dr.department?.toLowerCase().includes(query)
      )
    }

    // Apply department filter
    if (selectedDepartment) {
      result = result.filter(dr => dr.department?.trim() === selectedDepartment)
    }

    // Apply direct/indirect filter
    if (reportFilter === 'direct') {
      result = result.filter(dr => dr.isDirect)
    } else if (reportFilter === 'indirect') {
      result = result.filter(dr => !dr.isDirect)
    }

    return result
  }

  if (loading) {
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Team"
        subtitle="Manage direct reports"
        actions={
          <>
            <button
              onClick={() => setShowImport(true)}
              className="flex items-center gap-2 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
            >
              <Upload className="w-5 h-5" />
              Import CSV
            </button>
            <button
              onClick={() => {
                resetForm()
                setEditingId(null)
                setShowForm(true)
              }}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
            >
              <Plus className="w-5 h-5" />
              Add Team Member
            </button>
          </>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
          {error}
        </div>
      )}

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search team members... (press Enter)"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
          />
          {(searchInput || selectedDepartment || reportFilter !== 'all') && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Departments */}
        {allDepartments.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Building2 className="w-4 h-4 text-slate-400" />
            {allDepartments.map((dept) => (
              <button
                key={dept}
                onClick={() => setSelectedDepartment(selectedDepartment === dept ? null : dept)}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedDepartment === dept
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                }`}
              >
                {dept}
              </button>
            ))}
          </div>
        )}

        {/* Report Type Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <div className="flex gap-2 flex-wrap">
            {([
              { value: 'all', label: `All (${reportCounts.all})`, count: reportCounts.all },
              { value: 'direct', label: `Direct Reports (${reportCounts.direct})`, count: reportCounts.direct },
              { value: 'indirect', label: `Indirect Reports (${reportCounts.indirect})`, count: reportCounts.indirect },
            ] as const)
              .filter((f) => f.value === 'all' || f.count > 0)
              .map((f) => (
              <button
                key={f.value}
                onClick={() => setReportFilter(f.value)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  reportFilter === f.value
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                }`}
              >
                {f.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Add/Edit Form Modal */}
      {showForm && (
        <Modal title={editingId ? 'Edit Team Member' : 'Add Team Member'} onClose={closeModal}>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  First Name
                </label>
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Last Name
                </label>
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                  required
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Email
              </label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Job Title
                </label>
                <input
                  type="text"
                  value={formData.jobTitle}
                  onChange={(e) => setFormData({ ...formData, jobTitle: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Department
                </label>
                <input
                  type="text"
                  value={formData.department}
                  onChange={(e) => setFormData({ ...formData, department: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Hire Date
              </label>
              <input
                type="date"
                value={formData.hireDate}
                onChange={(e) => setFormData({ ...formData, hireDate: e.target.value })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                required
              />
            </div>
            <div className="flex items-center gap-3 py-2">
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.isDirect}
                  onChange={(e) => setFormData({ ...formData, isDirect: e.target.checked })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-amber-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-amber-500"></div>
                <span className="ml-3 text-sm font-medium text-slate-700">
                  {formData.isDirect ? 'Direct Report' : 'Indirect Report'}
                </span>
              </label>
            </div>
            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
              >
                {editingId ? 'Update' : 'Add'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Bulk Import Modal */}
      {showImport && (
        <Modal title="Import Team Members from CSV" onClose={closeImportModal} size="lg">
          <div className="space-y-4">
            {/* Instructions */}
            <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
              <h4 className="text-sm font-medium text-blue-900 mb-2">CSV Format</h4>
              <p className="text-sm text-blue-700 mb-2">
                Required columns: <code className="bg-blue-100 px-1 rounded">FirstName</code>, <code className="bg-blue-100 px-1 rounded">LastName</code>, <code className="bg-blue-100 px-1 rounded">Email</code>
              </p>
              <p className="text-sm text-blue-700">
                Optional columns: <code className="bg-blue-100 px-1 rounded">JobTitle</code>, <code className="bg-blue-100 px-1 rounded">Department</code>, <code className="bg-blue-100 px-1 rounded">HireDate</code>, <code className="bg-blue-100 px-1 rounded">IsDirect</code> (true/false)
              </p>
              <button
                onClick={downloadSampleCsv}
                className="mt-3 flex items-center gap-2 text-sm text-blue-600 hover:underline"
              >
                <Download className="w-4 h-4" />
                Download sample CSV template
              </button>
            </div>

            {/* File Upload */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Select CSV File
              </label>
              <input
                type="file"
                accept=".csv"
                onChange={handleFileChange}
                className="block w-full text-sm text-slate-500
                  file:mr-4 file:py-2 file:px-4
                  file:rounded-lg file:border-0
                  file:text-sm file:font-semibold
                  file:bg-amber-50 file:text-amber-700
                  hover:file:bg-amber-100
                  cursor-pointer"
                disabled={importLoading}
              />
            </div>

            {/* Options */}
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="skipDuplicates"
                checked={skipDuplicates}
                onChange={(e) => setSkipDuplicates(e.target.checked)}
                className="w-4 h-4 text-amber-500 bg-white border-slate-300 rounded focus:ring-amber-500"
                disabled={importLoading}
              />
              <label htmlFor="skipDuplicates" className="text-sm text-slate-700">
                Skip duplicate emails (otherwise report as error)
              </label>
            </div>

            {/* Error Message */}
            {importError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                {importError}
              </div>
            )}

            {/* Import Result */}
            {importResult && (
              <div className="space-y-4 pt-4 border-t">
                <h3 className="text-sm font-medium text-slate-700">Import Results</h3>

                <div className="grid grid-cols-4 gap-4">
                  <div className="p-4 bg-slate-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-slate-900">{importResult.totalRows}</div>
                    <div className="text-sm text-slate-500">Total</div>
                  </div>
                  <div className="p-4 bg-green-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-green-700">{importResult.successCount}</div>
                    <div className="text-sm text-green-600">Created</div>
                  </div>
                  <div className="p-4 bg-yellow-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-yellow-700">{importResult.skippedCount}</div>
                    <div className="text-sm text-yellow-600">Skipped</div>
                  </div>
                  <div className="p-4 bg-red-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-red-700">{importResult.errorCount}</div>
                    <div className="text-sm text-red-600">Errors</div>
                  </div>
                </div>

                {importResult.successCount > 0 && (
                  <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
                    <div className="flex items-center gap-2 text-green-700">
                      <CheckCircle className="w-5 h-5" />
                      <span className="font-medium">Successfully imported {importResult.successCount} team member{importResult.successCount !== 1 ? 's' : ''}!</span>
                    </div>
                  </div>
                )}

                {/* Row Results */}
                {importResult.results.length > 0 && (
                  <div className="max-h-48 overflow-y-auto border rounded-lg">
                    <table className="w-full text-sm">
                      <thead className="bg-slate-50 sticky top-0">
                        <tr>
                          <th className="px-3 py-2 text-left text-slate-700">Row</th>
                          <th className="px-3 py-2 text-left text-slate-700">Email</th>
                          <th className="px-3 py-2 text-left text-slate-700">Status</th>
                          <th className="px-3 py-2 text-left text-slate-700">Message</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {importResult.results.map((row, idx) => (
                          <tr key={idx} className="hover:bg-slate-50">
                            <td className="px-3 py-2 text-slate-600">{row.rowNumber}</td>
                            <td className="px-3 py-2 text-slate-600">{row.email}</td>
                            <td className="px-3 py-2">
                              <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${
                                row.status === 'Created'
                                  ? 'bg-green-100 text-green-700'
                                  : row.status === 'Skipped'
                                    ? 'bg-yellow-100 text-yellow-700'
                                    : 'bg-red-100 text-red-700'
                              }`}>
                                {row.status === 'Created' && <CheckCircle className="w-3 h-3" />}
                                {row.status === 'Skipped' && <AlertCircle className="w-3 h-3" />}
                                {row.status === 'Error' && <XCircle className="w-3 h-3" />}
                                {row.status}
                              </span>
                            </td>
                            <td className="px-3 py-2 text-slate-500">{row.message || '-'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {importResult.errors.length > 0 && (
                  <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                    <h4 className="text-sm font-medium text-red-800 mb-2 flex items-center gap-2">
                      <XCircle className="w-4 h-4" />
                      Errors:
                    </h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-red-700">
                      {importResult.errors.map((error, idx) => (
                        <li key={idx}>{error}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            )}

            {/* Actions */}
            <div className="flex gap-3 pt-4 border-t">
              <button
                type="button"
                onClick={closeImportModal}
                className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
              >
                {importResult ? 'Close' : 'Cancel'}
              </button>
              {!importResult && (
                <button
                  onClick={handleImport}
                  disabled={importLoading || !csvContent}
                  className="flex-1 flex items-center justify-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <FileText className="w-5 h-5" />
                  {importLoading ? 'Importing...' : 'Import'}
                </button>
              )}
            </div>
          </div>
        </Modal>
      )}

      {/* Team Members Grid */}
      {filteredDirectReports().length === 0 ? (
        <Card>
          <EmptyState title={searchQuery ? 'No team members match your search.' : 'No team members yet. Add your first direct report!'} />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredDirectReports().map((dr) => (
            <Card key={dr.id}>
              <CardContent>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center text-amber-700 font-semibold text-lg">
                      {dr.firstName[0]}{dr.lastName[0]}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold text-slate-900">{dr.fullName}</h3>
                        <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs font-medium ${
                          dr.isDirect
                            ? 'bg-green-100 text-green-700'
                            : 'bg-slate-100 text-slate-600'
                        }`}>
                          {dr.isDirect ? <Users className="w-3 h-3" /> : <UserMinus className="w-3 h-3" />}
                          {dr.isDirect ? 'Direct' : 'Indirect'}
                        </span>
                      </div>
                      <p className="text-sm text-slate-500">{dr.jobTitle || 'No title'}</p>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 rounded">
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(dr)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(dr.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
                <div className="mt-4 space-y-2">
                  <div className="flex items-center gap-2 text-sm text-slate-600">
                    <Mail className="w-4 h-4 text-slate-400" />
                    {dr.email}
                  </div>
                  {dr.department && (
                    <div className="flex items-center gap-2 text-sm text-slate-600">
                      <Building2 className="w-4 h-4 text-slate-400" />
                      {dr.department}
                    </div>
                  )}
                  <div className="flex items-center gap-2 text-sm text-slate-600">
                    <Calendar className="w-4 h-4 text-slate-400" />
                    {calculateTenure(dr.hireDate)} tenure
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
