import { useMemo } from 'react'
import {
  RadarChart,
  Radar,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  ResponsiveContainer,
  Tooltip,
  Legend
} from 'recharts'
import type { SkillAssessment } from '../types'

interface RadarData {
  category: string
  current: number
  target: number
}

interface SkillRadarChartProps {
  assessments: SkillAssessment[]
  teamMemberName: string
  showTarget?: boolean
}

export function SkillRadarChart({ assessments, teamMemberName, showTarget = true }: SkillRadarChartProps) {
  // Calculate average proficiency per category
  const data = useMemo((): RadarData[] => {
    // Group assessments by category
    const categoryMap = new Map<string, { name: string; assessments: SkillAssessment[] }>()

    assessments.forEach(assessment => {
      const categoryId = assessment.skillCategoryId
      if (!categoryMap.has(categoryId)) {
        categoryMap.set(categoryId, {
          name: assessment.skillCategoryName,
          assessments: []
        })
      }
      categoryMap.get(categoryId)!.assessments.push(assessment)
    })

    // Convert to radar data, sorted by category name
    return Array.from(categoryMap.entries())
      .sort(([, a], [, b]) => a.name.localeCompare(b.name))
      .map(([, { name, assessments: categoryAssessments }]) => {
        const currentAvg = categoryAssessments.length > 0
          ? categoryAssessments.reduce((sum, a) => sum + a.level, 0) / categoryAssessments.length
          : 0

        const targetAvg = categoryAssessments.length > 0
          ? categoryAssessments.reduce((sum, a) => sum + (a.targetLevel || 0), 0) / categoryAssessments.length
          : 0

        return {
          category: name,
          current: Math.round(currentAvg * 10) / 10,
          target: Math.round(targetAvg * 10) / 10
        }
      })
  }, [assessments])

  const hasData = data.some(d => d.current > 0 || d.target > 0)

  if (!hasData) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
        No skill assessments available for {teamMemberName}
      </div>
    )
  }

  return (
    <div>
      <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100 mb-4 text-center">
        {teamMemberName}
      </h3>
      <ResponsiveContainer width="100%" height={300}>
        <RadarChart data={data}>
          <PolarGrid stroke="#94a3b8" className="dark:stroke-slate-600" />
          <PolarAngleAxis
            dataKey="category"
            tick={{ fill: '#64748b', fontSize: 12 }}
            className="dark:fill-slate-400"
          />
          <PolarRadiusAxis
            domain={[0, 5]}
            tick={{ fill: '#94a3b8', fontSize: 11 }}
            tickCount={6}
          />
          <Radar
            name="Current"
            dataKey="current"
            stroke="#3b82f6"
            fill="#3b82f6"
            fillOpacity={0.5}
            strokeWidth={2}
          />
          {showTarget && (
            <Radar
              name="Target"
              dataKey="target"
              stroke="#f59e0b"
              fill="#f59e0b"
              fillOpacity={0.3}
              strokeWidth={2}
              strokeDasharray="5 5"
            />
          )}
          <Tooltip
            contentStyle={{
              backgroundColor: '#1e293b',
              border: '1px solid #475569',
              borderRadius: '0.375rem',
              color: '#f1f5f9'
            }}
            formatter={(value: number) => value.toFixed(1)}
          />
          <Legend
            wrapperStyle={{
              paddingTop: '1rem',
              fontSize: '0.875rem'
            }}
          />
        </RadarChart>
      </ResponsiveContainer>
    </div>
  )
}
