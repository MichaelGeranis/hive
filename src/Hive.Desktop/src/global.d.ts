/// <reference types="vite/client" />

// Log entry type for main process logs
interface MainLogEntry {
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
}

// Global type declarations for Electron API exposed via preload script
declare global {
  interface Window {
    electronAPI?: {
      platform: string
      versions: {
        node: string
        chrome: string
        electron: string
      }
      calendar?: {
        checkAccess: () => Promise<{ success: boolean; hasAccess?: boolean; error?: string }>
        fetchEvents: (calendarEmail: string) => Promise<{ success: boolean; events?: any[]; error?: string }>
        sync: (calendarEmail: string) => Promise<{ success: boolean; result?: any; error?: string }>
      }
      onMainLog?: (callback: (entry: MainLogEntry) => void) => void
      onBackendPort?: (callback: (port: number) => void) => void
    }
  }
}

export {}
