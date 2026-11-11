import { ReactNode } from 'react'
import { RefreshCw } from 'lucide-react'

/**
 * Something is loading. `fill` takes the page's height, for a whole page; otherwise it sits in a
 * card or a panel.
 */
export function LoadingState({ label, fill = false }: { label?: string; fill?: boolean }) {
  return (
    <div
      role="status"
      aria-label={label ?? 'Loading'}
      className={`flex flex-col items-center justify-center gap-3 ${fill ? 'h-64' : 'h-40'}`}
    >
      <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-amber-500" />
      {label && <p className="text-sm text-slate-500">{label}</p>}
    </div>
  )
}

/**
 * Something could not be loaded, and can be tried again.
 */
export function ErrorState({ what, detail, onRetry }: { what: string; detail?: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="flex flex-col items-center justify-center gap-3 h-40 text-center">
      <p className="text-sm text-slate-500">Could not load {what}.</p>
      {detail && <p className="text-xs text-slate-400">{detail}</p>}
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="flex items-center gap-2 rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-700 transition-colors hover:bg-slate-50"
        >
          <RefreshCw className="h-4 w-4" />
          Retry
        </button>
      )}
    </div>
  )
}

/**
 * There is nothing here yet: what would be here, and how to get it.
 */
export function EmptyState({
  icon,
  title,
  description,
  action
}: {
  icon?: ReactNode
  title: string
  description?: ReactNode
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      {icon && <div className="text-slate-300 [&>svg]:h-10 [&>svg]:w-10">{icon}</div>}
      <p className="font-medium text-slate-700">{title}</p>
      {description && <p className="max-w-md text-sm text-slate-500">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}
