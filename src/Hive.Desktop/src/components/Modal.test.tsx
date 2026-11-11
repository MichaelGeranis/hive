import { describe, it, expect, vi } from 'vitest'
import { useState } from 'react'
import { render, screen, userEvent, within } from '../test/test-utils'
import { Modal } from './Modal'
import { useConfirm } from '../contexts/ConfirmContext'
import { EmptyState, ErrorState, LoadingState } from './PageState'
import { PageHeader } from './PageHeader'

describe('Modal', () => {
  it('is a dialog named by its title, with a close button', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(<Modal title="Edit task" onClose={onClose}><input aria-label="Title" /></Modal>)

    const dialog = screen.getByRole('dialog', { name: 'Edit task' })
    expect(within(dialog).getByLabelText('Title')).toHaveFocus()

    await user.click(within(dialog).getByRole('button', { name: 'Close' }))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('closes the innermost dialog on Escape, and only that one', async () => {
    const user = userEvent.setup()
    const outer = vi.fn()
    const inner = vi.fn()
    render(
      <>
        <Modal title="Outer" onClose={outer}>outer</Modal>
        <Modal title="Inner" onClose={inner}>inner</Modal>
      </>
    )

    await user.keyboard('{Escape}')

    expect(inner).toHaveBeenCalledOnce()
    expect(outer).not.toHaveBeenCalled()
  })

  it('keeps open on a click outside unless asked, so a half-filled form is not lost', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const { rerender } = render(<Modal title="Form" onClose={onClose}>form</Modal>)
    const backdrop = screen.getByRole('dialog').parentElement!

    await user.click(backdrop)
    expect(onClose).not.toHaveBeenCalled()

    rerender(<Modal title="Form" onClose={onClose} closeOnBackdrop>form</Modal>)
    await user.click(screen.getByRole('dialog').parentElement!)
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('gives focus back to what had it when it closes', async () => {
    const user = userEvent.setup()
    function Opener() {
      const [open, setOpen] = useState(false)
      return (
        <>
          <button onClick={() => setOpen(true)}>Open</button>
          {open && <Modal title="Details" onClose={() => setOpen(false)}>details</Modal>}
        </>
      )
    }
    render(<Opener />)

    await user.click(screen.getByRole('button', { name: 'Open' }))
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Open' })).toHaveFocus()
  })
})

describe('useConfirm', () => {
  function Deleter({ onAnswer }: { onAnswer: (answer: boolean) => void }) {
    const confirm = useConfirm()
    return (
      <button onClick={async () => onAnswer(await confirm({ title: 'Delete this task?', message: 'It cannot be undone.', confirmLabel: 'Delete' }))}>
        Delete task
      </button>
    )
  }

  it('answers yes when the action is chosen', async () => {
    const user = userEvent.setup()
    const onAnswer = vi.fn()
    render(<Deleter onAnswer={onAnswer} />)

    await user.click(screen.getByRole('button', { name: 'Delete task' }))
    const dialog = screen.getByRole('dialog', { name: 'Delete this task?' })
    expect(dialog).toHaveTextContent('It cannot be undone.')
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(onAnswer).toHaveBeenCalledWith(true)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it.each([
    ['Cancel', async (user: ReturnType<typeof userEvent.setup>) => user.click(screen.getByRole('button', { name: 'Cancel' }))],
    ['Escape', async (user: ReturnType<typeof userEvent.setup>) => user.keyboard('{Escape}')]
  ])('answers no on %s', async (_, dismiss) => {
    const user = userEvent.setup()
    const onAnswer = vi.fn()
    render(<Deleter onAnswer={onAnswer} />)

    await user.click(screen.getByRole('button', { name: 'Delete task' }))
    await dismiss(user)

    expect(onAnswer).toHaveBeenCalledWith(false)
  })
})

describe('Page states', () => {
  it('shows a page header with its actions', () => {
    render(<PageHeader title="Tasks" subtitle="Everything the team is doing" actions={<button>Add task</button>} />)

    expect(screen.getByRole('heading', { level: 1, name: 'Tasks' })).toBeInTheDocument()
    expect(screen.getByText('Everything the team is doing')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add task' })).toBeInTheDocument()
  })

  it('says what is loading, what failed and what is empty', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()
    render(
      <>
        <LoadingState label="Loading tasks" />
        <ErrorState what="tasks" onRetry={onRetry} />
        <EmptyState title="No tasks yet" description="Sync them from Jira." />
      </>
    )

    expect(screen.getByRole('status', { name: 'Loading tasks' })).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Could not load tasks.')
    await user.click(screen.getByRole('button', { name: 'Retry' }))
    expect(onRetry).toHaveBeenCalledOnce()
    expect(screen.getByText('No tasks yet')).toBeInTheDocument()
  })
})
