/**
 * Colours for the Jira issue types Hive shows. Epic keeps the purple it always had; a type
 * not listed here still gets a neutral badge.
 */
const TYPE_COLORS: Record<string, string> = {
  epic: 'bg-purple-100 text-purple-700',
  story: 'bg-green-100 text-green-700',
  task: 'bg-blue-100 text-blue-700',
  support: 'bg-amber-100 text-amber-700',
  bug: 'bg-red-100 text-red-700',
  spike: 'bg-cyan-100 text-cyan-700'
}

export function issueTypeColor(type: string): string {
  return TYPE_COLORS[type.toLowerCase()] ?? 'bg-slate-100 text-slate-700'
}
