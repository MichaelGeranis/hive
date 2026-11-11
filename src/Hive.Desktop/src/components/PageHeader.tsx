import { ReactNode } from 'react'

interface PageHeaderProps {
  title: ReactNode
  /** One line on what the page is for */
  subtitle?: ReactNode
  /** Shown before the title */
  icon?: ReactNode
  /** The page's own actions, right-aligned: sync, add, import */
  actions?: ReactNode
}

/**
 * The top of every page: its title, what it is for, and its actions.
 */
export function PageHeader({ title, subtitle, icon, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div className="min-w-0">
        <h1 className="flex items-center gap-2 text-2xl font-bold text-slate-900">
          {icon}
          {title}
        </h1>
        {subtitle && <p className="mt-1 text-slate-500">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  )
}
