import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import { Bold, CheckSquare, Eye, Heading2, Italic, List, ListOrdered, Pencil } from 'lucide-react'
import MarkdownPreview from './MarkdownPreview'

interface MarkdownEditorProps {
  /** Changing this opens a fresh document: preview mode and focus are reset. */
  documentId: string
  content: string
  saving: boolean
  startInEditMode?: boolean
  /** Buttons and controls for the right-hand side of the toolbar. */
  actions?: ReactNode
  /** Optional panel rendered between the toolbar and the writing surface. */
  belowToolbar?: ReactNode
  placeholder?: string
  contentLabel?: string
  onContentChange: (value: string) => void
}

/** Markers that continue onto the next line when Enter is pressed. */
const LIST_PATTERN = /^(\s*)([-*+]\s\[[ xX]\]\s|[-*+]\s|(\d+)([.)])\s)/

/**
 * The writing surface shared by notes and 1:1s. Plain markdown text in, rendered
 * markdown out — what is written is always saved by the owning page, never by a button.
 */
export default function MarkdownEditor({
  documentId,
  content,
  saving,
  startInEditMode = false,
  actions,
  belowToolbar,
  placeholder = 'Start writing. Markdown works: # heading, **bold**, - list, - [ ] task.',
  contentLabel = 'Content',
  onContentChange
}: MarkdownEditorProps) {
  const [preview, setPreview] = useState(false)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  // A different document opens in preview unless the owner explicitly requests editing.
  useEffect(() => {
    setPreview(!startInEditMode)
    if (startInEditMode) {
      const textarea = textareaRef.current
      if (textarea) {
        textarea.focus()
        textarea.setSelectionRange(textarea.value.length, textarea.value.length)
      }
    }
  }, [documentId, startInEditMode])

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

  const toolbarButton = (label: string, icon: ReactNode, onClick: () => void) => (
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

        {actions}
      </div>

      {belowToolbar}

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
          placeholder={placeholder}
          aria-label={contentLabel}
          className="flex-1 resize-none bg-transparent px-8 py-6 font-mono text-[15px] leading-relaxed text-slate-800 outline-none placeholder:text-slate-400 dark:text-slate-100"
        />
      )}
    </div>
  )
}
