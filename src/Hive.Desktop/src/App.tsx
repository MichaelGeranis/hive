import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import DirectReports from './pages/DirectReports'
import Reviews from './pages/Reviews'
import Meetings from './pages/Meetings'
import Projects from './pages/Projects'
import Tasks from './pages/Tasks'

function App() {
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
      </Route>
    </Routes>
  )
}

export default App
