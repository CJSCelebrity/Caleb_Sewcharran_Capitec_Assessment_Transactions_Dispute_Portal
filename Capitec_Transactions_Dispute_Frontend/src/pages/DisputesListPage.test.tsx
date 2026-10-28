import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { server } from '../test/msw/server'
import { withRole } from '../test/withRole'
import { renderWithProviders } from '../test/renderWithProviders'
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

  it('offers a customer the way back to their transactions, which is where raising starts', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('Customer', <DisputesListPage />))

    expect(await screen.findByRole('link', { name: /view transactions/i })).toHaveAttribute(
      'href',
      '/transactions',
    )
  })

  it('offers staff no raise action, because only a customer can raise a dispute', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('DisputeAdmin', <DisputesListPage />))

    await screen.findByRole('row', { name: /Takealot/ })

    expect(screen.queryByRole('link', { name: /raise a dispute/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /view transactions/i })).not.toBeInTheDocument()
  })

  it('heads the list as a personal history for a customer and as the queue for staff', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    const customer = renderWithProviders(withRole('Customer', <DisputesListPage />))
    expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent('My disputes')
    customer.unmount()

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))
    expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent('Disputes')
  })

  it('shows an error when the list request fails', async () => {
    server.use(http.get('*/api/disputes', () => new HttpResponse(null, { status: 500 })))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })

  it('keeps the disputes list as a real table inside the scroll container', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    const table = await screen.findByRole('table')

    expect(within(table).getAllByRole('row')).toHaveLength(2)
    expect(within(table).getAllByRole('columnheader').map((header) => header.textContent)).toEqual([
      'Merchant',
      'Amount',
      'Transaction date',
      'Status',
      'Raised',
      'Resolved',
      'Detail',
    ])
  })

  it('shows when a dispute was resolved, which is what makes the list a history', async () => {
    server.use(
      http.get('*/api/disputes', () =>
        HttpResponse.json([
          { ...takealot, status: 'Resolved', resolvedAt: '2026-10-20T14:00:00+00:00' },
        ]),
      ),
    )

    renderWithProviders(withRole('Customer', <DisputesListPage />))

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getAllByRole('cell')[5].textContent).not.toBe('Not resolved')
  })

  it('marks an unresolved dispute as not resolved', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('Customer', <DisputesListPage />))

    const row = await screen.findByRole('row', { name: /Takealot/ })

    expect(within(row).getAllByRole('cell')[5]).toHaveTextContent('Not resolved')
  })

  it('keeps the status badge text free of icon content', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([takealot])))

    renderWithProviders(withRole('ReadOnly', <DisputesListPage />))

    const row = await screen.findByRole('row', { name: /Takealot/ })
    const badge = within(row).getByText('Initiated')

    expect(badge).toHaveClass('badge', 'badge-initiated')
    expect(badge.textContent).toBe('Initiated')
  })
})
