import { Link, useParams } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowLeft, faArrowRight, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { ErrorBanner } from '../components/ErrorBanner'
import { StatusBadge } from '../components/StatusBadge'
import { formatAmount, formatDateTime, nextStatus, STATUS_LABELS } from '../domain/dispute'
import { DisputeTimeline } from '../components/DisputeTimeline'
import { useAdvanceStatusMutation, useDisputeHistoryQuery, useDisputeQuery } from '../hooks/useDisputes'

export function DisputeDetailPage() {
  const { id = '' } = useParams()
  const { session } = useAuth()
  const { data: dispute, isPending, isError, error } = useDisputeQuery(id)
  const history = useDisputeHistoryQuery(id)
  const advance = useAdvanceStatusMutation(id)

  if (isPending) {
    return (
      <p className="note">
        <FontAwesomeIcon icon={faSpinner} spin />
        Loading dispute...
      </p>
    )
  }

  if (isError) {
    return error instanceof ApiError && error.status === 404 ? (
      <ErrorBanner title="That dispute could not be found." />
    ) : (
      <ErrorBanner title="Could not load the dispute" message={error.message} />
    )
  }

  const next = nextStatus(dispute.status)
  const canAdvance = session?.role === 'DisputeAdmin' && next !== null

  return (
    <section className="card">
      <div className="toolbar">
        <div className="page-heading">
          <h1>{dispute.merchantName}</h1>
          <StatusBadge status={dispute.status} />
        </div>
        <Link to="/disputes" className="link-back">
          <FontAwesomeIcon icon={faArrowLeft} />
          Back to disputes
        </Link>
      </div>

      {advance.isError ? (
        <ErrorBanner
          title={
            advance.error instanceof ApiError && advance.error.status === 403
              ? 'You do not have permission to change this status.'
              : 'Could not change the status'
          }
          message={advance.error instanceof ApiError ? advance.error.message : undefined}
        />
      ) : null}

      <dl className="detail-grid">
        <dt>Amount</dt>
        <dd>{formatAmount(dispute.transactionAmount, dispute.transactionCurrency)}</dd>

        <dt>Transaction date</dt>
        <dd>{formatDateTime(dispute.transactionDate)}</dd>

        <dt>Transaction id</dt>
        <dd className="mono">{dispute.transactionId}</dd>

        <dt>Reason</dt>
        <dd>{dispute.reason}</dd>

        <dt>Raised</dt>
        <dd>{formatDateTime(dispute.createdAt)}</dd>

        <dt>Resolved</dt>
        <dd>{dispute.resolvedAt ? formatDateTime(dispute.resolvedAt) : 'Not yet resolved'}</dd>
      </dl>

      <h2>History</h2>
      <DisputeTimeline
        history={history.data}
        isPending={history.isPending}
        isError={history.isError}
      />

      {canAdvance && next ? (
        <button type="button" disabled={advance.isPending} onClick={() => advance.mutate(next)}>
          <FontAwesomeIcon
            icon={advance.isPending ? faSpinner : faArrowRight}
            spin={advance.isPending}
          />
          {advance.isPending ? 'Updating...' : `Advance to ${STATUS_LABELS[next]}`}
        </button>
      ) : null}

      {next === null ? (
        <p className="note">This dispute is resolved. No further status changes are possible.</p>
      ) : null}

      {next !== null && session?.role !== 'DisputeAdmin' ? (
        <p className="note">Only a DisputeAdmin can advance a dispute.</p>
      ) : null}
    </section>
  )
}
