import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import {
  Bold,
  CheckSquare,
  Eye,
  Heading2,
  Italic,
  List,
  ListOrdered,
  Pencil,
  Pin,
  PinOff,
  Sliders,
  Trash2
} from 'lucide-react'
import MarkdownPreview from './MarkdownPreview'
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

/** Markers that continue onto the next line when Enter is pressed. */
const LIST_PATTERN = /^(\s*)([-*+]\s\[[ xX]\]\s|[-*+]\s|(\d+)([.)])\s)/

/**
 * The writing surface. Plain markdown text in, rendered markdown out — the note is
 * always saved by its owner page, never by a Save button.
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
  const [preview, setPreview] = useState(false)
  const [showInspector, setShowInspector] = useState(false)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  // A different note opens in preview unless the owner explicitly requests editing.
  useEffect(() => {
    setPreview(!startInEditMode)
    if (startInEditMode) {
      const textarea = textareaRef.current
      if (textarea) {
        textarea.focus()
        textarea.setSelectionRange(textarea.value.length, textarea.value.length)
      }
    }
  }, [note.id, startInEditMode])

  const applyEdit = (value: string, selectionStart: number, selectionEnd = selectionStart) => {
    onContentChange(value)
    requestAnimationFrame(() => {
      const textarea = textareaRef.current
      if (textarea) {
        textarea.focus()
        textarea.setSelectionRange(selectionStart, selectionEnd)
      }
    })
  }

  const wrapSelection = (marker: string) => {
    const textarea = textareaRef.current
    if (!textarea) return
    const { selectionStart, selectionEnd } = textarea
    const selected = content.slice(selectionStart, selectionEnd)
    const next = `${content.slice(0, selectionStart)}${marker}${selected}${marker}${content.slice(selectionEnd)}`
    applyEdit(next, selectionStart + marker.length, selectionEnd + marker.length)
  }

  const prefixLine = (prefix: string) => {
    const textarea = textareaRef.current
    if (!textarea) return
    const { selectionStart } = textarea
    const lineStart = content.lastIndexOf('\n', selectionStart - 1) + 1
    const next = `${content.slice(0, lineStart)}${prefix}${content.slice(lineStart)}`
    applyEdit(next, selectionStart + prefix.length)
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    const modifier = event.metaKey || event.ctrlKey

    if (modifier && event.key.toLowerCase() === 'b') {
      event.preventDefault()
      wrapSelection('**')
      return
    }

    if (modifier && event.key.toLowerCase() === 'i') {
      event.preventDefault()
      wrapSelection('_')
      return
    }

    if (event.key === 'Tab') {
      event.preventDefault()
      const textarea = event.currentTarget
      const { selectionStart, selectionEnd } = textarea
      const next = `${content.slice(0, selectionStart)}  ${content.slice(selectionEnd)}`
      applyEdit(next, selectionStart + 2)
      return
    }

    if (event.key === 'Enter' && !event.shiftKey) {
      const textarea = event.currentTarget
      const { selectionStart } = textarea
      const lineStart = content.lastIndexOf('\n', selectionStart - 1) + 1
      const currentLine = content.slice(lineStart, selectionStart)
      const match = currentLine.match(LIST_PATTERN)
      if (!match) return

      const [marker, indent, , orderedNumber, orderedSuffix] = match

      // An empty list item ends the list instead of adding another one.
      if (currentLine.trim() === marker.trim()) {
        event.preventDefault()
        const next = `${content.slice(0, lineStart)}${content.slice(selectionStart)}`
        applyEdit(next, lineStart)
        return
      }

      event.preventDefault()
      const continuation = orderedNumber
        ? `${indent}${Number(orderedNumber) + 1}${orderedSuffix} `
        : marker.replace(/\[[xX]\]/, '[ ]')
      const next = `${content.slice(0, selectionStart)}\n${continuation}${content.slice(selectionStart)}`
      applyEdit(next, selectionStart + continuation.length + 1)
    }
  }

  /** Flips the nth `- [ ]` / `- [x]` checkbox in the source when clicked in the preview. */
  const toggleTaskAtIndex = (index: number) => {
    let seen = -1
    const next = content
      .split('\n')
      .map((line) => {
        const match = line.match(/^(\s*[-*+]\s\[)([ xX])(\]\s?)/)
        if (!match) return line
        seen += 1
        if (seen !== index) return line
        const checked = match[2].toLowerCase() === 'x'
        return `${match[1]}${checked ? ' ' : 'x'}${match[3]}${line.slice(match[0].length)}`
      })
      .join('\n')
    onContentChange(next)
  }

  const toolbarButton = (label: string, icon: React.ReactNode, onClick: () => void) => (
    <button
      key={label}
      onClick={onClick}
      title={label}
      aria-label={label}
      className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-800 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-white"
    >
      {icon}
    </button>
  )

  return (
    <div className="flex h-full flex-col bg-white dark:bg-slate-800">
      {/* Toolbar */}
      <div className="flex items-center gap-1 border-b border-slate-200 dark:border-slate-700 px-3 py-2">
        {!preview && (
          <>
            {toolbarButton('Bold', <Bold className="h-4 w-4" />, () => wrapSelection('**'))}
            {toolbarButton('Italic', <Italic className="h-4 w-4" />, () => wrapSelection('_'))}
            {toolbarButton('Heading', <Heading2 className="h-4 w-4" />, () => prefixLine('## '))}
            {toolbarButton('Bulleted list', <List className="h-4 w-4" />, () => prefixLine('- '))}
            {toolbarButton('Numbered list', <ListOrdered className="h-4 w-4" />, () => prefixLine('1. '))}
            {toolbarButton('Checklist', <CheckSquare className="h-4 w-4" />, () => prefixLine('- [ ] '))}
            <span className="mx-1 h-5 w-px bg-slate-200 dark:bg-slate-700" />
          </>
        )}

        <button
          onClick={() => setPreview((value) => !value)}
          className="flex items-center gap-1.5 rounded-md px-2 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-700"
        >
          {preview ? <Pencil className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
          {preview ? 'Edit' : 'Preview'}
        </button>

        <div className="flex-1" />

        <span className="mr-1 text-xs text-slate-400">{saving ? 'Saving…' : 'Saved'}</span>

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
            showInspector ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40' : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700'
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
      </div>

      {/* Note details */}
      {showInspector && (
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
      )}

      {/* Writing surface */}
      {preview ? (
        <div className="flex-1 overflow-y-auto px-8 py-6">
          {content.trim() ? (
            <MarkdownPreview content={content} onToggleTask={toggleTaskAtIndex} />
          ) : (
            <p className="text-slate-400">Nothing written yet.</p>
          )}
        </div>
      ) : (
        <textarea
          ref={textareaRef}
          value={content}
          onChange={(e) => onContentChange(e.target.value)}
          onKeyDown={handleKeyDown}
          spellCheck
          placeholder="Start writing. Markdown works: # heading, **bold**, - list, - [ ] task."
          aria-label="Note content"
          className="flex-1 resize-none bg-transparent px-8 py-6 font-mono text-[15px] leading-relaxed text-slate-800 outline-none placeholder:text-slate-400 dark:text-slate-100"
        />
      )}
    </div>
  )
}
