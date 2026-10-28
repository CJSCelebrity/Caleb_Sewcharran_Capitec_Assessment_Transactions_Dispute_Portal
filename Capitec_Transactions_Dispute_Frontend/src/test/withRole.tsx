import type { ReactNode } from 'react'
import { vi } from 'vitest'
import type { Role } from '../api/auth'
import { AuthContext } from '../auth/context'
import type { Session } from '../auth/session'

export const TEST_CUSTOMER_ID = '0199c0de-0003-7000-8000-000000000001'

export function sessionFor(role: Role, customerId: string | null = null): Session {
  return {
    accessToken: 'test-token',
    email: `${role.toLowerCase()}@capitec.test`,
    role,
    customerId: role === 'Customer' ? (customerId ?? TEST_CUSTOMER_ID) : null,
    expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
  }
}

export function withRole(role: Role, children: ReactNode, customerId: string | null = null) {
  const value = {
    session: sessionFor(role, customerId),
    signIn: vi.fn(),
    signOut: vi.fn(),
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
