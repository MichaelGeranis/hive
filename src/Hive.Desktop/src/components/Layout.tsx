import { useEffect, useState } from 'react'
import { Outlet, NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  Calendar,
  CalendarDays,
  FolderKanban,
  Layers,
  Package,
  CheckSquare,
  Settings,
  Palmtree,
  StickyNote,
  Zap,
  ScrollText,
  Activity,
  Brain,
  Target,
  PanelLeftClose,
  PanelLeft,
  BookOpen,
  Trophy
} from 'lucide-react'
import { subscribe, getUnseenErrorCount } from '../services/logStore'
import { HiveMark, HiveWordmark, hiveMarkLabel } from './HiveMark'
import { useHiveStatus } from '../contexts/HiveStatusContext'

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
    { name: 'Products', to: '/products', icon: Package },
    { name: 'Parents', to: '/parents', icon: Layers },
    { name: 'Tasks', to: '/tasks', icon: CheckSquare },
    { name: 'Projects', to: '/projects', icon: FolderKanban },
    { name: 'Knowledge Matrix', to: '/project-knowledge', icon: Brain },
  ],
  // People
  [
    { name: 'Team', to: '/team', icon: Users },
    { name: '1:1 Meetings', to: '/meetings', icon: Calendar },
    { name: 'Career Plans', to: '/career-plans', icon: Trophy },
    { name: 'Leaves', to: '/leaves', icon: Palmtree },
  ],
  // Resources
  [
    { name: 'Activity Feed', to: '/activity-feed', icon: Activity },
    { name: 'Tutorials', to: '/tutorials', icon: BookOpen },
  ],
]

export default function Layout() {
  // Check if running on macOS
  const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0
  const [isMenuCollapsed, setIsMenuCollapsed] = useState(false)
  const [unseenErrors, setUnseenErrors] = useState(getUnseenErrorCount())
  const { attention, busy } = useHiveStatus()

  useEffect(() => subscribe(() => setUnseenErrors(getUnseenErrorCount())), [])

  return (
    <div className="flex h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className={`${isMenuCollapsed ? 'w-16' : 'w-64'} bg-slate-900 text-white flex flex-col transition-all duration-300`}>
        {/* Logo / Title bar area - extra padding on macOS for traffic lights */}
        <div className={`flex items-center px-4 titlebar-drag border-b border-slate-700 ${isMac ? 'h-16 pt-6' : 'h-14'}`}>
          <NavLink to="/dashboard" title={hiveMarkLabel(attention)} className={`flex items-center gap-2.5 titlebar-no-drag ${isMac && !isMenuCollapsed ? 'ml-16' : ''}`}>
            <HiveMark size={32} attention={attention} busy={busy} animateIn />
            {!isMenuCollapsed && <HiveWordmark />}
          </NavLink>
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
                    <item.icon className="w-5 h-5 shrink-0" />
                    {!isMenuCollapsed && item.name}
                  </NavLink>
                ))}
              </div>
              {/* Divider between groups (except after last group) */}
              {groupIndex < navigationGroups.length - 1 && (
                <div className="my-3 border-t border-slate-700" />
              )}
            </div>
          ))}
        </nav>

        {/* Settings / Footer */}
        <div className="px-3 py-4 border-t border-slate-700 space-y-1">
          <NavLink
            to="/logs"
            title={unseenErrors > 0 ? `Logs: ${unseenErrors} new errors` : 'Logs'}
            className={({ isActive }) =>
              `flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-amber-500 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              }`
            }
          >
            <span className="relative shrink-0">
              <ScrollText className="w-5 h-5" />
              {isMenuCollapsed && unseenErrors > 0 && (
                <span className="absolute -top-1 -right-1 w-2.5 h-2.5 rounded-full bg-red-500 ring-2 ring-slate-900" />
              )}
            </span>
            {!isMenuCollapsed && 'Logs'}
            {!isMenuCollapsed && unseenErrors > 0 && (
              <span
                className="ml-auto px-1.5 min-w-5 text-center rounded-full bg-red-500 text-white text-xs font-semibold tabular-nums"
                aria-label={`${unseenErrors} new errors`}
              >
                {unseenErrors > 99 ? '99+' : unseenErrors}
              </span>
            )}
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
            <Settings className="w-5 h-5 shrink-0" />
            {!isMenuCollapsed && 'Settings'}
          </NavLink>
          {/* Toggle button */}
          <button
            onClick={() => setIsMenuCollapsed(!isMenuCollapsed)}
            className={`flex items-center ${isMenuCollapsed ? 'justify-center' : 'gap-3'} px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors text-slate-300 hover:bg-slate-800 hover:text-white`}
            title={isMenuCollapsed ? 'Expand menu' : 'Collapse menu'}
          >
            {isMenuCollapsed ? (
              <PanelLeft className="w-5 h-5 shrink-0" />
            ) : (
              <>
                <PanelLeftClose className="w-5 h-5 shrink-0" />
                Collapse
              </>
            )}
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto bg-slate-50">
        {/* Title bar drag area for macOS */}
        <div className="h-8 titlebar-drag bg-slate-100 border-b border-slate-200" />

        {/* Page content */}
        <div className="p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
