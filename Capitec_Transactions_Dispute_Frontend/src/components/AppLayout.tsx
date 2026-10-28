import { Link, Outlet } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowRightFromBracket, faReceipt, faScaleBalanced } from '@fortawesome/free-solid-svg-icons'
import { useAuth } from '../auth/useAuth'
import { ThemeToggle } from './ThemeToggle'

export function AppLayout() {
  const { session, signOut } = useAuth()
  const isCustomer = session?.role === 'Customer'

  return (
    <>
      <header className="app-header">
        <div className="app-header__inner">
          <Link to={isCustomer ? '/transactions' : '/disputes'} className="brand">
            <FontAwesomeIcon icon={faScaleBalanced} />
            <strong>Disputes Portal</strong>
          </Link>
          <nav>
            {session ? (
              <>
                {isCustomer ? (
                  <Link to="/transactions" className="nav-link">
                    <FontAwesomeIcon icon={faReceipt} />
                    Transactions
                  </Link>
                ) : null}
                <Link to="/disputes" className="nav-link">
                  <FontAwesomeIcon icon={faScaleBalanced} />
                  {isCustomer ? 'My disputes' : 'Disputes'}
                </Link>
                <span className="mono">
                  {session.email} ({session.role})
                </span>
                <button type="button" className="secondary" onClick={signOut}>
                  <FontAwesomeIcon icon={faArrowRightFromBracket} />
                  Sign out
                </button>
              </>
            ) : null}
            <ThemeToggle />
          </nav>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
    </>
  )
}
