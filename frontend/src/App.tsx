import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { getStoredToken } from './api'
import { AuthProvider, useAuth } from './auth'
import { Layout } from './components/Layout'
import { Amc } from './pages/Amc'
import { Customers } from './pages/Customers'
import { Dashboard } from './pages/Dashboard'
import { Installations } from './pages/Installations'
import { Leads } from './pages/Leads'
import { Login } from './pages/Login'
import { OpsTasks } from './pages/OpsTasks'
import { Products } from './pages/Products'
import { Quotations } from './pages/Quotations'
import { ServiceRequests } from './pages/ServiceRequests'

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { auth } = useAuth()
  // Storage + in-memory session (api.ts) + React state after flushSync — any is enough to enter the app.
  if (!getStoredToken() && !auth.token) return <Navigate to="/login" replace />
  return children
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="leads" element={<Leads />} />
        <Route path="customers" element={<Customers />} />
        <Route path="products" element={<Products />} />
        <Route path="quotations" element={<Quotations />} />
        <Route path="installations" element={<Installations />} />
        <Route path="amc" element={<Amc />} />
        <Route path="services" element={<ServiceRequests />} />
        <Route path="ops-tasks" element={<OpsTasks />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  )
}
