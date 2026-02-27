import { useState, useEffect, useRef } from 'react'
import { BookOpen, ChevronRight, Home, TrendingUp, Calculator, Zap, GitBranch, Award, Users, BarChart3, Search, X, ArrowUp, ArrowDown, Calendar, Star } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'

type TutorialId = 'knowledge-matrix' | 'team' | 'dashboard' | 'quarterly-planning' | 'sprints-tasks' | 'leaves' | 'reviews'

interface Tutorial {
  id: TutorialId
  title: string
  icon: typeof BookOpen
  description: string
  category: string
}

const tutorials: Tutorial[] = [
  {
    id: 'dashboard',
    title: 'Dashboard',
    icon: BarChart3,
    description: 'Understand all metrics, widgets, and the sprint history filter',
    category: 'Analytics & Reporting'
  },
  {
    id: 'team',
    title: 'Team Management',
    icon: Users,
    description: 'Manage your team members, track tenure, and organize by department',
    category: 'People Management'
  },
  {
    id: 'knowledge-matrix',
    title: 'Knowledge Matrix',
    icon: Award,
    description: 'Learn how to track team knowledge across projects using levels and points',
    category: 'Team Development'
  },
  {
    id: 'quarterly-planning',
    title: 'Quarterly Planning',
    icon: TrendingUp,
    description: 'Plan quarterly initiatives, allocate team members, and track dependencies',
    category: 'Strategic Planning'
  },
  {
    id: 'sprints-tasks',
    title: 'Sprints & Tasks',
    icon: Zap,
    description: 'Manage sprints, tasks, story points, capacity planning, and Jira imports',
    category: 'Delivery Management'
  },
  {
    id: 'leaves',
    title: 'Leave Management',
    icon: Calendar,
    description: 'Track team leave, view the calendar, and manage public holidays',
    category: 'People Management'
  },
  {
    id: 'reviews',
    title: 'Performance Reviews',
    icon: Star,
    description: 'Create and manage performance reviews with ratings and feedback',
    category: 'People Management'
  }
]

export default function Tutorials() {
  const [selectedTutorial, setSelectedTutorial] = useState<TutorialId | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [searchQuery, setSearchQuery] = useState('')

  // Filter tutorials based on search query
  const filteredTutorials = tutorials.filter(tutorial => {
    if (!searchQuery.trim()) return true
    const query = searchQuery.toLowerCase()
    return (
      tutorial.title.toLowerCase().includes(query) ||
      tutorial.description.toLowerCase().includes(query) ||
      tutorial.category.toLowerCase().includes(query)
    )
  })

  if (selectedTutorial) {
    return <TutorialContent tutorialId={selectedTutorial} onBack={() => setSelectedTutorial(null)} />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Tutorials</h1>
        <p className="text-slate-500 dark:text-slate-400">
          In-depth guides explaining how each feature works and the calculations behind the scenes
        </p>
      </div>

      {/* Search Bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search tutorials... (press Enter)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') setSearchQuery(searchInput) }}
          className="w-full pl-10 pr-10 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
        />
        {searchInput && (
          <button
            onClick={() => { setSearchInput(''); setSearchQuery('') }}
            className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Tutorial Cards */}
      {filteredTutorials.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500 dark:text-slate-400">
              No tutorials match your search "{searchQuery}"
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredTutorials.map((tutorial) => {
            const Icon = tutorial.icon
            return (
              <div
                key={tutorial.id}
                className="cursor-pointer hover:shadow-lg transition-shadow"
                onClick={() => setSelectedTutorial(tutorial.id)}
              >
                <Card>
                  <CardContent>
                  <div className="flex items-start gap-4">
                    <div className="p-3 bg-amber-100 dark:bg-amber-900/30 rounded-lg">
                      <Icon className="w-6 h-6 text-amber-600 dark:text-amber-400" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">
                        {tutorial.title}
                      </h3>
                      <p className="text-xs text-amber-600 dark:text-amber-400 mb-2">
                        {tutorial.category}
                      </p>
                      <p className="text-sm text-slate-600 dark:text-slate-400">
                        {tutorial.description}
                      </p>
                    </div>
                    <ChevronRight className="w-5 h-5 text-slate-400 flex-shrink-0" />
                  </div>
                </CardContent>
                </Card>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

interface TutorialContentProps {
  tutorialId: TutorialId
  onBack: () => void
}

function TutorialContent({ tutorialId, onBack }: TutorialContentProps) {
  const [tutorialSearchQuery, setTutorialSearchQuery] = useState('')
  const [currentMatchIndex, setCurrentMatchIndex] = useState(0)
  const [totalMatches, setTotalMatches] = useState(0)
  const contentRef = useRef<HTMLDivElement>(null)

  // Clear highlights and search when tutorial changes
  useEffect(() => {
    setTutorialSearchQuery('')
    setCurrentMatchIndex(0)
    setTotalMatches(0)
  }, [tutorialId])

  // Perform search and highlight
  useEffect(() => {
    if (!contentRef.current) return

    // Remove existing highlights
    const existingHighlights = contentRef.current.querySelectorAll('.tutorial-search-highlight')
    existingHighlights.forEach(el => {
      const parent = el.parentNode
      if (parent) {
        parent.replaceChild(document.createTextNode(el.textContent || ''), el)
        parent.normalize()
      }
    })

    if (!tutorialSearchQuery.trim()) {
      setTotalMatches(0)
      setCurrentMatchIndex(0)
      return
    }

    // Find and highlight matches
    const walker = document.createTreeWalker(
      contentRef.current,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: (node) => {
          // Skip script and style elements
          const parent = node.parentElement
          if (!parent || parent.tagName === 'SCRIPT' || parent.tagName === 'STYLE') {
            return NodeFilter.FILTER_REJECT
          }
          // Skip if already highlighted
          if (parent.classList.contains('tutorial-search-highlight')) {
            return NodeFilter.FILTER_REJECT
          }
          return NodeFilter.FILTER_ACCEPT
        }
      }
    )

    const textNodes: Text[] = []
    let node: Node | null
    while ((node = walker.nextNode())) {
      textNodes.push(node as Text)
    }

    const query = tutorialSearchQuery.toLowerCase()
    const highlights: HTMLElement[] = []

    textNodes.forEach(textNode => {
      const text = textNode.textContent || ''
      const lowerText = text.toLowerCase()
      let lastIndex = 0
      const indices: number[] = []

      let index = lowerText.indexOf(query, lastIndex)
      while (index !== -1) {
        indices.push(index)
        lastIndex = index + query.length
        index = lowerText.indexOf(query, lastIndex)
      }

      if (indices.length > 0) {
        const parent = textNode.parentNode
        if (!parent) return

        const fragment = document.createDocumentFragment()
        let currentPos = 0

        indices.forEach(matchIndex => {
          // Add text before match
          if (matchIndex > currentPos) {
            fragment.appendChild(document.createTextNode(text.substring(currentPos, matchIndex)))
          }

          // Add highlighted match
          const mark = document.createElement('mark')
          mark.className = 'tutorial-search-highlight'
          mark.style.backgroundColor = '#fef08a' // yellow-200
          mark.style.padding = '2px'
          mark.style.borderRadius = '2px'
          mark.textContent = text.substring(matchIndex, matchIndex + query.length)
          fragment.appendChild(mark)
          highlights.push(mark)

          currentPos = matchIndex + query.length
        })

        // Add remaining text
        if (currentPos < text.length) {
          fragment.appendChild(document.createTextNode(text.substring(currentPos)))
        }

        parent.replaceChild(fragment, textNode)
      }
    })

    setTotalMatches(highlights.length)
    if (highlights.length > 0) {
      setCurrentMatchIndex(0)
      // Highlight first match
      highlights[0].style.backgroundColor = '#fbbf24' // amber-400
      highlights[0].style.fontWeight = 'bold'
      highlights[0].scrollIntoView({ behavior: 'smooth', block: 'center' })
    }
  }, [tutorialSearchQuery])

  // Navigate to next/previous match
  const navigateMatch = (direction: 'next' | 'prev') => {
    if (!contentRef.current || totalMatches === 0) return

    const highlights = Array.from(contentRef.current.querySelectorAll('.tutorial-search-highlight')) as HTMLElement[]
    if (highlights.length === 0) return

    // Reset current highlight to default
    highlights[currentMatchIndex].style.backgroundColor = '#fef08a' // yellow-200
    highlights[currentMatchIndex].style.fontWeight = 'normal'

    // Calculate new index
    let newIndex = currentMatchIndex
    if (direction === 'next') {
      newIndex = (currentMatchIndex + 1) % totalMatches
    } else {
      newIndex = (currentMatchIndex - 1 + totalMatches) % totalMatches
    }

    // Highlight new match
    highlights[newIndex].style.backgroundColor = '#fbbf24' // amber-400
    highlights[newIndex].style.fontWeight = 'bold'
    highlights[newIndex].scrollIntoView({ behavior: 'smooth', block: 'center' })

    setCurrentMatchIndex(newIndex)
  }

  const renderTutorial = () => {
    if (tutorialId === 'dashboard') {
      return <DashboardTutorial onBack={onBack} />
    }
    if (tutorialId === 'team') {
      return <TeamTutorial onBack={onBack} />
    }
    if (tutorialId === 'knowledge-matrix') {
      return <KnowledgeMatrixTutorial onBack={onBack} />
    }
    if (tutorialId === 'quarterly-planning') {
      return <QuarterlyPlanningTutorial onBack={onBack} />
    }
    if (tutorialId === 'sprints-tasks') {
      return <SprintsTasksTutorial onBack={onBack} />
    }
    if (tutorialId === 'leaves') {
      return <LeavesTutorial onBack={onBack} />
    }
    if (tutorialId === 'reviews') {
      return <ReviewsTutorial onBack={onBack} />
    }
    return null
  }

  return (
    <div className="space-y-4">
      {/* Search Bar */}
      <div className="sticky top-0 z-10 bg-white dark:bg-slate-900 pb-4">
        <div className="flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              placeholder="Search within tutorial..."
              value={tutorialSearchQuery}
              onChange={(e) => setTutorialSearchQuery(e.target.value)}
              className="w-full pl-10 pr-10 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500 text-sm"
            />
            {tutorialSearchQuery && (
              <button
                onClick={() => setTutorialSearchQuery('')}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>
          {totalMatches > 0 && (
            <>
              <div className="flex items-center gap-2 px-3 py-2 bg-slate-100 dark:bg-slate-800 rounded-lg text-sm text-slate-600 dark:text-slate-300">
                <span>{currentMatchIndex + 1} / {totalMatches}</span>
              </div>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => navigateMatch('prev')}
                  className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg text-slate-600 dark:text-slate-300"
                  title="Previous match"
                >
                  <ArrowUp className="w-4 h-4" />
                </button>
                <button
                  onClick={() => navigateMatch('next')}
                  className="p-2 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg text-slate-600 dark:text-slate-300"
                  title="Next match"
                >
                  <ArrowDown className="w-4 h-4" />
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Tutorial Content */}
      <div ref={contentRef}>
        {renderTutorial()}
      </div>
    </div>
  )
}

interface TutorialProps {
  onBack: () => void
}

function DashboardTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Dashboard Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Your central hub for team metrics, analytics, and insights
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Sprint History Filter',
              'Top Stats',
              'Sprint & Tasks Overview',
              'Widget Customization',
              'Task Analytics',
              'Capacity Analysis',
              'Team Performance Metrics',
              'Project & Member Distribution',
              'Support & Maintenance Tracking',
              'Team Insights',
              'Export Functionality'
            ].map((section, index) => (
              <button
                key={index}
                onClick={() => {
                  const element = document.getElementById(`dashboard-section-${index + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="dashboard-section-1">
        <Card>
          <CardHeader title="1. Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Dashboard is your command center, providing a real-time snapshot of your team's performance,
                capacity, and work distribution. It aggregates data from multiple sources to give you actionable insights.
              </p>
              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What You'll See</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li>Key team metrics (members, projects, warnings, action items)</li>
                  <li>Task distribution across types, projects, and team members</li>
                  <li>Sprint capacity planning and utilization</li>
                  <li>Team velocity and estimation accuracy trends</li>
                  <li>Support and maintenance work tracking</li>
                  <li>Team sentiment analysis and knowledge gaps</li>
                </ul>
              </div>
              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Pro Tip:</strong> The Dashboard is fully customizable! You can hide widgets you don't need
                and use the Sprint History filter to focus on specific time periods.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Sprint History Filter */}
      <div id="dashboard-section-2">
        <Card>
          <CardHeader title="2. Sprint History Filter" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Sprint History dropdown filter lets you limit dashboard data to the last N sprints.
                This helps you focus on recent trends rather than all-time data.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How It Works</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm">
                    <li><strong>All Sprints</strong> (default) - Shows all historical data</li>
                    <li><strong>Last 1 Sprint</strong> - Shows current sprint only</li>
                    <li><strong>Last 3, 5, 10 Sprints</strong> - Shows recent sprint history</li>
                  </ul>
                  <p className="text-sm mt-2 text-amber-600 dark:text-amber-400">
                    The filter applies to most widgets but not all. See below for details.
                  </p>
                </div>

                <div>
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Widgets Affected by Sprint Filter</h4>
                  <p className="text-sm mb-2">These widgets update when you change the sprint filter:</p>
                  <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                    <ul className="list-disc list-inside space-y-1 text-sm">
                      <li>Warnings stat (depends on filtered capacity/velocity)</li>
                      <li>Tasks Distribution (all variants: count, story points, hours)</li>
                      <li>Members Workload</li>
                      <li>Support & Maintenance Hours</li>
                      <li>Capacity Analysis</li>
                      <li>Team Velocity chart</li>
                      <li>Estimation Accuracy chart</li>
                    </ul>
                  </div>
                </div>

                <div>
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Widgets NOT Affected (All-Time Data)</h4>
                  <p className="text-sm mb-2">These widgets always show all-time data and display an "All sprints" badge:</p>
                  <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
                    <ul className="list-disc list-inside space-y-1 text-sm">
                      <li>Team Members stat</li>
                      <li>Projects stat</li>
                      <li>1:1 Action Items stat</li>
                      <li>TODOs stat</li>
                      <li>Projects Distribution pie chart</li>
                      <li>Members by Project</li>
                      <li>Team Sentiment</li>
                      <li>Knowledge Level Suggestions</li>
                    </ul>
                  </div>
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Start with "All Sprints" to see the big picture, then narrow down
                to "Last 3 Sprints" to focus on recent trends when planning upcoming work.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Top Stats */}
      <div id="dashboard-section-3">
        <Card>
          <CardHeader title="3. Top Stats" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The top row displays key metrics at a glance. Click on any stat card to drill into details.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Team Members</h4>
                  <p className="text-sm">
                    Total count of direct reports in the system. Click to navigate to the Team page.
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">Badge: All sprints</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Projects</h4>
                  <p className="text-sm">
                    Total count of all projects (Planning, Active, On Hold, Completed, Cancelled).
                    Click to navigate to the Projects page.
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">Badge: All sprints</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Warnings</h4>
                  <p className="text-sm mb-2">
                    Count of potential issues detected across capacity, workload, knowledge, and engagement.
                    Click to see detailed warnings.
                  </p>
                  <p className="text-sm font-semibold mt-2 mb-1">Warning Types:</p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-xs">
                    <li><strong>Capacity Warnings:</strong> Sprints over-allocated or under-allocated</li>
                    <li><strong>Workload Warnings:</strong> Team members overloaded or unassigned</li>
                    <li><strong>Knowledge Silos:</strong> Projects with only one knowledgeable person</li>
                    <li><strong>Unengaged Members:</strong> Team members with no active tasks (and not on leave)</li>
                  </ul>
                  <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">Respects sprint filter</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">1:1 Action Items</h4>
                  <p className="text-sm">
                    Count of open action items from 1:1 meetings (status: Open or In Progress).
                    Click to see the full list with assignees and due dates.
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">Badge: All sprints</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">TODOs</h4>
                  <p className="text-sm">
                    Count of pending manager notes marked as priority/TODO.
                    Click to see the list and mark items as complete.
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">Badge: All sprints</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: Sprint & Tasks Overview */}
      <div id="dashboard-section-4">
        <Card>
          <CardHeader title="4. Sprint & Tasks Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                This full-width widget sits below the stat cards and gives you a quick snapshot of the current sprint's progress.
                Click anywhere on it to navigate to the Tasks page.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What It Shows</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Sprint Name:</strong> The name of the current sprint (e.g., "LP_1Q26_S3") with a
                    "Current Sprint" badge.
                  </li>
                  <li>
                    <strong>Story Points Progress:</strong> Completed new SP out of total new SP, shown as a fraction
                    (e.g., "12/20 SP 60%"). Carried-over points are excluded from both the numerator and denominator
                    to give you an accurate picture of new work progress.
                  </li>
                  <li>
                    <strong>Tasks Progress:</strong> Done tasks out of total tasks in the current sprint, shown as a
                    fraction with percentage (e.g., "8/15 Tasks 53%").
                  </li>
                </ul>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How Counts Are Calculated</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Parent tasks excluded:</strong> Only child/leaf tasks are counted. Parent tasks (epics, stories
                    with sub-tasks) are excluded from both the task count and story point totals to avoid double-counting.
                  </li>
                  <li>
                    <strong>Latest sprint assignment:</strong> If a task belongs to multiple sprints (e.g., "Sprint 5, Sprint 6"),
                    it is counted only under its latest sprint. This prevents the same task from inflating counts across sprints.
                  </li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Warning Indicators</h4>
                <p className="text-sm mb-2">
                  The widget may display one or more warning pills on the right side:
                </p>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Carried Over SP (blue):</strong> Shows how many story points were carried over from previous
                    sprints (e.g., "+5 SP carried"). This helps you track technical debt or unfinished work.
                  </li>
                  <li>
                    <strong>Scope Creep (amber):</strong> Appears when the total SP in the sprint exceeds the originally
                    committed points (e.g., "+3 SP creep"). Signals that work was added after sprint planning.
                  </li>
                  <li>
                    <strong>Unmatched Tasks (amber):</strong> Shows tasks that are not matched to any project
                    (e.g., "4 unmatched"). These tasks may need to be assigned to the correct project.
                  </li>
                </ul>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Note:</strong> This widget always shows data for the current sprint regardless of the Sprint
                History filter setting. It is not affected by the filter.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Widget Customization */}
      <div id="dashboard-section-5">
        <Card>
          <CardHeader title="5. Widget Customization" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Customize your dashboard by showing or hiding widgets based on what's most relevant to you.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How to Customize</h4>
                  <ol className="list-decimal list-inside space-y-2 text-sm">
                    <li>Click the "Customize Widgets" button (gear icon) in the top-right</li>
                    <li>Toggle widgets on/off using the eye icons</li>
                    <li>Click "Reset to Default" to restore all widgets</li>
                    <li>Close the modal - your preferences are saved automatically</li>
                  </ol>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 text-sm">
                  <strong>Persistence:</strong> Widget visibility preferences are saved in your browser's localStorage
                  and persist across sessions.
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Available Widgets</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm">
                    <li>Top Stats (Team Members, Projects, Warnings, Action Items, TODOs)</li>
                    <li>Projects Distribution</li>
                    <li>Members by Project</li>
                    <li>Tasks Distribution (3 variants: count, story points, hours)</li>
                    <li>Tasks Distribution by Label</li>
                    <li>Support Distribution</li>
                    <li>Team Sentiment</li>
                    <li>Capacity Analysis</li>
                    <li>Knowledge Level Suggestions</li>
                    <li>Estimation Accuracy</li>
                    <li>Team Velocity</li>
                    <li>Members Workload</li>
                  </ul>
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Tip:</strong> Hide widgets you don't actively use to reduce clutter and improve dashboard load times.
                For example, if you don't track story points, hide the "Tasks Distribution (SP)" widget.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Task Analytics */}
      <div id="dashboard-section-6">
        <Card>
          <CardHeader title="6. Task Analytics" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Task analytics widgets show how work is distributed across different dimensions.
                All of these respect the sprint history filter.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Tasks Distribution (by Type)</h4>
                  <p className="text-sm mb-2">
                    Pie chart showing task breakdown by type (Story, Bug, Task, Spike, Support, etc.).
                    Color-coded for easy identification.
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Filtered by sprint history</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Tasks Distribution (Story Points)</h4>
                  <p className="text-sm mb-2">
                    Same as above but weighted by story points instead of task count.
                    Shows where effort is being spent.
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Filtered by sprint history</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Tasks Distribution (Hours)</h4>
                  <p className="text-sm mb-2">
                    Same breakdown weighted by estimated hours instead of story points.
                    Useful if you track time estimates.
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Filtered by sprint history</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Tasks Distribution by Label</h4>
                  <p className="text-sm mb-2">
                    Pie chart showing task distribution across labels (e.g., "backend", "frontend", "infra").
                    Helps identify work streams and technical areas.
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Filtered by sprint history</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Members Workload</h4>
                  <p className="text-sm mb-2">
                    Bar chart showing task count per team member, broken down by status (To Do, In Progress, Done).
                    Click a member's name to see their assigned projects.
                  </p>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Filtered by sprint history</p>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Understanding Task Counts:</strong> Tasks are counted based on their assignment to sprints
                within the selected history window. A task assigned to multiple sprints may be counted multiple times.
              </p>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Parent vs Child Story Points</h4>
                <p className="text-sm mb-2">
                  The system distinguishes between parent tasks and their child tasks when calculating story points:
                </p>
                <ul className="list-disc list-inside ml-4 space-y-2 text-sm">
                  <li>
                    <strong>Child Tasks Only:</strong> All task distribution widgets (by Type, Story Points, Hours, Labels)
                    count only child tasks. Parent tasks are excluded from these aggregations.
                  </li>
                  <li>
                    <strong>Parent Story Points:</strong> If a parent task has its own story points assigned
                    (acting as both a parent and a task), those points are <strong>not</strong> included in dashboard
                    task analytics. Only the child tasks contribute to these metrics.
                  </li>
                  <li>
                    <strong>Viewing Parent Metrics:</strong> To see parent-level aggregations (sum of all child story points),
                    visit the Parents page where both child totals and parent task story points are displayed separately.
                  </li>
                  <li>
                    <strong>Epic Parents:</strong> Parents with zero child tasks (marked as "Epic") represent planned work
                    and don't contribute to dashboard task analytics until child tasks are created.
                  </li>
                </ul>
                <p className="text-xs text-blue-600 dark:text-blue-400 mt-2">
                  Example: A parent "API Redesign" with 3 child tasks (5 SP, 3 SP, 2 SP) contributes 10 SP total
                  from children. If the parent itself has 1 SP assigned, that 1 SP is not counted in dashboard task analytics.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 7: Capacity Analysis */}
      <div id="dashboard-section-7">
        <Card>
          <CardHeader title="7. Capacity Analysis" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Capacity Analysis widget compares planned capacity vs. actual task allocation for each sprint.
                This helps identify over-allocation or under-utilization.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Bar Chart Visualization</h4>
                  <p className="text-sm mb-2">Each sprint shows two bars:</p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li><strong className="text-amber-600 dark:text-amber-400">Planned (Orange):</strong> Total capacity set in sprint planning</li>
                    <li><strong className="text-blue-600 dark:text-blue-400">Actual (Blue):</strong> Sum of story points from assigned tasks</li>
                  </ul>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How Capacity is Calculated</h4>
                  <p className="text-sm mb-2"><strong>Planned Capacity:</strong></p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li>Comes from Sprint Capacity entries (per team member, per sprint)</li>
                    <li>Set manually in the Sprints page</li>
                    <li>Represents theoretical available capacity</li>
                  </ul>
                  <p className="text-sm mt-2 mb-2"><strong>Actual Allocation:</strong></p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li>Sum of story points from all tasks assigned to the sprint</li>
                    <li>Calculated automatically based on task assignments</li>
                    <li>Updates in real-time as tasks are added/removed from sprints</li>
                    <li className="text-blue-600 dark:text-blue-400">
                      <strong>Note:</strong> Only child tasks are counted. Parent task story points are excluded from capacity calculations.
                    </li>
                  </ul>
                </div>

                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3">
                  <h4 className="font-semibold text-red-900 dark:text-red-100 mb-2">Warning Indicators</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm">
                    <li><strong>Over-allocated:</strong> Actual &gt; Planned (warning icon appears)</li>
                    <li><strong>Under-allocated:</strong> Actual significantly &lt; Planned (may indicate poor planning)</li>
                  </ul>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 text-sm">
                  <strong>Sprint Filter Impact:</strong> Only sprints within the selected history window are shown.
                  Use "All Sprints" to see the complete capacity history.
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Aim for Actual to be 80-90% of Planned. This leaves buffer for
                unplanned work while maximizing team utilization.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 8: Team Performance Metrics */}
      <div id="dashboard-section-8">
        <Card>
          <CardHeader title="8. Team Performance Metrics" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Two key charts track your team's delivery performance over time.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Team Velocity</h4>
                  <p className="text-sm mb-2">
                    Line chart showing story points completed per sprint over time.
                    Helps track team throughput and identify trends.
                  </p>
                  <p className="text-sm font-semibold mt-2 mb-1">Calculation:</p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-xs">
                    <li>Sum of story points from tasks with status = Done</li>
                    <li>Grouped by sprint</li>
                    <li>Respects sprint history filter</li>
                    <li className="text-blue-600 dark:text-blue-400">
                      <strong>Only child tasks counted</strong> - parent task story points excluded
                    </li>
                  </ul>
                  <p className="text-xs text-blue-600 dark:text-blue-400 mt-2">Filtered by sprint history</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Estimation Accuracy</h4>
                  <p className="text-sm mb-2">
                    Line chart showing what percentage of estimated work was actually completed each sprint.
                    Helps calibrate future estimates.
                  </p>
                  <p className="text-sm font-semibold mt-2 mb-1">Calculation:</p>
                  <code className="block bg-slate-900 dark:bg-slate-950 text-green-400 p-2 rounded text-xs mt-1">
                    Accuracy = (Completed SP / Total Estimated SP) × 100
                  </code>
                  <p className="text-sm mt-2">Example: If you estimated 50 SP but only completed 40 SP, accuracy = 80%</p>
                  <p className="text-xs text-blue-600 dark:text-blue-400 mt-2">Filtered by sprint history</p>
                </div>

                <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3 text-sm">
                  <strong>Understanding Trends:</strong>
                  <ul className="list-disc list-inside ml-4 mt-1 space-y-1 text-xs">
                    <li>Increasing velocity = team getting faster (or tasks getting easier)</li>
                    <li>Decreasing velocity = team slowing down (or tasks getting harder)</li>
                    <li>Accuracy &gt; 100% = team over-delivered (completed more than estimated)</li>
                    <li>Accuracy &lt; 80% = team consistently under-delivering or over-estimating</li>
                  </ul>
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Track these metrics over 5-10 sprints to identify meaningful trends.
                Single-sprint fluctuations are normal and not cause for concern.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 9: Project & Member Distribution */}
      <div id="dashboard-section-9">
        <Card>
          <CardHeader title="9. Project & Member Distribution" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                These widgets show how work and people are distributed across projects.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Projects Distribution</h4>
                  <p className="text-sm mb-2">
                    Pie chart showing task count per project. Based on all tasks (not filtered by sprint).
                  </p>
                  <p className="text-sm"><strong>Calculation:</strong> Groups all tasks by ProjectId and counts them.</p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">Badge: All sprints</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Members by Project</h4>
                  <p className="text-sm mb-2">
                    Bar chart showing how many unique team members are working on each project.
                    Based on task assignments (all tasks, not filtered).
                  </p>
                  <p className="text-sm"><strong>Calculation:</strong> Counts distinct assignees per project.</p>
                  <p className="text-sm mt-2">
                    <strong>Use Case:</strong> Identify projects with very few contributors (potential knowledge silos)
                    or projects with too many contributors (potential coordination overhead).
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">Badge: All sprints</p>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Note:</strong> These widgets show all-time data because they're meant to give you the big picture
                of project structure, not sprint-specific allocation.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 10: Support & Maintenance Tracking */}
      <div id="dashboard-section-10">
        <Card>
          <CardHeader title="10. Support & Maintenance Tracking" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Track time spent on support and maintenance work vs. feature development.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How Support/Maintenance is Identified</h4>
                  <p className="text-sm mb-2">
                    Tasks are classified as support or maintenance based on labels configured in App Settings:
                  </p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li><strong>Support Labels:</strong> Set in Settings → Support Label (e.g., "support", "customer-issue")</li>
                    <li><strong>Maintenance Labels:</strong> Set in Settings → Maintenance Label (e.g., "maintenance", "tech-debt")</li>
                  </ul>
                  <p className="text-sm mt-2">
                    Tasks with matching labels are counted toward support or maintenance hours/points.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Support Distribution Widget</h4>
                  <p className="text-sm mb-2">
                    Shows two views:
                  </p>
                  <ol className="list-decimal list-inside ml-4 space-y-2 text-sm">
                    <li>
                      <strong>Support vs Maintenance Hours (Pie Chart):</strong>
                      <ul className="list-disc list-inside ml-4 mt-1 space-y-1 text-xs">
                        <li>Sum of hours for support-labeled tasks</li>
                        <li>Sum of hours for maintenance-labeled tasks</li>
                      </ul>
                    </li>
                    <li>
                      <strong>Support Hours by Assignee (Bar Chart):</strong>
                      <ul className="list-disc list-inside ml-4 mt-1 space-y-1 text-xs">
                        <li>Shows which team members are handling most support work</li>
                        <li>Helps balance support load</li>
                      </ul>
                    </li>
                  </ol>
                  <p className="text-xs text-blue-600 dark:text-blue-400 mt-2">Filtered by sprint history</p>
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Aim to keep support/maintenance under 20-30% of total capacity
                to ensure sufficient time for feature development and strategic work.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 11: Team Insights */}
      <div id="dashboard-section-11">
        <Card>
          <CardHeader title="11. Team Insights" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                AI-powered insights and recommendations to improve team performance and knowledge distribution.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Team Sentiment</h4>
                  <p className="text-sm mb-2">
                    Analyzes manager notes and 1:1 meeting notes using AI sentiment analysis to track team morale.
                  </p>
                  <p className="text-sm"><strong>Shows:</strong></p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-xs">
                    <li>Overall team sentiment (Positive/Neutral/Negative)</li>
                    <li>Per-member sentiment scores</li>
                    <li>Sentiment trends over time</li>
                    <li>Common themes extracted from notes</li>
                  </ul>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">Badge: All sprints</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Knowledge Level Suggestions</h4>
                  <p className="text-sm mb-2">
                    Recommends team members for knowledge level increases based on accumulated points.
                  </p>
                  <p className="text-sm"><strong>Logic:</strong></p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-xs">
                    <li>Tracks manual points + automatic points (from completed tasks)</li>
                    <li>Suggests level increase when points reach thresholds (5, 13, 21, 55 points)</li>
                    <li>Shows current level, total points, and suggested new level</li>
                  </ul>
                  <p className="text-sm mt-2">Click a suggestion to navigate to Knowledge Matrix and apply the level increase.</p>
                  <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">Badge: All sprints</p>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Privacy Note:</strong> Sentiment analysis runs locally and does not send data to external services.
                Analysis is cached to improve performance.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 12: Export Functionality */}
      <div id="dashboard-section-12">
        <Card>
          <CardHeader title="12. Export Functionality" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Export dashboard data for reporting, sharing, or archival purposes.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How to Export</h4>
                  <ol className="list-decimal list-inside space-y-2 text-sm">
                    <li>Click the "Export Dashboard" button (download icon) in the top-right</li>
                    <li>Wait for data to be compiled</li>
                    <li>A JSON file will download automatically</li>
                  </ol>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What's Included in Export</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm">
                    <li>All dashboard metrics (team, tasks, projects, capacity)</li>
                    <li>Team velocity data</li>
                    <li>Estimation accuracy data</li>
                    <li>Capacity analysis per sprint</li>
                    <li>Task distribution breakdowns</li>
                    <li>Export timestamp and sprint filter setting</li>
                  </ul>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 text-sm">
                  <strong>File Format:</strong> JSON format for easy parsing and integration with other tools.
                  You can import the JSON into spreadsheet tools or custom reporting dashboards.
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Use Case:</strong> Export snapshots at the end of each quarter to track long-term trends
                or create executive summary reports.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function TeamTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Team Management Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Learn how to manage your team members and track organizational structure
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Direct vs Indirect Reports',
              'Adding Team Members',
              'Bulk CSV Import',
              'Filtering and Search',
              'Tenure Tracking'
            ].map((section, index) => (
              <button
                key={index}
                onClick={() => {
                  const element = document.getElementById(`team-section-${index + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="team-section-1">
        <Card>
          <CardHeader title="1. Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Team page is the central hub for managing your organization's people. It allows you to:
              </p>
              <ul className="list-disc list-inside space-y-2 ml-4">
                <li>Track all team members with their basic information</li>
                <li>Distinguish between direct and indirect reports</li>
                <li>Organize people by department</li>
                <li>Calculate and display tenure automatically</li>
                <li>Bulk import team members from CSV files</li>
                <li>Search and filter to find specific team members</li>
              </ul>
              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Key Concept:</strong> Team members are the foundation of Hive. Once added, they can be assigned to tasks,
                projects, performance reviews, 1:1 meetings, and more throughout the system.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Direct vs Indirect Reports */}
      <div id="team-section-2">
        <Card>
          <CardHeader title="2. Direct vs Indirect Reports" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Hive distinguishes between two types of team members:
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Direct Reports</h4>
                  <p className="text-sm">
                    People who report directly to you. These are typically the team members you manage day-to-day,
                    have 1:1 meetings with, and write performance reviews for.
                  </p>
                  <p className="text-sm mt-2 text-amber-600 dark:text-amber-400">
                    <strong>Flag:</strong> isDirect = true
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Indirect Reports</h4>
                  <p className="text-sm">
                    People in your broader organization who don't report directly to you (e.g., reports of your direct reports,
                    team members in other departments you need to track).
                  </p>
                  <p className="text-sm mt-2 text-amber-600 dark:text-amber-400">
                    <strong>Flag:</strong> isDirect = false
                  </p>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Why it matters:</strong> Many features (like knowledge matrix, skill assessments) can be filtered
                to show only direct reports, helping you focus on your immediate team.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Adding Team Members */}
      <div id="team-section-3">
        <Card>
          <CardHeader title="3. Adding Team Members" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>To add a new team member manually:</p>

              <ol className="list-decimal list-inside space-y-3 ml-4">
                <li>Click the "Add Team Member" button</li>
                <li>Fill in the required information:
                  <ul className="list-disc list-inside ml-6 mt-2 space-y-1 text-sm">
                    <li><strong>First Name</strong> and <strong>Last Name</strong> - Used to create the full name</li>
                    <li><strong>Email</strong> - Contact email address</li>
                    <li><strong>Job Title</strong> - Current role (e.g., "Senior Software Engineer")</li>
                    <li><strong>Department</strong> - Organizational unit (e.g., "Engineering", "Product")</li>
                    <li><strong>Hire Date</strong> - Start date, used to calculate tenure</li>
                    <li><strong>Is Direct Report</strong> - Toggle to mark as direct or indirect</li>
                  </ul>
                </li>
                <li>Click "Save" to create the team member</li>
              </ol>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4 mt-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Editing Team Members</h4>
                <p className="text-sm">
                  Click the three-dot menu next to any team member and select "Edit" to update their information.
                  All fields can be modified after creation.
                </p>
              </div>

              <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3 text-sm">
                <strong>⚠️ Deleting Team Members:</strong> Deleting a team member will remove them from the system.
                This action cannot be undone. Consider carefully before deleting someone who has associated data
                (tasks, reviews, knowledge assessments, etc.).
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: Bulk CSV Import */}
      <div id="team-section-4">
        <Card>
          <CardHeader title="4. Bulk CSV Import" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                For larger teams, you can import multiple team members at once using a CSV file:
              </p>

              <div className="space-y-3">
                <div>
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Steps:</h4>
                  <ol className="list-decimal list-inside space-y-2 ml-4">
                    <li>Click the "Import CSV" button</li>
                    <li>Download the sample template to see the required format</li>
                    <li>Fill in your team members' data in the CSV file</li>
                    <li>Upload the completed CSV file</li>
                    <li>Review the import results</li>
                  </ol>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">CSV Format</h4>
                  <p className="text-sm mb-2">Required columns (in this exact order):</p>
                  <code className="block bg-slate-900 dark:bg-slate-950 text-green-400 p-3 rounded text-xs overflow-x-auto">
                    FirstName,LastName,Email,JobTitle,Department,HireDate,IsDirect
                  </code>
                  <p className="text-sm mt-2">Example row:</p>
                  <code className="block bg-slate-900 dark:bg-slate-950 text-green-400 p-3 rounded text-xs overflow-x-auto">
                    John,Doe,john.doe@example.com,Software Engineer,Engineering,2023-01-15,true
                  </code>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 text-sm">
                  <strong>Skip Duplicates:</strong> The import dialog has a "Skip duplicates" checkbox. When enabled,
                  team members with matching email addresses will be skipped. When disabled, duplicates will cause an error.
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Import Results</h4>
                <p className="text-sm">
                  After import, you'll see a summary showing:
                </p>
                <ul className="list-disc list-inside ml-4 mt-2 space-y-1 text-sm">
                  <li><span className="text-green-600 dark:text-green-400">Success count</span> - Successfully imported members</li>
                  <li><span className="text-red-600 dark:text-red-400">Error count</span> - Failed imports with error messages</li>
                  <li><span className="text-amber-600 dark:text-amber-400">Skipped count</span> - Duplicates skipped</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Filtering and Search */}
      <div id="team-section-5">
        <Card>
          <CardHeader title="5. Filtering and Search" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Team page provides multiple ways to find team members quickly:
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Search Bar</h4>
                  <p className="text-sm">
                    Use the search bar to filter team members by name, email, job title, or department.
                    The search is case-insensitive and updates results in real-time as you type.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Department Filter</h4>
                  <p className="text-sm">
                    Click on a department chip to show only team members from that department.
                    Click "All Departments" to clear the filter.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Report Type Filter</h4>
                  <p className="text-sm mb-2">Filter by direct/indirect status:</p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li><strong>All</strong> - Show both direct and indirect reports</li>
                    <li><strong>Direct Reports Only</strong> - Show only isDirect = true</li>
                    <li><strong>Indirect Reports Only</strong> - Show only isDirect = false</li>
                  </ul>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Tip:</strong> Filters can be combined! For example, search for "engineer" + filter by
                "Engineering" department + show "Direct Reports Only" to see your direct report engineers.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Tenure Tracking */}
      <div id="team-section-6">
        <Card>
          <CardHeader title="6. Tenure Tracking" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Hive automatically calculates each team member's tenure based on their hire date:
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How It's Calculated</h4>
                  <p className="text-sm mb-2">
                    Tenure is calculated from the hire date to the current date in months, then converted to a readable format:
                  </p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li><strong>Less than 12 months:</strong> Shows as "X months" (e.g., "8 months")</li>
                    <li><strong>12+ months:</strong> Shows as "Xy Xm" (e.g., "2y 3m" for 2 years 3 months)</li>
                    <li><strong>Even years:</strong> Shows as "X years" (e.g., "3 years")</li>
                  </ul>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 text-sm">
                  <strong>Real-Time Updates:</strong> Tenure is calculated dynamically each time you view the Team page,
                  so it's always up to date without needing manual updates.
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Use Cases</h4>
                  <p className="text-sm mb-2">Understanding tenure helps with:</p>
                  <ul className="list-disc list-inside ml-4 space-y-1 text-sm">
                    <li>Identifying team members eligible for performance reviews</li>
                    <li>Recognizing work anniversaries</li>
                    <li>Understanding team composition and retention</li>
                    <li>Planning career development conversations</li>
                  </ul>
                </div>
              </div>

              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3 text-sm">
                <strong>✓ Best Practice:</strong> Ensure hire dates are accurate when adding team members,
                as they're used for tenure calculation and may affect other features like leave accrual in the future.
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function KnowledgeMatrixTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Knowledge Matrix Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          A comprehensive guide to tracking team knowledge across projects
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Knowledge Levels',
              'Points System',
              'Level Progression',
              'Label-Based Matching',
              'Common Workflows',
              'Business Rules',
              'Best Practices'
            ].map((section, idx) => (
              <button
                key={idx}
                onClick={() => {
                  const element = document.getElementById(`section-${idx + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="text-left text-amber-600 dark:text-amber-400 hover:underline text-sm py-1"
              >
                {idx + 1}. {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="section-1">
        <Card>
          <CardHeader title="1. Overview" />
        <CardContent>
          <div className="space-y-4 text-slate-700 dark:text-slate-300">
            <p>
              The <strong>Knowledge Matrix</strong> is a tool for tracking your team members' knowledge levels across different projects.
              It combines <strong>manual assessments</strong> with <strong>automatic point tracking</strong> to provide a comprehensive view
              of each person's expertise and growth.
            </p>

            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">What You Can Track</h4>
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Knowledge levels from "No clue" to "Confident" (expert)</li>
                <li>Manual points awarded by managers for contributions</li>
                <li>Automatic points calculated from completed tasks</li>
                <li>Progression history showing how knowledge has grown over time</li>
                <li>Suggestions for when team members are ready to level up</li>
              </ul>
            </div>

            <h4 className="font-semibold mt-6 mb-2">Core Entities</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-3">
                <h5 className="font-medium text-amber-600 dark:text-amber-400 mb-1">ProjectKnowledge</h5>
                <p className="text-sm">
                  Stores the assessed knowledge level (1-5) for each team member on each project.
                </p>
              </div>
              <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-3">
                <h5 className="font-medium text-amber-600 dark:text-amber-400 mb-1">KnowledgePoint</h5>
                <p className="text-sm">
                  Tracks contribution points (manual + automatic) used to determine when to suggest level increases.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 2: Knowledge Levels */}
      <div id="section-2">
        <Card>
        <CardHeader title="2. Knowledge Levels" />
        <CardContent>
          <div className="space-y-4">
            <p className="text-slate-700 dark:text-slate-300">
              Knowledge is assessed on a scale from 1 to 5. Each level represents a distinct stage of expertise:
            </p>

            <div className="space-y-3">
              {[
                { level: 1, label: 'No clue', color: 'bg-red-200 dark:bg-red-900 text-red-800 dark:text-red-200', desc: 'Lacks foundational knowledge. Needs significant guidance and training.' },
                { level: 2, label: 'Limited', color: 'bg-orange-200 dark:bg-orange-900 text-orange-800 dark:text-orange-200', desc: 'Basic understanding. Can perform simple tasks with supervision.' },
                { level: 3, label: 'Moderate', color: 'bg-yellow-200 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200', desc: 'Solid intermediate knowledge. Can work independently on common tasks.' },
                { level: 4, label: 'Good', color: 'bg-lime-200 dark:bg-lime-900 text-lime-800 dark:text-lime-200', desc: 'Strong, confident knowledge. Can handle complex tasks and help others.' },
                { level: 5, label: 'Confident', color: 'bg-green-200 dark:bg-green-900 text-green-800 dark:text-green-200', desc: 'Expert level. Ready to mentor others and make architectural decisions.' }
              ].map(({ level, label, color, desc }) => (
                <div key={level} className="flex items-start gap-3">
                  <span className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium flex-shrink-0 ${color}`}>
                    {level}
                  </span>
                  <div className="flex-1">
                    <h5 className="font-semibold text-slate-900 dark:text-slate-100">{label}</h5>
                    <p className="text-sm text-slate-600 dark:text-slate-400">{desc}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg p-4 mt-4">
              <h5 className="font-medium text-slate-900 dark:text-slate-100 mb-2">Level 0: Unassessed</h5>
              <p className="text-sm text-slate-600 dark:text-slate-400">
                When a team member hasn't been assessed for a project yet, they appear as <span className="text-slate-400">"-"</span> in the matrix.
                This is the default state before any assessment is made.
              </p>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 3: Points System */}
      <div id="section-3">
        <Card>
        <CardHeader
          title="3. Points System"
          subtitle="How manual and automatic points work together"
        />
        <CardContent>
          <div className="space-y-6">
            {/* Manual Points */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Award className="w-5 h-5 text-amber-600 dark:text-amber-400" />
                <h4 className="font-semibold text-slate-900 dark:text-slate-100">Manual Points</h4>
              </div>
              <p className="text-slate-700 dark:text-slate-300 mb-3">
                Points you explicitly assign to recognize contributions not captured in tasks, such as:
              </p>
              <ul className="list-disc list-inside space-y-1 text-sm text-slate-600 dark:text-slate-400 ml-4">
                <li>Mentoring other team members</li>
                <li>Documentation contributions</li>
                <li>Knowledge sharing presentations</li>
                <li>Code reviews and architectural decisions</li>
                <li>On-call support and incident handling</li>
              </ul>
              <div className="mt-3 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded">
                <p className="text-sm text-slate-700 dark:text-slate-300">
                  💡 <strong>Tip:</strong> Use the +/- buttons in the matrix to quickly add or remove manual points.
                  You can also add notes explaining why points were awarded.
                </p>
              </div>
            </div>

            {/* Automatic Points */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Zap className="w-5 h-5 text-green-600 dark:text-green-400" />
                <h4 className="font-semibold text-slate-900 dark:text-slate-100">Automatic Points</h4>
              </div>
              <p className="text-slate-700 dark:text-slate-300 mb-3">
                Automatically calculated from <strong>completed tasks</strong> (Status = Done). Each task contributes its story points value
                (or 1 if no story points are set).
              </p>

              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                <h5 className="font-medium text-green-900 dark:text-green-100 mb-2">How Tasks Are Matched to Projects</h5>
                <ol className="list-decimal list-inside space-y-2 text-sm">
                  <li>
                    <strong>Direct Match:</strong> Task's Project field matches the project
                  </li>
                  <li>
                    <strong>Label Match:</strong> Task and project share at least one common label
                    <div className="ml-6 mt-1 text-xs text-green-700 dark:text-green-300">
                      Example: Task labeled "backend, payments" matches project labeled "payments, api"
                    </div>
                  </li>
                </ol>
                <p className="text-xs text-green-700 dark:text-green-300 mt-3">
                  ⚠️ <strong>Important:</strong> Automatic points are recalculated every time you view the matrix.
                  They are NOT stored in the database, so they always reflect the latest completed tasks.
                </p>
              </div>
            </div>

            {/* Total Points Formula */}
            <div className="border-t border-slate-200 dark:border-slate-700 pt-4">
              <div className="flex items-center gap-2 mb-3">
                <Calculator className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                <h4 className="font-semibold text-slate-900 dark:text-slate-100">Total Points</h4>
              </div>
              <div className="bg-blue-50 dark:bg-blue-900/20 border-2 border-blue-300 dark:border-blue-700 rounded-lg p-4 font-mono text-center">
                <div className="text-2xl font-bold text-blue-900 dark:text-blue-100">
                  Total = Manual + Automatic
                </div>
                <div className="text-sm text-blue-700 dark:text-blue-300 mt-2">
                  Example: 5 manual + 8 automatic = 13 total points
                </div>
              </div>
            </div>

            {/* Display Format */}
            <div className="bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <h5 className="font-medium text-slate-900 dark:text-slate-100 mb-2">How Points Are Displayed</h5>
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <p className="text-sm text-slate-600 dark:text-slate-400 mb-2">In the matrix, you'll see:</p>
                  <div className="font-mono text-sm">
                    <span className="font-medium">5</span>
                    <span className="text-green-600 dark:text-green-400">+8</span>
                  </div>
                  <p className="text-xs text-slate-500 dark:text-slate-500 mt-1">
                    (manual points)<span className="text-green-600 dark:text-green-400">+(automatic points)</span>
                  </p>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 4: Level Progression */}
      <div id="section-4">
        <Card>
        <CardHeader
          title="4. Level Progression & Thresholds"
          subtitle="When to suggest a level increase"
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-slate-700 dark:text-slate-300">
              The system uses <strong>cumulative point thresholds</strong> to determine when a team member should be considered
              for a level increase. These thresholds are based on total points accumulated.
            </p>

            {/* Thresholds Table */}
            <div className="overflow-x-auto">
              <table className="min-w-full border border-slate-200 dark:border-slate-700">
                <thead className="bg-slate-50 dark:bg-slate-800">
                  <tr>
                    <th className="px-4 py-2 text-left text-sm font-medium text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700">
                      Current Level
                    </th>
                    <th className="px-4 py-2 text-left text-sm font-medium text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700">
                      Points Needed
                    </th>
                    <th className="px-4 py-2 text-left text-sm font-medium text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700">
                      Target Level
                    </th>
                    <th className="px-4 py-2 text-left text-sm font-medium text-slate-700 dark:text-slate-300 border-b border-slate-200 dark:border-slate-700">
                      Meaning
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                  {[
                    { current: '0 or 1', points: '5+', target: '2', meaning: 'Starting level → Limited knowledge' },
                    { current: '2', points: '13+', target: '3', meaning: 'Limited → Moderate knowledge' },
                    { current: '3', points: '21+', target: '4', meaning: 'Moderate → Good knowledge' },
                    { current: '4', points: '55+', target: '5', meaning: 'Good → Confident (expert)' },
                    { current: '5', points: 'N/A', target: '-', meaning: 'Maximum level reached' }
                  ].map((row, idx) => (
                    <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                      <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.current}</td>
                      <td className="px-4 py-2 text-sm font-mono text-amber-600 dark:text-amber-400">{row.points}</td>
                      <td className="px-4 py-2 text-sm font-semibold text-green-600 dark:text-green-400">{row.target}</td>
                      <td className="px-4 py-2 text-sm text-slate-600 dark:text-slate-400">{row.meaning}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Level Increase Workflow */}
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <TrendingUp className="w-5 h-5 text-amber-600 dark:text-amber-400" />
                <h5 className="font-medium text-amber-900 dark:text-amber-100">Typical Level Increase Workflow</h5>
              </div>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>Team member accumulates points (manual + automatic) for a project</li>
                <li>When total points reach the threshold, a <strong>suggestion appears</strong> (ring highlight on level badge)</li>
                <li>You review their work and confirm they're ready for the next level</li>
                <li>Click the level badge and select the new level (e.g., change from 1 to 2)</li>
                <li>The system logs this change in the progression history</li>
                <li><strong>Recommended:</strong> Reset manual points to 0 to start fresh for the next level</li>
                <li>Team member now works toward the next threshold with a clean slate</li>
              </ol>
            </div>

            {/* Example Scenario */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <h5 className="font-medium text-slate-900 dark:text-slate-100 mb-3">📖 Example Scenario</h5>
              <div className="space-y-2 text-sm">
                <div className="flex items-start gap-2">
                  <span className="font-mono text-slate-500 dark:text-slate-400 w-16 flex-shrink-0">Day 1:</span>
                  <span className="text-slate-700 dark:text-slate-300">
                    Sarah starts at level 1 for the "Payments API" project with 0 points
                  </span>
                </div>
                <div className="flex items-start gap-2">
                  <span className="font-mono text-slate-500 dark:text-slate-400 w-16 flex-shrink-0">Week 1:</span>
                  <span className="text-slate-700 dark:text-slate-300">
                    Completes 3 tasks (3+2+1 story points) → 6 automatic points
                  </span>
                </div>
                <div className="flex items-start gap-2">
                  <span className="font-mono text-slate-500 dark:text-slate-400 w-16 flex-shrink-0">Week 2:</span>
                  <span className="text-slate-700 dark:text-slate-300">
                    ✨ <strong>Suggestion appears</strong> (6 points ≥ 5 threshold) → Ready for level 2!
                  </span>
                </div>
                <div className="flex items-start gap-2">
                  <span className="font-mono text-slate-500 dark:text-slate-400 w-16 flex-shrink-0">Action:</span>
                  <span className="text-slate-700 dark:text-slate-300">
                    You review her work, confirm quality, and update level from 1 → 2
                  </span>
                </div>
                <div className="flex items-start gap-2">
                  <span className="font-mono text-slate-500 dark:text-slate-400 w-16 flex-shrink-0">Week 3:</span>
                  <span className="text-slate-700 dark:text-slate-300">
                    Reset manual points (if any) to 0. Sarah now needs 13 total points for level 3.
                  </span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 5: Label-Based Matching */}
      <div id="section-5">
        <Card>
        <CardHeader
          title="5. Label-Based Project Matching"
          subtitle="How tasks contribute to multiple projects"
        />
        <CardContent>
          <div className="space-y-4">
            <p className="text-slate-700 dark:text-slate-300">
              Tasks can contribute automatic points to multiple projects through <strong>label-based matching</strong>.
              This is useful when work spans multiple projects or when you use labels to organize work instead of strict project assignments.
            </p>

            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h5 className="font-medium text-blue-900 dark:text-blue-100 mb-3">How Label Matching Works</h5>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>Both projects and tasks can have comma-separated labels (e.g., "backend, api, payments")</li>
                <li>Labels are matched <strong>case-insensitively</strong> after trimming whitespace</li>
                <li>If a task and project share <strong>at least one label</strong>, the task contributes points to that project</li>
                <li>A single task can contribute to multiple projects if it matches multiple label sets</li>
              </ol>
            </div>

            {/* Example */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <h5 className="font-medium text-slate-900 dark:text-slate-100 mb-3">📋 Real-World Example</h5>

              <div className="space-y-4 text-sm">
                <div>
                  <p className="font-medium text-slate-700 dark:text-slate-300 mb-1">Projects:</p>
                  <ul className="space-y-1 ml-4">
                    <li className="text-slate-600 dark:text-slate-400">
                      <span className="font-mono text-xs bg-purple-100 dark:bg-purple-900/30 px-2 py-1 rounded">Rights Manager</span>
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">rights-manager, backend</code>
                    </li>
                    <li className="text-slate-600 dark:text-slate-400">
                      <span className="font-mono text-xs bg-blue-100 dark:bg-blue-900/30 px-2 py-1 rounded">Payments API</span>
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">payments, api, backend</code>
                    </li>
                    <li className="text-slate-600 dark:text-slate-400">
                      <span className="font-mono text-xs bg-green-100 dark:bg-green-900/30 px-2 py-1 rounded">Admin Portal</span>
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">frontend, admin</code>
                    </li>
                  </ul>
                </div>

                <div>
                  <p className="font-medium text-slate-700 dark:text-slate-300 mb-1">Tasks (all completed):</p>
                  <ul className="space-y-2 ml-4">
                    <li>
                      <strong className="text-slate-900 dark:text-slate-100">Task A</strong> (5 SP)
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">rights-manager, frontend</code>
                      <div className="ml-6 mt-1 text-xs text-green-600 dark:text-green-400">
                        ✓ Contributes 5 points to <span className="font-mono bg-purple-100 dark:bg-purple-900/30 px-1 rounded">Rights Manager</span> (matches "rights-manager")
                      </div>
                    </li>
                    <li>
                      <strong className="text-slate-900 dark:text-slate-100">Task B</strong> (3 SP)
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">backend, payments</code>
                      <div className="ml-6 mt-1 text-xs text-green-600 dark:text-green-400">
                        ✓ Contributes 3 points to <span className="font-mono bg-purple-100 dark:bg-purple-900/30 px-1 rounded">Rights Manager</span> (matches "backend")<br/>
                        ✓ Contributes 3 points to <span className="font-mono bg-blue-100 dark:bg-blue-900/30 px-1 rounded">Payments API</span> (matches both "backend" and "payments")
                      </div>
                    </li>
                    <li>
                      <strong className="text-slate-900 dark:text-slate-100">Task C</strong> (2 SP)
                      <span className="mx-2">→</span>
                      Labels: <code className="text-xs">frontend, dashboard</code>
                      <div className="ml-6 mt-1 text-xs text-slate-500 dark:text-slate-500">
                        ✗ No matches (doesn't share labels with any project)
                      </div>
                    </li>
                  </ul>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded p-3 mt-3">
                  <p className="font-medium text-slate-900 dark:text-slate-100 mb-1">Result:</p>
                  <ul className="space-y-1">
                    <li className="text-slate-700 dark:text-slate-300">
                      <span className="font-mono bg-purple-100 dark:bg-purple-900/30 px-2 py-0.5 rounded">Rights Manager</span> → 8 automatic points (5 + 3)
                    </li>
                    <li className="text-slate-700 dark:text-slate-300">
                      <span className="font-mono bg-blue-100 dark:bg-blue-900/30 px-2 py-0.5 rounded">Payments API</span> → 3 automatic points
                    </li>
                    <li className="text-slate-700 dark:text-slate-300">
                      <span className="font-mono bg-green-100 dark:bg-green-900/30 px-2 py-0.5 rounded">Admin Portal</span> → 0 automatic points
                    </li>
                  </ul>
                </div>
              </div>
            </div>

            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
              <p className="text-sm text-amber-900 dark:text-amber-100">
                💡 <strong>Tip:</strong> Use consistent labeling conventions across tasks and projects to ensure accurate automatic point tracking.
                Labels like "backend", "frontend", "api", "ui" work well as they're commonly used and easy to match.
              </p>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 6: Common Workflows */}
      <div id="section-6">
        <Card>
        <CardHeader title="6. Common Workflows" />
        <CardContent>
          <div className="space-y-6">
            {/* Workflow 1 */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <GitBranch className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                <h5 className="font-medium text-slate-900 dark:text-slate-100">Assessing a New Team Member</h5>
              </div>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>Go to the Knowledge Matrix page</li>
                <li>Find the row for the relevant project</li>
                <li>Click the cell for the new team member (will show "-" for unassessed)</li>
                <li>Select their current knowledge level (usually start at level 1 or 2)</li>
                <li>System saves the assessment and shows the colored badge</li>
                <li>Optionally add manual points if they've already made contributions</li>
              </ol>
            </div>

            {/* Workflow 2 */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <Award className="w-5 h-5 text-amber-600 dark:text-amber-400" />
                <h5 className="font-medium text-slate-900 dark:text-slate-100">Awarding Manual Points</h5>
              </div>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>In the Knowledge Matrix, locate the team member + project cell</li>
                <li>Click the <strong>+</strong> button below the level badge</li>
                <li>Points increment by 1 each click (displays as manual points)</li>
                <li>To remove points, click the <strong>-</strong> button</li>
                <li>Points display updates immediately showing manual + automatic</li>
              </ol>
              <p className="text-xs text-slate-500 dark:text-slate-500 mt-3">
                💡 Use manual points for contributions like code reviews, mentoring, documentation, or incident response.
              </p>
            </div>

            {/* Workflow 3 */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <TrendingUp className="w-5 h-5 text-green-600 dark:text-green-400" />
                <h5 className="font-medium text-slate-900 dark:text-slate-100">Processing a Level Increase</h5>
              </div>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>Check the "Level Increase Suggestions" banner (appears when points meet thresholds)</li>
                <li>Review the team member's actual work quality and contributions</li>
                <li>If you agree they're ready, click "Increase Level" or click their level badge</li>
                <li>Select the new level from the dropdown</li>
                <li>The system logs this change in the progression history</li>
                <li>After confirming the increase, consider resetting their manual points to 0</li>
                <li>Team member now works toward the next level threshold</li>
              </ol>
            </div>

            {/* Workflow 4 */}
            <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-3">
                <TrendingUp className="w-5 h-5 text-purple-600 dark:text-purple-400" />
                <h5 className="font-medium text-slate-900 dark:text-slate-100">Viewing Progression History</h5>
              </div>
              <ol className="list-decimal list-inside space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>Go to the Knowledge Matrix page and click the "Progression" tab</li>
                <li>Choose view mode: "By Individual" or "By Project"</li>
                <li>Select a team member (for individual view) or project (for project view)</li>
                <li>View the chart showing knowledge level progression over time</li>
                <li>Dashed lines show points progression alongside level changes</li>
                <li>Review "Recent Changes" table to see detailed history with dates</li>
              </ol>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 7: Business Rules */}
      <div id="section-7">
        <Card>
        <CardHeader title="7. Business Rules & Constraints" />
        <CardContent>
          <div className="space-y-3">
            {[
              { rule: 'Knowledge levels must be 1-5', desc: 'Level 0 is a system state for "unassessed", not user-selectable' },
              { rule: 'Only direct reports appear', desc: 'Matrix and suggestions only include team members with Direct flag = true' },
              { rule: 'One assessment per combination', desc: 'Each (team member, project) pair can only have one knowledge assessment' },
              { rule: 'Points cannot go negative', desc: 'Both manual and automatic points are always >= 0' },
              { rule: 'Automatic points are readonly', desc: 'Never stored in database, always calculated from completed tasks' },
              { rule: 'Label matching is case-insensitive', desc: 'Labels "Backend", "backend", and "BACKEND" all match' },
              { rule: 'Story points default to 1', desc: 'Tasks without story points count as 1 point each' },
              { rule: 'Level 5 is terminal', desc: 'No suggestions or progression beyond expert level (5)' },
              { rule: 'Notes support multiline', desc: 'When adding points, notes are appended with newline separator' },
              { rule: 'Reset is idempotent', desc: 'Resetting points on non-existent records does nothing (no error)' }
            ].map((item, idx) => (
              <div key={idx} className="flex items-start gap-3 pb-3 border-b border-slate-100 dark:border-slate-800 last:border-0">
                <span className="flex-shrink-0 w-6 h-6 rounded-full bg-amber-100 dark:bg-amber-900/30 text-amber-600 dark:text-amber-400 flex items-center justify-center text-xs font-medium">
                  {idx + 1}
                </span>
                <div className="flex-1">
                  <h6 className="font-medium text-slate-900 dark:text-slate-100 text-sm">{item.rule}</h6>
                  <p className="text-xs text-slate-600 dark:text-slate-400 mt-0.5">{item.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Section 8: Best Practices */}
      <div id="section-8">
        <Card>
        <CardHeader title="8. Best Practices" />
        <CardContent>
          <div className="space-y-4">
            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
              <h5 className="font-medium text-green-900 dark:text-green-100 mb-3">✓ Do's</h5>
              <ul className="space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>Regularly review and update knowledge levels as team members grow</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>Use manual points to recognize non-task contributions like mentoring and code reviews</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>Reset manual points to 0 after confirming a level increase</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>Use consistent labels across tasks and projects for accurate automatic tracking</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>View progression history to understand growth patterns</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-600 dark:text-green-400 flex-shrink-0">✓</span>
                  <span>Add notes when awarding manual points to document the reason</span>
                </li>
              </ul>
            </div>

            <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
              <h5 className="font-medium text-red-900 dark:text-red-100 mb-3">✗ Don'ts</h5>
              <ul className="space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li className="flex items-start gap-2">
                  <span className="text-red-600 dark:text-red-400 flex-shrink-0">✗</span>
                  <span>Don't auto-increase levels just because points meet thresholds - verify actual skill growth</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-600 dark:text-red-400 flex-shrink-0">✗</span>
                  <span>Don't forget to reset points after level increases - it creates confusion for next progression</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-600 dark:text-red-400 flex-shrink-0">✗</span>
                  <span>Don't rely solely on automatic points - manual points are important for non-task work</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-600 dark:text-red-400 flex-shrink-0">✗</span>
                  <span>Don't use inconsistent labeling - it will cause automatic points to miss relevant tasks</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-600 dark:text-red-400 flex-shrink-0">✗</span>
                  <span>Don't set knowledge levels without actually assessing the person's work</span>
                </li>
              </ul>
            </div>

            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h5 className="font-medium text-blue-900 dark:text-blue-100 mb-2">💡 Pro Tips</h5>
              <ul className="space-y-2 text-sm text-slate-700 dark:text-slate-300">
                <li>• Use the Knowledge Radar chart to quickly identify projects with low team knowledge</li>
                <li>• Check the progression tab regularly to celebrate team member growth</li>
                <li>• Consider project labels as "skills" (e.g., "payments", "api", "backend") for better tracking</li>
                <li>• Use the matrix sorting to find team members with the most/least knowledge per project</li>
              </ul>
            </div>
          </div>
        </CardContent>
        </Card>
      </div>

      {/* Footer */}
      <div className="text-center py-8 border-t border-slate-200 dark:border-slate-700">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Have questions or suggestions for this tutorial?<br/>
          Reach out to your Hive administrator.
        </p>
      </div>
    </div>
  )
}

function QuarterlyPlanningTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Quarterly Planning Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Strategic planning for initiatives, team allocations, and quarterly goals
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Creating Quarters',
              'Managing Initiatives',
              'T-Shirt Sizing',
              'Allocations & Planning Matrix',
              'Sprint Goals',
              'Dependencies',
              'Insights & Warnings',
              'Best Practices'
            ].map((section, idx) => (
              <button
                key={idx}
                onClick={() => {
                  const element = document.getElementById(`qp-section-${idx + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="qp-section-1">
        <Card>
          <CardHeader title="1. Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Quarterly Planning helps you manage strategic initiatives across quarters, allocate team members to work,
                track dependencies, and identify potential conflicts or bottlenecks. It bridges high-level OKRs with
                sprint-level execution.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Key Concepts</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Quarters</strong> - Time periods (Q1-Q4) with associated OKRs and status tracking</li>
                  <li><strong>Initiatives</strong> - Major work items with T-shirt size estimates</li>
                  <li><strong>Allocations</strong> - Team member assignments to initiatives within sprints</li>
                  <li><strong>Sprint Goals</strong> - Goals and notes for each sprint within a quarter</li>
                  <li><strong>Dependencies</strong> - Relationships between initiatives (Finish-to-Start, etc.)</li>
                  <li><strong>Insights</strong> - Automated warnings about workload, conflicts, and risks</li>
                </ul>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How It Works</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Create a quarter and link it to your OKRs</li>
                  <li>Add initiatives with T-shirt size estimates</li>
                  <li>Allocate team members to initiatives across sprints</li>
                  <li>Track dependencies between initiatives</li>
                  <li>Monitor insights for workload issues, leave conflicts, and blockers</li>
                </ol>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Pro Tip:</strong> Start planning at the quarter level, then drill down to sprint allocations.
                The system will automatically detect conflicts and suggest optimizations.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Creating Quarters */}
      <div id="qp-section-2">
        <Card>
          <CardHeader title="2. Creating Quarters" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Quarters are the top-level organizing structure for planning. Each quarter has a year, quarter number (1-4),
                and optional OKR reference.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Quarter Status</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Planning</strong> - Draft quarter, still being planned</li>
                  <li><strong>Active</strong> - Currently executing this quarter</li>
                  <li><strong>Completed</strong> - Quarter has finished</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Creating a Quarter</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Click "Create Quarter" button</li>
                  <li>Select year and quarter number (1-4)</li>
                  <li>Optionally add OKR reference (e.g., link to OKR document)</li>
                  <li>Click "Create" - the quarter will be set to Planning status</li>
                  <li>The system will load relevant sprints for the quarter period</li>
                </ol>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Create quarters in advance (e.g., create Q3 during Q2) to allow
                proper planning time. Link OKRs to maintain alignment between strategy and execution.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Managing Initiatives */}
      <div id="qp-section-3">
        <Card>
          <CardHeader title="3. Managing Initiatives" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Initiatives represent major work items or projects you plan to tackle in a quarter. Each initiative
                can be sized, linked to projects, and allocated to team members.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Initiative Fields</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Name</strong> - Short, descriptive title</li>
                  <li><strong>Description</strong> - Detailed explanation of the work</li>
                  <li><strong>Color</strong> - Visual identifier in the planning matrix (auto-assigned, editable)</li>
                  <li><strong>T-Shirt Size</strong> - Effort estimate (XS, S, M, L, XL)</li>
                  <li><strong>Project Link</strong> - Associate with an existing project</li>
                  <li><strong>URL</strong> - Link to external docs, tickets, or specs</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Creating an Initiative</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Select a quarter from the dropdown</li>
                  <li>Click "Add Initiative" in the Initiatives panel</li>
                  <li>Enter name and description</li>
                  <li>Select T-shirt size (see next section for sizing guide)</li>
                  <li>Optionally link to a project and add URL</li>
                  <li>Initiative appears in the Initiatives panel and Planning Matrix</li>
                </ol>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Initiative Management</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li>Edit initiative by clicking the edit icon</li>
                  <li>Delete initiative using the delete icon (removes all allocations)</li>
                  <li>Change color to group related initiatives visually</li>
                  <li>View allocation count to see how many team/sprint allocations exist</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: T-Shirt Sizing */}
      <div id="qp-section-4">
        <Card>
          <CardHeader title="4. T-Shirt Sizing" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                T-shirt sizing provides high-level effort estimates for initiatives without requiring detailed story points.
                The system maps sizes to story points for capacity planning.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Standard T-Shirt Size Mapping</h4>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-slate-300 dark:border-slate-600">
                      <th className="text-left py-2">Size</th>
                      <th className="text-left py-2">Story Points</th>
                      <th className="text-left py-2">Typical Scope</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                    <tr>
                      <td className="py-2"><strong>XS</strong></td>
                      <td className="py-2">3 SP</td>
                      <td className="py-2">Small bug fix or minor enhancement</td>
                    </tr>
                    <tr>
                      <td className="py-2"><strong>S</strong></td>
                      <td className="py-2">8 SP</td>
                      <td className="py-2">Small feature, 1-2 sprints</td>
                    </tr>
                    <tr>
                      <td className="py-2"><strong>M</strong></td>
                      <td className="py-2">13 SP</td>
                      <td className="py-2">Medium feature, 2-3 sprints</td>
                    </tr>
                    <tr>
                      <td className="py-2"><strong>L</strong></td>
                      <td className="py-2">21 SP</td>
                      <td className="py-2">Large feature, 4-6 sprints</td>
                    </tr>
                    <tr>
                      <td className="py-2"><strong>XL</strong></td>
                      <td className="py-2">34 SP</td>
                      <td className="py-2">Major initiative, full quarter or more</td>
                    </tr>
                  </tbody>
                </table>
                <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">
                  Note: These mappings can be customized in Settings
                </p>
              </div>

              <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How Sizing Affects Planning</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li>T-shirt size converts to story points for capacity calculations</li>
                  <li>Each allocation spreads the effort across assigned sprints</li>
                  <li>Example: L initiative (21 SP) with 3 sprint allocations = 7 SP per sprint</li>
                  <li>Insights will warn if total workload exceeds team capacity</li>
                </ul>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Use T-shirt sizing for high-level planning. Break down larger initiatives
                (L, XL) into smaller tasks in the Tasks page once sprint planning begins.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Allocations & Planning Matrix */}
      <div id="qp-section-5">
        <Card>
          <CardHeader title="5. Allocations & Planning Matrix" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Planning Matrix is the heart of quarterly planning. It shows a grid of initiatives (rows) by sprints (columns),
                with team member allocations in each cell.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How Allocations Work</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li>Each allocation assigns one team member to work on one initiative during one sprint</li>
                  <li>Multiple team members can be allocated to the same initiative in the same sprint</li>
                  <li>The same team member can work on multiple initiatives in a sprint (multitasking)</li>
                  <li>Each allocation contributes to sprint workload calculations</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Creating an Allocation</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Find the matrix cell for Initiative X Sprint</li>
                  <li>Click the "+" button in the cell</li>
                  <li>Select a team member from the dropdown</li>
                  <li>Click "Allocate"</li>
                  <li>The team member appears as a pill/badge in the cell</li>
                </ol>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Matrix Indicators</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Colored pills</strong> - Team members allocated to this initiative + sprint</li>
                  <li><strong>Leave icon</strong> - Team member has approved leave during this sprint</li>
                  <li><strong>Empty cell</strong> - No allocations for this initiative in this sprint</li>
                  <li><strong>Cell hover</strong> - Shows allocation count and team member names</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Workload Calculation Example</h4>
                <div className="text-sm space-y-2">
                  <p><strong>Scenario:</strong> Initiative "API Redesign" sized as L (21 SP)</p>
                  <ul className="list-disc list-inside ml-4 space-y-1">
                    <li>Sprint 1: Alice, Bob allocated → 21 SP / 3 sprints / 2 people = 3.5 SP each</li>
                    <li>Sprint 2: Alice allocated → 21 SP / 3 sprints / 1 person = 7 SP</li>
                    <li>Sprint 3: Charlie, Diana allocated → 21 SP / 3 sprints / 2 people = 3.5 SP each</li>
                  </ul>
                  <p className="text-amber-600 dark:text-amber-400 mt-2">
                    The system distributes effort evenly across allocated sprints and team members.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Allocate conservatively. Leave buffer for unplanned work, bugs, and support.
                The Insights panel will warn you about over-allocation.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Sprint Goals */}
      <div id="qp-section-6">
        <Card>
          <CardHeader title="6. Sprint Goals" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Sprint Goals connect high-level quarterly objectives to sprint-level execution. They appear in the
                Planning Matrix header for each sprint.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Sprint Goal Fields</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Goal</strong> - Main objective for the sprint</li>
                  <li><strong>Notes</strong> - Additional context, risks, or dependencies</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Setting Sprint Goals</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Locate the sprint column in the Planning Matrix</li>
                  <li>Click "Set Goal" or edit icon in the sprint header</li>
                  <li>Enter goal and optional notes</li>
                  <li>Save - the goal appears in the sprint header</li>
                </ol>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Pro Tip:</strong> Sprint goals should align with initiatives allocated to that sprint.
                Review allocations before setting goals to ensure coherence.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 7: Dependencies */}
      <div id="qp-section-7">
        <Card>
          <CardHeader title="7. Dependencies" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Dependencies track relationships between initiatives. They help identify blockers and sequence work properly.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Dependency Types</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Finish-to-Start (FS)</strong> - Most common: B can't start until A finishes</li>
                  <li><strong>Start-to-Start (SS)</strong> - B can't start until A starts</li>
                  <li><strong>Finish-to-Finish (FF)</strong> - B can't finish until A finishes</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Creating a Dependency</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Click "Manage Dependencies" in the Initiatives panel</li>
                  <li>Select the dependent initiative (what's blocked)</li>
                  <li>Select the dependency initiative (what blocks it)</li>
                  <li>Choose dependency type (Finish-to-Start is default)</li>
                  <li>Add optional notes</li>
                  <li>Save - dependency appears in the dependencies list</li>
                </ol>
              </div>

              <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Dependency Insights</h4>
                <p className="text-sm mb-2">The system automatically detects dependency risks:</p>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li>Dependent initiative scheduled before dependency completes</li>
                  <li>Circular dependencies (A depends on B, B depends on A)</li>
                  <li>Critical path bottlenecks</li>
                </ul>
              </div>

              <p className="text-sm bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
                <strong>✓ Best Practice:</strong> Document dependencies early in planning. Use notes to capture
                specific requirements or coordination points.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 8: Insights & Warnings */}
      <div id="qp-section-8">
        <Card>
          <CardHeader title="8. Insights & Warnings" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Insights sidebar automatically analyzes your quarterly plan and flags potential issues.
                It helps prevent over-allocation, conflicts, and execution risks.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Insight Types</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Leave Conflict</strong> - Team member allocated during approved leave
                    <span className="block text-xs text-amber-600 dark:text-amber-400 ml-5">
                      Severity: Warning or Critical (depending on overlap)
                    </span>
                  </li>
                  <li>
                    <strong>Dependency Risk</strong> - Dependent initiative starts before dependency finishes
                    <span className="block text-xs text-amber-600 dark:text-amber-400 ml-5">
                      Severity: Warning or Critical (based on timing)
                    </span>
                  </li>
                  <li>
                    <strong>Bottleneck</strong> - Team member over-allocated (workload exceeds capacity)
                    <span className="block text-xs text-amber-600 dark:text-amber-400 ml-5">
                      Severity: Critical if significantly over-allocated
                    </span>
                  </li>
                  <li>
                    <strong>Unassigned Work</strong> - Initiative has no allocations
                    <span className="block text-xs text-amber-600 dark:text-amber-400 ml-5">
                      Severity: Info
                    </span>
                  </li>
                </ul>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Insight Severity</h4>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong className="text-blue-600 dark:text-blue-400">Info</strong> - Informational, no action required</li>
                  <li><strong className="text-amber-600 dark:text-amber-400">Warning</strong> - Should be addressed, not blocking</li>
                  <li><strong className="text-red-600 dark:text-red-400">Critical</strong> - Likely to cause execution problems</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Using Insights</h4>
                <ol className="list-decimal list-inside space-y-1 text-sm">
                  <li>Insights appear in the right sidebar with severity badges</li>
                  <li>Click an insight to highlight affected cells in the Planning Matrix</li>
                  <li>Review the message for specific details and recommendations</li>
                  <li>Adjust allocations or dependencies to resolve the issue</li>
                  <li>Insights update automatically as you make changes</li>
                </ol>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Summary Metrics</h4>
                <p className="text-sm mb-2">The Insights panel shows summary statistics:</p>
                <ul className="list-disc list-inside space-y-1 text-sm">
                  <li><strong>Total Initiatives</strong> - All initiatives in the quarter</li>
                  <li><strong>Allocated Initiatives</strong> - Initiatives with at least one allocation</li>
                  <li><strong>Issue Count</strong> - Total insights (Info level)</li>
                  <li><strong>Warning Count</strong> - Insights requiring attention</li>
                  <li><strong>Critical Count</strong> - Serious issues blocking execution</li>
                </ul>
              </div>

              <p className="text-sm bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3">
                <strong>⚠ Important:</strong> Address all Critical insights before starting the quarter.
                Warnings should be reviewed and mitigated if possible.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 9: Best Practices */}
      <div id="qp-section-9">
        <Card>
          <CardHeader title="9. Best Practices" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3 flex items-center gap-2">
                  <Zap className="w-5 h-5" />
                  Planning Workflow
                </h4>
                <ol className="list-decimal list-inside space-y-2 text-sm">
                  <li>
                    <strong>Start Early</strong> - Begin planning next quarter during the current quarter
                  </li>
                  <li>
                    <strong>Define OKRs First</strong> - Link quarters to OKRs before creating initiatives
                  </li>
                  <li>
                    <strong>Size Initiatives</strong> - Use T-shirt sizing to estimate effort
                  </li>
                  <li>
                    <strong>Identify Dependencies</strong> - Document blocking relationships early
                  </li>
                  <li>
                    <strong>Allocate Conservatively</strong> - Leave 20-30% buffer for unplanned work
                  </li>
                  <li>
                    <strong>Check Leave Calendar</strong> - Review team leave before allocating
                  </li>
                  <li>
                    <strong>Review Insights</strong> - Address Critical issues before quarter starts
                  </li>
                  <li>
                    <strong>Set Sprint Goals</strong> - Align sprint goals with allocated initiatives
                  </li>
                </ol>
              </div>

              <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3 flex items-center gap-2">
                  <Calculator className="w-5 h-5" />
                  Capacity Planning Tips
                </h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Account for Support</strong> - Reserve capacity for bug fixes and production issues
                  </li>
                  <li>
                    <strong>Buffer for Meetings</strong> - Not all hours are coding hours (ceremonies, 1:1s, etc.)
                  </li>
                  <li>
                    <strong>Consider Ramp Time</strong> - New team members need time to onboard
                  </li>
                  <li>
                    <strong>Respect Leave</strong> - Don't allocate team members during approved leave
                  </li>
                  <li>
                    <strong>Avoid Over-allocation</strong> - Working multiple initiatives reduces focus and efficiency
                  </li>
                </ul>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3 flex items-center gap-2">
                  <GitBranch className="w-5 h-5" />
                  Dependency Management
                </h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Use Finish-to-Start</strong> - Most dependencies are "B starts after A finishes"
                  </li>
                  <li>
                    <strong>Document Why</strong> - Use notes to explain the dependency relationship
                  </li>
                  <li>
                    <strong>Sequence Work</strong> - Schedule dependent initiatives after dependencies complete
                  </li>
                  <li>
                    <strong>Parallelize When Possible</strong> - Reduce critical path by running independent work concurrently
                  </li>
                  <li>
                    <strong>Monitor Critical Path</strong> - Pay extra attention to initiatives that block others
                  </li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3">Common Pitfalls to Avoid</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Planning Too Much</strong> - Don't allocate 100% capacity, leave buffer
                  </li>
                  <li>
                    <strong>Ignoring Warnings</strong> - Insights exist for a reason, address them
                  </li>
                  <li>
                    <strong>No Dependencies</strong> - Most complex work has dependencies, document them
                  </li>
                  <li>
                    <strong>Skipping Sprint Goals</strong> - Goals help teams focus and align
                  </li>
                  <li>
                    <strong>One-Person Initiatives</strong> - Consider redundancy and knowledge sharing
                  </li>
                  <li>
                    <strong>Set and Forget</strong> - Review quarterly plans regularly and adjust
                  </li>
                </ul>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Remember:</strong> Quarterly planning is a forecast, not a contract. Stay flexible and adjust
                as priorities shift, dependencies change, or new information emerges. The goal is informed decision-making,
                not perfect prediction.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Footer */}
      <div className="text-center py-8 border-t border-slate-200 dark:border-slate-700">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Have questions or suggestions for this tutorial?<br/>
          Reach out to your Hive administrator.
        </p>
      </div>
    </div>
  )
}

function SprintsTasksTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Sprints & Tasks Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Learn how to manage sprints, tasks, story points, capacity, and Jira imports
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Creating Sprints',
              'Editing Sprint Capacity',
              'Sprint Team Filter (Jira)',
              'Managing Tasks',
              'Story Points & Carried-Over',
              'Filtering & Pagination',
              'Jira Import',
              'Bulk Operations'
            ].map((section, index) => (
              <button
                key={index}
                onClick={() => {
                  const element = document.getElementById(`sprints-tasks-section-${index + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="sprints-tasks-section-1">
        <Card>
          <CardHeader title="1. Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Sprints and Tasks are the core of delivery management in Hive. Together they let you:
              </p>
              <ul className="list-disc list-inside space-y-2 ml-4">
                <li>Create time-boxed sprints with capacity targets</li>
                <li>Track individual tasks with status, priority, story points, and assignees</li>
                <li>Monitor progress through 10 workflow statuses (Backlog through Done)</li>
                <li>Import tasks and sprints from Jira via CSV</li>
                <li>Understand velocity by separating new work from carried-over work</li>
              </ul>
              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Key Concept:</strong> Sprints define the time period and capacity, while tasks represent the individual
                units of work. Tasks are assigned to sprints, and their story points contribute to the sprint's progress.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Creating Sprints */}
      <div id="sprints-tasks-section-2">
        <Card>
          <CardHeader title="2. Creating Sprints" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>To create a new sprint, click "New Sprint" on the Sprints page and fill in:</p>
              <ol className="list-decimal list-inside space-y-3 ml-4">
                <li><strong>Team Name</strong> — A short identifier for your team (e.g., "LP", "PM")</li>
                <li><strong>Quarter</strong> — Select Q1, Q2, Q3, or Q4</li>
                <li><strong>Year</strong> — The sprint year (defaults to current year)</li>
                <li><strong>Sprint Number</strong> — Sequential number within the quarter (starting at 1)</li>
              </ol>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Sprint Name Format</h4>
                <p className="text-sm mb-2">
                  Hive auto-generates the sprint name using this pattern:
                </p>
                <code className="block bg-slate-900 dark:bg-slate-950 text-green-400 p-3 rounded text-xs">
                  {'{TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}'}
                </code>
                <p className="text-sm mt-2">
                  <strong>Example:</strong> Team "LP", Q4, Year 2025, Sprint 6 → <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">LP_4Q25_S6</code>
                </p>
                <p className="text-sm mt-1">
                  A live preview of the generated name is shown below the inputs as you type.
                </p>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Note:</strong> The naming convention matters if you use the Jira import feature — the team name prefix
                is used to filter which sprints get imported.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Editing Sprint Capacity */}
      <div id="sprints-tasks-section-3">
        <Card>
          <CardHeader title="3. Editing Sprint Capacity" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Click the edit (pencil) icon on any sprint to configure it:</p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Start & End Dates</h4>
                  <p className="text-sm">
                    Set the sprint boundaries. If you don't set dates, Hive estimates them from the quarter and sprint number
                    (assuming 2-week sprints starting from the quarter start).
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Committed Story Points</h4>
                  <p className="text-sm">
                    The total story points the team commits to completing in this sprint.
                    This is used for capacity utilization calculations on the Dashboard.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Available Members (Auto-Calculated)</h4>
                  <p className="text-sm mb-2">
                    This field is read-only and auto-recalculates when you save. The formula is:
                  </p>
                  <code className="block bg-slate-900 dark:bg-slate-950 text-green-400 p-3 rounded text-xs">
                    Available = Total Team Size - (Total Leave Days / Sprint Working Days)
                  </code>
                  <p className="text-sm mt-2">
                    It counts all direct reports, calculates working days in the sprint (excluding weekends),
                    then subtracts the proportional capacity lost to approved leave.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Tip:</strong> Set accurate sprint dates and keep leave records up to date — the available members
                calculation depends on both to give you a realistic capacity figure.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: Sprint Team Filter */}
      <div id="sprints-tasks-section-4">
        <Card>
          <CardHeader title="4. Sprint Team Filter (Jira)" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Sprint Team Filter controls which sprints are imported when you import tasks from Jira.
                It's configured in the blue info box at the top of the Sprints page.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">When Filter Is Empty</h4>
                  <p className="text-sm">All sprints from the Jira CSV are imported regardless of team name.</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">When Filter Is Set (e.g., "LP")</h4>
                  <p className="text-sm">
                    Only sprints whose name starts with "LP" are imported. Other sprints (e.g., "PM_1Q25_S1") are skipped
                    with a warning in the import results.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>When to use:</strong> Set this filter if your Jira board contains sprints from multiple teams
                and you only want to track your own team's sprints in Hive.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Managing Tasks */}
      <div id="sprints-tasks-section-5">
        <Card>
          <CardHeader title="5. Managing Tasks" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Tasks are the individual units of work within sprints. Click "New Task" to create one.</p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Task Fields</h4>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                  <li><strong>Title</strong> (required) — Task name</li>
                  <li><strong>Description</strong> — Additional details</li>
                  <li><strong>Priority</strong> — Low, Medium (default), High, or Critical</li>
                  <li><strong>Type</strong> — Task, Epic, Story, Sub-task, Bug, Spike, or Support</li>
                  <li><strong>Assignee</strong> — Team member responsible</li>
                  <li><strong>Due Date</strong> — Deadline</li>
                  <li><strong>Story Points</strong> — Effort estimate (1, 2, 3, 5, 8…)</li>
                  <li><strong>Time Spent</strong> — Actual time in minutes</li>
                  <li><strong>Sprint</strong> — Sprint assignment (e.g., "Sprint 1")</li>
                  <li><strong>Labels</strong> — Comma-separated tags (e.g., "frontend, urgent")</li>
                  <li><strong>Components</strong> — Comma-separated technical areas (e.g., "API, Database")</li>
                </ul>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Task Statuses (10 Stages)</h4>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-slate-400" />Backlog
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-blue-400" />To Do
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-orange-400" />Blocked
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-amber-400" />In Progress
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-purple-400" />In Review
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-cyan-400" />In Test
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-indigo-400" />PO Acceptance
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-emerald-400" />Ready To Release
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-green-500" />Done
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-red-400" />Cancelled
                  </div>
                </div>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Task Actions</h4>
                <p className="text-sm">Each task has a context menu (three dots) with:</p>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4 mt-2">
                  <li><strong>Edit</strong> — Modify all task fields</li>
                  <li><strong>Duplicate</strong> — Create a full copy of the task</li>
                  <li><strong>Pin (Override)</strong> — Lock specific fields so they're preserved during Jira re-imports</li>
                  <li><strong>Delete</strong> — Remove the task (with confirmation)</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Story Points & Carried-Over */}
      <div id="sprints-tasks-section-6">
        <Card>
          <CardHeader title="6. Story Points & Carried-Over" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Hive tracks three metrics per task: story points, estimated hours, and actual time spent.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Story Points (SP)</h4>
                  <p className="text-sm">
                    The effort estimate for a task. Displayed with a bolt icon. The backend automatically converts
                    story points to estimated hours using the mapping configured in Settings.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Carried-Over Points</h4>
                  <p className="text-sm mb-2">
                    When a task spans multiple sprints, Hive tracks how much work was done in previous sprints vs. the
                    current sprint:
                  </p>
                  <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                    <li><strong>New SP</strong> = Total SP - Previous Sprints SP</li>
                    <li><strong>Carried-Over SP</strong> = Previous Sprints SP (from original sprint)</li>
                  </ul>
                  <p className="text-sm mt-2">
                    On task cards, this appears as <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">3+2 SP</code> meaning
                    3 new + 2 carried over.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Why it matters:</strong> The Dashboard's velocity chart separates new work from carried-over work,
                giving you an accurate picture of the team's actual throughput rather than inflating velocity with
                re-counted points.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 7: Filtering & Pagination */}
      <div id="sprints-tasks-section-7">
        <Card>
          <CardHeader title="7. Filtering & Pagination" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>The Tasks page provides powerful filtering to quickly find what you need:</p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Search</h4>
                  <p className="text-sm">Full-text search across task titles and descriptions. Results update in real-time with a 300ms debounce.</p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Status Filter</h4>
                  <p className="text-sm">
                    Filter by any of the 10 statuses, or use "All" and "Overdue" as special filters.
                    Each status button shows its task count. Only statuses with tasks appear.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Label & Sprint Filters</h4>
                  <p className="text-sm">
                    Click any label or sprint name to filter tasks to just that label/sprint.
                    Click again to deselect. These can be combined with the status filter.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Exclude Parents Toggle</h4>
                  <p className="text-sm">
                    When enabled, hides parent tasks (e.g., Epics) to show only leaf-level tasks.
                    Useful for focusing on the actual work items.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Pagination</h4>
                  <p className="text-sm">
                    Choose to display 10, 20 (default), 50, or 100 tasks per page. Navigate between pages
                    with Previous/Next buttons. The summary bar shows aggregate story points, estimated hours,
                    and time spent for the current page.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>URL Deep Linking:</strong> You can link directly to a filtered view using the <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">?filter=status</code> URL
                parameter (e.g., <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">?filter=overdue</code>). The Dashboard uses this to link to overdue tasks.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 8: Jira Import */}
      <div id="sprints-tasks-section-8">
        <Card>
          <CardHeader title="8. Jira Import" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Import tasks from Jira by uploading a CSV export. Click "Import from Jira" on the Tasks page.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Import Flow</h4>
                <ol className="list-decimal list-inside space-y-2 text-sm ml-4">
                  <li>Export tasks from Jira as CSV (the dialog includes step-by-step instructions)</li>
                  <li>Upload the CSV file and click "Preview Import"</li>
                  <li>Review the preview: valid/invalid rows, detected columns, sample data</li>
                  <li>Configure import options:
                    <ul className="list-disc list-inside ml-6 mt-1 space-y-1">
                      <li><strong>Update Existing</strong> — Update matching tasks or skip them</li>
                      <li><strong>Match By</strong> — Issue Key (recommended) or Title</li>
                    </ul>
                  </li>
                  <li>Click "Import" and review the results (imported, skipped, errors)</li>
                </ol>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Field Overrides (Pinning)</h4>
                <p className="text-sm mb-2">
                  When you re-import from Jira, some fields you've manually edited in Hive might get overwritten.
                  Use "Pin" (via the task context menu) to lock fields like:
                </p>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                  <li>Assignee</li>
                  <li>Story Points & Estimated Hours</li>
                  <li>Time Spent</li>
                  <li>Previous Sprints SP (for multi-sprint tasks)</li>
                  <li>Sprint assignment</li>
                </ul>
                <p className="text-sm mt-2">
                  Pinned fields show a pin icon and are preserved during future imports.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 9: Bulk Operations */}
      <div id="sprints-tasks-section-9">
        <Card>
          <CardHeader title="9. Bulk Operations" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>For managing multiple tasks at once:</p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Bulk Delete</h4>
                <ol className="list-decimal list-inside space-y-2 text-sm ml-4">
                  <li>Use the checkboxes next to individual tasks, or "Select All" for the current page</li>
                  <li>A "Delete Selected (X)" button appears showing the count</li>
                  <li>Confirm the deletion in the dialog</li>
                </ol>
              </div>

              <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3 text-sm">
                <strong>Warning:</strong> Bulk delete is permanent and cannot be undone. Double-check your selection
                before confirming.
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Footer */}
      <div className="text-center py-8 border-t border-slate-200 dark:border-slate-700">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Have questions or suggestions for this tutorial?<br/>
          Reach out to your Hive administrator.
        </p>
      </div>
    </div>
  )
}

function LeavesTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Leave Management Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Learn how to track team leave, use the calendar view, and manage public holidays
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview & Leave Types',
              'Creating Leave Records',
              'Editing & Deleting',
              'Calendar View',
              'Analytics & Overview Cards',
              'Business Days Calculation',
              'Tips & Best Practices'
            ].map((section, index) => (
              <button
                key={index}
                onClick={() => {
                  const element = document.getElementById(`leaves-section-${index + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="leaves-section-1">
        <Card>
          <CardHeader title="1. Overview & Leave Types" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Leaves page is a capacity-planning tool that lets you track when team members are away.
                It provides a calendar view, analytics, and integrates with sprint capacity calculations.
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3">Four Leave Types</h4>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="flex items-center gap-3 p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <span className="w-3 h-3 rounded-full bg-green-500" />
                    <div>
                      <p className="font-medium text-sm">Vacation</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Planned time off</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 p-2 bg-orange-50 dark:bg-orange-900/20 rounded-lg">
                    <span className="w-3 h-3 rounded-full bg-orange-500" />
                    <div>
                      <p className="font-medium text-sm">Sick Leave</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Illness or medical leave</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <span className="w-3 h-3 rounded-full bg-blue-500" />
                    <div>
                      <p className="font-medium text-sm">Other</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Any other time away</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 p-2 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                    <span className="w-3 h-3 rounded-full bg-purple-500" />
                    <div>
                      <p className="font-medium text-sm">Public Holiday</p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">Applies to all team members</p>
                    </div>
                  </div>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Key Concept:</strong> Leave records feed directly into sprint capacity calculations. When a team
                member has leave during a sprint, their availability is automatically reduced in the sprint's
                "Available Members" count.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Creating Leave Records */}
      <div id="leaves-section-2">
        <Card>
          <CardHeader title="2. Creating Leave Records" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Click "New Leave" to open the creation form. The fields adapt based on the leave type:</p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Regular Leave (Vacation, Sick, Other)</h4>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                  <li><strong>Team Member</strong> — Select from the dropdown</li>
                  <li><strong>Leave Type</strong> — Vacation, Sick Leave, or Other</li>
                  <li><strong>Start Date</strong> — First day of leave</li>
                  <li><strong>End Date</strong> — Last day of leave (inclusive)</li>
                  <li><strong>Notes</strong> — Optional details</li>
                </ul>
              </div>

              <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Public Holiday (Special)</h4>
                <p className="text-sm mb-2">
                  When you select "Public Holiday", the Team Member field disappears because the holiday is automatically
                  created for <strong>all active team members</strong>.
                </p>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                  <li><strong>Holiday Name</strong> — e.g., "Christmas Day", "New Year's Day"</li>
                  <li><strong>Start Date & End Date</strong> — The holiday period</li>
                </ul>
                <p className="text-sm mt-2">
                  After creation, you'll see a summary: how many members it was created for and any members
                  skipped due to existing leave on those dates.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Editing & Deleting */}
      <div id="leaves-section-3">
        <Card>
          <CardHeader title="3. Editing & Deleting" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Click on any leave badge in the calendar to edit it.</p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What You Can Edit</h4>
                  <p className="text-sm">
                    Leave type, start date, end date, and notes can all be modified.
                    The team member cannot be changed — delete and recreate instead.
                  </p>
                </div>

                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3 text-sm">
                  <strong>Deleting:</strong> The edit dialog includes a red "Delete" button at the bottom.
                  Deletion is permanent and works on any leave record, including past ones.
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Note:</strong> Hive tracks leave for capacity planning purposes. Formal approvals are
                expected to be handled in your HR system (e.g., HiBob).
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: Calendar View */}
      <div id="leaves-section-4">
        <Card>
          <CardHeader title="4. Calendar View" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The main interface is a monthly calendar grid showing all leave records:
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Daily Cells</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                    <li>Each cell shows up to 4 leave entries with the member's first name and a color-coded icon</li>
                    <li>If more than 4 people are on leave, a "+X more" indicator appears</li>
                    <li>Click any leave badge to edit it</li>
                    <li>Today's date is highlighted with an amber border</li>
                    <li>Weekends have a subtle grey background</li>
                  </ul>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Hover Tooltip</h4>
                  <p className="text-sm">
                    Hover over any day cell for 1 second to see a detailed tooltip showing all people on leave
                    that day, their leave type, and any notes.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Navigation</h4>
                  <p className="text-sm">
                    Use the left/right arrows to move between months, or click "Today" to jump back to the current month.
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Analytics */}
      <div id="leaves-section-5">
        <Card>
          <CardHeader title="5. Analytics & Overview Cards" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Above the calendar, four overview cards provide a quick summary:</p>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
                  <p className="font-medium text-sm">Total Records</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">All leave records ever created</p>
                </div>
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
                  <p className="font-medium text-sm">On Leave Today</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">Team members currently away</p>
                </div>
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
                  <p className="font-medium text-sm">This Week</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">Team members on leave this week</p>
                </div>
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
                  <p className="font-medium text-sm">Upcoming</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">Upcoming leave records</p>
                </div>
              </div>

              <p className="text-sm">
                A <strong>trend line chart</strong> below the cards shows the number of people on leave per day
                for the current month, helping you spot high-absence periods at a glance.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Business Days Calculation */}
      <div id="leaves-section-6">
        <Card>
          <CardHeader title="6. Business Days Calculation" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Hive automatically calculates business days for each leave record:
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">How It Works</h4>
                <ul className="list-disc list-inside space-y-2 text-sm ml-4">
                  <li>
                    <strong>Total Days</strong> = End Date - Start Date + 1 (inclusive on both ends)
                  </li>
                  <li>
                    <strong>Business Days</strong> = Total Days minus Saturdays and Sundays
                  </li>
                </ul>
                <p className="text-sm mt-2">
                  <strong>Example:</strong> Friday to Monday (4 calendar days) = 2 business days (Friday + Monday).
                </p>
              </div>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Validation Rules</h4>
                <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                  <li>End date cannot be before start date</li>
                  <li>Leave duration cannot exceed 365 days</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 7: Tips */}
      <div id="leaves-section-7">
        <Card>
          <CardHeader title="7. Tips & Best Practices" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3">Recommendations</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Create public holidays at the start of the year</strong> — This ensures sprint capacity
                    is accurate from day one
                  </li>
                  <li>
                    <strong>Log leave as soon as it's known</strong> — The earlier leave is recorded, the more accurate
                    your sprint capacity planning becomes
                  </li>
                  <li>
                    <strong>Use the trend chart</strong> — Identify weeks with high absence before sprint planning so
                    you can adjust committed points accordingly
                  </li>
                  <li>
                    <strong>Check "On Leave Today"</strong> — Quick glance at who's available for meetings or urgent tasks
                  </li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Footer */}
      <div className="text-center py-8 border-t border-slate-200 dark:border-slate-700">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Have questions or suggestions for this tutorial?<br/>
          Reach out to your Hive administrator.
        </p>
      </div>
    </div>
  )
}

function ReviewsTutorial({ onBack }: TutorialProps) {
  return (
    <div className="space-y-6 max-w-5xl">
      {/* Back Button */}
      <button
        onClick={onBack}
        className="flex items-center gap-2 text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 transition-colors"
      >
        <Home className="w-4 h-4" />
        Back to Tutorials
      </button>

      {/* Title */}
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          Performance Reviews Tutorial
        </h1>
        <p className="text-lg text-slate-600 dark:text-slate-400">
          Learn how to create and manage performance reviews with ratings and feedback
        </p>
      </div>

      {/* Table of Contents */}
      <Card>
        <CardHeader title="Table of Contents" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {[
              'Overview',
              'Creating Reviews',
              'Rating System',
              'Editing & Deleting',
              'Filtering & Search',
              'Tips & Best Practices'
            ].map((section, index) => (
              <button
                key={index}
                onClick={() => {
                  const element = document.getElementById(`reviews-section-${index + 1}`)
                  element?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }}
                className="flex items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded transition-colors"
              >
                <ChevronRight className="w-4 h-4 text-amber-500" />
                {section}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Section 1: Overview */}
      <div id="reviews-section-1">
        <Card>
          <CardHeader title="1. Overview" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                The Performance Reviews page lets you create, manage, and track reviews for your direct reports.
                Each review captures:
              </p>
              <ul className="list-disc list-inside space-y-2 ml-4">
                <li>A rating on a 5-level scale</li>
                <li>Key strengths and achievements</li>
                <li>Areas for improvement and development goals</li>
                <li>Private manager notes</li>
              </ul>
              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Key Concept:</strong> Reviews are linked to a team member and a review period (e.g., "2025 Q4").
                Once created, the team member and period cannot be changed — this ensures an accurate historical record.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 2: Creating Reviews */}
      <div id="reviews-section-2">
        <Card>
          <CardHeader title="2. Creating Reviews" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Click "New Review" to open the creation form:</p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Form Fields</h4>
                <ul className="list-disc list-inside space-y-2 text-sm ml-4">
                  <li><strong>Team Member</strong> (required) — Select from your direct reports dropdown</li>
                  <li><strong>Review Period</strong> (required) — Free-text field (e.g., "2025 Q4", "H1 2025")</li>
                  <li><strong>Rating</strong> — Select from the 5 rating levels (see next section)</li>
                  <li><strong>Strengths</strong> — Key strengths and achievements</li>
                  <li><strong>Areas for Improvement</strong> — Development areas and goals</li>
                  <li><strong>Manager Notes</strong> — Private notes (not shared with the team member)</li>
                </ul>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Note:</strong> Team Member and Review Period are locked after creation. Plan your
                review period naming convention upfront (e.g., always use "YYYY QN" format) for consistency.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 3: Rating System */}
      <div id="reviews-section-3">
        <Card>
          <CardHeader title="3. Rating System" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Reviews use a 5-level rating scale, displayed as star ratings on review cards:
              </p>

              <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                <div className="space-y-3">
                  <div className="flex items-center justify-between p-2 border-b border-slate-200 dark:border-slate-700">
                    <span className="font-medium text-sm">Not Rated</span>
                    <span className="text-sm text-slate-400">No stars (default)</span>
                  </div>
                  <div className="flex items-center justify-between p-2 border-b border-slate-200 dark:border-slate-700">
                    <span className="font-medium text-sm">Needs Improvement</span>
                    <div className="flex gap-0.5">
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                    </div>
                  </div>
                  <div className="flex items-center justify-between p-2 border-b border-slate-200 dark:border-slate-700">
                    <span className="font-medium text-sm">Meets Expectations</span>
                    <div className="flex gap-0.5">
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                    </div>
                  </div>
                  <div className="flex items-center justify-between p-2 border-b border-slate-200 dark:border-slate-700">
                    <span className="font-medium text-sm">Exceeds Expectations</span>
                    <div className="flex gap-0.5">
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-slate-200 dark:text-slate-600" />
                    </div>
                  </div>
                  <div className="flex items-center justify-between p-2">
                    <span className="font-medium text-sm">Outstanding</span>
                    <div className="flex gap-0.5">
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                      <Star className="w-4 h-4 text-amber-400 fill-amber-400" />
                    </div>
                  </div>
                </div>
              </div>

              <p className="text-sm bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
                <strong>Note:</strong> "Not Rated" shows no stars and is the default. It's useful for creating draft reviews
                where you fill in the qualitative feedback first and set the rating later.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 4: Editing & Deleting */}
      <div id="reviews-section-4">
        <Card>
          <CardHeader title="4. Editing & Deleting" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>
                Use the three-dot menu on each review card to access edit and delete actions.
              </p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What You Can Edit</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                    <li><strong>Rating</strong> — Change the rating at any time</li>
                    <li><strong>Strengths</strong> — Update achievements and strengths</li>
                    <li><strong>Areas for Improvement</strong> — Refine development goals</li>
                    <li><strong>Manager Notes</strong> — Add or update private notes</li>
                  </ul>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">What's Locked</h4>
                  <ul className="list-disc list-inside space-y-1 text-sm ml-4">
                    <li><strong>Team Member</strong> — Cannot be changed after creation (shown as disabled)</li>
                    <li><strong>Review Period</strong> — Cannot be changed after creation (shown as disabled)</li>
                  </ul>
                </div>
              </div>

              <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3 text-sm">
                <strong>Deleting:</strong> Select "Delete" from the context menu. A confirmation dialog
                will appear before the review is permanently removed.
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 5: Filtering & Search */}
      <div id="reviews-section-5">
        <Card>
          <CardHeader title="5. Filtering & Search" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <p>Find reviews quickly using the built-in search and filter tools:</p>

              <div className="space-y-3">
                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Search Bar</h4>
                  <p className="text-sm">
                    Full-text search across team member names, review periods, strengths, and areas for improvement.
                    Results filter in real-time as you type.
                  </p>
                </div>

                <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
                  <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">Team Member Quick Filters</h4>
                  <p className="text-sm">
                    Below the search bar, buttons for each team member let you filter to see only that person's reviews.
                    Click a name to toggle the filter on/off.
                  </p>
                </div>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Tip:</strong> Combine search with the team member filter to find specific reviews.
                For example, filter by "Alice" then search for "Q4" to see Alice's Q4 review.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Section 6: Tips & Best Practices */}
      <div id="reviews-section-6">
        <Card>
          <CardHeader title="6. Tips & Best Practices" />
          <CardContent>
            <div className="space-y-4 text-slate-700 dark:text-slate-300">
              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                <h4 className="font-semibold text-slate-900 dark:text-slate-100 mb-3">Recommendations</h4>
                <ul className="list-disc list-inside space-y-2 text-sm">
                  <li>
                    <strong>Use a consistent period format</strong> — Pick one convention like "YYYY QN" (e.g., "2025 Q4")
                    and stick to it across all reviews for easy filtering and comparison
                  </li>
                  <li>
                    <strong>Start with "Not Rated"</strong> — Create draft reviews with qualitative feedback first,
                    then set the rating once you've finalized your assessment
                  </li>
                  <li>
                    <strong>Be specific in strengths</strong> — Reference concrete achievements, projects delivered,
                    or behaviors observed rather than generic praise
                  </li>
                  <li>
                    <strong>Make improvement areas actionable</strong> — Instead of "improve communication",
                    try "lead sprint retrospectives to practice facilitating team discussions"
                  </li>
                  <li>
                    <strong>Use manager notes for context</strong> — Record calibration decisions, compensation
                    considerations, or promotion readiness that you don't want in the official review
                  </li>
                </ul>
              </div>

              <p className="text-sm bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                <strong>Remember:</strong> Performance reviews are a snapshot in time. Combine them with regular 1:1
                meetings and manager notes for a complete picture of each team member's growth trajectory.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Footer */}
      <div className="text-center py-8 border-t border-slate-200 dark:border-slate-700">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Have questions or suggestions for this tutorial?<br/>
          Reach out to your Hive administrator.
        </p>
      </div>
    </div>
  )
}
