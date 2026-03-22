import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { flushSync } from 'react-dom'
import { api, clearStoredAuth, CRM_AUTH_CLEARED_EVENT, setTenantId, setToken } from './api'
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

/** Handles both camelCase and PascalCase API JSON (e.g. Token vs token). */
function parseAuthResponse(raw: unknown): AuthResponse {
  const o = raw as Record<string, unknown>
  const pick = (a: string, b: string) => o[a] ?? o[b]
  const str = (v: unknown) => (v === null || v === undefined ? '' : String(v).trim())

  const token = str(pick('token', 'Token'))
  const userId = str(pick('userId', 'UserId'))
  const tenantId = str(pick('tenantId', 'TenantId'))
  const tenantSubdomain = str(pick('tenantSubdomain', 'TenantSubdomain'))
  const email = str(pick('email', 'Email'))
  const name = str(pick('name', 'Name'))
  const role = str(pick('role', 'Role'))

  if (!token || !userId || !tenantId)
    throw new Error('Login response missing token or ids. Check API JSON shape.')
  return { token, userId, tenantId, tenantSubdomain, email, name, role }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState>(loadInitial)

  useEffect(() => {
    const sync = () => setAuth(loadInitial())
    window.addEventListener(CRM_AUTH_CLEARED_EVENT, sync)
    return () => window.removeEventListener(CRM_AUTH_CLEARED_EVENT, sync)
  }, [])

  const login = useCallback(async (email: string, password: string, tenantSubdomain: string) => {
    const raw = await api<unknown>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, tenantSubdomain }),
    })
    const r = parseAuthResponse(raw)
    persistAuth(r)
    flushSync(() => {
      setAuth({
        token: r.token,
        email: r.email,
        name: r.name,
        userId: r.userId,
        tenantId: r.tenantId,
        tenantSubdomain: r.tenantSubdomain,
        role: r.role,
      })
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
      const raw = await api<unknown>('/api/auth/register-tenant', {
        method: 'POST',
        body: JSON.stringify({ companyName, subdomain, email, password, name }),
      })
      const r = parseAuthResponse(raw)
      persistAuth(r)
      flushSync(() => {
        setAuth({
          token: r.token,
          email: r.email,
          name: r.name,
          userId: r.userId,
          tenantId: r.tenantId,
          tenantSubdomain: r.tenantSubdomain,
          role: r.role,
        })
      })
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
