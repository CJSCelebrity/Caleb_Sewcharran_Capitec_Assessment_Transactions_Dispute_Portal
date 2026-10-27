import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faRightToBracket } from '@fortawesome/free-solid-svg-icons'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { ErrorBanner } from '../components/ErrorBanner'
import { Field } from '../components/Field'

const signInSchema = z.object({
  email: z
    .string()
    .min(1, 'Enter your email address.')
    .refine((value) => /.+@.+\..+/.test(value), 'Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
})

type SignInForm = z.infer<typeof signInSchema>

export function SignInPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SignInForm>({ resolver: zodResolver(signInSchema) })

  const onSubmit = async (values: SignInForm) => {
    setSubmitError(null)

    try {
      await signIn(values.email, values.password)
      const from = (location.state as { from?: string } | null)?.from
      navigate(from ?? '/disputes', { replace: true })
    } catch (error) {
      setSubmitError(
        error instanceof ApiError
          ? error.message
          : 'Could not reach the server. Check that the API is running.',
      )
    }
  }

  return (
    <section className="card auth-card">
      <h1>Sign in</h1>
      <p className="note">Use a seeded portal account.</p>

      {submitError ? <ErrorBanner title={submitError} /> : null}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <Field id="email" label="Email address" error={errors.email?.message}>
          <input id="email" type="email" autoComplete="username" {...register('email')} />
        </Field>

        <Field id="password" label="Password" error={errors.password?.message}>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            {...register('password')}
          />
        </Field>

        <button type="submit" disabled={isSubmitting}>
          <FontAwesomeIcon icon={faRightToBracket} />
          {isSubmitting ? 'Signing in...' : 'Sign in'}
        </button>
      </form>
    </section>
  )
}
