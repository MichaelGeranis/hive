import { contextBridge, ipcRenderer } from 'electron'

type LogEntry = {
  timestamp: string
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  source: 'main' | 'renderer'
}

type LogCallback = (entry: LogEntry) => void

let logCallback: LogCallback | null = null

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
  }
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
    }
  }
}
