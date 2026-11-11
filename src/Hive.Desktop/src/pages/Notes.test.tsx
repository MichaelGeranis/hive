import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { http, HttpResponse } from 'msw'
import { server } from '../test/mocks/server'
import { createNoteFolder, resetMockData } from '../test/mocks/handlers'
import Notes from './Notes'

function renderNotes() {
  return render(
    <ToastProvider>
      <Notes />
    </ToastProvider>
  )
}

describe('Notes page', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should list folders alongside the notes', async () => {
    renderNotes()

    expect(await screen.findByRole('heading', { name: 'All Notes' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Work' })).toBeInTheDocument()
    expect(await screen.findByText('First Note')).toBeInTheDocument()
    expect(screen.getByText('Second Note')).toBeInTheDocument()
  })

  it('should open on the journal when there is one', async () => {
    server.use(
      http.get('*/notefolders', () =>
        HttpResponse.json([
          createNoteFolder({ id: 'f1', name: 'Work', noteCount: 1 }),
          createNoteFolder({ id: 'f9', name: 'Journal', isJournal: true }),
        ])
      )
    )
    renderNotes()

    expect(await screen.findByRole('heading', { name: 'Journal' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'All Notes' })).not.toBeInTheDocument()
  })

  it('should open the note a link names', async () => {
    render(
      <ToastProvider>
        <Notes />
      </ToastProvider>,
      { routerProps: { initialEntries: ['/notes?note=2'] } }
    )

    const linked = await screen.findByText('Second Note')
    await waitFor(() => expect(linked.closest('button')).toHaveClass('bg-amber-500/10'))
    expect(screen.getByText('First Note').closest('button')).not.toHaveClass('bg-amber-500/10')
  })

  it('should open existing notes in preview mode', async () => {
    renderNotes()

    expect(await screen.findByRole('button', { name: 'Edit' })).toBeInTheDocument()
  })

  it('should create an empty note ready to write in', async () => {
    const user = userEvent.setup()
    renderNotes()
    await screen.findByText('First Note')

    await user.click(screen.getByRole('button', { name: 'New note' }))

    const editor = await screen.findByLabelText('Note content')
    expect(editor).toHaveValue('')
    expect(await screen.findByText('New Note')).toBeInTheDocument()
  })

  it('should autosave what is typed and title the note from its first line', async () => {
    const user = userEvent.setup()
    renderNotes()
    await screen.findByText('First Note')

    await user.click(screen.getByRole('button', { name: 'New note' }))
    const editor = await screen.findByLabelText('Note content')
    await user.type(editor, 'Retro actions')

    await waitFor(() => expect(screen.getByText('Retro actions')).toBeInTheDocument(), { timeout: 3000 })
  })

  it('should filter the list by search term', async () => {
    const user = userEvent.setup()
    renderNotes()
    await screen.findByText('First Note')

    await user.type(screen.getByLabelText('Search notes'), 'Second')

    await waitFor(() => expect(screen.queryByText('First Note')).not.toBeInTheDocument(), { timeout: 3000 })
    expect(screen.getByText('Second Note')).toBeInTheDocument()
  })

  it('should show only the notes in the selected folder', async () => {
    const user = userEvent.setup()
    renderNotes()
    await screen.findByText('First Note')

    await user.click(screen.getByRole('button', { name: 'Work' }))

    await waitFor(() => expect(screen.queryByText('Second Note')).not.toBeInTheDocument())
    expect(screen.getByText('First Note')).toBeInTheDocument()
  })

  it('should render markdown in preview mode', async () => {
    const user = userEvent.setup()
    renderNotes()
    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const editor = await screen.findByLabelText('Note content')
    await user.clear(editor)
    await user.type(editor, '# Heading')

    await user.click(screen.getByRole('button', { name: /Preview/ }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Heading' })).toBeInTheDocument()
  })
})
