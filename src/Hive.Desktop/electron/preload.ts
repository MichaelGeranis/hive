import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('electronAPI', {
  platform: process.platform,
  versions: {
    node: process.versions.node,
    chrome: process.versions.chrome,
    electron: process.versions.electron
  },
  calendar: {
    checkAccess: () => ipcRenderer.invoke('calendar:checkAccess'),
    fetchEvents: (calendarEmail: string) => ipcRenderer.invoke('calendar:fetchEvents', calendarEmail),
    sync: (calendarEmail: string) => ipcRenderer.invoke('calendar:sync', calendarEmail)
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
      calendar: {
        checkAccess: () => Promise<{ success: boolean; hasAccess?: boolean; error?: string }>
        fetchEvents: (calendarEmail: string) => Promise<{ success: boolean; events?: any[]; error?: string }>
        sync: (calendarEmail: string) => Promise<{ success: boolean; result?: any; error?: string }>
      }
    }
  }
}
