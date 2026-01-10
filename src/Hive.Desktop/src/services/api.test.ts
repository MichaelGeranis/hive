import { describe, it, expect, vi, beforeEach } from 'vitest'
import { server } from '../test/mocks/server'
import { http, HttpResponse } from 'msw'

// Import the API modules
import {
  directReportsApi,
  notesApi,
  leavesApi,
  tasksApi,
} from './api'

const API_BASE = 'http://localhost:5002/api'

describe('API Service', () => {
  beforeEach(() => {
    // Suppress console output during tests
    vi.spyOn(console, 'log').mockImplementation(() => {})
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  describe('directReportsApi', () => {
    it('should fetch all direct reports', async () => {
      const reports = await directReportsApi.getAll()

      expect(Array.isArray(reports)).toBe(true)
      expect(reports.length).toBeGreaterThan(0)
      expect(reports[0]).toHaveProperty('id')
      expect(reports[0]).toHaveProperty('firstName')
      expect(reports[0]).toHaveProperty('lastName')
    })

    it('should fetch a single direct report by ID', async () => {
      const report = await directReportsApi.getById('1')

      expect(report).toBeDefined()
      expect(report.id).toBe('1')
      expect(report.firstName).toBe('John')
    })

    it('should create a new direct report', async () => {
      const newReport = await directReportsApi.create({
        firstName: 'Alice',
        lastName: 'Johnson',
        email: 'alice@example.com',
        jobTitle: 'Designer',
        department: 'Design',
        hireDate: '2024-01-01',
        isDirect: true,
      })

      expect(newReport).toBeDefined()
      expect(newReport.firstName).toBe('Alice')
      expect(newReport.lastName).toBe('Johnson')
      expect(newReport.fullName).toBe('Alice Johnson')
    })

    it('should handle 404 for non-existent direct report', async () => {
      server.use(
        http.get(`${API_BASE}/directreports/999`, () => {
          return new HttpResponse(null, { status: 404 })
        })
      )

      await expect(directReportsApi.getById('999')).rejects.toThrow()
    })
  })

  describe('notesApi', () => {
    it('should fetch all notes', async () => {
      const notes = await notesApi.getAll()

      expect(Array.isArray(notes)).toBe(true)
      expect(notes.length).toBeGreaterThan(0)
    })

    it('should fetch pending notes', async () => {
      const notes = await notesApi.getPending()

      expect(Array.isArray(notes)).toBe(true)
      // All returned notes should not be completed
      notes.forEach(note => {
        expect(note.isCompleted).toBe(false)
      })
    })

    it('should fetch completed notes', async () => {
      const notes = await notesApi.getCompleted()

      expect(Array.isArray(notes)).toBe(true)
      // All returned notes should be completed
      notes.forEach(note => {
        expect(note.isCompleted).toBe(true)
      })
    })

    it('should search notes by query', async () => {
      const notes = await notesApi.search('First')

      expect(Array.isArray(notes)).toBe(true)
      // Results should contain the search term in title or content
      expect(notes.some(n => n.title.includes('First') || n.content.includes('First'))).toBe(true)
    })

    it('should toggle note completion', async () => {
      const note = await notesApi.toggle('1')

      expect(note).toBeDefined()
      expect(note.id).toBe('1')
    })

    it('should create a new note', async () => {
      const newNote = await notesApi.create({
        title: 'New Note',
        content: 'New content',
        priority: 1,
      })

      expect(newNote).toBeDefined()
      expect(newNote.title).toBe('New Note')
    })
  })

  describe('leavesApi', () => {
    it('should fetch all leaves', async () => {
      const leaves = await leavesApi.getAll()

      expect(Array.isArray(leaves)).toBe(true)
      expect(leaves.length).toBeGreaterThan(0)
    })

    it('should fetch leave overview', async () => {
      const overview = await leavesApi.getOverview()

      expect(overview).toBeDefined()
      expect(overview).toHaveProperty('totalLeaveRecords')
      expect(overview).toHaveProperty('upcomingLeaves')
    })

    it('should create a new leave', async () => {
      const newLeave = await leavesApi.create({
        directReportId: '1',
        type: 'Vacation',
        startDate: '2024-06-01',
        endDate: '2024-06-05',
      })

      expect(newLeave).toBeDefined()
      expect(newLeave.type).toBe('Vacation')
    })
  })

  describe('tasksApi', () => {
    it('should fetch all tasks', async () => {
      const tasks = await tasksApi.getAll()

      expect(Array.isArray(tasks)).toBe(true)
      expect(tasks.length).toBeGreaterThan(0)
    })

    it('should fetch overdue tasks', async () => {
      const tasks = await tasksApi.getOverdue()

      expect(Array.isArray(tasks)).toBe(true)
      // All returned tasks should be overdue
      tasks.forEach(task => {
        expect(task.isOverdue).toBe(true)
      })
    })

    it('should create a new task', async () => {
      const newTask = await tasksApi.create({
        title: 'New Task',
        description: 'Task description',
        priority: 2,
        status: 1,
      })

      expect(newTask).toBeDefined()
      expect(newTask.title).toBe('New Task')
    })
  })

  describe('error handling', () => {
    it('should handle network errors', async () => {
      server.use(
        http.get(`${API_BASE}/directreports`, () => {
          return HttpResponse.error()
        })
      )

      await expect(directReportsApi.getAll()).rejects.toThrow()
    })

    it('should handle server errors', async () => {
      server.use(
        http.get(`${API_BASE}/directreports`, () => {
          return new HttpResponse(
            JSON.stringify({ message: 'Internal Server Error' }),
            { status: 500 }
          )
        })
      )

      await expect(directReportsApi.getAll()).rejects.toThrow()
    })

    it('should handle validation errors', async () => {
      server.use(
        http.post(`${API_BASE}/directreports`, () => {
          return new HttpResponse(
            JSON.stringify({
              errors: {
                email: ['Invalid email format'],
                firstName: ['First name is required'],
              },
            }),
            { status: 400 }
          )
        })
      )

      await expect(
        directReportsApi.create({
          firstName: '',
          lastName: 'Test',
          email: 'invalid',
          jobTitle: 'Test',
          department: 'Test',
          hireDate: '2024-01-01',
          isDirect: true,
        })
      ).rejects.toThrow()
    })
  })

  describe('request logging', () => {
    it('should log mutation requests', async () => {
      const consoleSpy = vi.spyOn(console, 'log')

      await directReportsApi.create({
        firstName: 'Test',
        lastName: 'User',
        email: 'test@example.com',
        jobTitle: 'Tester',
        department: 'QA',
        hireDate: '2024-01-01',
        isDirect: true,
      })

      // Should have logged the POST request
      expect(consoleSpy).toHaveBeenCalled()
    })
  })
})
