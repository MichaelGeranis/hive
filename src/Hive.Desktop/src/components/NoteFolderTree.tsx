import { useState } from 'react'
import type { KeyboardEvent } from 'react'
import { ChevronDown, ChevronRight, Folder, FolderPlus, Pencil, StickyNote, Trash2 } from 'lucide-react'
import type { NoteFolder } from '../types'

interface NoteFolderTreeProps {
  folders: NoteFolder[]
  selectedFolderId: string | null
  totalNoteCount: number
  onSelect: (folderId: string | null) => void
  onCreate: (name: string, parentFolderId: string | null) => void | Promise<void>
  onRename: (folder: NoteFolder, name: string) => void | Promise<void>
  onDelete: (folder: NoteFolder) => void | Promise<void>
  onDropNote: (noteId: string, folderId: string | null) => void | Promise<void>
}

/**
 * The folder sidebar: a nested list of folders with note counts, inline rename, and
 * notes that can be dragged from the list straight onto a folder.
 */
export default function NoteFolderTree({
  folders,
  selectedFolderId,
  totalNoteCount,
  onSelect,
  onCreate,
  onRename,
  onDelete,
  onDropNote
}: NoteFolderTreeProps) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set())
  const [renamingId, setRenamingId] = useState<string | null>(null)
  const [creatingUnder, setCreatingUnder] = useState<string | null | undefined>(undefined)
  const [draftName, setDraftName] = useState('')
  const [dropTargetId, setDropTargetId] = useState<string | null | undefined>(undefined)

  const childrenOf = (parentId: string | null) => folders.filter((f) => (f.parentFolderId ?? null) === parentId)

  const toggleExpanded = (id: string) => {
    setExpanded((current) => {
      const next = new Set(current)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const startCreating = (parentId: string | null) => {
    if (parentId) {
      setExpanded((current) => new Set(current).add(parentId))
    }
    setCreatingUnder(parentId)
    setDraftName('')
  }

  const submitCreate = async () => {
    const name = draftName.trim()
    if (name) {
      await onCreate(name, creatingUnder ?? null)
    }
    setCreatingUnder(undefined)
    setDraftName('')
  }

  const submitRename = async (folder: NoteFolder) => {
    const name = draftName.trim()
    if (name && name !== folder.name) {
      await onRename(folder, name)
    }
    setRenamingId(null)
    setDraftName('')
  }

  const handleNameKeyDown = (event: KeyboardEvent<HTMLInputElement>, submit: () => void, cancel: () => void) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      submit()
    } else if (event.key === 'Escape') {
      event.preventDefault()
      cancel()
    }
  }

  const handleDrop = async (event: React.DragEvent, folderId: string | null) => {
    event.preventDefault()
    setDropTargetId(undefined)
    const noteId = event.dataTransfer.getData('text/hive-note-id')
    if (noteId) {
      await onDropNote(noteId, folderId)
    }
  }

  const nameInput = (submit: () => void, cancel: () => void, placeholder: string) => (
    <input
      autoFocus
      value={draftName}
      placeholder={placeholder}
      onChange={(e) => setDraftName(e.target.value)}
      onKeyDown={(e) => handleNameKeyDown(e, submit, cancel)}
      onBlur={submit}
      className="w-full rounded-md border border-amber-400 bg-white dark:bg-slate-700 px-2 py-1 text-sm text-slate-900 dark:text-white focus:outline-none"
    />
  )

  const renderFolder = (folder: NoteFolder, depth: number) => {
    const children = childrenOf(folder.id)
    const isExpanded = expanded.has(folder.id)
    const isSelected = selectedFolderId === folder.id
    const isDropTarget = dropTargetId === folder.id

    return (
      <div key={folder.id}>
        <div
          onDragOver={(e) => {
            e.preventDefault()
            setDropTargetId(folder.id)
          }}
          onDragLeave={() => setDropTargetId(undefined)}
          onDrop={(e) => handleDrop(e, folder.id)}
          className={`group flex items-center gap-1 rounded-lg pr-1 transition-colors ${
            isSelected
              ? 'bg-amber-500 text-white'
              : isDropTarget
              ? 'bg-amber-100 dark:bg-amber-900/40 text-slate-700 dark:text-slate-200'
              : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700/60'
          }`}
          style={{ paddingLeft: `${depth * 12}px` }}
        >
          <button
            onClick={() => toggleExpanded(folder.id)}
            className={`p-1 ${children.length === 0 ? 'invisible' : ''}`}
            aria-label={isExpanded ? `Collapse ${folder.name}` : `Expand ${folder.name}`}
          >
            {isExpanded ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
          </button>

          {renamingId === folder.id ? (
            <div className="flex-1 py-1 pr-1">
              {nameInput(() => submitRename(folder), () => setRenamingId(null), 'Folder name')}
            </div>
          ) : (
            <>
              <button
                onClick={() => onSelect(folder.id)}
                className="flex flex-1 items-center gap-2 py-1.5 text-left text-sm font-medium truncate"
              >
                <Folder className={`h-4 w-4 shrink-0 ${isSelected ? 'text-white' : 'text-amber-500'}`} />
                <span className="truncate">{folder.name}</span>
              </button>
              <span className={`text-xs ${isSelected ? 'text-amber-100' : 'text-slate-400'}`}>{folder.noteCount}</span>
              <div className="flex opacity-0 transition-opacity group-hover:opacity-100">
                <button
                  onClick={() => startCreating(folder.id)}
                  title="New sub-folder"
                  className="p-1 hover:text-amber-600"
                >
                  <FolderPlus className="h-3.5 w-3.5" />
                </button>
                <button
                  onClick={() => {
                    setRenamingId(folder.id)
                    setDraftName(folder.name)
                  }}
                  title="Rename folder"
                  className="p-1 hover:text-amber-600"
                >
                  <Pencil className="h-3.5 w-3.5" />
                </button>
                <button onClick={() => onDelete(folder)} title="Delete folder" className="p-1 hover:text-red-500">
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            </>
          )}
        </div>

        {creatingUnder === folder.id && (
          <div className="py-1" style={{ paddingLeft: `${(depth + 1) * 12 + 24}px` }}>
            {nameInput(submitCreate, () => setCreatingUnder(undefined), 'New folder')}
          </div>
        )}

        {isExpanded && children.map((child) => renderFolder(child, depth + 1))}
      </div>
    )
  }

  return (
    <div className="flex h-full flex-col">
      <div className="flex-1 space-y-0.5 overflow-y-auto p-2">
        <div
          onDragOver={(e) => {
            e.preventDefault()
            setDropTargetId(null)
          }}
          onDragLeave={() => setDropTargetId(undefined)}
          onDrop={(e) => handleDrop(e, null)}
          className={`flex items-center gap-2 rounded-lg px-2 transition-colors ${
            selectedFolderId === null
              ? 'bg-amber-500 text-white'
              : dropTargetId === null
              ? 'bg-amber-100 dark:bg-amber-900/40 text-slate-700 dark:text-slate-200'
              : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700/60'
          }`}
        >
          <button onClick={() => onSelect(null)} className="flex flex-1 items-center gap-2 py-2 text-left text-sm font-medium">
            <StickyNote className={`h-4 w-4 ${selectedFolderId === null ? 'text-white' : 'text-amber-500'}`} />
            All Notes
          </button>
          <span className={`text-xs ${selectedFolderId === null ? 'text-amber-100' : 'text-slate-400'}`}>
            {totalNoteCount}
          </span>
        </div>

        {childrenOf(null).map((folder) => renderFolder(folder, 0))}

        {creatingUnder === null && <div className="px-2 py-1">{nameInput(submitCreate, () => setCreatingUnder(undefined), 'New folder')}</div>}
      </div>

      <button
        onClick={() => startCreating(null)}
        className="flex items-center gap-2 border-t border-slate-200 dark:border-slate-700 px-4 py-3 text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-amber-600"
      >
        <FolderPlus className="h-4 w-4" />
        New Folder
      </button>
    </div>
  )
}
