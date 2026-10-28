import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { Route, Routes } from 'react-router'
import { server } from '../test/msw/server'
import { withRole } from '../test/withRole'
import { renderWithProviders } from '../test/renderWithProviders'
import { RaiseDisputePage } from './RaiseDisputePage'

function transaction(overrides: Record<string, unknown> = {}) {
  return {
    id: 'txn-1',
    reference: 'TXN-1001',
    amount: 1499.99,
    currency: 'ZAR',
    occurredAt: '2026-10-02T09:14:00+00:00',
    merchantName: 'Takealot',
    disputeId: null,
    disputeStatus: null,
    ...overrides,
  }
}

function serveTransactions(rows: unknown[]) {
  server.use(http.get('*/api/transactions', () => HttpResponse.json(rows)))
}

function renderPage(route = '/transactions/txn-1/dispute') {
  return renderWithProviders(
    withRole(
      'Customer',
      <Routes>
        <Route path="/transactions/:transactionId/dispute" element={<RaiseDisputePage />} />
      </Routes>,
    ),
    { route },
  )
}

describe('RaiseDisputePage', () => {
  it('renders the transaction read only, with the reason as the only control', async () => {
    serveTransactions([transaction()])
    renderPage()

    expect(await screen.findByText('TXN-1001')).toBeInTheDocument()
    expect(screen.getByText('Takealot')).toBeInTheDocument()
    expect(screen.getByText(/1\s?499[.,]99/)).toBeInTheDocument()

    expect(screen.queryByRole('textbox', { name: /merchant/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('spinbutton')).not.toBeInTheDocument()
    expect(screen.getAllByRole('textbox')).toHaveLength(1)
  })

  it('posts only the transaction id and the reason', async () => {
    let body: Record<string, unknown> = {}
    serveTransactions([transaction()])
    server.use(
      http.post('*/api/disputes', async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 'dispute-1' }, { status: 201 })
      }),
    )
    renderPage()

    await screen.findByText('TXN-1001')
    await userEvent.type(screen.getByRole('textbox'), 'I did not make this purchase.')
    await userEvent.click(screen.getByRole('button', { name: /Raise the dispute/i }))

    await waitFor(() => expect(Object.keys(body)).toHaveLength(2))
    expect(body).toEqual({ transactionId: 'txn-1', reason: 'I did not make this purchase.' })
  })

  it('rejects an empty reason without sending a request', async () => {
    let posted = false
    serveTransactions([transaction()])
    server.use(
      http.post('*/api/disputes', () => {
        posted = true
        return HttpResponse.json({ id: 'dispute-1' }, { status: 201 })
      }),
    )
    renderPage()

    await screen.findByText('TXN-1001')
    await userEvent.click(screen.getByRole('button', { name: /Raise the dispute/i }))

    expect(await screen.findByText('Enter a reason.')).toBeInTheDocument()
    expect(posted).toBe(false)
  })

  it('maps a server 400 onto the reason field', async () => {
    serveTransactions([transaction()])
    server.use(
      http.post('*/api/disputes', () =>
        HttpResponse.json(
          { title: 'Validation failed', status: 400, errors: { Reason: ['Say more than that.'] } },
          { status: 400 },
        ),
      ),
    )
    renderPage()

    await screen.findByText('TXN-1001')
    await userEvent.type(screen.getByRole('textbox'), 'No')
    await userEvent.click(screen.getByRole('button', { name: /Raise the dispute/i }))

    expect(await screen.findByText('Say more than that.')).toBeInTheDocument()
  })

  it('shows a 409 detail verbatim in the banner', async () => {
    serveTransactions([transaction()])
    server.use(
      http.post('*/api/disputes', () =>
        HttpResponse.json(
          {
            title: 'Transaction already disputed',
            status: 409,
            detail: 'Transaction txn-1 already has dispute dispute-9.',
          },
          { status: 409 },
        ),
      ),
    )
    renderPage()

    await screen.findByText('TXN-1001')
    await userEvent.type(screen.getByRole('textbox'), 'Charged twice.')
    await userEvent.click(screen.getByRole('button', { name: /Raise the dispute/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Transaction txn-1 already has dispute dispute-9.',
    )
  })

  it('explains when the transaction is not one of the callers', async () => {
    serveTransactions([transaction({ id: 'someone-else' })])
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent('Transaction not found')
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('offers the existing dispute instead of the form when already disputed', async () => {
    serveTransactions([transaction({ disputeId: 'dispute-1', disputeStatus: 'Initiated' })])
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent('Already disputed')
    expect(screen.getByRole('link', { name: /View the dispute/i })).toHaveAttribute(
      'href',
      '/disputes/dispute-1',
    )
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('shows an error banner when the transactions cannot be loaded', async () => {
    server.use(http.get('*/api/transactions', () => new HttpResponse(null, { status: 500 })))
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load the transaction')
  })
})
