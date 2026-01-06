import { useEffect, useState, useCallback } from 'react'
import { Plus, Mail, Building2, Calendar, MoreVertical, Trash2, Edit, Search, Upload, FileText, CheckCircle, XCircle, AlertCircle, Download } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { directReportsApi } from '../services/api'
import type { DirectReport, CreateDirectReportDto, BulkImportResultDto } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

export default function DirectReports() {
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState<CreateDirectReportDto>({
    firstName: '',
    lastName: '',
    email: '',
    jobTitle: '',
    department: '',
    hireDate: new Date().toISOString().split('T')[0]
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
      hireDate: new Date().toISOString().split('T')[0]
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

  useEscapeKey(closeModal, showForm)
  useEscapeKey(closeImportModal, showImport && !showForm)

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
    }
  }

  const handleEdit = (dr: DirectReport) => {
    setFormData({
      firstName: dr.firstName,
      lastName: dr.lastName,
      email: dr.email,
      jobTitle: dr.jobTitle,
      department: dr.department,
      hireDate: dr.hireDate.split('T')[0]
    })
    setEditingId(dr.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to remove this team member?')) {
      try {
        await directReportsApi.delete(id)
        loadDirectReports()
      } catch (err) {
        console.error(err)
      }
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
    } catch (err: any) {
      console.error('Failed to import', err)
      setImportError(err.response?.data?.message || err.message || 'Failed to import CSV')
    } finally {
      setImportLoading(false)
    }
  }

  const downloadSampleCsv = () => {
    const sample = `FirstName,LastName,Email,JobTitle,Department,HireDate
John,Doe,john.doe@example.com,Software Engineer,Engineering,2023-01-15
Jane,Smith,jane.smith@example.com,Product Manager,Product,2022-06-01
Bob,Johnson,bob.johnson@example.com,Designer,Design,2024-03-10`

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

  const filteredDirectReports = () => {
    if (!searchQuery.trim()) return directReports

    const query = searchQuery.toLowerCase()
    return directReports.filter(dr =>
      dr.fullName.toLowerCase().includes(query) ||
      dr.firstName.toLowerCase().includes(query) ||
      dr.lastName.toLowerCase().includes(query) ||
      dr.email.toLowerCase().includes(query) ||
      dr.jobTitle?.toLowerCase().includes(query) ||
      dr.department?.toLowerCase().includes(query)
    )
  }

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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Team</h1>
          <p className="text-slate-500 dark:text-slate-400">Manage direct reports</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowImport(true)}
            className="flex items-center gap-2 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
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
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
          {error}
        </div>
      )}

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search team members..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
        />
      </div>

      {/* Add/Edit Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Team Member' : 'Add Team Member'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                      First Name
                    </label>
                    <input
                      type="text"
                      value={formData.firstName}
                      onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                      Last Name
                    </label>
                    <input
                      type="text"
                      value={formData.lastName}
                      onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                      required
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Email
                  </label>
                  <input
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                      Job Title
                    </label>
                    <input
                      type="text"
                      value={formData.jobTitle}
                      onChange={(e) => setFormData({ ...formData, jobTitle: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                      Department
                    </label>
                    <input
                      type="text"
                      value={formData.department}
                      onChange={(e) => setFormData({ ...formData, department: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Hire Date
                  </label>
                  <input
                    type="date"
                    value={formData.hireDate}
                    onChange={(e) => setFormData({ ...formData, hireDate: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                    required
                  />
                </div>
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={() => setShowForm(false)}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
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
            </CardContent>
          </Card>
        </div>
      )}

      {/* Bulk Import Modal */}
      {showImport && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto">
            <CardHeader title="Import Team Members from CSV" />
            <CardContent>
              <div className="space-y-4">
                {/* Instructions */}
                <div className="p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
                  <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">CSV Format</h4>
                  <p className="text-sm text-blue-700 dark:text-blue-300 mb-2">
                    Required columns: <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">FirstName</code>, <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">LastName</code>, <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">Email</code>
                  </p>
                  <p className="text-sm text-blue-700 dark:text-blue-300">
                    Optional columns: <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">JobTitle</code>, <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">Department</code>, <code className="bg-blue-100 dark:bg-blue-800 px-1 rounded">HireDate</code>
                  </p>
                  <button
                    onClick={downloadSampleCsv}
                    className="mt-3 flex items-center gap-2 text-sm text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    <Download className="w-4 h-4" />
                    Download sample CSV template
                  </button>
                </div>

                {/* File Upload */}
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Select CSV File
                  </label>
                  <input
                    type="file"
                    accept=".csv"
                    onChange={handleFileChange}
                    className="block w-full text-sm text-slate-500 dark:text-slate-400
                      file:mr-4 file:py-2 file:px-4
                      file:rounded-lg file:border-0
                      file:text-sm file:font-semibold
                      file:bg-amber-50 dark:file:bg-amber-900/20 file:text-amber-700 dark:file:text-amber-400
                      hover:file:bg-amber-100 dark:hover:file:bg-amber-900/30
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
                    className="w-4 h-4 text-amber-500 bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 rounded focus:ring-amber-500"
                    disabled={importLoading}
                  />
                  <label htmlFor="skipDuplicates" className="text-sm text-slate-700 dark:text-slate-300">
                    Skip duplicate emails (otherwise report as error)
                  </label>
                </div>

                {/* Error Message */}
                {importError && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                    {importError}
                  </div>
                )}

                {/* Import Result */}
                {importResult && (
                  <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                    <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Import Results</h3>

                    <div className="grid grid-cols-4 gap-4">
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                        <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{importResult.totalRows}</div>
                        <div className="text-sm text-slate-500 dark:text-slate-400">Total</div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg text-center">
                        <div className="text-2xl font-bold text-green-700 dark:text-green-400">{importResult.successCount}</div>
                        <div className="text-sm text-green-600 dark:text-green-500">Created</div>
                      </div>
                      <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg text-center">
                        <div className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">{importResult.skippedCount}</div>
                        <div className="text-sm text-yellow-600 dark:text-yellow-500">Skipped</div>
                      </div>
                      <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg text-center">
                        <div className="text-2xl font-bold text-red-700 dark:text-red-400">{importResult.errorCount}</div>
                        <div className="text-sm text-red-600 dark:text-red-500">Errors</div>
                      </div>
                    </div>

                    {importResult.successCount > 0 && (
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                        <div className="flex items-center gap-2 text-green-700 dark:text-green-400">
                          <CheckCircle className="w-5 h-5" />
                          <span className="font-medium">Successfully imported {importResult.successCount} team member{importResult.successCount !== 1 ? 's' : ''}!</span>
                        </div>
                      </div>
                    )}

                    {/* Row Results */}
                    {importResult.results.length > 0 && (
                      <div className="max-h-48 overflow-y-auto border dark:border-slate-700 rounded-lg">
                        <table className="w-full text-sm">
                          <thead className="bg-slate-50 dark:bg-slate-700 sticky top-0">
                            <tr>
                              <th className="px-3 py-2 text-left text-slate-700 dark:text-slate-300">Row</th>
                              <th className="px-3 py-2 text-left text-slate-700 dark:text-slate-300">Email</th>
                              <th className="px-3 py-2 text-left text-slate-700 dark:text-slate-300">Status</th>
                              <th className="px-3 py-2 text-left text-slate-700 dark:text-slate-300">Message</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y dark:divide-slate-700">
                            {importResult.results.map((row, idx) => (
                              <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-800">
                                <td className="px-3 py-2 text-slate-600 dark:text-slate-400">{row.rowNumber}</td>
                                <td className="px-3 py-2 text-slate-600 dark:text-slate-400">{row.email}</td>
                                <td className="px-3 py-2">
                                  <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${
                                    row.status === 'Created'
                                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                                      : row.status === 'Skipped'
                                        ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                                        : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                                  }`}>
                                    {row.status === 'Created' && <CheckCircle className="w-3 h-3" />}
                                    {row.status === 'Skipped' && <AlertCircle className="w-3 h-3" />}
                                    {row.status === 'Error' && <XCircle className="w-3 h-3" />}
                                    {row.status}
                                  </span>
                                </td>
                                <td className="px-3 py-2 text-slate-500 dark:text-slate-400">{row.message || '-'}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    )}

                    {importResult.errors.length > 0 && (
                      <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                        <h4 className="text-sm font-medium text-red-800 dark:text-red-400 mb-2 flex items-center gap-2">
                          <XCircle className="w-4 h-4" />
                          Errors:
                        </h4>
                        <ul className="list-disc list-inside space-y-1 text-sm text-red-700 dark:text-red-500">
                          {importResult.errors.map((error, idx) => (
                            <li key={idx}>{error}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                )}

                {/* Actions */}
                <div className="flex gap-3 pt-4 border-t dark:border-slate-700">
                  <button
                    type="button"
                    onClick={closeImportModal}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
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
            </CardContent>
          </Card>
        </div>
      )}

      {/* Team Members Grid */}
      {filteredDirectReports().length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500">
              {searchQuery ? 'No team members match your search.' : 'No team members yet. Add your first direct report!'}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredDirectReports().map((dr) => (
            <Card key={dr.id}>
              <CardContent>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 bg-amber-100 dark:bg-amber-900/30 rounded-full flex items-center justify-center text-amber-700 dark:text-amber-400 font-semibold text-lg">
                      {dr.firstName[0]}{dr.lastName[0]}
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-900 dark:text-slate-100">{dr.fullName}</h3>
                      <p className="text-sm text-slate-500 dark:text-slate-400">{dr.jobTitle || 'No title'}</p>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded">
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(dr)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(dr.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
                <div className="mt-4 space-y-2">
                  <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
                    <Mail className="w-4 h-4 text-slate-400" />
                    {dr.email}
                  </div>
                  {dr.department && (
                    <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
                      <Building2 className="w-4 h-4 text-slate-400" />
                      {dr.department}
                    </div>
                  )}
                  <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
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
