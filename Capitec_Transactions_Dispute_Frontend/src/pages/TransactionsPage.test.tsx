import { describe, expect, it } from 'vitest'
import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { server } from '../test/msw/server'
import { withRole } from '../test/withRole'
import { renderWithProviders } from '../test/renderWithProviders'
import { TransactionsPage } from './TransactionsPage'

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

function renderPage() {
  return renderWithProviders(withRole('Customer', <TransactionsPage />))
}

describe('TransactionsPage', () => {
  it('renders a row per transaction with its reference and merchant', async () => {
    serveTransactions([transaction(), transaction({ id: 'txn-2', reference: 'TXN-1002', merchantName: 'Uber Eats' })])
    renderPage()

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getByText('TXN-1001')).toBeInTheDocument()
    expect(await screen.findByRole('row', { name: /Uber Eats/ })).toBeInTheDocument()
  })

  it('offers a dispute action for a transaction with no dispute', async () => {
    serveTransactions([transaction()])
    renderPage()

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getByText('Not disputed')).toBeInTheDocument()
    expect(within(row).getByRole('link', { name: /Dispute/ })).toHaveAttribute(
      'href',
      '/transactions/txn-1/dispute',
    )
  })

  it('links to the existing dispute of a disputed transaction', async () => {
    serveTransactions([transaction({ disputeId: 'dispute-1', disputeStatus: 'UnderInvestigation' })])
    renderPage()

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getByRole('link', { name: /View dispute/ })).toHaveAttribute(
      'href',
      '/disputes/dispute-1',
    )
    expect(within(row).queryByText('Not disputed')).not.toBeInTheDocument()
  })

  it('shows the status badge of a disputed transaction', async () => {
    serveTransactions([transaction({ disputeId: 'dispute-1', disputeStatus: 'UnderInvestigation' })])
    renderPage()

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getByText('Under investigation')).toBeInTheDocument()
  })

  it('shows an empty state when there are no transactions', async () => {
    serveTransactions([])
    renderPage()

    expect(await screen.findByText('You have no transactions yet.')).toBeInTheDocument()
  })

  it('shows an error banner when the request fails', async () => {
    server.use(http.get('*/api/transactions', () => new HttpResponse(null, { status: 500 })))
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load transactions')
  })

  it('keeps the transactions list a real table inside the scroll container', async () => {
    serveTransactions([transaction()])
    renderPage()

    const region = await screen.findByRole('region', { name: 'Transactions table' })
    const table = within(region).getByRole('table')

    expect(within(table).getAllByRole('columnheader').map((header) => header.textContent)).toEqual([
      'Reference',
      'Merchant',
      'Amount',
      'Date',
      'Dispute',
      'Action',
    ])
  })

  it('keeps the formatted amount in a single text node', async () => {
    serveTransactions([transaction()])
    renderPage()

    const row = await screen.findByRole('row', { name: /Takealot/ })
    const amountCell = within(row).getAllByRole('cell')[2]

    expect(amountCell.childNodes).toHaveLength(1)
    expect(amountCell.textContent).toMatch(/1\s?499[.,]99/)
  })
})
