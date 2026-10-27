import { useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { ErrorBanner } from '../components/ErrorBanner'
import { StatusBadge } from '../components/StatusBadge'
import { DISPUTE_STATUSES, formatAmount, formatDateTime, STATUS_LABELS } from '../domain/dispute'
import type { StatusFilter } from '../hooks/useDisputes'
import { useDisputesQuery } from '../hooks/useDisputes'

export function DisputesListPage() {
  const { session } = useAuth()
  const [filter, setFilter] = useState<StatusFilter>('All')
  const { data, isPending, isError, error } = useDisputesQuery(filter)

  return (
    <section>
      <div className="toolbar">
        <div className="field" style={{ marginBottom: 0 }}>
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

        {session?.role === 'DisputeAdmin' ? <Link to="/disputes/new">Raise a dispute</Link> : null}
      </div>

      {isError ? <ErrorBanner title="Could not load disputes" message={error.message} /> : null}

      {isPending ? <p>Loading disputes...</p> : null}

      {data && data.length === 0 ? <p>No disputes match this filter.</p> : null}

      {data && data.length > 0 ? (
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
                  <Link to={`/disputes/${dispute.id}`}>View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  )
}
