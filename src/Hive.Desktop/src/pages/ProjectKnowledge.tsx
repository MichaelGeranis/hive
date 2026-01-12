import { useEffect, useState, useRef } from 'react'
import { X, BookOpen, Info } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { projectKnowledgeApi } from '../services/api'
import type {
  ProjectKnowledgeMatrix,
  ProjectKnowledge,
  CreateOrUpdateProjectKnowledgeDto
} from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const KNOWLEDGE_LEVELS = [
  { level: 0, label: '-', color: 'bg-slate-100 dark:bg-slate-700 text-slate-400 dark:text-slate-500' },
  { level: 1, label: 'No clue', color: 'bg-red-200 dark:bg-red-900 text-red-800 dark:text-red-200' },
  { level: 2, label: 'Limited', color: 'bg-orange-200 dark:bg-orange-900 text-orange-800 dark:text-orange-200' },
  { level: 3, label: 'Moderate', color: 'bg-yellow-200 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200' },
  { level: 4, label: 'Good', color: 'bg-lime-200 dark:bg-lime-900 text-lime-800 dark:text-lime-200' },
  { level: 5, label: 'Confident', color: 'bg-green-200 dark:bg-green-900 text-green-800 dark:text-green-200' }
]

const getKnowledgeColor = (level: number | undefined): string => {
  if (!level) return 'bg-slate-100 dark:bg-slate-700 text-slate-400 dark:text-slate-500'
  const found = KNOWLEDGE_LEVELS.find(k => k.level === level)
  return found?.color || 'bg-slate-100 dark:bg-slate-700'
}

export default function ProjectKnowledgePage() {
  const [matrix, setMatrix] = useState<ProjectKnowledgeMatrix | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showLegend, setShowLegend] = useState(false)
  const [activeDropdown, setActiveDropdown] = useState<{
    projectId: string
    directReportId: string
  } | null>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEscapeKey(() => {
    setShowLegend(false)
    setActiveDropdown(null)
  })

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setActiveDropdown(null)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  useEffect(() => {
    loadMatrix()
  }, [])

  const loadMatrix = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await projectKnowledgeApi.getMatrix()
      setMatrix(data)
    } catch (err) {
      console.error('Failed to load project knowledge matrix:', err)
      setError('Failed to load project knowledge matrix')
    } finally {
      setLoading(false)
    }
  }

  const handleCellClick = (projectId: string, directReportId: string) => {
    if (activeDropdown?.projectId === projectId && activeDropdown?.directReportId === directReportId) {
      setActiveDropdown(null)
    } else {
      setActiveDropdown({ projectId, directReportId })
    }
  }

  const handleLevelSelect = async (projectId: string, directReportId: string, level: number) => {
    try {
      if (level === 0) {
        // Find and delete the existing score
        const existingScore = matrix?.scores.find(
          s => s.projectId === projectId && s.directReportId === directReportId
        )
        if (existingScore) {
          await projectKnowledgeApi.delete(existingScore.id)
        }
      } else {
        const dto: CreateOrUpdateProjectKnowledgeDto = {
          directReportId,
          projectId,
          knowledgeLevel: level
        }
        await projectKnowledgeApi.createOrUpdate(dto)
      }
      await loadMatrix()
      setActiveDropdown(null)
    } catch (err) {
      console.error('Failed to update knowledge assessment:', err)
    }
  }

  const getScoreForCell = (projectId: string, directReportId: string): ProjectKnowledge | undefined => {
    return matrix?.scores.find(
      s => s.projectId === projectId && s.directReportId === directReportId
    )
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Knowledge Matrix</h1>
          <p className="text-slate-500 dark:text-slate-400">Track knowledge gaps across projects and team members</p>
        </div>
        <div className="flex items-center justify-center h-64">
          <div className="text-slate-500 dark:text-slate-400">Loading knowledge matrix...</div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Knowledge Matrix</h1>
          <p className="text-slate-500 dark:text-slate-400">Track knowledge gaps across projects and team members</p>
        </div>
        <div className="flex items-center justify-center h-64">
          <div className="text-red-500">{error}</div>
        </div>
      </div>
    )
  }

  const hasProjects = matrix && matrix.projects.length > 0
  const hasDirectReports = matrix && matrix.directReports.length > 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Knowledge Matrix</h1>
          <p className="text-slate-500 dark:text-slate-400">Track knowledge gaps across projects and team members</p>
        </div>
        <button
          onClick={() => setShowLegend(true)}
          className="flex items-center gap-2 px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
        >
          <Info className="w-5 h-5" />
          Legend
        </button>
      </div>

      {/* Matrix Table */}
      <Card>
        <CardHeader
          title="Project Knowledge"
          subtitle={hasProjects && hasDirectReports ? 'Click any cell to set knowledge level' : undefined}
        />
        <CardContent>
          {!hasProjects || !hasDirectReports ? (
            <div className="text-center py-12 text-slate-500 dark:text-slate-400">
              <BookOpen className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>
                {!hasProjects && !hasDirectReports
                  ? 'No projects or team members found. Add some to start tracking knowledge.'
                  : !hasProjects
                  ? 'No projects found. Create some projects first.'
                  : 'No direct reports found. Add some team members first.'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full">
                <thead>
                  <tr>
                    <th className="sticky left-0 z-10 bg-slate-50 dark:bg-slate-800 px-4 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-700 min-w-[200px]">
                      Project
                    </th>
                    {matrix.directReports.map(dr => (
                      <th
                        key={dr.id}
                        className="px-4 py-3 text-center text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-700 min-w-[100px]"
                      >
                        {dr.name}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                  {matrix.projects.map(project => (
                    <tr key={project.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                      <td className="sticky left-0 z-10 bg-white dark:bg-slate-900 px-4 py-3 text-sm font-medium text-slate-900 dark:text-slate-100 border-r border-slate-200 dark:border-slate-700">
                        {project.name}
                      </td>
                      {matrix.directReports.map(dr => {
                        const score = getScoreForCell(project.id, dr.id)
                        const isActive = activeDropdown?.projectId === project.id && activeDropdown?.directReportId === dr.id
                        return (
                          <td
                            key={`${project.id}-${dr.id}`}
                            className="px-4 py-3 text-center relative"
                          >
                            <button
                              onClick={() => handleCellClick(project.id, dr.id)}
                              className={`inline-flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium transition-all hover:ring-2 hover:ring-amber-500 hover:ring-offset-2 dark:hover:ring-offset-slate-900 ${getKnowledgeColor(score?.knowledgeLevel)}`}
                            >
                              {score?.knowledgeLevel || '-'}
                            </button>

                            {/* Dropdown */}
                            {isActive && (
                              <div
                                ref={dropdownRef}
                                className="absolute z-20 mt-1 left-1/2 -translate-x-1/2 bg-white dark:bg-slate-800 rounded-lg shadow-lg border border-slate-200 dark:border-slate-700 py-1 min-w-[140px]"
                              >
                                {KNOWLEDGE_LEVELS.map(({ level, label, color }) => (
                                  <button
                                    key={level}
                                    onClick={() => handleLevelSelect(project.id, dr.id, level)}
                                    className={`w-full px-3 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-700 flex items-center gap-2 ${
                                      score?.knowledgeLevel === level ? 'bg-slate-50 dark:bg-slate-700/50' : ''
                                    }`}
                                  >
                                    <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-medium ${color}`}>
                                      {level || '-'}
                                    </span>
                                    <span className="text-slate-700 dark:text-slate-300">{label}</span>
                                  </button>
                                ))}
                              </div>
                            )}
                          </td>
                        )
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Legend Modal */}
      {showLegend && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl max-w-md w-full mx-4">
            <div className="flex justify-between items-center p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                Knowledge Level Legend
              </h2>
              <button
                onClick={() => setShowLegend(false)}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-6 h-6" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              {KNOWLEDGE_LEVELS.filter(k => k.level > 0).map(({ level, label, color }) => (
                <div key={level} className="flex items-center gap-4">
                  <span className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium ${color}`}>
                    {level}
                  </span>
                  <div>
                    <div className="font-medium text-slate-900 dark:text-slate-100">{label}</div>
                    <div className="text-sm text-slate-500 dark:text-slate-400">
                      {level === 1 && 'Very little or no knowledge about the topic'}
                      {level === 2 && 'Some awareness, basic understanding'}
                      {level === 3 && 'Reasonable understanding, familiar with key concepts'}
                      {level === 4 && 'Solid understanding, can discuss with confidence'}
                      {level === 5 && 'Excellent comprehensive understanding, expert level'}
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <div className="flex justify-end p-6 border-t border-slate-200 dark:border-slate-700">
              <button
                onClick={() => setShowLegend(false)}
                className="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white rounded-lg transition-colors"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
