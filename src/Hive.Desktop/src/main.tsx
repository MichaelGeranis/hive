import React from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ThemeProvider } from './contexts/ThemeContext'
import { ToastProvider } from './contexts/ToastContext'
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
          <ThemeProvider>
            <ToastProvider>
              <App />
            </ToastProvider>
          </ThemeProvider>
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
