import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { act, renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { server } from '../test/msw/server'
import { createTestQueryClient } from '../test/renderWithProviders'
import { disputeKeys, useAdvanceStatusMutation, useDisputesQuery } from './useDisputes'

function harness() {
  const queryClient = createTestQueryClient()

  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }

  return { queryClient, Wrapper }
}

describe('useDisputesQuery', () => {
  it('loads the list and exposes it as data', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([{ id: 'a' }, { id: 'b' }])))
    const { Wrapper } = harness()

    const { result } = renderHook(() => useDisputesQuery('All'), { wrapper: Wrapper })

    await waitFor(() => expect(result.current.data).toHaveLength(2))
  })

  it('keys the cache separately per status filter', () => {
    expect(disputeKeys.list('All')).not.toEqual(disputeKeys.list('Initiated'))
  })
})

describe('useAdvanceStatusMutation', () => {
  it('writes the updated dispute into the detail cache', async () => {
    server.use(
      http.patch('*/api/disputes/abc/status', () =>
        HttpResponse.json({ id: 'abc', status: 'Resolved' }),
      ),
    )
    const { queryClient, Wrapper } = harness()

    const { result } = renderHook(() => useAdvanceStatusMutation('abc'), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate('Resolved')
    })

    await waitFor(() =>
      expect(queryClient.getQueryData(disputeKeys.detail('abc'))).toMatchObject({
        status: 'Resolved',
      }),
    )
  })
})
