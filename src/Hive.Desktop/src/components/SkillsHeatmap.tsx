import { useMemo } from 'react'
import type { SkillMatrix, Skill, ProficiencyLevel, SkillCategory } from '../types'

export type MatrixSortBy = 'name' | 'avgProficiency' | 'gaps'
export type MatrixSortOrder = 'asc' | 'desc'

interface SkillsHeatmapProps {
  matrix: SkillMatrix
  onCellClick?: (directReportId: string, skillId: string, currentLevel?: ProficiencyLevel) => void
  categoryFilter?: SkillCategory | null
  searchQuery?: string
  teamMemberFilter?: string[]
  sortBy?: MatrixSortBy
  sortOrder?: MatrixSortOrder
}

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

const getProficiencyColor = (level: ProficiencyLevel): string => {
  const colors: Record<ProficiencyLevel, string> = {
    0: 'bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-400',
    1: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    2: 'bg-blue-200 text-blue-800 dark:bg-blue-800 dark:text-blue-200',
    3: 'bg-blue-400 text-white dark:bg-blue-600',
    4: 'bg-green-500 text-white dark:bg-green-600',
    5: 'bg-green-600 text-white dark:bg-green-700'
  }
  return colors[level]
}

export function SkillsHeatmap({
  matrix,
  onCellClick,
  categoryFilter,
  searchQuery = '',
  teamMemberFilter = [],
  sortBy = 'name',
  sortOrder = 'asc'
}: SkillsHeatmapProps) {
  // Filter skills by category and search query
  const filteredSkills = useMemo(() => {
    let skills = matrix.skills.filter(s => s.isActive)

    if (categoryFilter !== null && categoryFilter !== undefined) {
      skills = skills.filter(s => s.category === categoryFilter)
    }

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase().trim()
      skills = skills.filter(s =>
        s.name.toLowerCase().includes(query) ||
        s.description.toLowerCase().includes(query)
      )
    }

    return skills
  }, [matrix.skills, categoryFilter, searchQuery])

  // Group skills by category for headers
  const groupedSkills = useMemo(() => {
    const groups: Map<SkillCategory, Skill[]> = new Map()
    filteredSkills.forEach(skill => {
      if (!groups.has(skill.category)) {
        groups.set(skill.category, [])
      }
      groups.get(skill.category)!.push(skill)
    })
    return Array.from(groups.entries()).sort(([a], [b]) => a - b)
  }, [filteredSkills])

  // Filter and sort team members
  const sortedDirectReports = useMemo(() => {
    // First filter by team member selection
    let reports = [...matrix.directReports]
    if (teamMemberFilter.length > 0) {
      reports = reports.filter(dr => teamMemberFilter.includes(dr.directReportId))
    }

    // Calculate metrics for sorting
    const reportsWithMetrics = reports.map(dr => {
      const assessments = dr.assessments.filter(a =>
        filteredSkills.some(s => s.id === a.skillId)
      )
      const totalLevels = assessments.reduce((sum, a) => sum + a.level, 0)
      const avgProficiency = assessments.length > 0 ? totalLevels / assessments.length : 0
      const gapsCount = assessments.filter(a =>
        a.targetLevel !== undefined && a.level < a.targetLevel
      ).length

      return {
        ...dr,
        avgProficiency,
        gapsCount
      }
    })

    // Sort based on selected option
    reportsWithMetrics.sort((a, b) => {
      let comparison = 0
      switch (sortBy) {
        case 'name':
          comparison = a.directReportName.localeCompare(b.directReportName)
          break
        case 'avgProficiency':
          comparison = b.avgProficiency - a.avgProficiency // Higher proficiency first
          break
        case 'gaps':
          comparison = b.gapsCount - a.gapsCount // More gaps first
          break
      }
      return sortOrder === 'desc' ? -comparison : comparison
    })

    return reportsWithMetrics
  }, [matrix.directReports, teamMemberFilter, sortBy, sortOrder, filteredSkills])

  if (filteredSkills.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
        No skills found. {categoryFilter !== null && categoryFilter !== undefined && 'Try changing the category filter or '}
        <span className="text-amber-500 ml-1">add skills in the Manage tab</span>.
      </div>
    )
  }

  if (sortedDirectReports.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
        No direct reports found. Add direct reports to see their skills matrix.
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full border-collapse">
        <thead>
          {/* Category headers */}
          <tr className="bg-slate-50 dark:bg-slate-800">
            <th className="sticky left-0 z-20 bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-600 px-4 py-2 text-left text-sm font-semibold text-slate-900 dark:text-slate-100">
              Direct Report
            </th>
            {groupedSkills.map(([category, skills]) => (
              <th
                key={category}
                colSpan={skills.length}
                className="border border-slate-300 dark:border-slate-600 px-4 py-2 text-center text-sm font-semibold text-slate-900 dark:text-slate-100"
              >
                {CATEGORY_NAMES[category]}
              </th>
            ))}
          </tr>
          {/* Skill names */}
          <tr className="bg-slate-100 dark:bg-slate-700">
            <th className="sticky left-0 z-20 bg-slate-100 dark:bg-slate-700 border border-slate-300 dark:border-slate-600 px-4 py-2"></th>
            {groupedSkills.map(([_, skills]) =>
              skills.map(skill => (
                <th
                  key={skill.id}
                  className="border border-slate-300 dark:border-slate-600 px-2 py-2 text-xs font-medium text-slate-700 dark:text-slate-300 min-w-[80px] max-w-[120px]"
                  title={skill.description}
                >
                  <div className="truncate">{skill.name}</div>
                </th>
              ))
            )}
          </tr>
        </thead>
        <tbody>
          {sortedDirectReports.map(directReport => {
            // Create a map of skillId -> assessment for quick lookup
            const assessmentMap = new Map(
              directReport.assessments.map(a => [a.skillId, a])
            )

            return (
              <tr key={directReport.directReportId} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="sticky left-0 z-10 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 px-4 py-2 text-sm font-medium text-slate-900 dark:text-slate-100 whitespace-nowrap">
                  {directReport.directReportName}
                </td>
                {groupedSkills.map(([_, skills]) =>
                  skills.map(skill => {
                    const assessment = assessmentMap.get(skill.id)
                    const level = assessment?.level ?? 0
                    const targetLevel = assessment?.targetLevel
                    const hasGap = targetLevel !== undefined && level < targetLevel

                    return (
                      <td
                        key={skill.id}
                        className={`
                          border border-slate-300 dark:border-slate-600 px-2 py-2 text-center text-xs font-semibold
                          ${getProficiencyColor(level)}
                          ${onCellClick ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''}
                          ${hasGap ? 'ring-2 ring-red-400 ring-inset' : ''}
                        `}
                        onClick={() => onCellClick?.(directReport.directReportId, skill.id, level)}
                        title={`${LEVEL_NAMES[level]}${targetLevel !== undefined ? ` (Target: ${LEVEL_NAMES[targetLevel]})` : ''}${assessment?.notes ? `\n\n${assessment.notes}` : ''}`}
                      >
                        {level > 0 ? (
                          <div className="flex flex-col items-center">
                            <span>{level}</span>
                            {targetLevel !== undefined && (
                              <span className="text-[10px] opacity-75">
                                →{targetLevel}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>
                    )
                  })
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
      <div className="mt-4 flex flex-wrap gap-4 text-xs text-slate-600 dark:text-slate-400">
        <div className="flex items-center gap-2">
          <span className="font-semibold">Legend:</span>
        </div>
        {Object.entries(LEVEL_NAMES).map(([level, name]) => (
          <div key={level} className="flex items-center gap-2">
            <div className={`w-8 h-5 rounded ${getProficiencyColor(Number(level) as ProficiencyLevel)}`}></div>
            <span>{name} ({level})</span>
          </div>
        ))}
        <div className="flex items-center gap-2 ml-4">
          <div className="w-8 h-5 rounded bg-blue-400 ring-2 ring-red-400 ring-inset"></div>
          <span>Gap (below target)</span>
        </div>
      </div>
    </div>
  )
}
