import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from './components/AppLayout'
import { RequireAuth } from './auth/RequireAuth'
import { SignInPage } from './pages/SignInPage'
import { DisputesListPage } from './pages/DisputesListPage'
import { NewDisputePage } from './pages/NewDisputePage'
import { DisputeDetailPage } from './pages/DisputeDetailPage'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/signin', element: <SignInPage /> },
      { index: true, element: <Navigate to="/disputes" replace /> },
      {
        path: '/disputes',
        element: (
          <RequireAuth>
            <DisputesListPage />
          </RequireAuth>
        ),
      },
      {
        path: '/disputes/new',
        element: (
          <RequireAuth role="DisputeAdmin">
            <NewDisputePage />
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
      { path: '*', element: <Navigate to="/disputes" replace /> },
    ],
  },
])
