import { createContext } from 'react'
import type { Session } from './session'

export type AuthContextValue = {
  session: Session | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
