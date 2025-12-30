import { useState, useEffect, useCallback } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import LoadingScreen from './components/LoadingScreen'
import Dashboard from './pages/Dashboard'
import DirectReports from './pages/DirectReports'
import Reviews from './pages/Reviews'
import Meetings from './pages/Meetings'
import Projects from './pages/Projects'
import Tasks from './pages/Tasks'
import Leaves from './pages/Leaves'
import Settings from './pages/Settings'
import JiraImport from './pages/JiraImport'

type AppStatus = 'loading' | 'ready' | 'error'

const HEALTH_CHECK_URL = 'http://localhost:5000/health'
const MAX_RETRIES = 30
const RETRY_INTERVAL = 1000

function App() {
  const [status, setStatus] = useState<AppStatus>('loading')
  const [statusMessage, setStatusMessage] = useState('Connecting to backend...')

  const checkBackendHealth = useCallback(async (): Promise<boolean> => {
    try {
      const response = await fetch(HEALTH_CHECK_URL, {
        method: 'GET',
        signal: AbortSignal.timeout(2000)
      })
      return response.ok
    } catch {
      return false
    }
  }, [])

  const initializeApp = useCallback(async () => {
    setStatus('loading')
    setStatusMessage('Connecting to backend...')

    let retries = 0
    while (retries < MAX_RETRIES) {
      const isHealthy = await checkBackendHealth()

      if (isHealthy) {
        setStatus('ready')
        return
      }

      retries++
      setStatusMessage(`Waiting for backend... (${retries}/${MAX_RETRIES})`)
      await new Promise(resolve => setTimeout(resolve, RETRY_INTERVAL))
    }

    setStatus('error')
    setStatusMessage('Could not connect to the backend. Please ensure the API server is running on port 5000.')
  }, [checkBackendHealth])

  useEffect(() => {
    initializeApp()
  }, [initializeApp])

  // Periodic health check while app is running
  useEffect(() => {
    if (status !== 'ready') return

    const interval = setInterval(async () => {
      const isHealthy = await checkBackendHealth()
      if (!isHealthy) {
        setStatus('error')
        setStatusMessage('Lost connection to backend. The server may have crashed.')
      }
    }, 10000) // Check every 10 seconds

    return () => clearInterval(interval)
  }, [status, checkBackendHealth])

  if (status !== 'ready') {
    return (
      <LoadingScreen
        status={status}
        message={statusMessage}
        onRetry={status === 'error' ? initializeApp : undefined}
      />
    )
  }

  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="team" element={<DirectReports />} />
        <Route path="reviews" element={<Reviews />} />
        <Route path="meetings" element={<Meetings />} />
        <Route path="projects" element={<Projects />} />
        <Route path="tasks" element={<Tasks />} />
        <Route path="leaves" element={<Leaves />} />
        <Route path="import" element={<JiraImport />} />
        <Route path="settings" element={<Settings />} />
      </Route>
    </Routes>
  )
}

export default App
