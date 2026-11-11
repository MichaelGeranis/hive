import { useState, type ReactNode } from 'react'

interface CollapsibleTagsProps<T> {
  items: T[]
  renderItem: (item: T, index: number) => ReactNode
  /** How many items show while collapsed. */
  limit?: number
  /** Items that stay visible while collapsed, e.g. the selected filter. */
  alwaysShow?: (item: T) => boolean
}

/**
 * Renders a list of tag chips, hiding everything past `limit` behind a
 * "+N more" toggle. Returns a fragment so it slots into the caller's own
 * flex container next to any icon or label already there.
 */
export default function CollapsibleTags<T>({
  items,
  renderItem,
  limit = 5,
  alwaysShow
}: CollapsibleTagsProps<T>) {
  const [expanded, setExpanded] = useState(false)

  const collapsible = items.length > limit
  const visible = items
    .map((item, index) => ({ item, index }))
    .filter(({ item, index }) =>
      !collapsible || expanded || index < limit || (alwaysShow?.(item) ?? false))
  const hiddenCount = items.length - visible.length

  return (
    <>
      {visible.map(({ item, index }) => renderItem(item, index))}
      {collapsible && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            setExpanded(!expanded)
          }}
          aria-expanded={expanded}
          className="px-2 py-0.5 text-xs font-medium text-amber-600 hover:text-amber-700 hover:bg-amber-50 rounded-full transition-colors"
        >
          {expanded ? 'Show less' : `+${hiddenCount} more`}
        </button>
      )}
    </>
  )
}
