import { beforeEach, describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { server } from '../test/msw/server'
import { AuthProvider } from './AuthProvider'
import { SESSION_STORAGE_KEY } from './session'
import { useAuth } from './useAuth'

function Wrapper({ children }: { children: ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

const future = () => new Date(Date.now() + 3_600_000).toISOString()
const past = () => new Date(Date.now() - 3_600_000).toISOString()

beforeEach(() => {
  localStorage.clear()
})

describe('AuthProvider', () => {
  it('starts with no session', () => {
    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    expect(result.current.session).toBeNull()
  })

  it('stores the session returned by a successful sign in', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'signed-token',
          expiresAtUtc: future(),
          email: 'admin@capitec.test',
          role: 'DisputeAdmin',
          customerId: null,
        }),
      ),
    )
    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.signIn('admin@capitec.test', 'DisputeAdmin#2026')
    })

    expect(result.current.session).toMatchObject({
      email: 'admin@capitec.test',
      role: 'DisputeAdmin',
      accessToken: 'signed-token',
      customerId: null,
    })
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toContain('signed-token')
  })

  it('rethrows so the sign in form can show the error, and keeps no session', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json(
          { title: 'Invalid email or password', status: 401 },
          { status: 401, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    )
    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    await expect(
      act(async () => {
        await result.current.signIn('admin@capitec.test', 'wrong')
      }),
    ).rejects.toThrow('Invalid email or password')

    expect(result.current.session).toBeNull()
  })

  it('restores a stored session that has not expired', () => {
    localStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'stored',
        expiresAtUtc: future(),
        email: 'readonly@capitec.test',
        role: 'ReadOnly',
      }),
    )

    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    expect(result.current.session?.role).toBe('ReadOnly')
  })

  it('discards a stored session that has expired', () => {
    localStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'stale',
        expiresAtUtc: past(),
        email: 'readonly@capitec.test',
        role: 'ReadOnly',
      }),
    )

    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    expect(result.current.session).toBeNull()
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('discards unparseable stored json instead of crashing', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, 'not-json')

    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    expect(result.current.session).toBeNull()
  })

  it('clears the session on sign out', async () => {
    localStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'stored',
        expiresAtUtc: future(),
        email: 'readonly@capitec.test',
        role: 'ReadOnly',
      }),
    )
    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    act(() => result.current.signOut())

    await waitFor(() => expect(result.current.session).toBeNull())
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('stores the customer id of a customer sign in', async () => {
    server.use(
      http.post('*/api/auth/login', () =>
        HttpResponse.json({
          accessToken: 'signed-token',
          expiresAtUtc: future(),
          email: 'thandi@example.test',
          role: 'Customer',
          customerId: '0199c0de-0003-7000-8000-000000000001',
        }),
      ),
    )
    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.signIn('thandi@example.test', 'Thandi#2026')
    })

    expect(result.current.session).toMatchObject({
      role: 'Customer',
      customerId: '0199c0de-0003-7000-8000-000000000001',
    })
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toContain(
      '0199c0de-0003-7000-8000-000000000001',
    )
  })

  it('restores the customer id from a stored session', () => {
    localStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({
        accessToken: 'stored',
        expiresAtUtc: future(),
        email: 'thandi@example.test',
        role: 'Customer',
        customerId: '0199c0de-0003-7000-8000-000000000001',
      }),
    )

    const { result } = renderHook(() => useAuth(), { wrapper: Wrapper })

    expect(result.current.session?.customerId).toBe('0199c0de-0003-7000-8000-000000000001')
  })
})
