import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSpinner } from '@fortawesome/free-solid-svg-icons'
import { STATUS_CLASS, formatDateTime } from '../domain/dispute'
import type { DisputeStatusChange } from '../domain/disputeHistory'
import { historyEntryLabel } from '../domain/disputeHistory'

export function DisputeTimeline({
  history,
  isPending,
  isError,
}: {
  history: DisputeStatusChange[] | undefined
  isPending: boolean
  isError: boolean
}) {
  if (isPending) {
    return (
      <p className="note">
        <FontAwesomeIcon icon={faSpinner} spin />
        Loading the history...
      </p>
    )
  }

  if (isError || !history) {
    return <p className="note">The history for this dispute could not be loaded.</p>
  }

  return (
    <ol className="timeline">
      {history.map((change) => (
        <li key={`${change.toStatus}-${change.changedAt}`} className="timeline-entry">
          <span className={`timeline-dot ${STATUS_CLASS[change.toStatus]}`} />
          <div className="timeline-body">
            <span className="timeline-label">{historyEntryLabel(change)}</span>
            <span className="timeline-meta">
              {formatDateTime(change.changedAt)} by <span className="mono">{change.changedBy}</span>
            </span>
          </div>
        </li>
      ))}
    </ol>
  )
}
