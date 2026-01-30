import { useEffect, useState, useMemo, useRef } from 'react'
import {
  Award,
  Plus,
  Search,
  X,
  Edit2,
  Trash2,
  TrendingUp,
  Users,
  Target,
  ChevronUp,
  ChevronDown
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { SkillsHeatmap, type MatrixSortBy, type MatrixSortOrder, type SkillsHeatmapHandle } from '../components/SkillsHeatmap'
import { SkillRadarChart } from '../components/SkillRadarChart'
import { skillsApi, skillCategoriesApi, skillAssessmentsApi, directReportsApi } from '../services/api'
import type {
  Skill,
  SkillCategoryEntity,
  SkillAssessment,
  SkillMatrix,
  ProficiencyLevel,
  CreateSkillDto,
  CreateSkillAssessmentDto,
  UpdateSkillAssessmentDto,
  DirectReport,
  CreateSkillCategoryDto,
  UpdateSkillCategoryDto,
  SkillsSummaryDto
} from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useToast, getErrorMessage } from '../contexts/ToastContext'
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend
} from 'recharts'

const COLORS = ['#3b82f6', '#8b5cf6', '#f59e0b', '#10b981', '#ef4444']

const LEVEL_NAMES: Record<ProficiencyLevel, string> = {
  0: 'None',
  1: 'Novice',
  2: 'Beginner',
  3: 'Intermediate',
  4: 'Advanced',
  5: 'Expert'
}

type TabType = 'overview' | 'matrix' | 'profiles' | 'gaps' | 'manage'

export default function Skills() {
  const { showError } = useToast()
  const [activeTab, setActiveTab] = useState<TabType>('overview')
  const [skills, setSkills] = useState<Skill[]>([])
  const [skillCategories, setSkillCategories] = useState<SkillCategoryEntity[]>([])
  const [matrix, setMatrix] = useState<SkillMatrix | null>(null)
  const [gaps, setGaps] = useState<SkillAssessment[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [summary, setSummary] = useState<SkillsSummaryDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Filters
  const [categoryFilter, setCategoryFilter] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')

  // Matrix-specific filters
  const [matrixTeamFilter, setMatrixTeamFilter] = useState<string[]>([])
  const [matrixSortBy, setMatrixSortBy] = useState<MatrixSortBy>('name')
  const [matrixSortOrder, setMatrixSortOrder] = useState<MatrixSortOrder>('asc')

  // Modals
  const [showSkillModal, setShowSkillModal] = useState(false)
  const [showAssessmentModal, setShowAssessmentModal] = useState(false)
  const [showCategoryModal, setShowCategoryModal] = useState(false)
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null)
  const [editingAssessment, setEditingAssessment] = useState<SkillAssessment | null>(null)
  const [editingCategory, setEditingCategory] = useState<SkillCategoryEntity | null>(null)

  // Form state
  const [skillForm, setSkillForm] = useState<CreateSkillDto>({
    name: '',
    description: '',
    categoryId: ''
  })
  const [assessmentForm, setAssessmentForm] = useState<{
    directReportId: string
    skillId: string
    level: ProficiencyLevel
    targetLevel?: ProficiencyLevel
    notes: string
  }>({
    directReportId: '',
    skillId: '',
    level: 0,
    targetLevel: undefined,
    notes: ''
  })
  const [categoryForm, setCategoryForm] = useState<CreateSkillCategoryDto>({
    name: '',
    description: '',
    sortOrder: 0
  })

  // Ref for preserving scroll position
  const heatmapRef = useRef<SkillsHeatmapHandle>(null)
  const savedScrollPositionRef = useRef<number>(0)

  useEscapeKey(() => {
    setShowSkillModal(false)
    setShowAssessmentModal(false)
    setShowCategoryModal(false)
  })

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      setError(null)
      // Backend filters to direct reports only (directOnly=true is default)
      const [skillsData, categoriesData, matrixData, gapsData, reportsData, summaryData] = await Promise.all([
        skillsApi.getAll(false),
        skillCategoriesApi.getAll(false),
        skillAssessmentsApi.getMatrix(), // directOnly=true by default
        skillAssessmentsApi.getGaps(),   // directOnly=true by default
        directReportsApi.getAll(),
        skillAssessmentsApi.getSummary() // directOnly=true by default
      ])
      setSkills(skillsData)
      setSkillCategories(categoriesData)
      setMatrix(matrixData)
      setGaps(gapsData)
      setSummary(summaryData)
      setDirectReports(reportsData.filter(r => r.isDirect))
    } catch (err) {
      console.error('Failed to load skills data:', err)
      setError('Failed to load skills data')
    } finally {
      setLoading(false)
    }
  }

  // Computed data
  const activeSkills = useMemo(() => skills.filter(s => s.isActive), [skills])

  const allAssessments = useMemo(() => {
    if (!matrix) return []
    return matrix.directReports.flatMap(dr => dr.assessments)
  }, [matrix])

  // Use backend-provided chart data (falls back to empty arrays if not loaded)
  const skillsByCategoryData = summary?.skillsByCategory ?? []
  const proficiencyDistributionData = summary?.proficiencyDistribution ?? []

  // Handlers
  const handleCreateSkill = async () => {
    try {
      await skillsApi.create(skillForm)
      await loadData()
      setShowSkillModal(false)
      resetSkillForm()
    } catch (err) {
      console.error('Failed to create skill:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleUpdateSkill = async () => {
    if (!editingSkill) return
    try {
      await skillsApi.update(editingSkill.id, skillForm)
      await loadData()
      setShowSkillModal(false)
      resetSkillForm()
      setEditingSkill(null)
    } catch (err) {
      console.error('Failed to update skill:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleDeleteSkill = async (id: string) => {
    if (!confirm('Are you sure you want to delete this skill?')) return
    try {
      await skillsApi.delete(id)
      await loadData()
    } catch (err) {
      console.error('Failed to delete skill:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleCreateAssessment = async () => {
    try {
      // Save scroll position before update
      savedScrollPositionRef.current = heatmapRef.current?.getScrollPosition() ?? 0

      const data: CreateSkillAssessmentDto = {
        directReportId: assessmentForm.directReportId,
        skillId: assessmentForm.skillId,
        level: assessmentForm.level,
        targetLevel: assessmentForm.targetLevel,
        notes: assessmentForm.notes
      }
      await skillAssessmentsApi.create(data)
      await loadData()
      setShowAssessmentModal(false)
      resetAssessmentForm()

      // Restore scroll position after a brief delay to allow React to re-render
      requestAnimationFrame(() => {
        heatmapRef.current?.setScrollPosition(savedScrollPositionRef.current)
      })
    } catch (err) {
      console.error('Failed to create assessment:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleUpdateAssessment = async () => {
    if (!editingAssessment) return
    try {
      // Save scroll position before update
      savedScrollPositionRef.current = heatmapRef.current?.getScrollPosition() ?? 0

      const data: UpdateSkillAssessmentDto = {
        level: assessmentForm.level,
        targetLevel: assessmentForm.targetLevel,
        notes: assessmentForm.notes
      }
      await skillAssessmentsApi.update(editingAssessment.id, data)
      await loadData()
      setShowAssessmentModal(false)
      resetAssessmentForm()
      setEditingAssessment(null)

      // Restore scroll position after a brief delay to allow React to re-render
      requestAnimationFrame(() => {
        heatmapRef.current?.setScrollPosition(savedScrollPositionRef.current)
      })
    } catch (err) {
      console.error('Failed to update assessment:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleDeleteAssessment = async (id: string) => {
    if (!confirm('Are you sure you want to delete this assessment?')) return
    try {
      // Save scroll position before update
      savedScrollPositionRef.current = heatmapRef.current?.getScrollPosition() ?? 0

      await skillAssessmentsApi.delete(id)
      await loadData()
      setShowAssessmentModal(false)
      resetAssessmentForm()
      setEditingAssessment(null)

      // Restore scroll position after a brief delay to allow React to re-render
      requestAnimationFrame(() => {
        heatmapRef.current?.setScrollPosition(savedScrollPositionRef.current)
      })
    } catch (err) {
      console.error('Failed to delete assessment:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleCreateCategory = async () => {
    try {
      await skillCategoriesApi.create(categoryForm)
      await loadData()
      setShowCategoryModal(false)
      resetCategoryForm()
    } catch (err) {
      console.error('Failed to create category:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleUpdateCategory = async () => {
    if (!editingCategory) return
    try {
      const updateDto: UpdateSkillCategoryDto = {
        name: categoryForm.name,
        description: categoryForm.description,
        sortOrder: categoryForm.sortOrder
      }
      await skillCategoriesApi.update(editingCategory.id, updateDto)
      await loadData()
      setShowCategoryModal(false)
      resetCategoryForm()
      setEditingCategory(null)
    } catch (err) {
      console.error('Failed to update category:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleDeleteCategory = async (id: string) => {
    if (!confirm('Are you sure you want to delete this category? Skills assigned to this category will need to be reassigned.')) return
    try {
      await skillCategoriesApi.delete(id)
      await loadData()
    } catch (err) {
      console.error('Failed to delete category:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleToggleCategoryActive = async (category: SkillCategoryEntity) => {
    try {
      if (category.isActive) {
        await skillCategoriesApi.deactivate(category.id)
      } else {
        await skillCategoriesApi.activate(category.id)
      }
      await loadData()
    } catch (err) {
      console.error('Failed to toggle category status:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleCellClick = (directReportId: string, skillId: string, _currentLevel?: ProficiencyLevel) => {
    // Find existing assessment or create new one
    const assessment = allAssessments.find(
      a => a.directReportId === directReportId && a.skillId === skillId
    )

    if (assessment) {
      // Edit existing
      setEditingAssessment(assessment)
      setAssessmentForm({
        directReportId: assessment.directReportId,
        skillId: assessment.skillId,
        level: assessment.level,
        targetLevel: assessment.targetLevel,
        notes: assessment.notes
      })
    } else {
      // Create new
      setAssessmentForm({
        directReportId,
        skillId,
        level: 0,
        targetLevel: undefined,
        notes: ''
      })
    }
    setShowAssessmentModal(true)
  }

  const resetSkillForm = () => {
    const defaultCategoryId = skillCategories.length > 0 ? skillCategories[0].id : ''
    setSkillForm({ name: '', description: '', categoryId: defaultCategoryId })
  }

  const resetAssessmentForm = () => {
    setAssessmentForm({
      directReportId: '',
      skillId: '',
      level: 0,
      targetLevel: undefined,
      notes: ''
    })
  }

  const resetCategoryForm = () => {
    const nextSortOrder = skillCategories.length > 0
      ? Math.max(...skillCategories.map(c => c.sortOrder)) + 1
      : 0
    setCategoryForm({ name: '', description: '', sortOrder: nextSortOrder })
  }

  const openCategoryModal = (category?: SkillCategoryEntity) => {
    if (category) {
      setEditingCategory(category)
      setCategoryForm({
        name: category.name,
        description: category.description,
        sortOrder: category.sortOrder
      })
    } else {
      resetCategoryForm()
      setEditingCategory(null)
    }
    setShowCategoryModal(true)
  }

  const openSkillModal = (skill?: Skill) => {
    if (skill) {
      setEditingSkill(skill)
      setSkillForm({
        name: skill.name,
        description: skill.description,
        categoryId: skill.categoryId
      })
    } else {
      resetSkillForm()
      setEditingSkill(null)
    }
    setShowSkillModal(true)
  }

  const filteredSkills = useMemo(() => {
    return skills.filter(skill => {
      const matchesCategory = categoryFilter === null || skill.categoryId === categoryFilter
      const matchesSearch = searchQuery === '' ||
        skill.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        skill.description.toLowerCase().includes(searchQuery.toLowerCase())
      return matchesCategory && matchesSearch
    })
  }, [skills, categoryFilter, searchQuery])

  // Helper to get category name by ID
  const getCategoryNameById = (categoryId: string): string => {
    const category = skillCategories.find(c => c.id === categoryId)
    return category?.name || 'Unknown'
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Skills</h1>
            <p className="text-slate-500 dark:text-slate-400">Manage direct reports' skills, track proficiency, and identify development opportunities</p>
          </div>
        </div>
        <div className="flex items-center justify-center h-64">
          <div className="text-slate-500 dark:text-slate-400">Loading skills data...</div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Skills</h1>
            <p className="text-slate-500 dark:text-slate-400">Manage direct reports' skills, track proficiency, and identify development opportunities</p>
          </div>
        </div>
        <div className="flex items-center justify-center h-64">
          <div className="text-red-500">{error}</div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Skills</h1>
          <p className="text-slate-500 dark:text-slate-400">Manage direct reports' skills, track proficiency, and identify development opportunities</p>
        </div>
        <button
          onClick={() => openSkillModal()}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Add Skill
        </button>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        {[
          { key: 'overview', label: 'Overview', icon: Award },
          { key: 'matrix', label: 'Skills Matrix', icon: Users },
          { key: 'profiles', label: 'Team Profiles', icon: Target },
          { key: 'gaps', label: 'Gap Analysis', icon: TrendingUp },
          { key: 'manage', label: 'Manage', icon: Edit2 }
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

      {/* Filters */}
      <div className="flex gap-4">
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-slate-400" />
          <input
            type="text"
            placeholder="Search skills..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 placeholder-slate-400 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
          />
        </div>
        <select
          value={categoryFilter === null ? '' : categoryFilter}
          onChange={(e) => setCategoryFilter(e.target.value === '' ? null : e.target.value)}
          className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
        >
          <option value="">All Categories</option>
          {skillCategories.map((category) => (
            <option key={category.id} value={category.id}>{category.name}</option>
          ))}
        </select>
        {(categoryFilter !== null || searchQuery !== '') && (
          <button
            onClick={() => {
              setCategoryFilter(null)
              setSearchQuery('')
            }}
            className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-700 flex items-center gap-2"
          >
            <X className="w-4 h-4" />
            Clear
          </button>
        )}
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="space-y-6">
          {/* Stats */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <StatCard
              title="Total Skills"
              value={summary?.totalSkills ?? skills.length}
              subtitle={`${summary?.activeSkills ?? activeSkills.length} active`}
              icon={<Award className="w-6 h-6" />}
              color="blue"
            />
            <StatCard
              title="Assessments"
              value={summary?.totalAssessments ?? allAssessments.length}
              subtitle={`Across ${summary?.directReportCount ?? matrix?.directReports.length ?? 0} direct reports`}
              icon={<Users className="w-6 h-6" />}
              color="purple"
            />
            <StatCard
              title="Skill Gaps"
              value={summary?.skillGapCount ?? gaps.length}
              subtitle="Development opportunities"
              icon={<TrendingUp className="w-6 h-6" />}
              color="amber"
            />
          </div>

          {/* Charts */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader title="Skills by Category" />
              <CardContent>
                {skillsByCategoryData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                      <Pie
                        data={skillsByCategoryData}
                        dataKey="value"
                        nameKey="name"
                        cx="50%"
                        cy="50%"
                        outerRadius={100}
                        label={(entry) => `${entry.name}: ${entry.value}`}
                      >
                        {skillsByCategoryData.map((_, index) => (
                          <Cell key={index} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip />
                      <Legend />
                    </PieChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-64 flex items-center justify-center text-slate-500 dark:text-slate-400">
                    No skills data available
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader title="Proficiency Distribution" />
              <CardContent>
                {proficiencyDistributionData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={proficiencyDistributionData}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-slate-300 dark:stroke-slate-600" />
                      <XAxis dataKey="name" tick={{ fill: '#64748b', fontSize: 12 }} />
                      <YAxis tick={{ fill: '#64748b', fontSize: 12 }} />
                      <Tooltip
                        contentStyle={{
                          backgroundColor: '#1e293b',
                          border: '1px solid #475569',
                          borderRadius: '0.375rem',
                          color: '#f1f5f9'
                        }}
                      />
                      <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-64 flex items-center justify-center text-slate-500 dark:text-slate-400">
                    No assessment data available
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {activeTab === 'matrix' && matrix && (
        <Card>
          <CardHeader
            title="Team Skills Matrix"
            subtitle="View and edit direct reports' proficiency levels across all skills"
          />
          <CardContent>
            {/* Matrix-specific filters */}
            <div className="flex flex-wrap gap-4 mb-6 pb-4 border-b border-slate-200 dark:border-slate-700">
              {/* Team member filter */}
              <div className="flex-1 min-w-[200px]">
                <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  Team Members
                </label>
                <select
                  multiple
                  value={matrixTeamFilter}
                  onChange={(e) => {
                    const selected = Array.from(e.target.selectedOptions, option => option.value)
                    setMatrixTeamFilter(selected)
                  }}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent min-h-[80px]"
                >
                  {directReports.map(dr => (
                    <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                  ))}
                </select>
                <p className="text-xs text-slate-400 mt-1">Hold Ctrl/Cmd to select multiple</p>
              </div>

              {/* Sort controls */}
              <div className="min-w-[180px]">
                <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">
                  Sort By
                </label>
                <div className="flex gap-2">
                  <select
                    value={matrixSortBy}
                    onChange={(e) => setMatrixSortBy(e.target.value as MatrixSortBy)}
                    className="flex-1 px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 text-sm focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  >
                    <option value="name">Name</option>
                    <option value="avgProficiency">Avg. Proficiency</option>
                    <option value="gaps">Skill Gaps</option>
                  </select>
                  <button
                    onClick={() => setMatrixSortOrder(prev => prev === 'asc' ? 'desc' : 'asc')}
                    className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                    title={matrixSortOrder === 'asc' ? 'Ascending' : 'Descending'}
                  >
                    {matrixSortOrder === 'asc' ? (
                      <ChevronUp className="w-4 h-4" />
                    ) : (
                      <ChevronDown className="w-4 h-4" />
                    )}
                  </button>
                </div>
              </div>

              {/* Clear matrix filters */}
              {(matrixTeamFilter.length > 0 || matrixSortBy !== 'name' || matrixSortOrder !== 'asc') && (
                <div className="flex items-end">
                  <button
                    onClick={() => {
                      setMatrixTeamFilter([])
                      setMatrixSortBy('name')
                      setMatrixSortOrder('asc')
                    }}
                    className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-700 flex items-center gap-2 text-sm transition-colors"
                  >
                    <X className="w-4 h-4" />
                    Clear Matrix Filters
                  </button>
                </div>
              )}
            </div>

            {/* Active filters summary */}
            {(matrixTeamFilter.length > 0 || categoryFilter !== null || searchQuery) && (
              <div className="mb-4 flex flex-wrap gap-2 text-sm">
                {matrixTeamFilter.length > 0 && (
                  <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded">
                    {matrixTeamFilter.length} team member{matrixTeamFilter.length > 1 ? 's' : ''} selected
                  </span>
                )}
                {categoryFilter !== null && (
                  <span className="px-2 py-1 bg-purple-100 dark:bg-purple-900 text-purple-700 dark:text-purple-300 rounded">
                    Category: {getCategoryNameById(categoryFilter)}
                  </span>
                )}
                {searchQuery && (
                  <span className="px-2 py-1 bg-amber-100 dark:bg-amber-900 text-amber-700 dark:text-amber-300 rounded">
                    Search: "{searchQuery}"
                  </span>
                )}
              </div>
            )}

            <SkillsHeatmap
              ref={heatmapRef}
              matrix={matrix}
              onCellClick={handleCellClick}
              categoryFilter={categoryFilter}
              searchQuery={searchQuery}
              teamMemberFilter={matrixTeamFilter}
              sortBy={matrixSortBy}
              sortOrder={matrixSortOrder}
            />
          </CardContent>
        </Card>
      )}

      {activeTab === 'profiles' && matrix && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {matrix.directReports.map(dr => (
            <Card key={dr.directReportId}>
              <CardContent>
                <SkillRadarChart
                  assessments={dr.assessments}
                  teamMemberName={dr.directReportName}
                  showTarget={true}
                />
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {activeTab === 'gaps' && (
        <div className="space-y-6">
          <Card>
            <CardHeader
              title="Skill Gaps"
              subtitle={`${gaps.length} skills below target proficiency level`}
            />
            <CardContent>
              {gaps.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                    <thead className="bg-slate-50 dark:bg-slate-800">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Direct Report
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Skill
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Category
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Current
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Target
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                          Gap
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-slate-900 divide-y divide-slate-200 dark:divide-slate-700">
                      {gaps.map(gap => (
                        <tr key={gap.id} className="hover:bg-slate-50 dark:hover:bg-slate-800">
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-900 dark:text-slate-100">
                            {gap.directReportName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                            {gap.skillName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                            {gap.skillCategoryName}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                            {gap.levelName} ({gap.level})
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                            {gap.targetLevelName} ({gap.targetLevel})
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm">
                            <span className="px-2 py-1 bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 rounded">
                              -{gap.skillGap}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="text-center py-12 text-slate-500 dark:text-slate-400">
                  No skill gaps found. All team members are meeting their targets!
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {activeTab === 'manage' && (
        <div className="space-y-6">
          {/* Categories Section */}
          <Card>
            <CardHeader
              title="Skill Categories"
              subtitle="Manage categories to organize skills"
              action={
                <button
                  onClick={() => openCategoryModal()}
                  className="bg-purple-500 hover:bg-purple-600 text-white px-4 py-2 rounded-lg flex items-center gap-2 transition-colors"
                >
                  <Plus className="w-4 h-4" />
                  Add Category
                </button>
              }
            />
            <CardContent>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                  <thead className="bg-slate-50 dark:bg-slate-800">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Order
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Description
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Skills
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white dark:bg-slate-900 divide-y divide-slate-200 dark:divide-slate-700">
                    {skillCategories
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map(category => {
                        const skillCount = skills.filter(s => s.categoryId === category.id).length
                        return (
                          <tr key={category.id} className="hover:bg-slate-50 dark:hover:bg-slate-800">
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                              {category.sortOrder}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-900 dark:text-slate-100">
                              {category.name}
                            </td>
                            <td className="px-6 py-4 text-sm text-slate-500 dark:text-slate-400 max-w-md truncate">
                              {category.description}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                              {skillCount}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm">
                              <button
                                onClick={() => handleToggleCategoryActive(category)}
                                className={`px-2 py-1 rounded cursor-pointer transition-colors ${
                                  category.isActive
                                    ? 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200 hover:bg-green-200 dark:hover:bg-green-800'
                                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-600'
                                }`}
                              >
                                {category.isActive ? 'Active' : 'Inactive'}
                              </button>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                              <button
                                onClick={() => openCategoryModal(category)}
                                className="text-blue-600 dark:text-blue-400 hover:text-blue-900 dark:hover:text-blue-300"
                              >
                                <Edit2 className="w-4 h-4 inline" />
                              </button>
                              <button
                                onClick={() => handleDeleteCategory(category.id)}
                                className="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                                title={skillCount > 0 ? 'Cannot delete: has skills assigned' : 'Delete category'}
                                disabled={skillCount > 0}
                              >
                                <Trash2 className={`w-4 h-4 inline ${skillCount > 0 ? 'opacity-50 cursor-not-allowed' : ''}`} />
                              </button>
                            </td>
                          </tr>
                        )
                      })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* Skills Library Section */}
          <Card>
            <CardHeader
              title="Skills Library"
              subtitle="Manage your organization's skill catalog"
              action={
                <button
                  onClick={() => openSkillModal()}
                  className="bg-amber-500 hover:bg-amber-600 text-white px-4 py-2 rounded-lg flex items-center gap-2 transition-colors"
                >
                  <Plus className="w-4 h-4" />
                  Add Skill
                </button>
              }
            />
            <CardContent>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                  <thead className="bg-slate-50 dark:bg-slate-800">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Category
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Description
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white dark:bg-slate-900 divide-y divide-slate-200 dark:divide-slate-700">
                    {filteredSkills.map(skill => (
                      <tr key={skill.id} className="hover:bg-slate-50 dark:hover:bg-slate-800">
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-900 dark:text-slate-100">
                          {skill.name}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 dark:text-slate-400">
                          {skill.categoryName}
                        </td>
                        <td className="px-6 py-4 text-sm text-slate-500 dark:text-slate-400 max-w-md truncate">
                          {skill.description}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm">
                          <span className={`px-2 py-1 rounded ${skill.isActive ? 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200' : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400'}`}>
                            {skill.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                          <button
                            onClick={() => openSkillModal(skill)}
                            className="text-blue-600 dark:text-blue-400 hover:text-blue-900 dark:hover:text-blue-300"
                          >
                            <Edit2 className="w-4 h-4 inline" />
                          </button>
                          <button
                            onClick={() => handleDeleteSkill(skill.id)}
                            className="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                          >
                            <Trash2 className="w-4 h-4 inline" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Skill Modal */}
      {showSkillModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl max-w-2xl w-full mx-4">
            <div className="flex justify-between items-center p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                {editingSkill ? 'Edit Skill' : 'Add New Skill'}
              </h2>
              <button
                onClick={() => {
                  setShowSkillModal(false)
                  resetSkillForm()
                  setEditingSkill(null)
                }}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-6 h-6" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Skill Name
                </label>
                <input
                  type="text"
                  value={skillForm.name}
                  onChange={(e) => setSkillForm({ ...skillForm, name: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  placeholder="e.g., React, TypeScript, Leadership"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Category
                </label>
                <select
                  value={skillForm.categoryId}
                  onChange={(e) => setSkillForm({ ...skillForm, categoryId: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                >
                  {skillCategories.map((category) => (
                    <option key={category.id} value={category.id}>{category.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Description
                </label>
                <textarea
                  value={skillForm.description}
                  onChange={(e) => setSkillForm({ ...skillForm, description: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  rows={3}
                  placeholder="Describe this skill..."
                />
              </div>
            </div>
            <div className="flex justify-end gap-3 p-6 border-t border-slate-200 dark:border-slate-700">
              <button
                onClick={() => {
                  setShowSkillModal(false)
                  resetSkillForm()
                  setEditingSkill(null)
                }}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={editingSkill ? handleUpdateSkill : handleCreateSkill}
                className="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white rounded-lg transition-colors"
              >
                {editingSkill ? 'Update' : 'Create'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Assessment Modal */}
      {showAssessmentModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl max-w-2xl w-full mx-4">
            <div className="flex justify-between items-center p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                {editingAssessment ? 'Edit Assessment' : 'Add Assessment'}
              </h2>
              <button
                onClick={() => {
                  setShowAssessmentModal(false)
                  resetAssessmentForm()
                  setEditingAssessment(null)
                }}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-6 h-6" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Direct Report
                </label>
                <select
                  value={assessmentForm.directReportId}
                  onChange={(e) => setAssessmentForm({ ...assessmentForm, directReportId: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  disabled={!!editingAssessment}
                >
                  <option value="">Select direct report</option>
                  {directReports.map(dr => (
                    <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Skill
                </label>
                <select
                  value={assessmentForm.skillId}
                  onChange={(e) => setAssessmentForm({ ...assessmentForm, skillId: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  disabled={!!editingAssessment}
                >
                  <option value="">Select skill</option>
                  {activeSkills.map(skill => (
                    <option key={skill.id} value={skill.id}>{skill.name} ({skill.categoryName})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Current Level
                </label>
                <select
                  value={assessmentForm.level}
                  onChange={(e) => setAssessmentForm({ ...assessmentForm, level: Number(e.target.value) as ProficiencyLevel })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                >
                  {Object.entries(LEVEL_NAMES).map(([value, label]) => (
                    <option key={value} value={value}>{label} ({value})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Target Level (Optional)
                </label>
                <select
                  value={assessmentForm.targetLevel ?? ''}
                  onChange={(e) => setAssessmentForm({ ...assessmentForm, targetLevel: e.target.value === '' ? undefined : Number(e.target.value) as ProficiencyLevel })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                >
                  <option value="">No target</option>
                  {Object.entries(LEVEL_NAMES).map(([value, label]) => (
                    <option key={value} value={value}>{label} ({value})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Notes (Optional)
                </label>
                <textarea
                  value={assessmentForm.notes}
                  onChange={(e) => setAssessmentForm({ ...assessmentForm, notes: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                  rows={3}
                  placeholder="Add any notes about this assessment..."
                />
              </div>
            </div>
            <div className="flex justify-end gap-3 p-6 border-t border-slate-200 dark:border-slate-700">
              <button
                onClick={() => {
                  setShowAssessmentModal(false)
                  resetAssessmentForm()
                  setEditingAssessment(null)
                }}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
              >
                Cancel
              </button>
              {editingAssessment && (
                <button
                  onClick={() => handleDeleteAssessment(editingAssessment.id)}
                  className="px-4 py-2 bg-red-500 hover:bg-red-600 text-white rounded-lg transition-colors"
                >
                  Delete
                </button>
              )}
              <button
                onClick={editingAssessment ? handleUpdateAssessment : handleCreateAssessment}
                className="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white rounded-lg transition-colors"
                disabled={!assessmentForm.directReportId || !assessmentForm.skillId}
              >
                {editingAssessment ? 'Update' : 'Create'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Category Modal */}
      {showCategoryModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-lg shadow-xl max-w-2xl w-full mx-4">
            <div className="flex justify-between items-center p-6 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                {editingCategory ? 'Edit Category' : 'Add New Category'}
              </h2>
              <button
                onClick={() => {
                  setShowCategoryModal(false)
                  resetCategoryForm()
                  setEditingCategory(null)
                }}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-6 h-6" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Category Name
                </label>
                <input
                  type="text"
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                  placeholder="e.g., Technical, Soft Skills, Leadership"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Description
                </label>
                <textarea
                  value={categoryForm.description}
                  onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                  rows={3}
                  placeholder="Describe this category..."
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                  Sort Order
                </label>
                <input
                  type="number"
                  value={categoryForm.sortOrder}
                  onChange={(e) => setCategoryForm({ ...categoryForm, sortOrder: parseInt(e.target.value) || 0 })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                  min={0}
                />
                <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                  Lower numbers appear first in the list
                </p>
              </div>
            </div>
            <div className="flex justify-end gap-3 p-6 border-t border-slate-200 dark:border-slate-700">
              <button
                onClick={() => {
                  setShowCategoryModal(false)
                  resetCategoryForm()
                  setEditingCategory(null)
                }}
                className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={editingCategory ? handleUpdateCategory : handleCreateCategory}
                className="px-4 py-2 bg-purple-500 hover:bg-purple-600 text-white rounded-lg transition-colors"
                disabled={!categoryForm.name.trim()}
              >
                {editingCategory ? 'Update' : 'Create'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
