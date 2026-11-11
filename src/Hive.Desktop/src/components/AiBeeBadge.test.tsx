import { describe, it, expect } from 'vitest'
import { render, screen } from '../test/test-utils'
import { AiBeeBadge } from './AiBeeBadge'
import type { TaskAiReview } from '../types'

const review = (overrides: Partial<TaskAiReview> = {}): TaskAiReview => ({
  verdict: 'NeedsWork',
  summary: 'Keeps drafts.',
  missing: ['Acceptance criteria', 'Offline behaviour'],
  model: 'llama3.1:8b',
  analyzedAt: '2026-09-30T08:00:00Z',
  isUpToDate: true,
  ...overrides,
})

describe('AiBeeBadge', () => {
  it('shows only the bee, and explains itself on hover', () => {
    render(<AiBeeBadge review={review()} />)

    const badge = screen.getByLabelText('AI review: Needs work')
    expect(badge.textContent).toBe('')
    expect(badge).toHaveClass('border-amber-300')
    expect(badge.querySelector('svg')).not.toBeNull()
    expect(badge.getAttribute('title')).toContain('Reviewed by AI (llama3.1:8b): Needs work')
    expect(badge.getAttribute('title')).toContain('Keeps drafts.')
    expect(badge.getAttribute('title')).toContain('Missing: Acceptance criteria; Offline behaviour')
    expect(badge).not.toHaveClass('border-dashed')
  })

  it('takes the colour of a ready verdict', () => {
    render(<AiBeeBadge review={review({ verdict: 'Ready', missing: [] })} />)

    expect(screen.getByLabelText('AI review: Ready')).toHaveClass('border-emerald-300')
  })

  it('has a dashed ring when the task changed since it was reviewed', () => {
    render(<AiBeeBadge review={review({ isUpToDate: false })} />)

    const badge = screen.getByLabelText('AI review: Needs work (the task changed since)')
    expect(badge).toHaveClass('border-dashed')
  })
})
