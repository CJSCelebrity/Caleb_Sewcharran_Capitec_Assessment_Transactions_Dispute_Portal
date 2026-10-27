import { beforeEach, describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { server } from '../test/msw/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { AuthProvider } from '../auth/AuthProvider'
import { SESSION_STORAGE_KEY } from '../auth/session'
import { SignInPage } from './SignInPage'

function renderPage() {
  return renderWithProviders(
    <AuthProvider>
      <SignInPage />
    </AuthProvider>,
    { route: '/signin' },
  )
}

beforeEach(() => {
  localStorage.clear()
})

describe('SignInPage', () => {
  it('signs in and stores the session', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'signed-token',
          expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
          email: 'admin@capitec.test',
          role: 'DisputeAdmin',
        }),
      ),
    )
    renderPage()

    await userEvent.type(screen.getByLabelText(/email/i), 'admin@capitec.test')
    await userEvent.type(screen.getByLabelText(/password/i), 'DisputeAdmin#2026')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() =>
      expect(localStorage.getItem(SESSION_STORAGE_KEY)).toContain('signed-token'),
    )
  })

  it('shows the server message when the credentials are rejected', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json(
          { title: 'Invalid email or password', status: 401 },
          { status: 401, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    )
    renderPage()

    await userEvent.type(screen.getByLabelText(/email/i), 'admin@capitec.test')
    await userEvent.type(screen.getByLabelText(/password/i), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password')
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('does not call the api when the fields are empty', async () => {
    renderPage()

    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText(/enter your email/i)).toBeInTheDocument()
  })
})
