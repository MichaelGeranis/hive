import { createContext, useContext, useState, useCallback, ReactNode } from 'react'
import { X, AlertCircle, CheckCircle, AlertTriangle } from 'lucide-react'

type ToastType = 'error' | 'success' | 'warning'

interface Toast {
  id: string
  message: string
  type: ToastType
  exiting: boolean
}

interface ToastContextType {
  showError: (message: string) => void
  showSuccess: (message: string) => void
  showWarning: (message: string) => void
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

const TOAST_STYLES: Record<ToastType, string> = {
  error: 'bg-red-50 dark:bg-red-900/40 border-red-300 dark:border-red-700 text-red-800 dark:text-red-200',
  success: 'bg-green-50 dark:bg-green-900/40 border-green-300 dark:border-green-700 text-green-800 dark:text-green-200',
  warning: 'bg-amber-50 dark:bg-amber-900/40 border-amber-300 dark:border-amber-700 text-amber-800 dark:text-amber-200',
}

const TOAST_ICONS: Record<ToastType, typeof AlertCircle> = {
  error: AlertCircle,
  success: CheckCircle,
  warning: AlertTriangle,
}

const DISMISS_ICON_STYLES: Record<ToastType, string> = {
  error: 'text-red-400 hover:text-red-600 dark:text-red-400 dark:hover:text-red-200',
  success: 'text-green-400 hover:text-green-600 dark:text-green-400 dark:hover:text-green-200',
  warning: 'text-amber-400 hover:text-amber-600 dark:text-amber-400 dark:hover:text-amber-200',
}

function ToastItem({ toast, onDismiss }: { toast: Toast; onDismiss: (id: string) => void }) {
  const Icon = TOAST_ICONS[toast.type]

  return (
    <div
      className={`${TOAST_STYLES[toast.type]} ${toast.exiting ? 'toast-exit' : 'toast-enter'} flex items-center gap-3 px-4 py-3 rounded-lg border shadow-lg min-w-[320px] max-w-[480px]`}
    >
      <Icon className="w-5 h-5 flex-shrink-0" />
      <p className="flex-1 text-sm font-medium">{toast.message}</p>
      <button
        onClick={() => onDismiss(toast.id)}
        className={`${DISMISS_ICON_STYLES[toast.type]} flex-shrink-0 p-0.5 rounded transition-colors`}
      >
        <X className="w-4 h-4" />
      </button>
    </div>
  )
}

function ToastContainer({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: string) => void }) {
  return (
    <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 flex flex-col items-center gap-2">
      {toasts.slice(0, 3).map(toast => (
        <ToastItem key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  )
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.map(t => t.id === id ? { ...t, exiting: true } : t))
    setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id))
    }, 300)
  }, [])

  const addToast = useCallback((message: string, type: ToastType) => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
    setToasts(prev => [...prev, { id, message, type, exiting: false }])

    setTimeout(() => {
      dismissToast(id)
    }, 4000)
  }, [dismissToast])

  const showError = useCallback((message: string) => addToast(message, 'error'), [addToast])
  const showSuccess = useCallback((message: string) => addToast(message, 'success'), [addToast])
  const showWarning = useCallback((message: string) => addToast(message, 'warning'), [addToast])

  return (
    <ToastContext.Provider value={{ showError, showSuccess, showWarning }}>
      {children}
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (context === undefined) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}

export function getErrorMessage(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const axiosError = error as { response?: { data?: { message?: string } } }
    if (axiosError.response?.data?.message) {
      return axiosError.response.data.message
    }
  }
  return 'An unexpected error occurred'
}
