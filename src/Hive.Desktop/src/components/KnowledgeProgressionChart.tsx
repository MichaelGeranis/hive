import { useMemo } from 'react'
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend
} from 'recharts'
import type { KnowledgeProgressionEntry } from '../types'

interface KnowledgeProgressionChartProps {
  data: KnowledgeProgressionEntry[]
  groupBy: 'project' | 'directReport'
}

interface ChartDataPoint {
  timestamp: string
  displayDate: string
  [key: string]: number | string | undefined
}

const COLORS = [
  '#f59e0b', // amber
  '#10b981', // emerald
  '#3b82f6', // blue
  '#8b5cf6', // violet
  '#ec4899', // pink
  '#06b6d4', // cyan
  '#f97316', // orange
  '#84cc16', // lime
]

export default function KnowledgeProgressionChart({
  data,
  groupBy
}: KnowledgeProgressionChartProps) {
  const { chartData, entities } = useMemo(() => {
    if (data.length === 0) {
      return { chartData: [], entities: [] }
    }

    // Group by the specified dimension
    const entityKey = groupBy === 'project' ? 'projectId' : 'directReportId'
    const entityNameKey = groupBy === 'project' ? 'projectName' : 'directReportName'

    // Get unique entities
    const uniqueEntities = [...new Map(
      data.map(d => [d[entityKey], d[entityNameKey]])
    ).entries()].map(([id, name]) => ({ id, name: name as string }))

    // Build chart data: each entry is a data point with levels and points for each entity
    // We need to track the "current" level and points as we process entries chronologically
    const levelTracker: Record<string, number> = {}
    const pointsTracker: Record<string, number | undefined> = {}

    // Sort by timestamp
    const sortedData = [...data].sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    )

    // Build data points
    const points: ChartDataPoint[] = []

    for (const entry of sortedData) {
      const entityName = entry[entityNameKey] as string

      // Update trackers with new level and points
      // Only update level if this entry has level data
      if (entry.newLevel !== undefined && entry.newLevel !== null) {
        levelTracker[entityName] = entry.newLevel
      }
      // Update points tracker
      if (entry.totalPoints !== undefined && entry.totalPoints !== null) {
        pointsTracker[entityName + '_points'] = entry.totalPoints
      }

      // Create data point
      const timestamp = new Date(entry.timestamp)
      const displayDate = timestamp.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: '2-digit'
      })

      const point: ChartDataPoint = {
        timestamp: entry.timestamp,
        displayDate,
        ...levelTracker,
        ...pointsTracker
      }

      points.push(point)
    }

    return { chartData: points, entities: uniqueEntities }
  }, [data, groupBy])

  if (data.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
        No progression data available. Add manual points or change knowledge levels to track progression.
      </div>
    )
  }

  return (
    <div className="h-80">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart
          data={chartData}
          margin={{ top: 5, right: 50, left: 20, bottom: 5 }}
        >
          <CartesianGrid strokeDasharray="3 3" className="stroke-slate-200 dark:stroke-slate-700" />
          <XAxis
            dataKey="displayDate"
            tick={{ fontSize: 11, fill: 'currentColor' }}
            className="text-slate-600 dark:text-slate-400"
          />
          {/* Left Y-axis for Knowledge Levels */}
          <YAxis
            yAxisId="level"
            domain={[0, 5]}
            ticks={[1, 2, 3, 4, 5]}
            tick={{ fontSize: 11, fill: 'currentColor' }}
            className="text-slate-600 dark:text-slate-400"
            label={{
              value: 'Knowledge Level',
              angle: -90,
              position: 'insideLeft',
              style: { textAnchor: 'middle', fontSize: 12 }
            }}
          />
          {/* Right Y-axis for Points */}
          <YAxis
            yAxisId="points"
            orientation="right"
            tick={{ fontSize: 11, fill: 'currentColor' }}
            className="text-slate-600 dark:text-slate-400"
            label={{
              value: 'Total Points',
              angle: 90,
              position: 'insideRight',
              style: { textAnchor: 'middle', fontSize: 12 }
            }}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: 'var(--tooltip-bg, #fff)',
              border: '1px solid var(--tooltip-border, #e2e8f0)',
              borderRadius: '8px'
            }}
            formatter={(value: number, name: string) => {
              if (name.endsWith('(Points)')) {
                return [`${value} pts`, name]
              }
              const levelLabel = getLevelLabel(value)
              return [`${value} - ${levelLabel}`, name]
            }}
            labelFormatter={(label) => `Date: ${label}`}
          />
          <Legend />
          {/* Knowledge Level Lines */}
          {entities.map((entity, index) => (
            <Line
              key={entity.id}
              yAxisId="level"
              type="monotone"
              dataKey={entity.name}
              name={`${entity.name} (Level)`}
              stroke={COLORS[index % COLORS.length]}
              strokeWidth={2}
              dot={{ r: 4 }}
              activeDot={{ r: 6 }}
              connectNulls
            />
          ))}
          {/* Points Lines */}
          {entities.map((entity, index) => (
            <Line
              key={`${entity.id}_points`}
              yAxisId="points"
              type="monotone"
              dataKey={`${entity.name}_points`}
              name={`${entity.name} (Points)`}
              stroke={COLORS[index % COLORS.length]}
              strokeWidth={1}
              strokeDasharray="5 5"
              dot={{ r: 3 }}
              activeDot={{ r: 5 }}
              connectNulls
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}

function getLevelLabel(level: number): string {
  switch (level) {
    case 1: return 'No clue'
    case 2: return 'Limited'
    case 3: return 'Moderate'
    case 4: return 'Good'
    case 5: return 'Confident'
    default: return 'Unknown'
  }
}
