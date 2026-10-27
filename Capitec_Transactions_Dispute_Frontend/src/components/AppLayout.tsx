import { Link, Outlet } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function AppLayout() {
  const { session, signOut } = useAuth()

  return (
    <>
      <header className="app-header">
        <Link to="/disputes">
          <strong>Disputes Portal</strong>
        </Link>
        {session ? (
          <nav>
            <span className="mono">
              {session.email} ({session.role})
            </span>
            <button type="button" className="secondary" onClick={signOut}>
              Sign out
            </button>
          </nav>
        ) : null}
      </header>
      <main>
        <Outlet />
      </main>
    </>
  )
}
