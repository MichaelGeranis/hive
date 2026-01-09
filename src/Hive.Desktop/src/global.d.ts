/// <reference types="vite/client" />

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
    }
  }
}

export {}
