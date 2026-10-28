import { Navigate } from 'react-router'
import { useAuth } from './useAuth'

export function HomeRedirect() {
  const { session } = useAuth()

  return <Navigate to={session?.role === 'Customer' ? '/transactions' : '/disputes'} replace />
}
