import { useState, useEffect, useCallback } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import LoadingScreen from './components/LoadingScreen'
import { initializeLogCapture } from './services/logStore'
import Dashboard from './pages/Dashboard'
import DirectReports from './pages/DirectReports'
import Reviews from './pages/Reviews'
import Meetings from './pages/Meetings'
import Projects from './pages/Projects'
import ProjectKnowledge from './pages/ProjectKnowledge'
import Parents from './pages/Parents'
import Tasks from './pages/Tasks'
import Sprints from './pages/Sprints'
import Skills from './pages/Skills'
import Leaves from './pages/Leaves'
import Calendar from './pages/Calendar'
import Notes from './pages/Notes'
import ActivityFeed from './pages/ActivityFeed'
import Logs from './pages/Logs'
import Checklists from './pages/Checklists'
import QuarterlyPlanning from './pages/QuarterlyPlanning'
import Settings from './pages/Settings'
import Tutorials from './pages/Tutorials'

// Initialize log capture immediately on app load
initializeLogCapture()

type AppStatus = 'loading' | 'ready' | 'error'

const HEALTH_CHECK_URL = 'http://localhost:5002/health'
const MAX_RETRIES = 30
const RETRY_INTERVAL = 1000

function App() {
  const [status, setStatus] = useState<AppStatus>('loading')
  const [statusMessage, setStatusMessage] = useState('Connecting to backend...')

  console.log('App component mounted, status:', status)

  const checkBackendHealth = useCallback(async (): Promise<boolean> => {
    console.log('Checking backend health at:', HEALTH_CHECK_URL)
    try {
      const response = await fetch(HEALTH_CHECK_URL, {
        method: 'GET',
        signal: AbortSignal.timeout(2000)
      })
      console.log('Health check response:', response.ok, response.status)
      return response.ok
    } catch (error) {
      console.error('Health check failed:', error)
      return false
    }
  }, [])

  const initializeApp = useCallback(async () => {
    console.log('Initializing app...')
    setStatus('loading')
    setStatusMessage('Connecting to backend...')

    let retries = 0
    while (retries < MAX_RETRIES) {
      const isHealthy = await checkBackendHealth()

      if (isHealthy) {
        console.log('Backend is healthy! Setting status to ready')
        setStatus('ready')
        return
      }

      retries++
      console.log(`Health check failed, retry ${retries}/${MAX_RETRIES}`)
      setStatusMessage(`Waiting for backend... (${retries}/${MAX_RETRIES})`)
      await new Promise(resolve => setTimeout(resolve, RETRY_INTERVAL))
    }

    console.error('Backend health check failed after max retries')
    setStatus('error')
    setStatusMessage('Could not connect to the backend. Please ensure the API server is running on the correct port.')
  }, [checkBackendHealth])

  useEffect(() => {
    initializeApp()
  }, [initializeApp])

  // Periodic health check - monitors connection and auto-recovers
  useEffect(() => {
    if (status === 'loading') return // Don't interfere with initial connection

    const interval = setInterval(async () => {
      const isHealthy = await checkBackendHealth()
      if (isHealthy && status === 'error') {
        // Auto-recover when backend comes online
        setStatus('ready')
      } else if (!isHealthy && status === 'ready') {
        setStatus('error')
        setStatusMessage('Lost connection to backend. Waiting for reconnection...')
      }
    }, status === 'error' ? 2000 : 10000) // Check more frequently when in error state

    return () => clearInterval(interval)
  }, [status, checkBackendHealth])

  console.log('App render, status:', status, 'message:', statusMessage)

  if (status !== 'ready') {
    console.log('Rendering LoadingScreen with status:', status)
    return (
      <LoadingScreen
        status={status}
        message={statusMessage}
        onRetry={status === 'error' ? initializeApp : undefined}
      />
    )
  }

  console.log('Status is ready, rendering main app')
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="team" element={<DirectReports />} />
        <Route path="reviews" element={<Reviews />} />
        <Route path="checklists" element={<Checklists />} />
        <Route path="meetings" element={<Meetings />} />
        <Route path="projects" element={<Projects />} />
        <Route path="project-knowledge" element={<ProjectKnowledge />} />
        <Route path="parents" element={<Parents />} />
        <Route path="tasks" element={<Tasks />} />
        <Route path="sprints" element={<Sprints />} />
        <Route path="activity-feed" element={<ActivityFeed />} />
        <Route path="skills" element={<Skills />} />
        <Route path="leaves" element={<Leaves />} />
        <Route path="calendar" element={<Calendar />} />
        <Route path="notes" element={<Notes />} />
        <Route path="tutorials" element={<Tutorials />} />
        <Route path="logs" element={<Logs />} />
        <Route path="quarterly-planning" element={<QuarterlyPlanning />} />
        <Route path="settings" element={<Settings />} />
      </Route>
    </Routes>
  )
}

export default App
