import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { formatDate } from '../utils/dateUtils'
import { useSearchParams } from 'react-router-dom'
import { Pin, Plus, Search, StickyNote, X } from 'lucide-react'
import NoteEditor from '../components/NoteEditor'
import NoteFolderTree from '../components/NoteFolderTree'
import { noteFoldersApi, notesApi } from '../services/api'
import type { ManagerNote, NoteFolder } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { EmptyState, LoadingState } from '../components/PageState'

const AUTOSAVE_DELAY_MS = 700
const PAGE_SIZE = 100

function formatListDate(note: ManagerNote): string {
  const date = new Date(note.updatedAt || note.createdAt)
  const now = new Date()
  const sameDay = date.toDateString() === now.toDateString()
  if (sameDay) {
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
  }
  return formatDate(date)
}

/**
 * Notes: a folder sidebar, the notes in that folder, and the note itself. A note is
 * created empty and saved as it is written, so there is no form and no Save button.
 */
export default function Notes() {
  const { showError } = useToast()
  const confirm = useConfirm()
  const [searchParams, setSearchParams] = useSearchParams()
  const initialSearch = searchParams.get('search') || ''
  const initialNoteId = searchParams.get('note')

  const [folders, setFolders] = useState<NoteFolder[]>([])
  const [notes, setNotes] = useState<ManagerNote[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)
  const [folderChosen, setFolderChosen] = useState(false)
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null)
  const [searchInput, setSearchInput] = useState(initialSearch)
  const [searchTerm, setSearchTerm] = useState(initialSearch)
  const [loading, setLoading] = useState(true)
  const [draft, setDraft] = useState('')
  const [saving, setSaving] = useState(false)
  const [startEditingNoteId, setStartEditingNoteId] = useState<string | null>(null)

  // Autosave bookkeeping: what is being edited, and what has not reached the server yet.
  const draftRef = useRef('')
  const editingNoteIdRef = useRef<string | null>(null)
  const dirtyRef = useRef(false)
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const requestedNoteIdRef = useRef<string | null>(initialNoteId)

  const selectedNote = useMemo(
    () => notes.find((note) => note.id === selectedNoteId) ?? null,
    [notes, selectedNoteId]
  )

  useEffect(() => {
    if (initialSearch || initialNoteId) {
      setSearchParams({}, { replace: true })
    }
  }, [initialSearch, initialNoteId, setSearchParams])

  const patchNote = useCallback((updated: ManagerNote) => {
    setNotes((current) => current.map((note) => (note.id === updated.id ? updated : note)))
  }, [])

  const loadFolders = useCallback(async (): Promise<NoteFolder[]> => {
    try {
      const loaded = await noteFoldersApi.getAll()
      setFolders(loaded)
      return loaded
    } catch (error) {
      console.error('Failed to load folders:', error)
      return []
    }
  }, [])

  const saveDraft = useCallback(async () => {
    const noteId = editingNoteIdRef.current
    if (!noteId || !dirtyRef.current) return

    dirtyRef.current = false
    setSaving(true)
    try {
      const updated = await notesApi.updateContent(noteId, draftRef.current)
      patchNote(updated)
    } catch (error) {
      dirtyRef.current = true
      console.error('Failed to save note:', error)
      showError(getErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }, [patchNote, showError])

  const flushPendingSave = useCallback(async () => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
      saveTimerRef.current = null
    }
    await saveDraft()
  }, [saveDraft])

  const openNote = useCallback((note: ManagerNote | null, startInEditMode = false) => {
    editingNoteIdRef.current = note?.id ?? null
    dirtyRef.current = false
    draftRef.current = note?.content ?? ''
    setSelectedNoteId(note?.id ?? null)
    setDraft(note?.content ?? '')
    setStartEditingNoteId(startInEditMode ? note?.id ?? null : null)
  }, [])

  const loadNotes = useCallback(
    async (folderId: string | null, search: string, keepSelection = false) => {
      setLoading(true)
      try {
        const result = await notesApi.getAll(1, PAGE_SIZE, search || undefined, undefined, folderId)
        let items = result.items

        // A link to one note (?note=<id>) opens it, even when it is past the first page.
        const requestedId = requestedNoteIdRef.current
        requestedNoteIdRef.current = null
        if (requestedId) {
          let requested = items.find((note) => note.id === requestedId)
          if (!requested) {
            requested = await notesApi.getById(requestedId).catch(() => undefined)
            if (requested) items = [requested, ...items]
          }
          setNotes(items)
          setTotalCount(result.totalCount)
          openNote(requested ?? items[0] ?? null)
          return
        }

        setNotes(items)
        setTotalCount(result.totalCount)

        const stillVisible = keepSelection && items.some((note) => note.id === editingNoteIdRef.current)
        if (!stillVisible) {
          openNote(items[0] ?? null)
        }
      } catch (error) {
        console.error('Failed to load notes:', error)
        showError(getErrorMessage(error))
      } finally {
        setLoading(false)
      }
    },
    [openNote, showError]
  )

  // The page opens on the journal when there is one, unless a link asks for a note or a search.
  useEffect(() => {
    const init = async () => {
      const loaded = await loadFolders()
      if (!initialNoteId && !initialSearch) {
        const journal = loaded.find((folder) => folder.isJournal)
        if (journal) setSelectedFolderId(journal.id)
      }
      setFolderChosen(true)
    }
    init()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // The list follows the folder and the search box. Whatever is being
  // written is saved first, so switching away never loses a keystroke.
  useEffect(() => {
    if (!folderChosen) return
    let cancelled = false
    const run = async () => {
      await flushPendingSave()
      if (!cancelled) {
        await loadNotes(selectedFolderId, searchTerm, true)
      }
    }
    run()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [folderChosen, selectedFolderId, searchTerm])

  // Debounced search.
  useEffect(() => {
    const timeoutId = setTimeout(() => setSearchTerm(searchInput), 300)
    return () => clearTimeout(timeoutId)
  }, [searchInput])

  // Save whatever is still unsaved when leaving the page.
  useEffect(() => {
    return () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current)
      }
      if (dirtyRef.current && editingNoteIdRef.current) {
        notesApi.updateContent(editingNoteIdRef.current, draftRef.current).catch(() => undefined)
      }
    }
  }, [])

  const handleContentChange = useCallback(
    (value: string) => {
      setDraft(value)
      draftRef.current = value
      dirtyRef.current = true
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current)
      }
      saveTimerRef.current = setTimeout(() => {
        saveTimerRef.current = null
        saveDraft()
      }, AUTOSAVE_DELAY_MS)
    },
    [saveDraft]
  )

  const handleNewNote = useCallback(async () => {
    await flushPendingSave()
    try {
      const created = await notesApi.createBlank(selectedFolderId)
      setNotes((current) => [created, ...current])
      setTotalCount((count) => count + 1)
      openNote(created, true)
      loadFolders()
    } catch (error) {
      console.error('Failed to create note:', error)
      showError(getErrorMessage(error))
    }
  }, [flushPendingSave, loadFolders, openNote, selectedFolderId, showError])

  // Cmd/Ctrl+N starts a new note from anywhere on the page.
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'n') {
        event.preventDefault()
        handleNewNote()
      }
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [handleNewNote])

  const handleSelectNote = async (note: ManagerNote) => {
    if (note.id === selectedNoteId) return
    await flushPendingSave()
    openNote(note)
  }

  const handleDeleteNote = async () => {
    if (!selectedNote) return
    if (!(await confirm({ title: `Delete "${selectedNote.title}"?`, message: 'This cannot be undone.', confirmLabel: 'Delete' }))) return

    const deletedId = selectedNote.id
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
      saveTimerRef.current = null
    }
    dirtyRef.current = false

    try {
      await notesApi.delete(deletedId)
      const remaining = notes.filter((note) => note.id !== deletedId)
      setNotes(remaining)
      setTotalCount((count) => Math.max(0, count - 1))
      openNote(remaining[0] ?? null)
      loadFolders()
    } catch (error) {
      console.error('Failed to delete note:', error)
      showError(getErrorMessage(error))
    }
  }

  const handleTogglePin = async () => {
    if (!selectedNote) return
    try {
      patchNote(await notesApi.togglePin(selectedNote.id))
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const moveNote = async (noteId: string, folderId: string | null) => {
    try {
      const updated = await notesApi.move(noteId, folderId)
      if (selectedFolderId !== null && folderId !== selectedFolderId) {
        // The note has left the folder being listed.
        const remaining = notes.filter((note) => note.id !== noteId)
        setNotes(remaining)
        setTotalCount((count) => Math.max(0, count - 1))
        if (noteId === editingNoteIdRef.current) {
          openNote(remaining[0] ?? null)
        }
      } else {
        patchNote(updated)
      }
      loadFolders()
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const handleUpdateMeta = async (patch: { tags?: string }) => {
    if (!selectedNote) return
    await flushPendingSave()
    try {
      const updated = await notesApi.update(selectedNote.id, {
        title: selectedNote.title,
        content: draftRef.current,
        tags: patch.tags ?? selectedNote.tags,
        folderId: selectedNote.folderId ?? null
      })
      patchNote(updated)
    } catch (error) {
      console.error('Failed to update note:', error)
      showError(getErrorMessage(error))
    }
  }

  const handleCreateFolder = async (name: string, parentFolderId: string | null) => {
    try {
      await noteFoldersApi.create({ name, parentFolderId })
      await loadFolders()
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const handleRenameFolder = async (folder: NoteFolder, name: string) => {
    try {
      await noteFoldersApi.update(folder.id, {
        name,
        parentFolderId: folder.parentFolderId ?? null,
        sortOrder: folder.sortOrder
      })
      await loadFolders()
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const handleDeleteFolder = async (folder: NoteFolder) => {
    if (!(await confirm({
      title: `Delete the folder "${folder.name}"?`,
      message: 'Its notes move to the folder above it.',
      confirmLabel: 'Delete'
    }))) return
    try {
      await noteFoldersApi.delete(folder.id)
      await loadFolders()
      if (selectedFolderId === folder.id) {
        setSelectedFolderId(folder.parentFolderId ?? null)
      } else {
        await loadNotes(selectedFolderId, searchTerm, true)
      }
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const currentFolderName = selectedFolderId
    ? folders.find((folder) => folder.id === selectedFolderId)?.name ?? 'Folder'
    : 'All Notes'

  const pinnedNotes = notes.filter((note) => note.isPinned)
  const otherNotes = notes.filter((note) => !note.isPinned)

  const renderNoteRow = (note: ManagerNote) => {
    const isSelected = note.id === selectedNoteId
    return (
      <button
        key={note.id}
        draggable
        onDragStart={(e) => e.dataTransfer.setData('text/hive-note-id', note.id)}
        onClick={() => handleSelectNote(note)}
        className={`w-full border-b border-slate-100 px-4 py-3 text-left transition-colors ${
          isSelected ? 'bg-amber-500/10' : 'hover:bg-slate-50'
        }`}
      >
        <div className="flex items-center gap-2">
          {note.isPinned && <Pin className="h-3 w-3 shrink-0 text-amber-500" />}
          <span className="flex-1 truncate text-sm font-semibold text-slate-900">
            {note.title}
          </span>
        </div>
        <div className="mt-1 flex items-center gap-2 text-xs text-slate-500">
          <span className="shrink-0">{formatListDate(note)}</span>
          <span className="truncate">{note.snippet || 'No additional text'}</span>
        </div>
      </button>
    )
  }

  return (
    <div className="flex h-[calc(100vh-5rem)] overflow-hidden rounded-xl border border-slate-200 bg-white">
      {/* Folders */}
      <aside className="hidden w-56 shrink-0 flex-col border-r border-slate-200 bg-slate-50 lg:flex">
        <NoteFolderTree
          folders={folders}
          selectedFolderId={selectedFolderId}
          totalNoteCount={totalCount}
          onSelect={setSelectedFolderId}
          onCreate={handleCreateFolder}
          onRename={handleRenameFolder}
          onDelete={handleDeleteFolder}
          onDropNote={moveNote}
        />
      </aside>

      {/* Note list */}
      <section className="flex w-full shrink-0 flex-col border-r border-slate-200 sm:w-80">
        <div className="space-y-3 border-b border-slate-200 px-4 py-3">
          <div className="flex items-center justify-between gap-2">
            <div className="min-w-0">
              <h1 className="truncate text-lg font-bold text-slate-900">{currentFolderName}</h1>
              <p className="text-xs text-slate-500">
                {totalCount} {totalCount === 1 ? 'note' : 'notes'}
              </p>
            </div>
            <button
              onClick={handleNewNote}
              title="New note"
              aria-label="New note"
              className="flex items-center gap-1.5 rounded-lg bg-amber-500 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-amber-600"
            >
              <Plus className="h-4 w-4" />
              New
            </button>
          </div>

          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Search notes"
              aria-label="Search notes"
              className="w-full rounded-lg border border-slate-300 bg-white py-2 pl-9 pr-8 text-sm text-slate-900 focus:ring-2 focus:ring-amber-500"
            />
            {searchInput && (
              <button
                onClick={() => setSearchInput('')}
                aria-label="Clear search"
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>

        </div>

        <div className="flex-1 overflow-y-auto">
          {loading && notes.length === 0 ? (
            <LoadingState />
          ) : notes.length === 0 ? (
            <EmptyState
              icon={<StickyNote />}
              title={searchTerm ? 'No notes match your search.' : 'No notes here yet.'}
              action={!searchTerm && (
                <button onClick={handleNewNote} className="text-sm font-medium text-amber-600 hover:text-amber-700">
                  Write the first one
                </button>
              )}
            />
          ) : (
            <>
              {pinnedNotes.length > 0 && (
                <>
                  <div className="bg-slate-50 px-4 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-400">
                    Pinned
                  </div>
                  {pinnedNotes.map(renderNoteRow)}
                  {otherNotes.length > 0 && (
                    <div className="bg-slate-50 px-4 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-400">
                      Notes
                    </div>
                  )}
                </>
              )}
              {otherNotes.map(renderNoteRow)}
              {totalCount > notes.length && (
                <p className="px-4 py-3 text-center text-xs text-slate-400">
                  Showing the first {notes.length} of {totalCount}. Narrow the list with search.
                </p>
              )}
            </>
          )}
        </div>
      </section>

      {/* Editor */}
      <section className="hidden flex-1 sm:flex">
        {selectedNote ? (
          <div className="h-full w-full">
            <NoteEditor
              note={selectedNote}
              folders={folders}
              content={draft}
              startInEditMode={selectedNote.id === startEditingNoteId}
              saving={saving}
              onContentChange={handleContentChange}
              onTogglePin={handleTogglePin}
              onDelete={handleDeleteNote}
              onMove={(folderId) => moveNote(selectedNote.id, folderId)}
              onUpdateMeta={handleUpdateMeta}
            />
          </div>
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center gap-3 text-center">
            <StickyNote className="h-12 w-12 text-slate-300" />
            <p className="text-slate-500">Select a note, or start a new one.</p>
            <button
              onClick={handleNewNote}
              className="flex items-center gap-2 rounded-lg bg-amber-500 px-4 py-2 text-sm font-medium text-white hover:bg-amber-600"
            >
              <Plus className="h-4 w-4" />
              New Note
            </button>
          </div>
        )}
      </section>
    </div>
  )
}
