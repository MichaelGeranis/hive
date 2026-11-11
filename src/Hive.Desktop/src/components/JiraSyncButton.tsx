import { SyncButton } from './SyncButton'

interface JiraSyncButtonProps {
  /** Runs the sync and reports its outcome; the button only shows that it is running. */
  onSync: () => Promise<void>
  /** Tooltip saying what this button pulls from Jira. */
  title: string
  label?: string
}

/**
 * The one "pull from Jira" button, used on the Dashboard for everything and on each page
 * for just that page's data.
 */
export function JiraSyncButton({ onSync, title, label = 'Sync Jira' }: JiraSyncButtonProps) {
  return <SyncButton onSync={onSync} title={title} label={label} />
}
