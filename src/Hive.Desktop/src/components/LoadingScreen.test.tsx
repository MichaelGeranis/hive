import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import LoadingScreen from './LoadingScreen'

describe('LoadingScreen', () => {
  describe('loading state', () => {
    it('should display default loading message', () => {
      render(<LoadingScreen status="loading" />)

      expect(screen.getByText('Starting up...')).toBeInTheDocument()
    })

    it('should display custom loading message', () => {
      render(<LoadingScreen status="loading" message="Connecting to server..." />)

      expect(screen.getByText('Connecting to server...')).toBeInTheDocument()
    })

    it('should show spinner in loading state', () => {
      render(<LoadingScreen status="loading" />)

      // The Loader2 icon has animate-spin class
      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('should not show retry button in loading state', () => {
      render(<LoadingScreen status="loading" />)

      expect(screen.queryByText('Retry')).not.toBeInTheDocument()
    })
  })

  describe('error state', () => {
    it('should display default error message', () => {
      render(<LoadingScreen status="error" />)

      expect(screen.getByText('Failed to start the application')).toBeInTheDocument()
    })

    it('should display custom error message', () => {
      render(<LoadingScreen status="error" message="Connection timeout" />)

      expect(screen.getByText('Connection timeout')).toBeInTheDocument()
    })

    it('should show retry button when onRetry is provided', () => {
      const handleRetry = vi.fn()

      render(<LoadingScreen status="error" onRetry={handleRetry} />)

      expect(screen.getByText('Retry')).toBeInTheDocument()
    })

    it('should not show retry button when onRetry is not provided', () => {
      render(<LoadingScreen status="error" />)

      expect(screen.queryByText('Retry')).not.toBeInTheDocument()
    })

    it('should call onRetry when retry button is clicked', async () => {
      const handleRetry = vi.fn()
      const user = userEvent.setup()

      render(<LoadingScreen status="error" onRetry={handleRetry} />)

      await user.click(screen.getByText('Retry'))

      expect(handleRetry).toHaveBeenCalledTimes(1)
    })

    it('should not show spinner in error state', () => {
      render(<LoadingScreen status="error" />)

      const spinner = document.querySelector('.animate-spin')
      expect(spinner).not.toBeInTheDocument()
    })
  })

  describe('branding elements', () => {
    it('should display Hive logo text', () => {
      render(<LoadingScreen status="loading" />)

      expect(screen.getByText('Hive')).toBeInTheDocument()
    })

    it('should display version info', () => {
      render(<LoadingScreen status="loading" />)

      expect(screen.getByText('Engineering Manager Dashboard')).toBeInTheDocument()
    })
  })

  describe('styling', () => {
    it('should have fixed positioning', () => {
      render(<LoadingScreen status="loading" />)

      const container = document.querySelector('.fixed')
      expect(container).toBeInTheDocument()
    })

    it('should cover full screen', () => {
      render(<LoadingScreen status="loading" />)

      const container = document.querySelector('.inset-0')
      expect(container).toBeInTheDocument()
    })

    it('should have dark background', () => {
      render(<LoadingScreen status="loading" />)

      const container = document.querySelector('.bg-slate-900')
      expect(container).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('should have accessible retry button', async () => {
      const handleRetry = vi.fn()

      render(<LoadingScreen status="error" onRetry={handleRetry} />)

      const button = screen.getByRole('button', { name: /retry/i })
      expect(button).toBeInTheDocument()
    })
  })
})
