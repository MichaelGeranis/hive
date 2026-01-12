import { useEffect, useState, useMemo } from 'react'
import {
  Award,
  Plus,
  Search,
  X,
  Edit2,
  Trash2,
  TrendingUp,
  Users,
  Target
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { SkillsHeatmap } from '../components/SkillsHeatmap'
import { SkillRadarChart } from '../components/SkillRadarChart'
import { skillsApi, skillAssessmentsApi, directReportsApi } from '../services/api'
import type {
  Skill,
  SkillAssessment,
  SkillMatrix,
  SkillCategory,
  ProficiencyLevel,
  CreateSkillDto,
  CreateSkillAssessmentDto,
  UpdateSkillAssessmentDto,
  DirectReport
} from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
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

const CATEGORY_NAMES: Record<SkillCategory, string> = {
  0: 'Technical',
  1: 'Soft Skills',
  2: 'Leadership',
  3: 'Domain Knowledge',
  4: 'Tools'
}

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
  const [activeTab, setActiveTab] = useState<TabType>('overview')
  const [skills, setSkills] = useState<Skill[]>([])
  const [matrix, setMatrix] = useState<SkillMatrix | null>(null)
  const [gaps, setGaps] = useState<SkillAssessment[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Filters
  const [categoryFilter, setCategoryFilter] = useState<SkillCategory | null>(null)
  const [searchQuery, setSearchQuery] = useState('')

  // Modals
  const [showSkillModal, setShowSkillModal] = useState(false)
  const [showAssessmentModal, setShowAssessmentModal] = useState(false)
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null)
  const [editingAssessment, setEditingAssessment] = useState<SkillAssessment | null>(null)

  // Form state
  const [skillForm, setSkillForm] = useState<CreateSkillDto>({
    name: '',
    description: '',
    category: 0
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

  useEscapeKey(() => {
    setShowSkillModal(false)
    setShowAssessmentModal(false)
  })

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      setError(null)
      const [skillsData, matrixData, gapsData, reportsData] = await Promise.all([
        skillsApi.getAll(false),
        skillAssessmentsApi.getMatrix(),
        skillAssessmentsApi.getGaps(),
        directReportsApi.getAll()
      ])
      setSkills(skillsData)

      // Filter matrix to show only direct reports
      const directReportIds = new Set(
        reportsData.filter(r => r.isDirect).map(r => r.id)
      )
      const filteredMatrix = {
        skills: matrixData.skills,
        directReports: matrixData.directReports.filter(dr =>
          directReportIds.has(dr.directReportId)
        )
      }
      setMatrix(filteredMatrix)

      // Filter gaps to show only direct reports
      const filteredGaps = gapsData.filter(gap =>
        directReportIds.has(gap.directReportId)
      )
      setGaps(filteredGaps)

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

  const skillsByCategoryData = useMemo(() => {
    const categories: Record<SkillCategory, number> = { 0: 0, 1: 0, 2: 0, 3: 0, 4: 0 }
    activeSkills.forEach(skill => {
      categories[skill.category]++
    })
    return Object.entries(categories)
      .filter(([_, count]) => count > 0)
      .map(([category, count]) => ({
        name: CATEGORY_NAMES[Number(category) as SkillCategory],
        value: count
      }))
  }, [activeSkills])

  const proficiencyDistributionData = useMemo(() => {
    const levels: Record<ProficiencyLevel, number> = { 0: 0, 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 }
    allAssessments.forEach(assessment => {
      levels[assessment.level]++
    })
    return Object.entries(levels)
      .filter(([level]) => Number(level) > 0) // Exclude "None"
      .map(([level, count]) => ({
        name: LEVEL_NAMES[Number(level) as ProficiencyLevel],
        value: count
      }))
  }, [allAssessments])

  // Handlers
  const handleCreateSkill = async () => {
    try {
      await skillsApi.create(skillForm)
      await loadData()
      setShowSkillModal(false)
      resetSkillForm()
    } catch (err) {
      console.error('Failed to create skill:', err)
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
    }
  }

  const handleDeleteSkill = async (id: string) => {
    if (!confirm('Are you sure you want to delete this skill?')) return
    try {
      await skillsApi.delete(id)
      await loadData()
    } catch (err) {
      console.error('Failed to delete skill:', err)
    }
  }

  const handleCreateAssessment = async () => {
    try {
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
    } catch (err) {
      console.error('Failed to create assessment:', err)
    }
  }

  const handleUpdateAssessment = async () => {
    if (!editingAssessment) return
    try {
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
    } catch (err) {
      console.error('Failed to update assessment:', err)
    }
  }

  const handleDeleteAssessment = async (id: string) => {
    if (!confirm('Are you sure you want to delete this assessment?')) return
    try {
      await skillAssessmentsApi.delete(id)
      await loadData()
    } catch (err) {
      console.error('Failed to delete assessment:', err)
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
    setSkillForm({ name: '', description: '', category: 0 })
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

  const openSkillModal = (skill?: Skill) => {
    if (skill) {
      setEditingSkill(skill)
      setSkillForm({
        name: skill.name,
        description: skill.description,
        category: skill.category
      })
    } else {
      resetSkillForm()
      setEditingSkill(null)
    }
    setShowSkillModal(true)
  }

  const filteredSkills = useMemo(() => {
    return skills.filter(skill => {
      const matchesCategory = categoryFilter === null || skill.category === categoryFilter
      const matchesSearch = searchQuery === '' ||
        skill.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        skill.description.toLowerCase().includes(searchQuery.toLowerCase())
      return matchesCategory && matchesSearch
    })
  }, [skills, categoryFilter, searchQuery])

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
          onChange={(e) => setCategoryFilter(e.target.value === '' ? null : Number(e.target.value) as SkillCategory)}
          className="px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
        >
          <option value="">All Categories</option>
          {Object.entries(CATEGORY_NAMES).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
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
              value={skills.length}
              subtitle={`${activeSkills.length} active`}
              icon={<Award className="w-6 h-6" />}
              color="blue"
            />
            <StatCard
              title="Assessments"
              value={allAssessments.length}
              subtitle={`Across ${matrix?.directReports.length || 0} direct reports`}
              icon={<Users className="w-6 h-6" />}
              color="purple"
            />
            <StatCard
              title="Skill Gaps"
              value={gaps.length}
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
            <SkillsHeatmap
              matrix={matrix}
              onCellClick={handleCellClick}
              categoryFilter={categoryFilter}
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
                            {CATEGORY_NAMES[gap.skillCategory]}
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
                  value={skillForm.category}
                  onChange={(e) => setSkillForm({ ...skillForm, category: Number(e.target.value) as SkillCategory })}
                  className="w-full px-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
                >
                  {Object.entries(CATEGORY_NAMES).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
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
    </div>
  )
}
