import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { Route, Routes } from 'react-router'
import { server } from '../test/msw/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { AuthContext } from '../auth/context'
import type { Role } from '../api/auth'
import type { Dispute, DisputeStatus } from '../domain/dispute'
import { DisputeDetailPage } from './DisputeDetailPage'

const id = '0199c0de-0001-7000-8000-000000000001'

function dispute(status: DisputeStatus): Dispute {
  return {
    id,
    transactionId: '0199c0de-1001-7000-8000-000000000001',
    transactionAmount: 1499.99,
    transactionCurrency: 'ZAR',
    transactionDate: '2026-10-12T09:14:00+00:00',
    merchantName: 'Takealot',
    reason: 'Charged twice for a single order.',
    status,
    createdAt: '2026-10-13T07:30:00+00:00',
    resolvedAt: status === 'Resolved' ? '2026-10-07T14:00:00+00:00' : null,
  }
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

function renderDetail(role: Role) {
  return renderWithProviders(
    withRole(
      role,
      <Routes>
        <Route path="/disputes/:id" element={<DisputeDetailPage />} />
      </Routes>,
    ),
    { route: `/disputes/${id}` },
  )
}

describe('DisputeDetailPage', () => {
  it('renders the dispute fields read only, with no inputs', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))

    renderDetail('DisputeAdmin')

    expect(await screen.findByText('Takealot')).toBeInTheDocument()
    expect(screen.getByText('Charged twice for a single order.')).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('offers only the single legal next status', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))

    renderDetail('DisputeAdmin')

    expect(
      await screen.findByRole('button', { name: /advance to under investigation/i }),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /advance to resolved/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
  })

  it('patches the status and shows the new one', async () => {
    let body: unknown
    server.use(
      http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('UnderInvestigation'))),
      http.patch(`*/api/disputes/${id}/status`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(dispute('Resolved'))
      }),
    )
    renderDetail('DisputeAdmin')

    await userEvent.click(await screen.findByRole('button', { name: /advance to resolved/i }))

    expect(await screen.findByText(/no further status changes/i)).toBeInTheDocument()
    expect(body).toEqual({ status: 'Resolved' })
  })

  it('offers no action once the dispute is resolved', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Resolved'))))

    renderDetail('DisputeAdmin')

    expect(await screen.findByText(/no further status changes/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /advance/i })).not.toBeInTheDocument()
  })

  it('hides the action from a ReadOnly user', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))

    renderDetail('ReadOnly')

    await screen.findByText('Takealot')
    expect(screen.queryByRole('button', { name: /advance/i })).not.toBeInTheDocument()
  })

  it('surfaces the 409 detail verbatim when the transition is rejected', async () => {
    server.use(
      http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))),
      http.patch(`*/api/disputes/${id}/status`, () =>
        HttpResponse.json(
          {
            title: 'Invalid dispute status transition',
            status: 409,
            detail: 'A dispute cannot move from Initiated to Resolved.',
          },
          { status: 409, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    )
    renderDetail('DisputeAdmin')

    await userEvent.click(await screen.findByRole('button', { name: /advance/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'A dispute cannot move from Initiated to Resolved.',
    )
  })

  it('shows a not found message on 404', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => new HttpResponse(null, { status: 404 })))

    renderDetail('DisputeAdmin')

    expect(await screen.findByText(/could not be found/i)).toBeInTheDocument()
  })
})
