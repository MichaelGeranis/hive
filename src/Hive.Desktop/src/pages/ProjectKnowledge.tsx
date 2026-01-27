import { useEffect, useState, useRef, useMemo } from 'react'
import { X, BookOpen, Info, Search, AlertTriangle, TrendingUp, LayoutGrid, Filter, Users } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { projectKnowledgeApi } from '../services/api'
import type {
  ProjectKnowledgeMatrix,
  ProjectKnowledge,
  CreateOrUpdateProjectKnowledgeDto,
  KnowledgeProgressionEntry
} from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import {
  ResponsiveContainer,
  RadarChart,
  Radar,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Tooltip,
  Legend
} from 'recharts'
import KnowledgeProgressionChart from '../components/KnowledgeProgressionChart'

type TabType = 'matrix' | 'progression'
type SortOrder = 'asc' | 'desc'

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

  // Tab state
  const [activeTab, setActiveTab] = useState<TabType>('matrix')

  // Progression tab state
  const [progressionView, setProgressionView] = useState<'byIndividual' | 'byProject'>('byIndividual')
  const [selectedDirectReportId, setSelectedDirectReportId] = useState<string>('')
  const [selectedProjectId, setSelectedProjectId] = useState<string>('')
  const [progressionData, setProgressionData] = useState<KnowledgeProgressionEntry[]>([])
  const [progressionLoading, setProgressionLoading] = useState(false)

  // Filter state
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedMembers, setSelectedMembers] = useState<string[]>([])

  // Sort state
  const [sortByAvg, setSortByAvg] = useState(false)
  const [projectSortOrder, setProjectSortOrder] = useState<SortOrder>('asc')
  const [memberSortOrder, setMemberSortOrder] = useState<SortOrder>('asc')

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

  const loadProgressionData = async () => {
    setProgressionLoading(true)
    try {
      if (progressionView === 'byIndividual' && selectedDirectReportId) {
        const data = await projectKnowledgeApi.getProgressionByDirectReport(selectedDirectReportId)
        setProgressionData(data)
      } else if (progressionView === 'byProject' && selectedProjectId) {
        const data = await projectKnowledgeApi.getProgressionByProject(selectedProjectId)
        setProgressionData(data)
      } else {
        setProgressionData([])
      }
    } catch (err) {
      console.error('Failed to load progression data:', err)
      setProgressionData([])
    } finally {
      setProgressionLoading(false)
    }
  }

  useEffect(() => {
    if (activeTab === 'progression') {
      loadProgressionData()
    }
  }, [activeTab, progressionView, selectedDirectReportId, selectedProjectId])

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

  // Calculate average knowledge for a project (across all direct reports)
  const getProjectAvgKnowledge = (projectId: string): number => {
    if (!matrix) return 0
    const scores = matrix.scores.filter(s => s.projectId === projectId && s.knowledgeLevel > 0)
    if (scores.length === 0) return 0
    return scores.reduce((sum, s) => sum + s.knowledgeLevel, 0) / scores.length
  }

  // Calculate average knowledge for a direct report (across all projects)
  const getMemberAvgKnowledge = (directReportId: string): number => {
    if (!matrix) return 0
    const scores = matrix.scores.filter(s => s.directReportId === directReportId && s.knowledgeLevel > 0)
    if (scores.length === 0) return 0
    return scores.reduce((sum, s) => sum + s.knowledgeLevel, 0) / scores.length
  }

  // Filtered and sorted projects
  const filteredProjects = useMemo(() => {
    if (!matrix) return []

    let projects = [...matrix.projects]

    // Filter by search
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase().trim()
      projects = projects.filter(p => p.name.toLowerCase().includes(query))
    }

    // Sort projects
    projects.sort((a, b) => {
      let comparison = 0
      if (!sortByAvg) {
        comparison = a.name.localeCompare(b.name)
      } else {
        comparison = getProjectAvgKnowledge(b.id) - getProjectAvgKnowledge(a.id)
      }
      return projectSortOrder === 'desc' ? -comparison : comparison
    })

    return projects
  }, [matrix, searchQuery, sortByAvg, projectSortOrder])

  // Filtered and sorted direct reports
  const filteredDirectReports = useMemo(() => {
    if (!matrix) return []

    let reports = [...matrix.directReports]

    // Filter by selected members
    if (selectedMembers.length > 0) {
      reports = reports.filter(dr => selectedMembers.includes(dr.id))
    }

    // Sort direct reports
    reports.sort((a, b) => {
      let comparison = 0
      if (!sortByAvg) {
        comparison = a.name.localeCompare(b.name)
      } else {
        comparison = getMemberAvgKnowledge(b.id) - getMemberAvgKnowledge(a.id)
      }
      return memberSortOrder === 'desc' ? -comparison : comparison
    })

    return reports
  }, [matrix, selectedMembers, sortByAvg, memberSortOrder])

  // Check if any filters are active
  const hasActiveFilters = searchQuery.trim() !== '' || selectedMembers.length > 0 || sortByAvg

  const clearAllFilters = () => {
    setSearchQuery('')
    setSelectedMembers([])
    setSortByAvg(false)
    setProjectSortOrder('asc')
    setMemberSortOrder('asc')
  }

  // Toggle member selection (for tag-style filter)
  const toggleMemberSelection = (memberId: string) => {
    setSelectedMembers(prev =>
      prev.includes(memberId)
        ? prev.filter(id => id !== memberId)
        : [...prev, memberId]
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
  const hasFilteredResults = filteredProjects.length > 0 && filteredDirectReports.length > 0

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

      {/* Tabs */}
      <div className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        {[
          { key: 'matrix', label: 'Matrix', icon: LayoutGrid },
          { key: 'progression', label: 'Progression', icon: TrendingUp }
        ].map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key as TabType)}
            className={`
              px-4 py-2 font-medium transition-colors flex items-center gap-2
              ${activeTab === key
                ? 'text-amber-500 border-b-2 border-amber-500'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
              }
            `}
          >
            <Icon className="w-4 h-4" />
            {label}
          </button>
        ))}
      </div>

      {/* Search & Filters - Only show on Matrix tab */}
      {activeTab === 'matrix' && hasProjects && hasDirectReports && (
        <div className="space-y-4">
          {/* Search Bar */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              placeholder="Search projects..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
            />
            {hasActiveFilters && (
              <button
                onClick={clearAllFilters}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          {/* Members Tags */}
          {matrix && matrix.directReports.length > 0 && (
            <div className="flex items-center gap-2 flex-wrap">
              <Users className="w-4 h-4 text-slate-400" />
              {matrix.directReports.map((dr) => (
                <button
                  key={dr.id}
                  onClick={() => toggleMemberSelection(dr.id)}
                  className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                    selectedMembers.includes(dr.id)
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                  }`}
                >
                  {dr.name}
                </button>
              ))}
            </div>
          )}

          {/* Filters */}
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-slate-400" />
            <div className="flex gap-2 flex-wrap">
              <button
                onClick={() => setSortByAvg(false)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  !sortByAvg
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                Sort by Name
              </button>
              <button
                onClick={() => setSortByAvg(true)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  sortByAvg
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                Sort by Avg
              </button>
            </div>
          </div>

          {/* Active filters summary */}
          {hasActiveFilters && (
            <div className="flex flex-wrap gap-2 text-sm">
              {searchQuery && (
                <span className="px-2 py-1 bg-amber-100 dark:bg-amber-900 text-amber-700 dark:text-amber-300 rounded">
                  Search: "{searchQuery}"
                </span>
              )}
              {selectedMembers.length > 0 && (
                <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded">
                  {selectedMembers.length} member{selectedMembers.length > 1 ? 's' : ''} selected
                </span>
              )}
              {sortByAvg && (
                <span className="px-2 py-1 bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300 rounded">
                  Sorted by Avg. Knowledge
                </span>
              )}
              <span className="text-slate-500 dark:text-slate-400">
                Showing {filteredProjects.length} project{filteredProjects.length !== 1 ? 's' : ''} × {filteredDirectReports.length} member{filteredDirectReports.length !== 1 ? 's' : ''}
              </span>
            </div>
          )}
        </div>
      )}

      {/* Matrix Tab Content */}
      {activeTab === 'matrix' && (
        <>
          {/* Knowledge Radar */}
          {hasProjects && hasDirectReports && matrix && matrix.scores.length > 0 && (() => {
        // Calculate average knowledge level per project
        const radarData = matrix.projects.map(project => {
          const projectScores = matrix.scores.filter(s => s.projectId === project.id)
          const avgLevel = projectScores.length > 0
            ? Math.round((projectScores.reduce((sum, s) => sum + s.knowledgeLevel, 0) / projectScores.length) * 10) / 10
            : 0
          const maxLevel = projectScores.length > 0
            ? Math.max(...projectScores.map(s => s.knowledgeLevel))
            : 0
          const coverage = Math.round((projectScores.length / matrix.directReports.length) * 100)
          return {
            project: project.name.length > 12 ? project.name.substring(0, 12) + '...' : project.name,
            fullName: project.name,
            avgLevel,
            maxLevel,
            coverage,
            assessments: projectScores.length,
            teamSize: matrix.directReports.length
          }
        }).filter(d => d.assessments > 0) // Only show projects with at least one assessment

        // Calculate overall team knowledge score
        const overallAvg = radarData.length > 0
          ? Math.round((radarData.reduce((sum, d) => sum + d.avgLevel, 0) / radarData.length) * 10) / 10
          : 0

        // Count projects with low average knowledge (< 3)
        const lowKnowledgeProjects = radarData.filter(d => d.avgLevel < 3).length

        if (radarData.length === 0) return null

        return (
          <Card>
            <CardHeader
              title="Knowledge Radar"
              subtitle={`Average knowledge levels across ${radarData.length} projects`}
            />
            <CardContent>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="h-80">
                  <ResponsiveContainer width="100%" height="100%">
                    <RadarChart data={radarData} cx="50%" cy="50%" outerRadius="70%">
                      <PolarGrid strokeDasharray="3 3" />
                      <PolarAngleAxis
                        dataKey="project"
                        tick={{ fontSize: 11, fill: 'currentColor' }}
                        className="text-slate-600 dark:text-slate-400"
                      />
                      <PolarRadiusAxis
                        angle={90}
                        domain={[0, 5]}
                        tick={{ fontSize: 10 }}
                        tickCount={6}
                      />
                      <Radar
                        name="Avg Level"
                        dataKey="avgLevel"
                        stroke="#f59e0b"
                        fill="#f59e0b"
                        fillOpacity={0.5}
                        strokeWidth={2}
                      />
                      <Radar
                        name="Max Level"
                        dataKey="maxLevel"
                        stroke="#10b981"
                        fill="transparent"
                        strokeWidth={1}
                        strokeDasharray="3 3"
                      />
                      <Tooltip
                        formatter={(value: number, name: string) => [value, name]}
                        labelFormatter={(label) => {
                          const item = radarData.find(d => d.project === label)
                          return item?.fullName || label
                        }}
                      />
                      <Legend />
                    </RadarChart>
                  </ResponsiveContainer>
                </div>
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                      <p className="text-2xl font-bold text-amber-500">{overallAvg}</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Avg Knowledge</p>
                    </div>
                    <div className="p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                      <p className="text-2xl font-bold text-blue-500">{radarData.length}</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Projects Tracked</p>
                    </div>
                  </div>
                  <div className="space-y-2 max-h-48 overflow-y-auto">
                    {radarData.sort((a, b) => a.avgLevel - b.avgLevel).slice(0, 5).map((item, idx) => (
                      <div
                        key={idx}
                        className={`flex items-center justify-between p-2 rounded-lg ${
                          item.avgLevel < 3 ? 'bg-red-50 dark:bg-red-900/20' : 'bg-slate-50 dark:bg-slate-700/50'
                        }`}
                      >
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-slate-900 dark:text-slate-100 truncate" title={item.fullName}>
                            {item.fullName}
                          </p>
                          <p className="text-xs text-slate-500 dark:text-slate-400">
                            {item.assessments}/{item.teamSize} assessed ({item.coverage}%)
                          </p>
                        </div>
                        <div className={`ml-2 px-2 py-1 rounded text-xs font-medium ${
                          item.avgLevel >= 4 ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
                          item.avgLevel >= 3 ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400' :
                          'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                        }`}>
                          {item.avgLevel}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
              {lowKnowledgeProjects > 0 && (
                <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                  <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Knowledge gaps detected</p>
                    <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                      {lowKnowledgeProjects} project{lowKnowledgeProjects !== 1 ? 's have' : ' has'} an average knowledge level below 3.
                    </p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        )
      })()}

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
          ) : !hasFilteredResults ? (
            <div className="text-center py-12 text-slate-500 dark:text-slate-400">
              <Search className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No results found with current filters.</p>
              <button
                onClick={clearAllFilters}
                className="mt-4 px-4 py-2 text-amber-500 hover:text-amber-600 transition-colors"
              >
                Clear filters
              </button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full">
                <thead>
                  <tr>
                    <th className="sticky left-0 z-10 bg-slate-50 dark:bg-slate-800 px-4 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-700 min-w-[200px]">
                      Project
                    </th>
                    {filteredDirectReports.map(dr => (
                      <th
                        key={dr.id}
                        className="px-4 py-3 text-center text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-700 min-w-[100px]"
                      >
                        <div>{dr.name}</div>
                        {sortByAvg && (
                          <div className="text-[10px] font-normal text-slate-400">
                            avg: {getMemberAvgKnowledge(dr.id).toFixed(1)}
                          </div>
                        )}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                  {filteredProjects.map(project => (
                    <tr key={project.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                      <td className="sticky left-0 z-10 bg-white dark:bg-slate-900 px-4 py-3 text-sm font-medium text-slate-900 dark:text-slate-100 border-r border-slate-200 dark:border-slate-700">
                        <div>{project.name}</div>
                        {sortByAvg && (
                          <div className="text-xs font-normal text-slate-400">
                            avg: {getProjectAvgKnowledge(project.id).toFixed(1)}
                          </div>
                        )}
                      </td>
                      {filteredDirectReports.map(dr => {
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
        </>
      )}

      {/* Progression Tab Content */}
      {activeTab === 'progression' && (
        <div className="space-y-6">
          {/* View Toggle */}
          <div className="flex gap-2">
            <button
              onClick={() => setProgressionView('byIndividual')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                progressionView === 'byIndividual'
                  ? 'bg-amber-500 text-white'
                  : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
              }`}
            >
              By Individual
            </button>
            <button
              onClick={() => setProgressionView('byProject')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                progressionView === 'byProject'
                  ? 'bg-amber-500 text-white'
                  : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
              }`}
            >
              By Project
            </button>
          </div>

          <Card>
            <CardHeader
              title="Knowledge Progression"
              subtitle={progressionView === 'byIndividual'
                ? "Track an individual's knowledge growth across projects"
                : "Track team knowledge growth for a specific project"}
            />
            <CardContent>
              {/* Selection */}
              <div className="mb-6">
                {progressionView === 'byIndividual' ? (
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                      Select Team Member
                    </label>
                    <select
                      value={selectedDirectReportId}
                      onChange={(e) => setSelectedDirectReportId(e.target.value)}
                      className="w-full max-w-md px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                    >
                      <option value="">Select a team member...</option>
                      {matrix?.directReports.map(dr => (
                        <option key={dr.id} value={dr.id}>{dr.name}</option>
                      ))}
                    </select>
                    {selectedDirectReportId && (
                      <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                        Showing knowledge progression across all projects for the selected team member.
                      </p>
                    )}
                  </div>
                ) : (
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                      Select Project
                    </label>
                    <select
                      value={selectedProjectId}
                      onChange={(e) => setSelectedProjectId(e.target.value)}
                      className="w-full max-w-md px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                    >
                      <option value="">Select a project...</option>
                      {matrix?.projects.map(p => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                    {selectedProjectId && (
                      <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
                        Showing knowledge progression for all team members on the selected project.
                      </p>
                    )}
                  </div>
                )}
              </div>

              {/* Chart */}
              {progressionLoading ? (
                <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                  Loading progression data...
                </div>
              ) : (
                <>
                  <KnowledgeProgressionChart
                    data={progressionData}
                    groupBy={progressionView === 'byIndividual' ? 'project' : 'directReport'}
                  />

                  {/* Summary Stats */}
                  {progressionData.length > 0 && (
                    <div className="mt-6 grid grid-cols-3 gap-4">
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                        <p className="text-2xl font-bold text-amber-500">
                          {progressionData.length}
                        </p>
                        <p className="text-xs text-slate-500 dark:text-slate-400">Total Changes</p>
                      </div>
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                        <p className="text-2xl font-bold text-green-500">
                          +{progressionData.filter(e => e.change > 0).reduce((sum, e) => sum + e.change, 0)}
                        </p>
                        <p className="text-xs text-slate-500 dark:text-slate-400">Total Improvement</p>
                      </div>
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg text-center">
                        <p className="text-2xl font-bold text-blue-500">
                          {progressionData.filter(e => e.change > 0).length}
                        </p>
                        <p className="text-xs text-slate-500 dark:text-slate-400">Improvements</p>
                      </div>
                    </div>
                  )}

                  {/* Recent Changes Table */}
                  {progressionData.length > 0 && (
                    <div className="mt-6">
                      <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">
                        Recent Changes
                      </h3>
                      <div className="overflow-x-auto max-h-64">
                        <table className="min-w-full text-sm">
                          <thead>
                            <tr className="text-left text-xs text-slate-500 dark:text-slate-400 uppercase">
                              <th className="pb-2 pr-4">Date</th>
                              <th className="pb-2 pr-4">
                                {progressionView === 'byIndividual' ? 'Project' : 'Team Member'}
                              </th>
                              <th className="pb-2 pr-4">Change</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                            {[...progressionData]
                              .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                              .slice(0, 10)
                              .map((entry) => (
                                <tr key={entry.id}>
                                  <td className="py-2 pr-4 text-slate-600 dark:text-slate-400">
                                    {new Date(entry.timestamp).toLocaleDateString()}
                                  </td>
                                  <td className="py-2 pr-4 text-slate-900 dark:text-slate-100">
                                    {progressionView === 'byIndividual' ? entry.projectName : entry.directReportName}
                                  </td>
                                  <td className="py-2 pr-4">
                                    <span className={`inline-flex items-center gap-1 ${
                                      entry.change > 0 ? 'text-green-600 dark:text-green-400' :
                                      entry.change < 0 ? 'text-red-600 dark:text-red-400' :
                                      'text-slate-500'
                                    }`}>
                                      {entry.oldLevel} → {entry.newLevel}
                                      <span className="text-xs">
                                        ({entry.change > 0 ? '+' : ''}{entry.change})
                                      </span>
                                    </span>
                                  </td>
                                </tr>
                              ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </div>
      )}

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
