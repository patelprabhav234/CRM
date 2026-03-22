import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api, clearStoredAuth, setTenantId, setToken } from './api'
import type { AuthResponse } from './types'

interface AuthState {
  token: string | null
  email: string | null
  name: string | null
  userId: string | null
  tenantId: string | null
  tenantSubdomain: string | null
  role: string | null
}

const AuthContext = createContext<{
  auth: AuthState
  login: (email: string, password: string, tenantSubdomain: string) => Promise<void>
  registerTenant: (
    companyName: string,
    subdomain: string,
    email: string,
    password: string,
    name: string,
  ) => Promise<void>
  logout: () => void
} | null>(null)

function loadInitial(): AuthState {
  const token = localStorage.getItem('crm_token')
  const email = localStorage.getItem('crm_email')
  const name = localStorage.getItem('crm_name')
  const userId = localStorage.getItem('crm_userId')
  const tenantId = localStorage.getItem('crm_tenantId')
  const tenantSubdomain = localStorage.getItem('crm_tenantSubdomain')
  const role = localStorage.getItem('crm_role')
  return { token, email, name, userId, tenantId, tenantSubdomain, role }
}

function persistAuth(r: AuthResponse) {
  setToken(r.token)
  setTenantId(r.tenantId)
  localStorage.setItem('crm_email', r.email)
  localStorage.setItem('crm_name', r.name)
  localStorage.setItem('crm_userId', r.userId)
  localStorage.setItem('crm_tenantId', r.tenantId)
  localStorage.setItem('crm_tenantSubdomain', r.tenantSubdomain)
  localStorage.setItem('crm_role', r.role)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState>(loadInitial)

  const login = useCallback(async (email: string, password: string, tenantSubdomain: string) => {
    const r = await api<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, tenantSubdomain }),
    })
    persistAuth(r)
    setAuth({
      token: r.token,
      email: r.email,
      name: r.name,
      userId: r.userId,
      tenantId: r.tenantId,
      tenantSubdomain: r.tenantSubdomain,
      role: r.role,
    })
  }, [])

  const registerTenant = useCallback(
    async (
      companyName: string,
      subdomain: string,
      email: string,
      password: string,
      name: string,
    ) => {
      await api('/api/auth/register-tenant', {
        method: 'POST',
        body: JSON.stringify({ companyName, subdomain, email, password, name }),
      })
      // We don't log in automatically here. 
      // User must go to login tab.
    },
    [],
  )

  const logout = useCallback(() => {
    clearStoredAuth()
    setAuth({
      token: null,
      email: null,
      name: null,
      userId: null,
      tenantId: null,
      tenantSubdomain: null,
      role: null,
    })
  }, [])

  const value = useMemo(
    () => ({ auth, login, registerTenant, logout }),
    [auth, login, registerTenant, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth outside AuthProvider')
  return ctx
}
