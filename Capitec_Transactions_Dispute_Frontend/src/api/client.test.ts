import { beforeEach, describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../test/msw/server'
import { ApiError, apiFetch, setTokenReader, setUnauthorizedHandler } from './client'

beforeEach(() => {
  setTokenReader(() => null)
  setUnauthorizedHandler(() => {})
})

describe('apiFetch', () => {
  it('returns the parsed json body on success', async () => {
    server.use(http.get('*/api/disputes', () => HttpResponse.json([{ id: 'a' }])))

    await expect(apiFetch('/disputes')).resolves.toEqual([{ id: 'a' }])
  })

  it('attaches the bearer token from the registered reader', async () => {
    setTokenReader(() => 'signed-token')
    let header: string | null = null
    server.use(
      http.get('*/api/disputes', ({ request }) => {
        header = request.headers.get('authorization')
        return HttpResponse.json([])
      }),
    )

    await apiFetch('/disputes')

    expect(header).toBe('Bearer signed-token')
  })

  it('omits the authorization header when there is no token', async () => {
    let header: string | null = 'unset'
    server.use(
      http.get('*/api/disputes', ({ request }) => {
        header = request.headers.get('authorization')
        return HttpResponse.json([])
      }),
    )

    await apiFetch('/disputes')

    expect(header).toBeNull()
  })

  it('sends json content type when there is a body', async () => {
    let header: string | null = null
    server.use(
      http.post('*/api/disputes', ({ request }) => {
        header = request.headers.get('content-type')
        return HttpResponse.json({}, { status: 201 })
      }),
    )

    await apiFetch('/disputes', { method: 'POST', body: JSON.stringify({}) })

    expect(header).toContain('application/json')
  })

  it('throws an ApiError carrying the problem detail on 409', async () => {
    server.use(
      http.patch('*/api/disputes/:id/status', () =>
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

    const error = await apiFetch('/disputes/1/status', { method: 'PATCH' }).then(
      () => {
        throw new Error('expected the request to reject')
      },
      (caught: unknown) => caught as ApiError,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(409)
    expect(error.message).toBe('A dispute cannot move from Initiated to Resolved.')
  })

  it('throws an ApiError with a null problem on an empty 404 body', async () => {
    server.use(http.get('*/api/disputes/:id', () => new HttpResponse(null, { status: 404 })))

    const error = await apiFetch('/disputes/1').then(
      () => {
        throw new Error('expected the request to reject')
      },
      (caught: unknown) => caught as ApiError,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(404)
    expect(error.problem).toBeNull()
  })

  it('maps pascal case validation keys to camel case field errors', async () => {
    server.use(
      http.post('*/api/disputes', () =>
        HttpResponse.json(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: {
              MerchantName: ['The field MerchantName is invalid.'],
              TransactionAmount: ['The TransactionAmount field is required.'],
            },
          },
          { status: 400, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    )

    const error = await apiFetch('/disputes', { method: 'POST', body: '{}' }).then(
      () => {
        throw new Error('expected the request to reject')
      },
      (caught: unknown) => caught as ApiError,
    )

    expect(error.fieldErrors).toEqual({
      merchantName: 'The field MerchantName is invalid.',
      transactionAmount: 'The TransactionAmount field is required.',
    })
  })

  it('calls the unauthorized handler on 401', async () => {
    const onUnauthorized = vi.fn()
    setUnauthorizedHandler(onUnauthorized)
    server.use(http.get('*/api/disputes', () => new HttpResponse(null, { status: 401 })))

    await apiFetch('/disputes').catch(() => {})

    expect(onUnauthorized).toHaveBeenCalledOnce()
  })

  it('does not call the unauthorized handler on 403', async () => {
    const onUnauthorized = vi.fn()
    setUnauthorizedHandler(onUnauthorized)
    server.use(http.post('*/api/disputes', () => new HttpResponse(null, { status: 403 })))

    await apiFetch('/disputes', { method: 'POST', body: '{}' }).catch(() => {})

    expect(onUnauthorized).not.toHaveBeenCalled()
  })
})
