import { useState } from 'react'
import { Link } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronRight, faInbox, faReceipt, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { useAuth } from '../auth/useAuth'
import { ErrorBanner } from '../components/ErrorBanner'
import { StatusBadge } from '../components/StatusBadge'
import { DISPUTE_STATUSES, formatAmount, formatDateTime, STATUS_LABELS } from '../domain/dispute'
import type { StatusFilter } from '../hooks/useDisputes'
import { useDisputesQuery } from '../hooks/useDisputes'

export function DisputesListPage() {
  const { session } = useAuth()
  const isCustomer = session?.role === 'Customer'
  const [filter, setFilter] = useState<StatusFilter>('All')
  const { data, isPending, isError, error } = useDisputesQuery(filter)

  return (
    <section>
      <h1>{isCustomer ? 'My disputes' : 'Disputes'}</h1>

      <div className="toolbar">
        <div className="field">
          <label htmlFor="status-filter">Status</label>
          <select
            id="status-filter"
            value={filter}
            onChange={(event) => setFilter(event.target.value as StatusFilter)}
          >
            <option value="All">All statuses</option>
            {DISPUTE_STATUSES.map((status) => (
              <option key={status} value={status}>
                {STATUS_LABELS[status]}
              </option>
            ))}
          </select>
        </div>

        {isCustomer ? (
          <Link to="/transactions" className="button-link">
            <FontAwesomeIcon icon={faReceipt} />
            View transactions
          </Link>
        ) : null}
      </div>

      {isError ? <ErrorBanner title="Could not load disputes" message={error.message} /> : null}

      {isPending ? (
        <p className="note">
          <FontAwesomeIcon icon={faSpinner} spin />
          Loading disputes...
        </p>
      ) : null}

      {data && data.length === 0 ? (
        <p className="note">
          <FontAwesomeIcon icon={faInbox} />
          No disputes match this filter.
        </p>
      ) : null}

      {data && data.length > 0 ? (
        <div className="table-scroll" role="region" aria-label={isCustomer ? 'My disputes table' : 'Disputes table'} tabIndex={0}>
          <table>
            <thead>
              <tr>
                <th scope="col">Merchant</th>
                <th scope="col" className="numeric">
                  Amount
                </th>
                <th scope="col">Transaction date</th>
                <th scope="col">Status</th>
                <th scope="col">Raised</th>
                <th scope="col">Resolved</th>
                <th scope="col">Detail</th>
              </tr>
            </thead>
            <tbody>
              {data.map((dispute) => (
                <tr key={dispute.id}>
                  <td>{dispute.merchantName}</td>
                  <td className="numeric">
                    {formatAmount(dispute.transactionAmount, dispute.transactionCurrency)}
                  </td>
                  <td>{formatDateTime(dispute.transactionDate)}</td>
                  <td>
                    <StatusBadge status={dispute.status} />
                  </td>
                  <td>{formatDateTime(dispute.createdAt)}</td>
                  <td>
                    {dispute.resolvedAt ? (
                      formatDateTime(dispute.resolvedAt)
                    ) : (
                      <span className="text-muted">Not resolved</span>
                    )}
                  </td>
                  <td>
                    <Link to={`/disputes/${dispute.id}`} className="cell-action">
                      View
                      <FontAwesomeIcon icon={faChevronRight} />
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  )
}
