import { useState } from 'react'
import { Upload, FileText, CheckCircle, AlertCircle, XCircle, Download } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { jiraImportApi } from '../services/api'
import type { JiraImportPreview, JiraImportResult, JiraImportRequest } from '../types'

export default function JiraImport() {
  const [csvContent, setCsvContent] = useState<string>('')
  const [preview, setPreview] = useState<JiraImportPreview | null>(null)
  const [result, setResult] = useState<JiraImportResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [importing, setImporting] = useState(false)
  const [updateExisting, setUpdateExisting] = useState(true)
  const [matchField, setMatchField] = useState<'IssueKey' | 'Title'>('IssueKey')
  const [error, setError] = useState<string | null>(null)

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (e) => {
      const content = e.target?.result as string
      setCsvContent(content)
      setPreview(null)
      setResult(null)
      setError(null)
    }
    reader.onerror = () => {
      setError('Failed to read file')
    }
    reader.readAsText(file)
  }

  const handlePreview = async () => {
    if (!csvContent) {
      setError('Please select a CSV file first')
      return
    }

    try {
      setLoading(true)
      setError(null)
      setResult(null)
      const previewData = await jiraImportApi.preview(csvContent)
      setPreview(previewData)
    } catch (err: any) {
      console.error('Failed to preview import', err)
      setError(err.response?.data || err.message || 'Failed to preview CSV')
    } finally {
      setLoading(false)
    }
  }

  const handleImport = async () => {
    if (!csvContent) {
      setError('Please select a CSV file first')
      return
    }

    if (!confirm(`Import ${preview?.validRows || 0} tasks from Jira? ${updateExisting ? 'Existing tasks will be updated.' : 'Existing tasks will be skipped.'}`)) {
      return
    }

    try {
      setImporting(true)
      setError(null)
      const importRequest: JiraImportRequest = {
        csvContent,
        updateExisting,
        matchField
      }
      const importResult = await jiraImportApi.import(importRequest)
      setResult(importResult)
      setPreview(null)
    } catch (err: any) {
      console.error('Failed to import', err)
      setError(err.response?.data || err.message || 'Failed to import CSV')
    } finally {
      setImporting(false)
    }
  }

  const handleReset = () => {
    setCsvContent('')
    setPreview(null)
    setResult(null)
    setError(null)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Import from Jira</h1>
        <p className="text-slate-500 dark:text-slate-400 mt-1">
          Import tasks from Jira CSV export into Hive
        </p>
      </div>

      {/* Instructions */}
      <Card>
        <CardHeader title="How to Export from Jira" />
        <CardContent>
          <ol className="list-decimal list-inside space-y-2 text-sm text-slate-600 dark:text-slate-400">
            <li>Go to your Jira project and navigate to Issues</li>
            <li>Click on the "..." menu and select "Export"</li>
            <li>Choose "Export CSV (all fields)" or "Export CSV (current fields)"</li>
            <li>Save the exported CSV file</li>
            <li>Upload the CSV file below to preview and import</li>
          </ol>
          <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
            <p className="text-sm text-blue-700 dark:text-blue-400">
              <strong>Tip:</strong> The importer automatically maps Jira fields (Issue Key, Summary, Status, Priority, Assignee, Story Points) to Hive tasks.
              The Jira Issue Key will be stored in the task tags for duplicate detection.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Upload Section */}
      <Card>
        <CardHeader title="Upload CSV File" />
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                Select Jira CSV Export
              </label>
              <div className="flex items-center gap-4">
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
                  disabled={loading || importing}
                />
                {csvContent && (
                  <button
                    onClick={handleReset}
                    className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                    disabled={loading || importing}
                  >
                    Clear
                  </button>
                )}
              </div>
            </div>

            {csvContent && !preview && !result && (
              <div className="flex gap-3">
                <button
                  onClick={handlePreview}
                  disabled={loading || importing}
                  className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <FileText className="w-5 h-5" />
                  {loading ? 'Loading Preview...' : 'Preview Import'}
                </button>
              </div>
            )}

            {/* Import Options */}
            {csvContent && (
              <div className="space-y-3 pt-4 border-t dark:border-slate-700">
                <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Import Options</h3>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="updateExisting"
                    checked={updateExisting}
                    onChange={(e) => setUpdateExisting(e.target.checked)}
                    className="w-4 h-4 text-amber-500 bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 rounded focus:ring-amber-500"
                    disabled={loading || importing}
                  />
                  <label htmlFor="updateExisting" className="text-sm text-slate-700 dark:text-slate-300">
                    Update existing tasks (if unchecked, existing tasks will be skipped)
                  </label>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Match existing tasks by:
                  </label>
                  <div className="flex gap-3">
                    <button
                      onClick={() => setMatchField('IssueKey')}
                      disabled={loading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'IssueKey'
                          ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                          : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                      }`}
                    >
                      Issue Key (Recommended)
                    </button>
                    <button
                      onClick={() => setMatchField('Title')}
                      disabled={loading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'Title'
                          ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                          : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                      }`}
                    >
                      Title
                    </button>
                  </div>
                </div>
              </div>
            )}

            {error && (
              <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                {error}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Preview Section */}
      {preview && (
        <Card>
          <CardHeader
            title="Import Preview"
            subtitle={`${preview.totalRows} rows detected (${preview.validRows} valid, ${preview.invalidRows} invalid)`}
          />
          <CardContent>
            <div className="space-y-4">
              {/* Summary */}
              <div className="grid grid-cols-3 gap-4">
                <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{preview.totalRows}</div>
                  <div className="text-sm text-slate-500 dark:text-slate-400">Total Rows</div>
                </div>
                <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <div className="text-2xl font-bold text-green-700 dark:text-green-400">{preview.validRows}</div>
                  <div className="text-sm text-green-600 dark:text-green-500">Valid</div>
                </div>
                <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                  <div className="text-2xl font-bold text-red-700 dark:text-red-400">{preview.invalidRows}</div>
                  <div className="text-sm text-red-600 dark:text-red-500">Invalid</div>
                </div>
              </div>

              {/* Detected Columns */}
              <div>
                <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Detected Columns:</h4>
                <div className="flex flex-wrap gap-2">
                  {preview.detectedColumns.map((col) => (
                    <span key={col} className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 text-xs rounded">
                      {col}
                    </span>
                  ))}
                </div>
              </div>

              {/* Warnings */}
              {preview.mappingWarnings.length > 0 && (
                <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                  <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2">Warnings:</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500">
                    {preview.mappingWarnings.map((warning, idx) => (
                      <li key={idx}>{warning}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Sample Rows */}
              {preview.sampleRows.length > 0 && (
                <div>
                  <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Sample Rows (first 10):</h4>
                  <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                      <thead className="bg-slate-50 dark:bg-slate-700/50">
                        <tr>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">#</th>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Issue Key</th>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Summary</th>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Type</th>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Status</th>
                          <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Valid</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                        {preview.sampleRows.map((row) => (
                          <tr key={row.rowNumber} className={row.isValid ? '' : 'bg-red-50 dark:bg-red-900/10'}>
                            <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.rowNumber}</td>
                            <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.issueKey || '-'}</td>
                            <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.summary || '-'}</td>
                            <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.issueType || '-'}</td>
                            <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.status || '-'}</td>
                            <td className="px-4 py-2">
                              {row.isValid ? (
                                <CheckCircle className="w-5 h-5 text-green-500" />
                              ) : (
                                <div className="flex items-center gap-1">
                                  <XCircle className="w-5 h-5 text-red-500" />
                                  <span className="text-xs text-red-600 dark:text-red-400">{row.validationErrors[0]}</span>
                                </div>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Import Button */}
              <div className="pt-4 border-t dark:border-slate-700">
                <button
                  onClick={handleImport}
                  disabled={importing || preview.validRows === 0}
                  className="flex items-center gap-2 px-6 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Upload className="w-5 h-5" />
                  {importing ? 'Importing...' : `Import ${preview.validRows} Tasks`}
                </button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Result Section */}
      {result && (
        <Card>
          <CardHeader
            title="Import Results"
            subtitle="Import completed"
          />
          <CardContent>
            <div className="space-y-4">
              {/* Summary */}
              <div className="grid grid-cols-4 gap-4">
                <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                  <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{result.totalRows}</div>
                  <div className="text-sm text-slate-500 dark:text-slate-400">Total</div>
                </div>
                <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <div className="text-2xl font-bold text-green-700 dark:text-green-400">{result.successCount}</div>
                  <div className="text-sm text-green-600 dark:text-green-500">Imported</div>
                </div>
                <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                  <div className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">{result.skippedCount}</div>
                  <div className="text-sm text-yellow-600 dark:text-yellow-500">Skipped</div>
                </div>
                <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                  <div className="text-2xl font-bold text-red-700 dark:text-red-400">{result.errorCount}</div>
                  <div className="text-sm text-red-600 dark:text-red-500">Errors</div>
                </div>
              </div>

              {/* Success Message */}
              {result.successCount > 0 && (
                <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                  <div className="flex items-center gap-2 text-green-700 dark:text-green-400">
                    <CheckCircle className="w-5 h-5" />
                    <span className="font-medium">Successfully imported {result.successCount} tasks!</span>
                  </div>
                  <div className="mt-2 text-sm text-green-600 dark:text-green-500">
                    {result.importedTasks.filter(t => t.isNew).length} new tasks created, {result.importedTasks.filter(t => t.isUpdated).length} tasks updated
                  </div>
                </div>
              )}

              {/* Warnings */}
              {result.warnings.length > 0 && (
                <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                  <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2 flex items-center gap-2">
                    <AlertCircle className="w-4 h-4" />
                    Warnings ({result.warnings.length}):
                  </h4>
                  <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500 max-h-48 overflow-y-auto">
                    {result.warnings.map((warning, idx) => (
                      <li key={idx}>{warning}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Errors */}
              {result.errors.length > 0 && (
                <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                  <h4 className="text-sm font-medium text-red-800 dark:text-red-400 mb-2 flex items-center gap-2">
                    <XCircle className="w-4 h-4" />
                    Errors ({result.errors.length}):
                  </h4>
                  <ul className="list-disc list-inside space-y-1 text-sm text-red-700 dark:text-red-500 max-h-48 overflow-y-auto">
                    {result.errors.map((error, idx) => (
                      <li key={idx}>{error}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Action Buttons */}
              <div className="pt-4 border-t dark:border-slate-700 flex gap-3">
                <button
                  onClick={handleReset}
                  className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                >
                  Import Another File
                </button>
                <a
                  href="/tasks"
                  className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                >
                  View Tasks
                </a>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
