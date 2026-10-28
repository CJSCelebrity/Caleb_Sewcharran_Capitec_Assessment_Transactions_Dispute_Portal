import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { login } from '../api/auth'
import { setTokenReader, setUnauthorizedHandler } from '../api/client'
import { AuthContext } from './context'
import type { Session } from './session'
import { clearStoredSession, readStoredSession, writeStoredSession } from './session'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(readStoredSession)

  const signOut = useCallback(() => {
    clearStoredSession()
    setSession(null)
  }, [])

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await login(email, password)

    const next: Session = {
      accessToken: response.accessToken,
      email: response.email,
      role: response.role,
      customerId: response.customerId,
      expiresAtUtc: response.expiresAtUtc,
    }

    writeStoredSession(next)
    setSession(next)
  }, [])

  useEffect(() => {
    setTokenReader(() => readStoredSession()?.accessToken ?? null)
    setUnauthorizedHandler(signOut)
  }, [signOut])

  const value = useMemo(() => ({ session, signIn, signOut }), [session, signIn, signOut])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
