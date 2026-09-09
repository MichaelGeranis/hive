import { useState } from 'react'
import { Outlet, NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  Star,
  Calendar,
  CalendarDays,
  FolderKanban,
  Layers,
  CheckSquare,
  Settings,
  Hexagon,
  Palmtree,
  StickyNote,
  Zap,
  FileText,
  ScrollText,
  Award,
  Activity,
  ClipboardList,
  Brain,
  Target,
  PanelLeftClose,
  PanelLeft,
  BookOpen
} from 'lucide-react'

const navigationGroups = [
  // Overview
  [
    { name: 'Dashboard', to: '/dashboard', icon: LayoutDashboard },
    { name: 'Calendar', to: '/calendar', icon: CalendarDays },
    { name: 'Notes', to: '/notes', icon: StickyNote },
  ],
  // Delivery
  [
    { name: 'Quarterly Planning', to: '/quarterly-planning', icon: Target },
    { name: 'Sprints', to: '/sprints', icon: Zap },
    { name: 'Parents', to: '/parents', icon: Layers },
    { name: 'Tasks', to: '/tasks', icon: CheckSquare },
    { name: 'Projects', to: '/projects', icon: FolderKanban },
    { name: 'Knowledge Matrix', to: '/project-knowledge', icon: Brain },
  ],
  // People
  [
    { name: 'Team', to: '/team', icon: Users },
    { name: '1:1 Meetings', to: '/meetings', icon: Calendar },
    { name: 'Leaves', to: '/leaves', icon: Palmtree },
    { name: 'Reviews', to: '/reviews', icon: Star },
    { name: 'Skills', to: '/skills', icon: Award },
  ],
  // Resources
  [
    { name: 'Activity Feed', to: '/activity-feed', icon: Activity },
    { name: 'Documents', to: '/documents', icon: FileText },
    { name: 'Hiring', to: '/checklists', icon: ClipboardList },
    { name: 'Tutorials', to: '/tutorials', icon: BookOpen },
  ],
]

export default function Layout() {
  // Check if running on macOS
  const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0
  const [isMenuCollapsed, setIsMenuCollapsed] = useState(false)

  return (
    <div className="flex h-screen bg-slate-50 dark:bg-slate-900">
      {/* Sidebar */}
      <aside className={`${isMenuCollapsed ? 'w-16' : 'w-64'} bg-slate-900 dark:bg-slate-950 text-white flex flex-col transition-all duration-300`}>
        {/* Logo / Title bar area - extra padding on macOS for traffic lights */}
        <div className={`flex items-center px-4 titlebar-drag border-b border-slate-700 dark:border-slate-800 ${isMac ? 'h-16 pt-6' : 'h-14'}`}>
          <div className={`flex items-center gap-2 titlebar-no-drag ${isMac ? 'ml-16' : ''}`}>
            <Hexagon className="w-8 h-8 text-amber-400" />
            {!isMenuCollapsed && <span className="text-xl font-bold">Hive</span>}
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-3 py-4 overflow-y-auto">
          {navigationGroups.map((group, groupIndex) => (
            <div key={groupIndex}>
              <div className="space-y-1">
                {group.map((item) => (
                  <NavLink
                    key={item.name}
                    to={item.to}
                    title={item.name}
                    className={({ isActive }) =>
                      `flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                        isActive
                          ? 'bg-amber-500 text-white'
                          : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                      }`
                    }
                  >
                    <item.icon className="w-5 h-5 flex-shrink-0" />
                    {!isMenuCollapsed && item.name}
                  </NavLink>
                ))}
              </div>
              {/* Divider between groups (except after last group) */}
              {groupIndex < navigationGroups.length - 1 && (
                <div className="my-3 border-t border-slate-700 dark:border-slate-800" />
              )}
            </div>
          ))}
        </nav>

        {/* Settings / Footer */}
        <div className="px-3 py-4 border-t border-slate-700 dark:border-slate-800 space-y-1">
          <NavLink
            to="/logs"
            title="Logs"
            className={({ isActive }) =>
              `flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-amber-500 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              }`
            }
          >
            <ScrollText className="w-5 h-5 flex-shrink-0" />
            {!isMenuCollapsed && 'Logs'}
          </NavLink>
          <NavLink
            to="/settings"
            title="Settings"
            className={({ isActive }) =>
              `flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-amber-500 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              }`
            }
          >
            <Settings className="w-5 h-5 flex-shrink-0" />
            {!isMenuCollapsed && 'Settings'}
          </NavLink>
          {/* Toggle button */}
          <button
            onClick={() => setIsMenuCollapsed(!isMenuCollapsed)}
            className={`flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors text-slate-300 hover:bg-slate-800 hover:text-white`}
            title={isMenuCollapsed ? 'Expand menu' : 'Collapse menu'}
          >
            {isMenuCollapsed ? (
              <PanelLeft className="w-5 h-5 flex-shrink-0" />
            ) : (
              <>
                <PanelLeftClose className="w-5 h-5 flex-shrink-0" />
                Collapse
              </>
            )}
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto bg-slate-50 dark:bg-slate-900">
        {/* Title bar drag area for macOS */}
        <div className="h-8 titlebar-drag bg-slate-100 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700" />

        {/* Page content */}
        <div className="p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
