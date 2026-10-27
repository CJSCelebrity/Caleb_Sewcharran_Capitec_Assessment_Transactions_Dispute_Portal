import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../test/msw/server'
import { advanceDisputeStatus, createDispute, listDisputes } from './disputes'

describe('listDisputes', () => {
  it('omits the status query param when no filter is given', async () => {
    let url = ''
    server.use(
      http.get('*/api/disputes', ({ request }) => {
        url = request.url
        return HttpResponse.json([])
      }),
    )

    await listDisputes()

    expect(url).not.toContain('status=')
  })

  it('sends the pascal case status name when filtered', async () => {
    let url = ''
    server.use(
      http.get('*/api/disputes', ({ request }) => {
        url = request.url
        return HttpResponse.json([])
      }),
    )

    await listDisputes('UnderInvestigation')

    expect(url).toContain('status=UnderInvestigation')
  })
})

describe('createDispute', () => {
  it('posts the payload and returns the created dispute', async () => {
    let body: unknown
    server.use(
      http.post('*/api/disputes', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ id: 'new-id' }, { status: 201 })
      }),
    )

    const payload = {
      transactionId: '0199c0de-1001-7000-8000-000000000009',
      transactionAmount: 250,
      transactionCurrency: 'ZAR',
      transactionDate: '2026-10-20T10:00:00.000Z',
      merchantName: 'Checkers',
      reason: 'Goods not received.',
    }

    const created = await createDispute(payload)

    expect(body).toEqual(payload)
    expect(created.id).toBe('new-id')
  })
})

describe('advanceDisputeStatus', () => {
  it('patches the status endpoint with a status body', async () => {
    let body: unknown
    server.use(
      http.patch('*/api/disputes/abc/status', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ id: 'abc', status: 'Resolved' })
      }),
    )

    const updated = await advanceDisputeStatus('abc', 'Resolved')

    expect(body).toEqual({ status: 'Resolved' })
    expect(updated.status).toBe('Resolved')
  })
})
