import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { server } from '../test/msw/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { AuthContext } from '../auth/context'
import type { Role } from '../api/auth'
import { DisputesListPage } from './DisputesListPage'

const takealot = {
  id: '0199c0de-0001-7000-8000-000000000001',
  transactionId: '0199c0de-1001-7000-8000-000000000001',
  transactionAmount: 1499.99,
  transactionCurrency: 'ZAR',
  transactionDate: '2026-10-12T09:14:00+00:00',
  merchantName: 'Takealot',
  reason: 'Charged twice for a single order.',
  status: 'Initiated',
  createdAt: '2026-10-13T07:30:00+00:00',
  resolvedAt: null,
}

function withRole(role: Role, children: ReactNode) {
  return (
    <AuthContext.Provider
      value={{
        session: {
          accessToken: 'token',
          email: `${role}@capitec.test`,
          role,
          expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
        },
        signIn: async () => {},
        signOut: () => {},
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

describe('DisputesListPage', () => {
  it('renders a row per dispute with the merchant, amount, and status', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    const row = await screen.findByRole('row', { name: /Takealot/ })
    expect(within(row).getByText(/1\s?499[,.]99/)).toBeInTheDocument()
    expect(within(row).getByText('Initiated')).toBeInTheDocument()
  })

  it('shows an empty state when there are no disputes', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([])))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    expect(await screen.findByText(/no disputes/i)).toBeInTheDocument()
  })

  it('refetches with a status query param when the filter changes', async () => {
    const requested: string[] = []
    server.use(
      http.get('*/api/disputes', ({ request }) => {
        requested.push(new URL(request.url).searchParams.get('status') ?? 'none')
        return HttpResponse.json([])
      }),
    )
    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    await screen.findByText(/no disputes/i)
    await userEvent.selectOptions(screen.getByLabelText(/status/i), 'Resolved')

    await waitFor(() => expect(requested).toContain('Resolved'))
  })

  it('hides the raise action from a ReadOnly user', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    await screen.findByRole('row', { name: /Takealot/ })
    expect(screen.queryByRole('link', { name: /raise a dispute/i })).not.toBeInTheDocument()
  })

  it('shows the raise action to a DisputeAdmin', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('DisputeAdmin', <DisputesListPage />))

    expect(await screen.findByRole('link', { name: /raise a dispute/i })).toBeInTheDocument()
  })

  it('shows an error when the list request fails', async () => {
    server.use(http.get('*/api/disputes', () => new HttpResponse(null, { status: 500 })))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })
})
