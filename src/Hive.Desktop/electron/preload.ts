import { contextBridge, ipcRenderer } from 'electron'

type LogEntry = {
  seq?: number
  timestamp: string
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  source: 'main' | 'renderer' | 'backend' | 'api'
  details?: string
}

type LogSessionInfo = {
  id: string
  startedAt: string
  size: number
  isCurrent: boolean
}

type AppInfo = {
  version: string
  isPackaged: boolean
  platform: string
  arch: string
  osRelease: string
  electron: string
  chrome: string
  node: string
}

type LogCallback = (entry: LogEntry) => void

let logCallback: LogCallback | null = null

type ApiCredentials = { username: string; password: string }

// This launch's API credentials, from the main process; null when the backend runs from source
const apiCredentials = ipcRenderer.sendSync('auth:get-api-credentials') as ApiCredentials | null

contextBridge.exposeInMainWorld('electronAPI', {
  platform: process.platform,
  versions: {
    node: process.versions.node,
    chrome: process.versions.chrome,
    electron: process.versions.electron
  },
  onMainLog: (callback: LogCallback) => {
    logCallback = callback
    ipcRenderer.on('main-log', (_event, entry: LogEntry) => {
      if (logCallback) {
        logCallback(entry)
      }
    })
  },
  removeMainLogListener: () => {
    logCallback = null
    ipcRenderer.removeAllListeners('main-log')
  },
  logs: {
    write: (entry: LogEntry) => ipcRenderer.send('log:write', entry),
    getBuffered: (): Promise<LogEntry[]> => ipcRenderer.invoke('log:get-buffered'),
    listSessions: (): Promise<LogSessionInfo[]> => ipcRenderer.invoke('log:list-sessions'),
    readSession: (id: string): Promise<LogEntry[]> => ipcRenderer.invoke('log:read-session', id),
    openFolder: (): Promise<string> => ipcRenderer.invoke('log:open-folder')
  },
  getAppInfo: (): Promise<AppInfo> => ipcRenderer.invoke('app:get-info'),
  apiCredentials
})

// Type declarations for the exposed API
declare global {
  interface Window {
    electronAPI: {
      platform: string
      versions: {
        node: string
        chrome: string
        electron: string
      }
      onMainLog: (callback: LogCallback) => void
      removeMainLogListener: () => void
      logs: {
        write: (entry: LogEntry) => void
        getBuffered: () => Promise<LogEntry[]>
        listSessions: () => Promise<LogSessionInfo[]>
        readSession: (id: string) => Promise<LogEntry[]>
        openFolder: () => Promise<string>
      }
      getAppInfo: () => Promise<AppInfo>
      apiCredentials: ApiCredentials | null
    }
  }
}
