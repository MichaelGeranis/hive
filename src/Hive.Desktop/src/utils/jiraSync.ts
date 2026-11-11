import type { JiraParentSyncResult, JiraProductSyncResult, JiraSprintSyncResult, JiraTaskSyncResult } from '../types'

/** What to tell the manager after a sync: always a success line, sometimes a warning. */
export interface SyncMessages {
  success: string
  warning?: string
}

const plural = (count: number, word: string) => `${count} ${word}${count === 1 ? '' : 's'}`

export function describeSprintDateSync(result: JiraSprintSyncResult): SyncMessages {
  const updated = result.updated.length
  const created = result.created?.length ?? 0
  const skipped = [
    result.notFoundInJira.length && `${result.notFoundInJira.length} not on the Jira board`,
    result.missingDatesInJira.length && `${result.missingDatesInJira.length} without dates in Jira`,
    result.ambiguousInJira.length && `${result.ambiguousInJira.length} with a duplicate name in Jira`,
    result.errors.length && `${result.errors.length} rejected`
  ].filter(Boolean)

  const changes = [
    created > 0 && `added ${plural(created, 'upcoming sprint')}`,
    updated > 0 && `updated dates for ${plural(updated, 'sprint')}`
  ].filter(Boolean)

  return {
    success: created > 0
      ? `Sprints synced from Jira: ${changes.join(', ')}`
      : updated > 0
        ? `Updated dates for ${plural(updated, 'sprint')} from Jira`
        : 'Sprint dates already match Jira',
    warning: skipped.length > 0 ? `Sprints skipped: ${skipped.join(', ')}` : undefined
  }
}

export function describeProductSync(result: JiraProductSyncResult): SyncMessages {
  const changes = describeChanges(result.created.length, result.updated.length, result.removed.length)

  const errors = result.errors.length
  return {
    success: changes.length > 0
      ? `Products synced from Jira: ${changes.join(', ')} (${plural(result.jiraComponentCount, 'component')} in Jira)`
      : `Products already match Jira (${plural(result.jiraComponentCount, 'component')} in Jira)`,
    warning: errors > 0
      ? `${plural(errors, 'Jira component')} not applied: ${result.errors[0]}${errors > 1 ? ' …' : ''}`
      : undefined
  }
}

export function describeParentSync(result: JiraParentSyncResult): SyncMessages {
  const changes = describeChanges(result.created.length, result.updated.length, result.removed?.length ?? 0)

  const errors = result.errors.length
  return {
    success: changes.length > 0
      ? `Parents synced from Jira: ${changes.join(', ')} (${result.jiraParentCount} in Jira)`
      : `Parents already match Jira (${result.jiraParentCount} in Jira)`,
    warning: errors > 0
      ? `${plural(errors, 'Jira issue')} not applied: ${result.errors[0]}${errors > 1 ? ' …' : ''}`
      : undefined
  }
}

/** Whether a parent sync changed anything worth mentioning next to a task sync. */
export function parentSyncChangedAnything(result: JiraParentSyncResult): boolean {
  return result.created.length + result.updated.length + (result.removed?.length ?? 0) + result.errors.length > 0
}

export function describeTaskSync(result: JiraTaskSyncResult): SyncMessages {
  const changes = describeChanges(result.created.length, result.updated.length, result.removed.length)
  const errors = result.errors.length
  return {
    success: changes.length > 0
      ? `Tasks synced from Jira: ${changes.join(', ')} (${result.jiraTaskCount} in sprints)`
      : `Tasks already match Jira (${result.jiraTaskCount} in sprints)`,
    warning: errors > 0
      ? `${plural(errors, 'task')} not applied: ${result.errors[0]}${errors > 1 ? ' …' : ''}`
      : undefined
  }
}

function describeChanges(created: number, updated: number, removed: number): string[] {
  return [
    created && `${created} added`,
    updated && `${updated} updated`,
    removed && `${removed} removed`
  ].filter((part): part is string => Boolean(part))
}
