import { describe, it, expect, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { render, screen } from '../test/test-utils'
import CollapsibleTags from './CollapsibleTags'

const tags = (n: number) => Array.from({ length: n }, (_, i) => `tag-${i + 1}`)

const renderTags = (items: string[], alwaysShow?: (item: string) => boolean) =>
  render(
    <div>
      <CollapsibleTags
        items={items}
        alwaysShow={alwaysShow}
        renderItem={(tag) => <span key={tag}>{tag}</span>}
      />
    </div>
  )

describe('CollapsibleTags', () => {
  it('should show every tag and no toggle when there are five or fewer', () => {
    renderTags(tags(5))

    expect(screen.getByText('tag-5')).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('should show the first five and a count of the rest when there are more', () => {
    renderTags(tags(8))

    expect(screen.getByText('tag-5')).toBeInTheDocument()
    expect(screen.queryByText('tag-6')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: '+3 more' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('should reveal all tags on click and hide them again', async () => {
    const user = userEvent.setup()
    renderTags(tags(8))

    await user.click(screen.getByRole('button', { name: '+3 more' }))
    expect(screen.getByText('tag-8')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Show less' }))
    expect(screen.queryByText('tag-8')).not.toBeInTheDocument()
  })

  it('should keep an always-shown tag visible while collapsed', () => {
    renderTags(tags(8), (tag) => tag === 'tag-7')

    expect(screen.getByText('tag-7')).toBeInTheDocument()
    expect(screen.queryByText('tag-6')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: '+2 more' })).toBeInTheDocument()
  })

  it('should not trigger a click on the surrounding element', async () => {
    const user = userEvent.setup()
    const onParentClick = vi.fn()
    render(
      <div onClick={onParentClick}>
        <CollapsibleTags items={tags(6)} renderItem={(tag) => <span key={tag}>{tag}</span>} />
      </div>
    )

    await user.click(screen.getByRole('button', { name: '+1 more' }))
    expect(onParentClick).not.toHaveBeenCalled()
  })
})
