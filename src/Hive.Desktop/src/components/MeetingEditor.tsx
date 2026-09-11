import { AlertCircle, Trash2, User } from 'lucide-react'
import MarkdownEditor from './MarkdownEditor'
import type { OneOnOneMeeting } from '../types'

interface MeetingEditorProps {
  meeting: OneOnOneMeeting
  content: string
  saving: boolean
  startInEditMode?: boolean
  onContentChange: (value: string) => void
  onTagsChange: (tags: string) => void
  onDateChange: (meetingDate: string) => void
  onDelete: () => void
}

/**
 * A 1:1 on the shared markdown writing surface. The tags are shown in full rather than
 * hidden away, because they are what links the note to a person and sets its date.
 */
export default function MeetingEditor({
  meeting,
  content,
  saving,
  startInEditMode = false,
  onContentChange,
  onTagsChange,
  onDateChange,
  onDelete
}: MeetingEditorProps) {
  const actions = (
    <>
      <input
        type="date"
        value={meeting.meetingDate}
        onChange={(e) => e.target.value && onDateChange(e.target.value)}
        title="Date of the 1:1"
        aria-label="Date of the 1:1"
        className="rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-600 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300"
      />

      <button
        onClick={onDelete}
        title="Delete 1:1"
        aria-label="Delete 1:1"
        className="rounded-md p-1.5 text-slate-500 hover:bg-red-50 hover:text-red-500 dark:text-slate-400 dark:hover:bg-red-900/30"
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </>
  )

  const tagBar = (
    <div className="flex flex-wrap items-center gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-900/40">
      <input
        type="text"
        key={meeting.id}
        defaultValue={meeting.tags}
        placeholder="#name to link the person, #YYYYMMDD to set the date"
        aria-label="1:1 tags"
        onBlur={(e) => {
          if (e.target.value !== meeting.tags) {
            onTagsChange(e.target.value)
          }
        }}
        className="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
      />

      {meeting.isUnlinked ? (
        <span className="flex items-center gap-1.5 rounded-full bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-700 dark:bg-amber-900/40 dark:text-amber-300">
          <AlertCircle className="h-3.5 w-3.5" />
          Not linked to anyone
        </span>
      ) : (
        <span className="flex items-center gap-1.5 rounded-full bg-purple-100 px-2.5 py-1 text-xs font-medium text-purple-700 dark:bg-purple-900/40 dark:text-purple-300">
          <User className="h-3.5 w-3.5" />
          {meeting.directReportName}
        </span>
      )}
    </div>
  )

  return (
    <MarkdownEditor
      documentId={meeting.id}
      content={content}
      saving={saving}
      startInEditMode={startInEditMode}
      actions={actions}
      belowToolbar={tagBar}
      placeholder="What did you talk about? Markdown works: # heading, **bold**, - list."
      contentLabel="1:1 notes"
      onContentChange={onContentChange}
    />
  )
}
