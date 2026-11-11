/// <reference types="vite/client" />

// A log entry as it crosses the preload bridge: from the main process, or from a session file
interface MainLogEntry {
  seq?: number
  timestamp?: string
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  source?: 'main' | 'renderer' | 'backend' | 'api'
  details?: string
}

interface LogSessionInfo {
  id: string
  startedAt: string
  size: number
  isCurrent: boolean
}

interface DesktopAppInfo {
  version: string
  isPackaged: boolean
  platform: string
  arch: string
  osRelease: string
  electron: string
  chrome: string
  node: string
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
      onMainLog?: (callback: (entry: MainLogEntry) => void) => void
      logs?: {
        write: (entry: MainLogEntry) => void
        getBuffered: () => Promise<MainLogEntry[]>
        listSessions: () => Promise<LogSessionInfo[]>
        readSession: (id: string) => Promise<MainLogEntry[]>
        openFolder: () => Promise<string>
      }
      getAppInfo?: () => Promise<DesktopAppInfo>
      onBackendPort?: (callback: (port: number) => void) => void
    }
  }
}

export {}
