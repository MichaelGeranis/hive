import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, within, userEvent } from '../test/test-utils'
import { ToastProvider } from '../contexts/ToastContext'
import { resetMockData } from '../test/mocks/handlers'
import Meetings from './Meetings'

/** The middle pane, so a title in the list is never confused with the same title in the editor. */
function list() {
  return within(screen.getByRole('region', { name: '1:1 list' }))
}

function renderMeetings() {
  return render(
    <ToastProvider>
      <Meetings />
    </ToastProvider>
  )
}

describe('1:1 Meetings page', () => {
  beforeEach(() => {
    resetMockData()
  })

  it('should list the team alongside their 1:1s', async () => {
    renderMeetings()

    expect(await screen.findByRole('heading', { name: 'All 1:1s' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'John Doe, 1 1:1s' })).toBeInTheDocument()
    expect(await list().findByText('Weekly sync')).toBeInTheDocument()
  })

  it('should open the most recent 1:1 in the editor', async () => {
    renderMeetings()

    expect(await screen.findByLabelText('1:1 tags')).toHaveValue('johndoe')
  })

  it('should show which 1:1s are not linked to anyone', async () => {
    renderMeetings()
    const user = userEvent.setup()
    await list().findByText('Weekly sync')

    await user.click(screen.getByRole('button', { name: 'Unlinked, 1 1:1s' }))

    await waitFor(() => expect(list().queryByText('Weekly sync')).not.toBeInTheDocument())
    expect(list().getByText('Career chat')).toBeInTheDocument()
  })

  it('should start an empty 1:1 ready to write in', async () => {
    const user = userEvent.setup()
    renderMeetings()
    await list().findByText('Weekly sync')

    await user.click(screen.getByRole('button', { name: 'New 1:1' }))

    const editor = await screen.findByLabelText('1:1 notes')
    expect(editor).toHaveValue('')
  })

  it('should autosave what is typed and title the 1:1 from its first line', async () => {
    const user = userEvent.setup()
    renderMeetings()
    await list().findByText('Weekly sync')

    await user.click(screen.getByRole('button', { name: 'New 1:1' }))
    const editor = await screen.findByLabelText('1:1 notes')
    await user.type(editor, 'Career chat with Alice')

    await waitFor(() => expect(list().getByText('Career chat with Alice')).toBeInTheDocument(), { timeout: 3000 })
  })

  it('should link the person named in the tags', async () => {
    const user = userEvent.setup()
    renderMeetings()
    await list().findByText('Career chat')

    await user.click(list().getByText('Career chat'))
    const tags = await screen.findByLabelText('1:1 tags')
    expect(await screen.findByText('Not linked to anyone')).toBeInTheDocument()

    await user.type(tags, 'doe')
    await user.tab()

    await waitFor(() => expect(screen.queryByText('Not linked to anyone')).not.toBeInTheDocument())
  })

  it('should filter the list by search term', async () => {
    const user = userEvent.setup()
    renderMeetings()
    await list().findByText('Weekly sync')

    await user.type(screen.getByLabelText('Search 1:1s'), 'Career')

    await waitFor(() => expect(list().queryByText('Weekly sync')).not.toBeInTheDocument(), { timeout: 3000 })
    expect(list().getByText('Career chat')).toBeInTheDocument()
  })
})
