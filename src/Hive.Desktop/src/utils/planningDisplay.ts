// How quarterly planning's enums and numbers are shown. Purely visual: every rule behind them
// lives on the server.

import {
  ExternalDependencyStatus,
  InitiativeKind,
  InitiativeStatus,
  WorkType
} from '../types/quarterlyPlanning'
import type { CellShare, Initiative, MixShares, PlanningBoard, UpdateInitiativeDto } from '../types/quarterlyPlanning'

export const STATUS_LABELS: Record<number, string> = {
  [InitiativeStatus.InDiscovery]: 'In discovery',
  [InitiativeStatus.InSolutionDesign]: 'In Solution Design',
  [InitiativeStatus.ReadyForCommitment]: 'Ready for commitment',
  [InitiativeStatus.Committed]: 'Committed',
  [InitiativeStatus.Stretch]: 'Stretch',
  [InitiativeStatus.Dropped]: 'Dropped',
  [InitiativeStatus.Delivered]: 'Delivered'
}

export const STATUS_STYLES: Record<number, string> = {
  [InitiativeStatus.InDiscovery]: 'bg-slate-100 text-slate-700',
  [InitiativeStatus.InSolutionDesign]: 'bg-sky-100 text-sky-800',
  [InitiativeStatus.ReadyForCommitment]: 'bg-teal-100 text-teal-800',
  [InitiativeStatus.Committed]: 'bg-green-100 text-green-800',
  [InitiativeStatus.Stretch]: 'bg-amber-100 text-amber-800',
  [InitiativeStatus.Dropped]: 'bg-red-100 text-red-700',
  [InitiativeStatus.Delivered]: 'bg-emerald-600 text-white'
}

/** The statuses in the order a quarter moves through them. */
export const STATUS_FLOW = [
  InitiativeStatus.InDiscovery,
  InitiativeStatus.InSolutionDesign,
  InitiativeStatus.ReadyForCommitment,
  InitiativeStatus.Committed,
  InitiativeStatus.Dropped,
  InitiativeStatus.Delivered
]

/** Decisions ask for a reason. */
export const DECISION_STATUSES: number[] = [InitiativeStatus.Committed, InitiativeStatus.Stretch, InitiativeStatus.Dropped]

export const WORK_TYPE_LABELS: Record<number, string> = {
  [WorkType.ProductRoadmap]: 'Product roadmap',
  [WorkType.TechRoadmap]: 'Tech roadmap',
  [WorkType.Maintenance]: 'Maintenance'
}

export const WORK_TYPE_COLORS: Record<number, string> = {
  [WorkType.ProductRoadmap]: '#8B5CF6',
  [WorkType.TechRoadmap]: '#3B82F6',
  [WorkType.Maintenance]: '#EF4444'
}

export const DEPENDENCY_STATUS_LABELS: Record<number, string> = {
  [ExternalDependencyStatus.NeedsCoordination]: 'Needs coordination',
  [ExternalDependencyStatus.Agreed]: 'Agreed',
  [ExternalDependencyStatus.AtRisk]: 'At risk',
  [ExternalDependencyStatus.Done]: 'Done'
}

export const DEPENDENCY_STATUS_STYLES: Record<number, string> = {
  [ExternalDependencyStatus.NeedsCoordination]: 'bg-amber-100 text-amber-800',
  [ExternalDependencyStatus.Agreed]: 'bg-sky-100 text-sky-800',
  [ExternalDependencyStatus.AtRisk]: 'bg-red-100 text-red-700',
  [ExternalDependencyStatus.Done]: 'bg-green-100 text-green-800'
}

export const SEVERITY_STYLES: Record<number, { dot: string; text: string; label: string }> = {
  0: { dot: 'bg-sky-400', text: 'text-sky-700', label: 'Info' },
  1: { dot: 'bg-amber-400', text: 'text-amber-700', label: 'Warning' },
  2: { dot: 'bg-red-500', text: 'text-red-700', label: 'Critical' }
}

/** Dev-sprints, shown with at most one decimal: 2.5, 3. */
export const formatEffort = (value: number | null | undefined) =>
  value === null || value === undefined ? '?' : (Math.round(value * 10) / 10).toString()

/** How the priority column reads, as in the Plan-Catalogue: 0 for leftovers, NEW for arrivals, – for lanes. */
export function priorityLabel(initiative: Pick<Initiative, 'kind' | 'rank' | 'arrivedOn'>) {
  if (initiative.kind === InitiativeKind.Bucket) return '–'
  if (initiative.kind === InitiativeKind.Leftover) return '0'
  if (initiative.arrivedOn) return 'NEW'
  return initiative.rank > 0 ? String(initiative.rank) : '·'
}

export const mixEntries = (mix: MixShares) => [
  { workType: WorkType.ProductRoadmap, percent: mix.productRoadmap },
  { workType: WorkType.TechRoadmap, percent: mix.techRoadmap },
  { workType: WorkType.Maintenance, percent: mix.maintenance }
]

export const isLane = (initiative: Pick<Initiative, 'kind'>) => initiative.kind === InitiativeKind.Bucket

export const leadOf = (initiative: Initiative) => initiative.members.find(m => m.role === 1)

/** Runs a change against the API, reports a failure, and reloads the board. */
export type RunChange = (action: () => Promise<unknown>, success?: string) => Promise<void>

const dateOnly = (value?: string | null) => (value ? value.slice(0, 10) : '')

/** An initiative's details as the update call expects them, with some replaced. */
export function toUpdateDto(initiative: Initiative, changes: Partial<UpdateInitiativeDto> = {}): UpdateInitiativeDto {
  return {
    name: initiative.name,
    description: initiative.description,
    color: initiative.color,
    tshirtSize: initiative.tshirtSize,
    url: initiative.url,
    workType: initiative.workType,
    kind: initiative.kind,
    arrivedOn: dateOnly(initiative.arrivedOn) || null,
    dueDate: dateOnly(initiative.dueDate) || null,
    readiness: initiative.readiness,
    earliestSprintId: initiative.earliestSprintId ?? null,
    ...changes
  }
}

/** The sizes to choose from: those mapped in Settings, and any size already in use. */
export function sizeOptions(board: PlanningBoard) {
  const mapped = board.sizeMappings.map(m => m.size.toUpperCase())
  const used = board.initiatives.filter(i => !isLane(i) && i.tshirtSize).map(i => i.tshirtSize as string)
  return Array.from(new Set([...mapped, ...used]))
}

/** What the allocation grid paints with: an initiative, the eraser, or nothing (select). */
export type Brush = string | 'erase' | null

/** The shares a cell should have after painting it: the brush takes the whole cell. */
export function paintCell(current: CellShare[], brush: Brush): CellShare[] {
  if (brush === null) return current
  if (brush === 'erase') return []
  return [{ initiativeId: brush, percent: 100 }]
}

/** The statuses an option can be created in, before any decision. */
export const STARTING_STATUSES = [InitiativeStatus.InDiscovery, InitiativeStatus.InSolutionDesign, InitiativeStatus.ReadyForCommitment]

/** An option is sized once it is ready for commitment; before that it has no size. */
export const canBeSized = (status: number) =>
  status !== InitiativeStatus.InDiscovery && status !== InitiativeStatus.InSolutionDesign

/** A size as the pickers show it, with its dev-sprints. */
export function describeSize(board: PlanningBoard, size: string) {
  const mapping = board.sizeMappings.find(m => m.size.toUpperCase() === size.toUpperCase())
  return mapping ? `${size} · ${formatEffort(mapping.sprints)} dev-sprints` : `${size} · no mapping`
}

