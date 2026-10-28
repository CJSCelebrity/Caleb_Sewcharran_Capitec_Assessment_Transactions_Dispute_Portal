import type { Role } from '../api/auth'

export const SESSION_STORAGE_KEY = 'capitec-disputes-session'

export type Session = {
  accessToken: string
  email: string
  role: Role
  customerId: string | null
  expiresAtUtc: string
}

export function readStoredSession(): Session | null {
  const raw = localStorage.getItem(SESSION_STORAGE_KEY)

  if (!raw) {
    return null
  }

  try {
    const stored = JSON.parse(raw) as Session

    if (!stored.accessToken || new Date(stored.expiresAtUtc).getTime() <= Date.now()) {
      localStorage.removeItem(SESSION_STORAGE_KEY)
      return null
    }

    return stored
  } catch {
    localStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}

export function writeStoredSession(session: Session) {
  localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

export function clearStoredSession() {
  localStorage.removeItem(SESSION_STORAGE_KEY)
}
