import { issueTypeColor } from '../utils/issueTypeDisplay'

export function IssueTypeBadge({ type }: { type: string }) {
  return (
    <span className={`px-2 py-0.5 text-xs font-medium rounded-full whitespace-nowrap ${issueTypeColor(type)}`}>
      {type}
    </span>
  )
}
