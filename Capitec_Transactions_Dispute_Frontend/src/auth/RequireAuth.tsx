import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import type { Role } from '../api/auth'
import { ErrorBanner } from '../components/ErrorBanner'
import { useAuth } from './useAuth'

export function RequireAuth({ role, children }: { role?: Role; children: ReactNode }) {
  const { session } = useAuth()
  const location = useLocation()

  if (!session) {
    return <Navigate to="/signin" state={{ from: location.pathname }} replace />
  }

  if (role && session.role !== role) {
    return (
      <ErrorBanner
        title="Not permitted"
        message={`This page requires the ${role} role. You are signed in as ${session.role}.`}
      />
    )
  }

  return children
}
