/**
 * How a task status is shown on the dashboard: one colour and one label per status, shared
 * by the sprint status bar and the workload chart. Keyed by the status name the API sends.
 */
export const STATUS_COLORS: Record<string, string> = {
  Backlog: '#94a3b8',
  Todo: '#f59e0b',
  Blocked: '#ef4444',
  InProgress: '#3b82f6',
  InReview: '#8b5cf6',
  InTest: '#06b6d4',
  POAcceptance: '#f97316',
  ReadyToRelease: '#14b8a6',
  Done: '#10b981',
  Cancelled: '#6b7280'
}

export const STATUS_LABELS: Record<string, string> = {
  Backlog: 'Backlog',
  Todo: 'To do',
  Blocked: 'Blocked',
  InProgress: 'In progress',
  InReview: 'In review',
  InTest: 'In test',
  POAcceptance: 'PO acceptance',
  ReadyToRelease: 'Ready to release',
  Done: 'Done',
  Cancelled: "Won't do"
}
