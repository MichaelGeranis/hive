import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Check, Clock, Pin, Plus, Search, StickyNote, X } from 'lucide-react'
import NoteEditor from '../components/NoteEditor'
import type { NoteMetaPatch } from '../components/NoteEditor'
import NoteFolderTree from '../components/NoteFolderTree'
import { noteFoldersApi, notesApi } from '../services/api'
import type { ManagerNote, NoteFolder } from '../types'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

type NoteFilter = 'all' | 'pending' | 'completed'

const AUTOSAVE_DELAY_MS = 700
const PAGE_SIZE = 100

const filterOptions: { value: NoteFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'pending', label: 'To-dos' },
  { value: 'completed', label: 'Done' }
]

const priorityDotColors: Record<number, string> = {
  0: 'bg-slate-400',
  1: 'bg-blue-500',
  2: 'bg-orange-500',
  3: 'bg-red-500'
}

function formatListDate(note: ManagerNote): string {
  const date = new Date(note.updatedAt || note.createdAt)
  const now = new Date()
  const sameDay = date.toDateString() === now.toDateString()
  if (sameDay) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }
  const sameYear = date.getFullYear() === now.getFullYear()
  return date.toLocaleDateString([], sameYear ? { month: 'short', day: 'numeric' } : { year: 'numeric', month: 'short', day: 'numeric' })
}

/**
 * Notes: a folder sidebar, the notes in that folder, and the note itself. A note is
 * created empty and saved as it is written, so there is no form and no Save button.
 */
export default function Notes() {
  const { showError } = useToast()
  const [searchParams, setSearchParams] = useSearchParams()
  const initialSearch = searchParams.get('search') || ''

  const [folders, setFolders] = useState<NoteFolder[]>([])
  const [notes, setNotes] = useState<ManagerNote[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null)
  const [filter, setFilter] = useState<NoteFilter>('all')
  const [searchInput, setSearchInput] = useState(initialSearch)
  const [searchTerm, setSearchTerm] = useState(initialSearch)
  const [loading, setLoading] = useState(true)
  const [draft, setDraft] = useState('')
  const [saving, setSaving] = useState(false)

  // Autosave bookkeeping: what is being edited, and what has not reached the server yet.
  const draftRef = useRef('')
  const editingNoteIdRef = useRef<string | null>(null)
  const dirtyRef = useRef(false)
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const selectedNote = useMemo(
    () => notes.find((note) => note.id === selectedNoteId) ?? null,
    [notes, selectedNoteId]
  )

  useEffect(() => {
    if (initialSearch) {
      setSearchParams({}, { replace: true })
    }
  }, [initialSearch, setSearchParams])

  const patchNote = useCallback((updated: ManagerNote) => {
    setNotes((current) => current.map((note) => (note.id === updated.id ? updated : note)))
  }, [])

  const loadFolders = useCallback(async () => {
    try {
      setFolders(await noteFoldersApi.getAll())
    } catch (error) {
      console.error('Failed to load folders:', error)
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

  const openNote = useCallback((note: ManagerNote | null) => {
    editingNoteIdRef.current = note?.id ?? null
    dirtyRef.current = false
    draftRef.current = note?.content ?? ''
    setSelectedNoteId(note?.id ?? null)
    setDraft(note?.content ?? '')
  }, [])

  const loadNotes = useCallback(
    async (folderId: string | null, currentFilter: NoteFilter, search: string, keepSelection = false) => {
      setLoading(true)
      try {
        const result = await notesApi.getAll(
          1,
          PAGE_SIZE,
          currentFilter,
          search || undefined,
          undefined,
          folderId,
          'recent'
        )
        setNotes(result.items)
        setTotalCount(result.totalCount)

        const stillVisible = keepSelection && result.items.some((note) => note.id === editingNoteIdRef.current)
        if (!stillVisible) {
          openNote(result.items[0] ?? null)
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

  useEffect(() => {
    loadFolders()
  }, [loadFolders])

  // The list follows the folder, the filter and the search box. Whatever is being
  // written is saved first, so switching away never loses a keystroke.
  useEffect(() => {
    let cancelled = false
    const run = async () => {
      await flushPendingSave()
      if (!cancelled) {
        await loadNotes(selectedFolderId, filter, searchTerm, true)
      }
    }
    run()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedFolderId, filter, searchTerm])

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
      openNote(created)
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
    if (!confirm(`Delete "${selectedNote.title}"? This cannot be undone.`)) return

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

  const handleToggleComplete = async () => {
    if (!selectedNote) return
    try {
      patchNote(await notesApi.toggle(selectedNote.id))
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

  const handleUpdateMeta = async (patch: NoteMetaPatch) => {
    if (!selectedNote) return
    await flushPendingSave()
    try {
      const updated = await notesApi.update(selectedNote.id, {
        title: selectedNote.title,
        content: draftRef.current,
        tags: patch.tags ?? selectedNote.tags,
        priority: patch.priority ?? selectedNote.priority,
        dueDate: patch.dueDate !== undefined ? patch.dueDate || undefined : selectedNote.dueDate,
        folderId: selectedNote.folderId ?? null,
        isTodo: patch.isTodo ?? selectedNote.isTodo
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
    if (!confirm(`Delete the folder "${folder.name}"? Its notes move to the folder above it.`)) return
    try {
      await noteFoldersApi.delete(folder.id)
      await loadFolders()
      if (selectedFolderId === folder.id) {
        setSelectedFolderId(folder.parentFolderId ?? null)
      } else {
        await loadNotes(selectedFolderId, filter, searchTerm, true)
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
        className={`w-full border-b border-slate-100 px-4 py-3 text-left transition-colors dark:border-slate-700/60 ${
          isSelected ? 'bg-amber-500/10 dark:bg-amber-500/15' : 'hover:bg-slate-50 dark:hover:bg-slate-700/40'
        }`}
      >
        <div className="flex items-center gap-2">
          {note.isPinned && <Pin className="h-3 w-3 shrink-0 text-amber-500" />}
          {note.isTodo && (
            <span
              className={`h-2 w-2 shrink-0 rounded-full ${
                note.isCompleted ? 'bg-green-500' : priorityDotColors[note.priority]
              }`}
            />
          )}
          <span
            className={`flex-1 truncate text-sm font-semibold text-slate-900 dark:text-slate-100 ${
              note.isCompleted ? 'line-through opacity-60' : ''
            }`}
          >
            {note.title}
          </span>
        </div>
        <div className="mt-1 flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
          <span className="shrink-0">{formatListDate(note)}</span>
          <span className="truncate">{note.snippet || 'No additional text'}</span>
        </div>
        {note.isTodo && note.dueDate && (
          <div
            className={`mt-1 flex items-center gap-1 text-xs ${
              note.isOverdue && !note.isCompleted ? 'text-red-500' : 'text-slate-400'
            }`}
          >
            {note.isCompleted ? <Check className="h-3 w-3" /> : <Clock className="h-3 w-3" />}
            {new Date(note.dueDate).toLocaleDateString()}
          </div>
        )}
      </button>
    )
  }

  return (
    <div className="flex h-[calc(100vh-5rem)] overflow-hidden rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
      {/* Folders */}
      <aside className="hidden w-56 shrink-0 flex-col border-r border-slate-200 bg-slate-50 dark:border-slate-700 dark:bg-slate-900/40 lg:flex">
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
      <section className="flex w-full shrink-0 flex-col border-r border-slate-200 dark:border-slate-700 sm:w-80">
        <div className="space-y-3 border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <div className="flex items-center justify-between gap-2">
            <div className="min-w-0">
              <h1 className="truncate text-lg font-bold text-slate-900 dark:text-white">{currentFolderName}</h1>
              <p className="text-xs text-slate-500 dark:text-slate-400">
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
              className="w-full rounded-lg border border-slate-300 bg-white py-2 pl-9 pr-8 text-sm text-slate-900 focus:ring-2 focus:ring-amber-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
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

          <div className="flex gap-1">
            {filterOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => setFilter(option.value)}
                className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${
                  filter === option.value
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          {loading && notes.length === 0 ? (
            <div className="flex h-32 items-center justify-center">
              <div className="h-6 w-6 animate-spin rounded-full border-b-2 border-amber-500" />
            </div>
          ) : notes.length === 0 ? (
            <div className="px-6 py-12 text-center">
              <StickyNote className="mx-auto mb-3 h-10 w-10 text-slate-300 dark:text-slate-600" />
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {searchTerm ? 'No notes match your search.' : 'No notes here yet.'}
              </p>
              {!searchTerm && (
                <button onClick={handleNewNote} className="mt-3 text-sm font-medium text-amber-600 hover:text-amber-700">
                  Write the first one
                </button>
              )}
            </div>
          ) : (
            <>
              {pinnedNotes.length > 0 && (
                <>
                  <div className="bg-slate-50 px-4 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-400 dark:bg-slate-900/40">
                    Pinned
                  </div>
                  {pinnedNotes.map(renderNoteRow)}
                  {otherNotes.length > 0 && (
                    <div className="bg-slate-50 px-4 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-400 dark:bg-slate-900/40">
                      Notes
                    </div>
                  )}
                </>
              )}
              {otherNotes.map(renderNoteRow)}
              {totalCount > notes.length && (
                <p className="px-4 py-3 text-center text-xs text-slate-400">
                  Showing the {notes.length} most recent of {totalCount}. Narrow the list with search.
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
              saving={saving}
              onContentChange={handleContentChange}
              onTogglePin={handleTogglePin}
              onToggleComplete={handleToggleComplete}
              onDelete={handleDeleteNote}
              onMove={(folderId) => moveNote(selectedNote.id, folderId)}
              onUpdateMeta={handleUpdateMeta}
            />
          </div>
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center gap-3 text-center">
            <StickyNote className="h-12 w-12 text-slate-300 dark:text-slate-600" />
            <p className="text-slate-500 dark:text-slate-400">Select a note, or start a new one.</p>
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
