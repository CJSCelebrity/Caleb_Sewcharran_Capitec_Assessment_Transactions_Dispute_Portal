import { Link } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronRight,
  faFlag,
  faInbox,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import { ErrorBanner } from '../components/ErrorBanner'
import { StatusBadge } from '../components/StatusBadge'
import { formatAmount, formatDateTime } from '../domain/dispute'
import { useTransactionsQuery } from '../hooks/useTransactions'

export function TransactionsPage() {
  const { data, isPending, isError, error } = useTransactionsQuery()

  return (
    <section>
      <h1>Transactions</h1>

      <p className="note">Pick a transaction to dispute it, or open a dispute you already raised.</p>

      {isError ? <ErrorBanner title="Could not load transactions" message={error.message} /> : null}

      {isPending ? (
        <p className="note">
          <FontAwesomeIcon icon={faSpinner} spin />
          Loading transactions...
        </p>
      ) : null}

      {data && data.length === 0 ? (
        <p className="note">
          <FontAwesomeIcon icon={faInbox} />
          You have no transactions yet.
        </p>
      ) : null}

      {data && data.length > 0 ? (
        <div className="table-scroll" role="region" aria-label="Transactions table" tabIndex={0}>
          <table>
            <thead>
              <tr>
                <th scope="col">Reference</th>
                <th scope="col">Merchant</th>
                <th scope="col" className="numeric">
                  Amount
                </th>
                <th scope="col">Date</th>
                <th scope="col">Dispute</th>
                <th scope="col">Action</th>
              </tr>
            </thead>
            <tbody>
              {data.map((transaction) => (
                <tr key={transaction.id}>
                  <td className="mono">{transaction.reference}</td>
                  <td>{transaction.merchantName}</td>
                  <td className="numeric">
                    {formatAmount(transaction.amount, transaction.currency)}
                  </td>
                  <td>{formatDateTime(transaction.occurredAt)}</td>
                  <td>
                    {transaction.disputeStatus ? (
                      <StatusBadge status={transaction.disputeStatus} />
                    ) : (
                      <span className="text-muted">Not disputed</span>
                    )}
                  </td>
                  <td>
                    {transaction.disputeId ? (
                      <Link to={`/disputes/${transaction.disputeId}`} className="cell-action">
                        View dispute
                        <FontAwesomeIcon icon={faChevronRight} />
                      </Link>
                    ) : (
                      <Link
                        to={`/transactions/${transaction.id}/dispute`}
                        className="cell-action"
                      >
                        <FontAwesomeIcon icon={faFlag} />
                        Dispute
                      </Link>
                    )}
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
