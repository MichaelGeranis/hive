import { useState } from 'react'
import { BookOpen, ChevronRight, Home, TrendingUp, Calculator, Zap, GitBranch, Award } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'

type TutorialId = 'knowledge-matrix'

interface Tutorial {
  id: TutorialId
  title: string
  icon: typeof BookOpen
  description: string
  category: string
}

const tutorials: Tutorial[] = [
  {
    id: 'knowledge-matrix',
    title: 'Knowledge Matrix',
    icon: Award,
    description: 'Learn how to track team knowledge across projects using levels and points',
    category: 'Team Development'
  }
]

export default function Tutorials() {
  const [selectedTutorial, setSelectedTutorial] = useState<TutorialId | null>(null)

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

      {/* Tutorial Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {tutorials.map((tutorial) => {
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
    </div>
  )
}

interface TutorialContentProps {
  tutorialId: TutorialId
  onBack: () => void
}

function TutorialContent({ tutorialId, onBack }: TutorialContentProps) {
  if (tutorialId === 'knowledge-matrix') {
    return <KnowledgeMatrixTutorial onBack={onBack} />
  }

  return null
}

interface TutorialProps {
  onBack: () => void
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
