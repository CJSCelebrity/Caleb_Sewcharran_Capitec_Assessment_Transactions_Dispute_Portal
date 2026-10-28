import { createBrowserRouter } from 'react-router'
import { AppLayout } from './components/AppLayout'
import { RequireAuth } from './auth/RequireAuth'
import { HomeRedirect } from './auth/HomeRedirect'
import { SignInPage } from './pages/SignInPage'
import { TransactionsPage } from './pages/TransactionsPage'
import { RaiseDisputePage } from './pages/RaiseDisputePage'
import { DisputesListPage } from './pages/DisputesListPage'
import { DisputeDetailPage } from './pages/DisputeDetailPage'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/signin', element: <SignInPage /> },
      { index: true, element: <HomeRedirect /> },
      {
        path: '/transactions',
        element: (
          <RequireAuth role="Customer">
            <TransactionsPage />
          </RequireAuth>
        ),
      },
      {
        path: '/transactions/:transactionId/dispute',
        element: (
          <RequireAuth role="Customer">
            <RaiseDisputePage />
          </RequireAuth>
        ),
      },
      {
        path: '/disputes',
        element: (
          <RequireAuth>
            <DisputesListPage />
          </RequireAuth>
        ),
      },
      {
        path: '/disputes/:id',
        element: (
          <RequireAuth>
            <DisputeDetailPage />
          </RequireAuth>
        ),
      },
      { path: '*', element: <HomeRedirect /> },
    ],
  },
])
