import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faFlag, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { ApiError } from '../api/client'
import { ErrorBanner } from '../components/ErrorBanner'
import { Field } from '../components/Field'
import { createDisputeSchema } from '../domain/createDisputeSchema'
import type { CreateDisputeForm, CreateDisputeValues } from '../domain/createDisputeSchema'
import { formatAmount, formatDateTime } from '../domain/dispute'
import { useCreateDisputeMutation } from '../hooks/useDisputes'
import { useTransactionsQuery } from '../hooks/useTransactions'

export function RaiseDisputePage() {
  const { transactionId } = useParams()
  const navigate = useNavigate()
  const createDispute = useCreateDisputeMutation()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const transactions = useTransactionsQuery()

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateDisputeForm, unknown, CreateDisputeValues>({
    resolver: zodResolver(createDisputeSchema),
  })

  const transaction = transactions.data?.find((candidate) => candidate.id === transactionId)

  const onSubmit = async (values: CreateDisputeValues) => {
    setSubmitError(null)

    if (!transactionId) {
      return
    }

    try {
      const created = await createDispute.mutateAsync({ transactionId, reason: values.reason })
      navigate(`/disputes/${created.id}`, { replace: true })
    } catch (error) {
      if (!(error instanceof ApiError)) {
        setSubmitError('Could not reach the server. Check that the API is running.')
        return
      }

      if (error.status === 403) {
        setSubmitError('You do not have permission to raise a dispute.')
        return
      }

      const matched = Object.entries(error.fieldErrors).filter(
        ([field]) => field in createDisputeSchema.shape,
      )

      matched.forEach(([field, message]) => setError(field as keyof CreateDisputeForm, { message }))

      if (matched.length === 0) {
        setSubmitError(error.message)
      }
    }
  }

  if (transactions.isPending) {
    return (
      <section>
        <p className="note">
          <FontAwesomeIcon icon={faSpinner} spin />
          Loading the transaction...
        </p>
      </section>
    )
  }

  if (transactions.isError) {
    return (
      <section>
        <ErrorBanner
          title="Could not load the transaction"
          message={transactions.error.message}
        />
        <Link to="/transactions" className="link-back">
          Back to transactions
        </Link>
      </section>
    )
  }

  if (!transaction) {
    return (
      <section>
        <ErrorBanner
          title="Transaction not found"
          message="This transaction is not one of yours, or it no longer exists."
        />
        <Link to="/transactions" className="link-back">
          Back to transactions
        </Link>
      </section>
    )
  }

  if (transaction.disputeId) {
    return (
      <section>
        <h1>Dispute a transaction</h1>
        <ErrorBanner
          title="Already disputed"
          message="You have already raised a dispute for this transaction."
        />
        <Link to={`/disputes/${transaction.disputeId}`} className="button-link">
          View the dispute
        </Link>
      </section>
    )
  }

  return (
    <section>
      <h1>Dispute a transaction</h1>

      <dl className="detail-grid">
        <dt>Reference</dt>
        <dd className="mono">{transaction.reference}</dd>
        <dt>Merchant</dt>
        <dd>{transaction.merchantName}</dd>
        <dt>Amount</dt>
        <dd className="numeric">{formatAmount(transaction.amount, transaction.currency)}</dd>
        <dt>Date</dt>
        <dd>{formatDateTime(transaction.occurredAt)}</dd>
      </dl>

      {submitError ? <ErrorBanner title="Could not raise the dispute" message={submitError} /> : null}

      <form className="page-form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <Field id="reason" label="Why are you disputing this transaction?" error={errors.reason?.message}>
          <textarea id="reason" rows={4} maxLength={500} {...register('reason')} />
        </Field>

        <div className="form-actions">
          <button type="submit" disabled={isSubmitting}>
            <FontAwesomeIcon icon={faFlag} />
            Raise the dispute
          </button>
          <Link to="/transactions" className="button-ghost">
            Cancel
          </Link>
        </div>
      </form>
    </section>
  )
}
