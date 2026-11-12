import type { TaskAiReview } from '../types'
import { VERDICT_LABELS, VERDICT_STYLES } from '../utils/taskReadinessDisplay'

/** A small bee: striped body, two wings, antennae. */
export function Bee({ size = 14 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" aria-hidden="true" className="shrink-0">
      <ellipse cx="8.5" cy="7.5" rx="4.2" ry="3" fill="#e0f2fe" stroke="#7dd3fc" strokeWidth="0.8" transform="rotate(-30 8.5 7.5)" />
      <ellipse cx="15.5" cy="7.5" rx="4.2" ry="3" fill="#e0f2fe" stroke="#7dd3fc" strokeWidth="0.8" transform="rotate(30 15.5 7.5)" />
      <ellipse cx="12" cy="14.5" rx="5.2" ry="6.2" fill="#fbbf24" stroke="#92400e" strokeWidth="0.8" />
      <path d="M7.2 12.6h9.6M6.9 15.4h10.2M7.8 18.2h8.4" stroke="#1f2937" strokeWidth="1.6" strokeLinecap="round" />
      <path d="M10.6 8.6 9 5.4M13.4 8.6 15 5.4" stroke="#1f2937" strokeWidth="0.9" strokeLinecap="round" />
      <circle cx="9" cy="5.2" r="0.9" fill="#1f2937" />
      <circle cx="15" cy="5.2" r="0.9" fill="#1f2937" />
    </svg>
  )
}

/**
 * Marks a task the AI has reviewed: just the bee, in a ring of the verdict's colour.
 * Hovering shows the verdict, the model's summary and what it found missing. A review of an
 * earlier version of the task has a dashed ring.
 */
export function AiBeeBadge({ review }: { review: TaskAiReview }) {
  const verdict = VERDICT_LABELS[review.verdict] ?? review.verdict
  const staleNote = review.isUpToDate ? '' : ' (the task changed since)'
  const missing = review.missing.length > 0 ? `\nMissing: ${review.missing.join('; ')}` : ''
  const tooltip = `Reviewed by AI (${review.model}): ${verdict}${staleNote}\n${review.summary}${missing}`

  return (
    <span
      title={tooltip}
      aria-label={`AI review: ${verdict}${staleNote}`}
      className={`inline-flex items-center justify-center p-0.5 rounded-full border ${VERDICT_STYLES[review.verdict] ?? VERDICT_STYLES.Unclear} ${review.isUpToDate ? '' : 'border-dashed'}`}
    >
      <Bee size={16} />
    </span>
  )
}
