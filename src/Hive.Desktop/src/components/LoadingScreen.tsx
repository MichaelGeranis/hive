import { Hexagon, Loader2, AlertCircle, RefreshCw } from 'lucide-react'

interface LoadingScreenProps {
  status: 'loading' | 'error' | 'ready'
  message?: string
  onRetry?: () => void
}

export default function LoadingScreen({ status, message, onRetry }: LoadingScreenProps) {
  return (
    <div className="fixed inset-0 bg-slate-900 flex flex-col items-center justify-center">
      {/* Logo */}
      <div className="flex items-center gap-3 mb-8">
        <Hexagon className="w-16 h-16 text-amber-400" />
        <span className="text-4xl font-bold text-white">Hive</span>
      </div>

      {/* Status indicator */}
      {status === 'loading' && (
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="w-8 h-8 text-amber-400 animate-spin" />
          <p className="text-slate-400 text-sm">
            {message || 'Starting up...'}
          </p>
        </div>
      )}

      {status === 'error' && (
        <div className="flex flex-col items-center gap-4">
          <AlertCircle className="w-12 h-12 text-red-400" />
          <p className="text-red-400 text-center max-w-md">
            {message || 'Failed to start the application'}
          </p>
          {onRetry && (
            <button
              onClick={onRetry}
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors mt-4"
            >
              <RefreshCw className="w-4 h-4" />
              Retry
            </button>
          )}
        </div>
      )}

      {/* Version info */}
      <div className="absolute bottom-6 text-slate-600 text-xs">
        Engineering Manager Dashboard
      </div>
    </div>
  )
}
