import type { DisputeStatus } from './dispute'
import { STATUS_LABELS } from './dispute'

export type DisputeStatusChange = {
  fromStatus: DisputeStatus | null
  toStatus: DisputeStatus
  changedBy: string
  changedAt: string
}

export function historyEntryLabel(change: DisputeStatusChange): string {
  return change.fromStatus === null
    ? 'Raised'
    : `Moved to ${STATUS_LABELS[change.toStatus].toLowerCase()}`
}
