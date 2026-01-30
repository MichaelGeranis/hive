import { useState } from 'react'
import {
  Lightbulb,
  AlertTriangle,
  AlertCircle,
  Info,
  Calendar,
  GitBranch,
  Users,
  ChevronDown,
  ChevronRight,
  Target,
  BarChart3
} from 'lucide-react'
import { Card, CardHeader, CardContent } from './Card'
import type {
  PlanningInsights,
  PlanningInsight,
  InsightSeverity,
  InsightType
} from '../types/quarterlyPlanning'

interface InsightsSidebarProps {
  insights: PlanningInsights | null
}

export default function InsightsSidebar({ insights }: InsightsSidebarProps) {
  const [expandedGroups, setExpandedGroups] = useState<Set<InsightType>>(new Set([0, 1, 2, 3]))

  if (!insights) {
    return (
      <Card>
        <CardHeader>
          <h3 className="font-semibold text-slate-800 dark:text-white flex items-center gap-2">
            <Lightbulb className="w-4 h-4" />
            Insights
          </h3>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center h-32 text-slate-500 dark:text-slate-400">
            <Lightbulb className="w-8 h-8 mb-2 opacity-50" />
            <p className="text-sm">Loading insights...</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  const toggleGroup = (type: InsightType) => {
    const newExpanded = new Set(expandedGroups)
    if (newExpanded.has(type)) {
      newExpanded.delete(type)
    } else {
      newExpanded.add(type)
    }
    setExpandedGroups(newExpanded)
  }

  const getSeverityIcon = (severity: InsightSeverity) => {
    switch (severity) {
      case 2: return <AlertCircle className="w-4 h-4 text-red-500" />
      case 1: return <AlertTriangle className="w-4 h-4 text-amber-500" />
      case 0: return <Info className="w-4 h-4 text-blue-500" />
      default: return <Info className="w-4 h-4 text-slate-500" />
    }
  }

  const getSeverityColor = (severity: InsightSeverity) => {
    switch (severity) {
      case 2: return 'border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-900/20'
      case 1: return 'border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-900/20'
      case 0: return 'border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-900/20'
      default: return 'border-slate-200 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/50'
    }
  }

  const getTypeIcon = (type: InsightType) => {
    switch (type) {
      case 0: return <Calendar className="w-4 h-4" /> // LeaveConflict
      case 1: return <GitBranch className="w-4 h-4" /> // DependencyRisk
      case 2: return <Users className="w-4 h-4" /> // Bottleneck
      case 3: return <AlertCircle className="w-4 h-4" /> // UnassignedWork
      default: return <Info className="w-4 h-4" />
    }
  }

  const getTypeLabel = (type: InsightType) => {
    switch (type) {
      case 0: return 'Leave Conflict'
      case 1: return 'Dependency Risk'
      case 2: return 'Bottleneck'
      case 3: return 'Unassigned Work'
      default: return 'Other'
    }
  }

  // Group insights by type
  const groupedInsights = insights.insights.reduce((acc, insight) => {
    if (!acc[insight.type]) acc[insight.type] = []
    acc[insight.type].push(insight)
    return acc
  }, {} as Record<InsightType, PlanningInsight[]>)

  return (
    <Card>
      <CardHeader>
        <h3 className="font-semibold text-slate-800 dark:text-white flex items-center gap-2">
          <Lightbulb className="w-4 h-4" />
          Insights
        </h3>

        {/* Summary Stats */}
        <div className="mt-3 space-y-2">
          {/* Initiatives Overview */}
          <div className="flex items-center justify-between text-sm">
            <span className="text-slate-600 dark:text-slate-400">Initiatives Allocated</span>
            <span className="font-medium text-slate-800 dark:text-white">
              {insights.allocatedInitiatives}/{insights.totalInitiatives}
            </span>
          </div>

          {/* Issue Counts */}
          <div className="flex gap-2 pt-2">
            {insights.criticalCount > 0 && (
              <div className="flex items-center gap-1 px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded text-xs">
                <AlertCircle className="w-3 h-3" />
                {insights.criticalCount} Critical
              </div>
            )}
            {insights.warningCount > 0 && (
              <div className="flex items-center gap-1 px-2 py-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 rounded text-xs">
                <AlertTriangle className="w-3 h-3" />
                {insights.warningCount} Warning
              </div>
            )}
            {insights.issueCount > 0 && insights.criticalCount === 0 && insights.warningCount === 0 && (
              <div className="flex items-center gap-1 px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded text-xs">
                <Info className="w-3 h-3" />
                {insights.issueCount} Info
              </div>
            )}
            {insights.issueCount === 0 && insights.criticalCount === 0 && insights.warningCount === 0 && (
              <div className="flex items-center gap-1 px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-xs">
                No issues detected
              </div>
            )}
          </div>
        </div>
      </CardHeader>

      <CardContent>
        {insights.insights.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-32 text-slate-500 dark:text-slate-400">
            <Target className="w-8 h-8 mb-2 opacity-50" />
            <p className="text-sm text-center">No issues detected!</p>
            <p className="text-xs mt-1">Your planning looks good.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {(Object.entries(groupedInsights) as [string, PlanningInsight[]][]).map(([typeStr, typeInsights]) => {
              const type = parseInt(typeStr) as InsightType
              const isExpanded = expandedGroups.has(type)

              return (
                <div key={type} className="border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden">
                  <button
                    onClick={() => toggleGroup(type)}
                    className="w-full flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-800/50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
                  >
                    <div className="flex items-center gap-2">
                      {getTypeIcon(type)}
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                        {getTypeLabel(type)}
                      </span>
                      <span className="text-xs px-1.5 py-0.5 bg-slate-200 dark:bg-slate-700 text-slate-600 dark:text-slate-400 rounded">
                        {typeInsights.length}
                      </span>
                    </div>
                    {isExpanded ? (
                      <ChevronDown className="w-4 h-4 text-slate-400" />
                    ) : (
                      <ChevronRight className="w-4 h-4 text-slate-400" />
                    )}
                  </button>

                  {isExpanded && (
                    <div className="p-2 space-y-2">
                      {typeInsights.map((insight, idx) => (
                        <div
                          key={idx}
                          className={`p-3 rounded border ${getSeverityColor(insight.severity)}`}
                        >
                          <div className="flex items-start gap-2">
                            {getSeverityIcon(insight.severity)}
                            <div className="flex-1 min-w-0">
                              <h5 className="text-sm font-medium text-slate-800 dark:text-white">
                                {insight.title}
                              </h5>
                              <p className="text-xs text-slate-600 dark:text-slate-400 mt-1">
                                {insight.message}
                              </p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}

        {/* Sprint Workload Summary */}
        {insights.sprintWorkloads && insights.sprintWorkloads.length > 0 && (
          <div className="mt-6">
            <h4 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3 flex items-center gap-2">
              <BarChart3 className="w-4 h-4" />
              Sprint Workload
            </h4>
            <div className="space-y-3">
              {insights.sprintWorkloads.map(sprint => (
                <div
                  key={sprint.sprintId}
                  className="border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden"
                >
                  <div className="p-3 bg-slate-50 dark:bg-slate-800/50">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                        {sprint.sprintName}
                      </span>
                      <div className="flex items-center gap-2">
                        <span className="text-xs px-2 py-0.5 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 rounded font-medium">
                          {sprint.totalSprintEffort} sprint{sprint.totalSprintEffort !== 1 ? 's' : ''} effort
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 mt-1 text-xs text-slate-500 dark:text-slate-400">
                      <span>{sprint.initiativeCount} initiative{sprint.initiativeCount !== 1 ? 's' : ''}</span>
                      <span>•</span>
                      <span>{sprint.teamMemberCount} team member{sprint.teamMemberCount !== 1 ? 's' : ''}</span>
                    </div>
                  </div>

                  {sprint.teamMemberWorkloads.length > 0 && (
                    <div className="p-2 space-y-2">
                      {sprint.teamMemberWorkloads.map(member => (
                        <div
                          key={member.directReportId}
                          className="p-2 bg-slate-50 dark:bg-slate-800/30 rounded"
                        >
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-xs font-medium text-slate-700 dark:text-slate-300">
                              {member.directReportName}
                            </span>
                            <span className="text-xs text-slate-500 dark:text-slate-400">
                              {member.sprintEffort} sprint{member.sprintEffort !== 1 ? 's' : ''} effort
                            </span>
                          </div>
                          <div className="flex flex-wrap gap-1">
                            {member.initiatives.map(initiative => (
                              <div
                                key={initiative.initiativeId}
                                className="flex items-center gap-1 px-1.5 py-0.5 rounded text-xs"
                                style={{
                                  backgroundColor: `${initiative.color}20`,
                                  borderLeft: `3px solid ${initiative.color}`
                                }}
                                title={`${initiative.initiativeName} (${initiative.tshirtSize})`}
                              >
                                <span className="truncate max-w-[100px] text-slate-700 dark:text-slate-300">
                                  {initiative.initiativeName}
                                </span>
                                <span className="text-slate-500 dark:text-slate-400 font-medium">
                                  {initiative.tshirtSize}
                                </span>
                              </div>
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Team Member Summaries */}
        {insights.teamMemberSummaries && insights.teamMemberSummaries.length > 0 && (
          <div className="mt-6">
            <h4 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3 flex items-center gap-2">
              <Users className="w-4 h-4" />
              Team Overview
            </h4>
            <div className="space-y-2">
              {insights.teamMemberSummaries.map(member => (
                <div
                  key={member.directReportId}
                  className="p-2 bg-slate-50 dark:bg-slate-800/50 rounded-lg"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-slate-700 dark:text-slate-300">
                      {member.name}
                    </span>
                    <span className="text-xs text-slate-500 dark:text-slate-400">
                      {member.initiativeCount} initiative{member.initiativeCount !== 1 ? 's' : ''}
                    </span>
                  </div>
                  {/* Mini sparkline of sprint allocations */}
                  <div className="flex gap-1 mt-1">
                    {member.sprintAllocations.map((sprint, idx) => (
                      <div
                        key={idx}
                        className={`flex-1 h-2 rounded ${
                          sprint.hasLeave
                            ? 'bg-slate-300 dark:bg-slate-600'
                            : sprint.allocationCount > 0
                            ? 'bg-amber-400'
                            : 'bg-slate-200 dark:bg-slate-700'
                        }`}
                        title={`${sprint.allocationCount} allocation${sprint.allocationCount !== 1 ? 's' : ''}${sprint.hasLeave ? ' (on leave)' : ''}`}
                      />
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
