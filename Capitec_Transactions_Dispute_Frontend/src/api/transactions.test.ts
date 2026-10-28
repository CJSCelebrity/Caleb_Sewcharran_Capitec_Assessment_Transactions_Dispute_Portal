import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../test/msw/server'
import { listTransactions } from './transactions'

describe('listTransactions', () => {
  it('reads the transactions endpoint and returns the parsed rows', async () => {
    let url = ''
    server.use(
      http.get('*/api/transactions', ({ request }) => {
        url = request.url
        return HttpResponse.json([
          {
            id: 'txn-1',
            reference: 'TXN-1001',
            amount: 1499.99,
            currency: 'ZAR',
            occurredAt: '2026-10-02T09:14:00Z',
            merchantName: 'Takealot',
            disputeId: null,
            disputeStatus: null,
          },
        ])
      }),
    )

    const transactions = await listTransactions()

    expect(url).toContain('/api/transactions')
    expect(transactions).toHaveLength(1)
    expect(transactions[0].reference).toBe('TXN-1001')
    expect(transactions[0].disputeId).toBeNull()
  })

  it('carries the dispute state of a disputed transaction', async () => {
    server.use(
      http.get('*/api/transactions', () =>
        HttpResponse.json([
          {
            id: 'txn-1',
            reference: 'TXN-1001',
            amount: 10,
            currency: 'ZAR',
            occurredAt: '2026-10-02T09:14:00Z',
            merchantName: 'Takealot',
            disputeId: 'dispute-1',
            disputeStatus: 'UnderInvestigation',
          },
        ]),
      ),
    )

    const transactions = await listTransactions()

    expect(transactions[0].disputeId).toBe('dispute-1')
    expect(transactions[0].disputeStatus).toBe('UnderInvestigation')
  })
})
