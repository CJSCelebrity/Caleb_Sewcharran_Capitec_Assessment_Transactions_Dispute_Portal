import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import { ApiError } from '../api/client'
import { ErrorBanner } from '../components/ErrorBanner'
import { Field } from '../components/Field'
import { createDisputeSchema } from '../domain/createDisputeSchema'
import type { CreateDisputeForm, CreateDisputePayload } from '../domain/createDisputeSchema'
import { useCreateDisputeMutation } from '../hooks/useDisputes'

export function NewDisputePage() {
  const navigate = useNavigate()
  const createDispute = useCreateDisputeMutation()
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateDisputeForm, unknown, CreateDisputePayload>({
    resolver: zodResolver(createDisputeSchema),
    defaultValues: { transactionCurrency: 'ZAR' },
  })

  const onSubmit = async (payload: CreateDisputePayload) => {
    setSubmitError(null)

    try {
      const created = await createDispute.mutateAsync(payload)
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

      matched.forEach(([field, message]) =>
        setError(field as keyof CreateDisputeForm, { message }),
      )

      if (matched.length === 0) {
        setSubmitError(error.message)
      }
    }
  }

  return (
    <section className="card page-form">
      <h1>Raise a dispute</h1>
      <p className="note">
        The disputed transaction is owned by another service, so enter its details by hand. The
        dispute starts as Initiated.
      </p>

      {submitError ? <ErrorBanner title={submitError} /> : null}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Field id="transactionId" label="Transaction id" error={errors.transactionId?.message}>
          <input
            id="transactionId"
            className="mono"
            placeholder="0199c0de-1001-7000-8000-000000000001"
            {...register('transactionId')}
          />
        </Field>

        <Field id="transactionAmount" label="Amount" error={errors.transactionAmount?.message}>
          <input
            id="transactionAmount"
            type="number"
            step="0.01"
            min="0.01"
            {...register('transactionAmount', { valueAsNumber: true })}
          />
        </Field>

        <Field
          id="transactionCurrency"
          label="Currency (3 letters)"
          error={errors.transactionCurrency?.message}
        >
          <input id="transactionCurrency" maxLength={3} {...register('transactionCurrency')} />
        </Field>

        <Field id="transactionDate" label="Transaction date" error={errors.transactionDate?.message}>
          <input id="transactionDate" type="datetime-local" {...register('transactionDate')} />
        </Field>

        <Field id="merchantName" label="Merchant name" error={errors.merchantName?.message}>
          <input id="merchantName" maxLength={200} {...register('merchantName')} />
        </Field>

        <Field id="reason" label="Reason" error={errors.reason?.message}>
          <textarea id="reason" rows={4} maxLength={500} {...register('reason')} />
        </Field>

        <div className="form-actions">
          <button type="submit" disabled={isSubmitting}>
            <FontAwesomeIcon icon={faPlus} />
            {isSubmitting ? 'Raising...' : 'Raise dispute'}
          </button>
          <Link to="/disputes" className="button-ghost">
            Cancel
          </Link>
        </div>
      </form>
    </section>
  )
}
