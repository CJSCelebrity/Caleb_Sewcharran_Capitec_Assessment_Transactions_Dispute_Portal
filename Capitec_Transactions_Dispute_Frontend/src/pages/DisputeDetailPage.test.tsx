import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router'
import { server } from '../test/msw/server'
import type { Role } from '../api/auth'
import { withRole } from '../test/withRole'
import { renderWithProviders } from '../test/renderWithProviders'
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

const raisedEntry = {
  fromStatus: null,
  toStatus: 'Initiated',
  changedBy: 'thandi@example.test',
  changedAt: '2026-10-13T07:30:00+00:00',
}

const investigatingEntry = {
  fromStatus: 'Initiated',
  toStatus: 'UnderInvestigation',
  changedBy: 'admin@capitec.test',
  changedAt: '2026-10-14T09:00:00+00:00',
}

function serveHistory(entries: unknown[] = [raisedEntry]) {
  server.use(http.get(`*/api/disputes/${id}/history`, () => HttpResponse.json(entries)))
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
    serveHistory()

    renderDetail('DisputeAdmin')

    expect(await screen.findByText('Takealot')).toBeInTheDocument()
    expect(screen.getByText('Charged twice for a single order.')).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('offers only the single legal next status', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))
    serveHistory()

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
    serveHistory([raisedEntry, investigatingEntry])

    renderDetail('DisputeAdmin')

    expect(await screen.findByText(/no further status changes/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /advance/i })).not.toBeInTheDocument()
  })

  it('hides the action from a ReadOnly user', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))
    serveHistory()

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

  it('renders the status timeline for the dispute', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('UnderInvestigation'))))
    serveHistory([raisedEntry, investigatingEntry])

    renderDetail('Customer')

    const items = await screen.findAllByRole('listitem')

    expect(items).toHaveLength(2)
    expect(items[0]).toHaveTextContent('Raised')
    expect(items[0]).toHaveTextContent('thandi@example.test')
    expect(items[1]).toHaveTextContent('Moved to under investigation')
    expect(items[1]).toHaveTextContent('admin@capitec.test')
  })

  it('still renders the dispute when the history cannot be loaded', async () => {
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))
    server.use(
      http.get(`*/api/disputes/${id}/history`, () => new HttpResponse(null, { status: 500 })),
    )

    renderDetail('Customer')

    expect(await screen.findByText('Takealot')).toBeInTheDocument()
    expect(
      await screen.findByText('The history for this dispute could not be loaded.'),
    ).toBeInTheDocument()
  })

  it('grows the timeline after an admin advances the dispute', async () => {
    let historyCalls = 0
    server.use(http.get(`*/api/disputes/${id}`, () => HttpResponse.json(dispute('Initiated'))))
    server.use(
      http.get(`*/api/disputes/${id}/history`, () => {
        historyCalls += 1
        return HttpResponse.json(historyCalls === 1 ? [raisedEntry] : [raisedEntry, investigatingEntry])
      }),
    )
    server.use(
      http.patch(`*/api/disputes/${id}/status`, () =>
        HttpResponse.json(dispute('UnderInvestigation')),
      ),
    )

    renderDetail('DisputeAdmin')

    expect(await screen.findAllByRole('listitem')).toHaveLength(1)

    await userEvent.click(
      await screen.findByRole('button', { name: /advance to under investigation/i }),
    )

    expect(await screen.findByText('Moved to under investigation')).toBeInTheDocument()
  })
})
