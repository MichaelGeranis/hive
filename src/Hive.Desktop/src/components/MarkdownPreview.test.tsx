import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../test/test-utils'
import MarkdownPreview from './MarkdownPreview'

describe('MarkdownPreview', () => {
  it('should render markdown headings and emphasis', () => {
    render(<MarkdownPreview content={'# Sprint plan\n\nShip **the thing**'} />)

    expect(screen.getByRole('heading', { level: 1, name: 'Sprint plan' })).toBeInTheDocument()
    expect(screen.getByText('the thing').tagName).toBe('STRONG')
  })

  it('should render list items', () => {
    render(<MarkdownPreview content={'- first\n- second'} />)

    expect(screen.getByText('first')).toBeInTheDocument()
    expect(screen.getByText('second')).toBeInTheDocument()
  })

  it('should render task list checkboxes with their checked state', () => {
    render(<MarkdownPreview content={'- [ ] open task\n- [x] done task'} />)

    const checkboxes = screen.getAllByRole('checkbox')
    expect(checkboxes).toHaveLength(2)
    expect(checkboxes[0]).not.toBeChecked()
    expect(checkboxes[1]).toBeChecked()
  })

  it('should report the index of the clicked task checkbox', async () => {
    const onToggleTask = vi.fn()
    const { userEvent } = await import('../test/test-utils')
    render(<MarkdownPreview content={'- [ ] first\n- [ ] second'} onToggleTask={onToggleTask} />)

    await userEvent.click(screen.getAllByRole('checkbox')[1])

    expect(onToggleTask).toHaveBeenCalledWith(1)
  })

  it('should disable checkboxes when no toggle handler is given', () => {
    render(<MarkdownPreview content={'- [ ] read only'} />)

    expect(screen.getByRole('checkbox')).toBeDisabled()
  })

  it('should render links that open externally', () => {
    render(<MarkdownPreview content={'[Hive](https://example.com)'} />)

    const link = screen.getByRole('link', { name: 'Hive' })
    expect(link).toHaveAttribute('href', 'https://example.com')
    expect(link).toHaveAttribute('target', '_blank')
  })
})
