import React from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ToastProvider } from './contexts/ToastContext'
import { ConfirmProvider } from './contexts/ConfirmContext'
import { HiveStatusProvider } from './contexts/HiveStatusContext'
import App from './App'
import './index.css'

console.log('Main.tsx loaded')

// Global error handler
window.addEventListener('error', (event) => {
  console.error('Global error:', event.error)
})

window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason)
})

console.log('Creating React root...')
const rootElement = document.getElementById('root')
console.log('Root element:', rootElement)

if (rootElement) {
  try {
    ReactDOM.createRoot(rootElement).render(
      <React.StrictMode>
        <HashRouter>
          <ToastProvider>
            <ConfirmProvider>
              <HiveStatusProvider>
                <App />
              </HiveStatusProvider>
            </ConfirmProvider>
          </ToastProvider>
        </HashRouter>
      </React.StrictMode>,
    )
    console.log('React app rendered successfully')
  } catch (error) {
    console.error('Failed to render React app:', error)
  }
} else {
  console.error('Root element not found!')
}
