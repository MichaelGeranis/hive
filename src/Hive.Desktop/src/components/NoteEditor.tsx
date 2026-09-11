import { useState } from 'react'
import { Pin, PinOff, Sliders, Trash2 } from 'lucide-react'
import MarkdownEditor from './MarkdownEditor'
import type { ManagerNote, NoteFolder } from '../types'

export interface NoteMetaPatch {
  tags?: string
}

interface NoteEditorProps {
  note: ManagerNote
  folders: NoteFolder[]
  content: string
  startInEditMode?: boolean
  saving: boolean
  onContentChange: (value: string) => void
  onTogglePin: () => void
  onDelete: () => void
  onMove: (folderId: string | null) => void
  onUpdateMeta: (patch: NoteMetaPatch) => void
}

/**
 * A note on the shared markdown writing surface, with the chrome a note needs:
 * the folder it lives in, whether it is pinned, and its tags.
 */
export default function NoteEditor({
  note,
  folders,
  content,
  startInEditMode = false,
  saving,
  onContentChange,
  onTogglePin,
  onDelete,
  onMove,
  onUpdateMeta
}: NoteEditorProps) {
  const [showInspector, setShowInspector] = useState(false)

  const actions = (
    <>
      <select
        value={note.folderId ?? ''}
        onChange={(e) => onMove(e.target.value || null)}
        title="Move to folder"
        className="rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-600 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300"
      >
        <option value="">All Notes</option>
        {folders.map((folder) => (
          <option key={folder.id} value={folder.id}>
            {folder.name}
          </option>
        ))}
      </select>

      <button
        onClick={() => setShowInspector((value) => !value)}
        title="Note details"
        aria-label="Note details"
        className={`rounded-md p-1.5 ${
          showInspector
            ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40'
            : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700'
        }`}
      >
        <Sliders className="h-4 w-4" />
      </button>

      <button
        onClick={onTogglePin}
        title={note.isPinned ? 'Unpin note' : 'Pin note'}
        aria-label={note.isPinned ? 'Unpin note' : 'Pin note'}
        className={`rounded-md p-1.5 ${
          note.isPinned ? 'text-amber-500' : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700'
        }`}
      >
        {note.isPinned ? <PinOff className="h-4 w-4" /> : <Pin className="h-4 w-4" />}
      </button>

      <button
        onClick={onDelete}
        title="Delete note"
        aria-label="Delete note"
        className="rounded-md p-1.5 text-slate-500 hover:bg-red-50 hover:text-red-500 dark:text-slate-400 dark:hover:bg-red-900/30"
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </>
  )

  const details = showInspector ? (
    <div className="border-b border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900/40">
      <input
        type="text"
        defaultValue={note.tags}
        placeholder="tags, comma separated"
        onBlur={(e) => {
          if (e.target.value !== note.tags) {
            onUpdateMeta({ tags: e.target.value })
          }
        }}
        className="rounded-lg border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
      />
    </div>
  ) : null

  return (
    <MarkdownEditor
      documentId={note.id}
      content={content}
      saving={saving}
      startInEditMode={startInEditMode}
      actions={actions}
      belowToolbar={details}
      contentLabel="Note content"
      onContentChange={onContentChange}
    />
  )
}
