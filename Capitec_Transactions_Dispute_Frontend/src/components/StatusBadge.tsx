import type { DisputeStatus } from '../domain/dispute'
import { STATUS_CLASS, STATUS_LABELS } from '../domain/dispute'

export function StatusBadge({ status }: { status: DisputeStatus }) {
  return <span className={`badge ${STATUS_CLASS[status]}`}>{STATUS_LABELS[status]}</span>
}
