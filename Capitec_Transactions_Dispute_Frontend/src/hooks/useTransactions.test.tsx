import { describe, expect, it } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import type { ReactNode } from 'react'
import { server } from '../test/msw/server'
import { createTestQueryClient } from '../test/renderWithProviders'
import { useTransactionsQuery } from './useTransactions'

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = createTestQueryClient()

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}

const row = {
  id: 'txn-1',
  reference: 'TXN-1001',
  amount: 1499.99,
  currency: 'ZAR',
  occurredAt: '2026-10-02T09:14:00Z',
  merchantName: 'Takealot',
  disputeId: null,
  disputeStatus: null,
}

describe('useTransactionsQuery', () => {
  it('loads the transactions into data', async () => {
    server.use(http.get('*/api/transactions', () => HttpResponse.json([row])))

    const { result } = renderHook(() => useTransactionsQuery(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data).toHaveLength(1)
    expect(result.current.data?.[0].reference).toBe('TXN-1001')
  })

  it('surfaces a failure as an error', async () => {
    server.use(http.get('*/api/transactions', () => new HttpResponse(null, { status: 500 })))

    const { result } = renderHook(() => useTransactionsQuery(), { wrapper })

    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
