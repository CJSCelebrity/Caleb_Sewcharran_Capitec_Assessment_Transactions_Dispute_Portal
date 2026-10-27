import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleCheck, faCircleDot, faMagnifyingGlass } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import type { DisputeStatus } from '../domain/dispute'
import { STATUS_CLASS, STATUS_LABELS } from '../domain/dispute'

const STATUS_ICON: Record<DisputeStatus, IconDefinition> = {
  Initiated: faCircleDot,
  UnderInvestigation: faMagnifyingGlass,
  Resolved: faCircleCheck,
}

export function StatusBadge({ status }: { status: DisputeStatus }) {
  return (
    <span className={`badge ${STATUS_CLASS[status]}`}>
      <FontAwesomeIcon icon={STATUS_ICON[status]} />
      {STATUS_LABELS[status]}
    </span>
  )
}
