import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { api, setToken } from './api'
import type { AuthResponse } from './types'

interface AuthState {
  token: string | null
  email: string | null
  name: string | null
  userId: string | null
  role: string | null
}

const AuthContext = createContext<{
  auth: AuthState
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, name: string) => Promise<void>
  logout: () => void
} | null>(null)

function loadInitial(): AuthState {
  const token = localStorage.getItem('crm_token')
  const email = localStorage.getItem('crm_email')
  const name = localStorage.getItem('crm_name')
  const userId = localStorage.getItem('crm_userId')
  const role = localStorage.getItem('crm_role')
  return { token, email, name, userId, role }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState>(loadInitial)

  const login = useCallback(async (email: string, password: string) => {
    const r = await api<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
    setToken(r.token)
    localStorage.setItem('crm_email', r.email)
    localStorage.setItem('crm_name', r.name)
    localStorage.setItem('crm_userId', r.userId)
    localStorage.setItem('crm_role', r.role)
    setAuth({
      token: r.token,
      email: r.email,
      name: r.name,
      userId: r.userId,
      role: r.role,
    })
  }, [])

  const register = useCallback(async (email: string, password: string, name: string) => {
    const r = await api<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, name }),
    })
    setToken(r.token)
    localStorage.setItem('crm_email', r.email)
    localStorage.setItem('crm_name', r.name)
    localStorage.setItem('crm_userId', r.userId)
    localStorage.setItem('crm_role', r.role)
    setAuth({
      token: r.token,
      email: r.email,
      name: r.name,
      userId: r.userId,
      role: r.role,
    })
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    localStorage.removeItem('crm_email')
    localStorage.removeItem('crm_name')
    localStorage.removeItem('crm_userId')
    localStorage.removeItem('crm_role')
    setAuth({ token: null, email: null, name: null, userId: null, role: null })
  }, [])

  const value = useMemo(
    () => ({ auth, login, register, logout }),
    [auth, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth outside AuthProvider')
  return ctx
}
