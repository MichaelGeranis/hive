import { Outlet, NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  Star,
  Calendar,
  FolderKanban,
  Layers,
  CheckSquare,
  Settings,
  Hexagon,
  Palmtree,
  StickyNote,
  Zap,
  FileText
} from 'lucide-react'

const navigation = [
  { name: 'Dashboard', to: '/dashboard', icon: LayoutDashboard },
  { name: 'Projects', to: '/projects', icon: FolderKanban },
  { name: 'Parents', to: '/parents', icon: Layers },
  { name: 'Tasks', to: '/tasks', icon: CheckSquare },
  { name: 'Sprints', to: '/sprints', icon: Zap },
  { name: 'Team', to: '/team', icon: Users },
  { name: '1:1 Meetings', to: '/meetings', icon: Calendar },
  { name: 'Reviews', to: '/reviews', icon: Star },
  { name: 'Leaves', to: '/leaves', icon: Palmtree },
  { name: 'TODOs', to: '/notes', icon: StickyNote },
  { name: 'Documents', to: '/documents', icon: FileText },
]

export default function Layout() {
  // Check if running on macOS
  const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0

  return (
    <div className="flex h-screen bg-slate-50 dark:bg-slate-900">
      {/* Sidebar */}
      <aside className="w-64 bg-slate-900 dark:bg-slate-950 text-white flex flex-col">
        {/* Logo / Title bar area - extra padding on macOS for traffic lights */}
        <div className={`flex items-center px-4 titlebar-drag border-b border-slate-700 dark:border-slate-800 ${isMac ? 'h-16 pt-6' : 'h-14'}`}>
          <div className={`flex items-center gap-2 titlebar-no-drag ${isMac ? 'ml-16' : ''}`}>
            <Hexagon className="w-8 h-8 text-amber-400" />
            <span className="text-xl font-bold">Hive</span>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-3 py-4 space-y-1">
          {navigation.map((item) => (
            <NavLink
              key={item.name}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-amber-500 text-white'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                }`
              }
            >
              <item.icon className="w-5 h-5" />
              {item.name}
            </NavLink>
          ))}
        </nav>

        {/* Settings / Footer */}
        <div className="px-3 py-4 border-t border-slate-700 dark:border-slate-800">
          <NavLink
            to="/settings"
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 w-full rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-amber-500 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              }`
            }
          >
            <Settings className="w-5 h-5" />
            Settings
          </NavLink>
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
