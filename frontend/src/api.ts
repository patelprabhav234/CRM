/** Mirrors localStorage so the first request after login always sees the new session (same JS tick). */
let memoryToken: string | null = null
let memoryTenantId: string | null = null

function getToken(): string | null {
  const fromStore = localStorage.getItem('crm_token')
  if (fromStore) return fromStore
  return memoryToken
}

/** Exported for route guards — same source as Bearer. */
export function getStoredToken(): string | null {
  return getToken()
}

function getTenantId(): string | null {
  const fromStore = localStorage.getItem('crm_tenantId')
  if (fromStore) return fromStore
  return memoryTenantId
}

/** Base URL for API (optional). Set VITE_API_URL when not using the Vite dev proxy (e.g. https://127.0.0.1:7096). */
function resolveApiUrl(path: string): string {
  const raw = import.meta.env.VITE_API_URL as string | undefined
  const base = raw?.trim().replace(/\/$/, '') ?? ''
  const p = path.startsWith('/') ? path : `/${path}`
  return base ? `${base}${p}` : p
}

export function setToken(token: string | null) {
  memoryToken = token
  if (token) localStorage.setItem('crm_token', token)
  else localStorage.removeItem('crm_token')
}

export function setTenantId(tenantId: string | null) {
  memoryTenantId = tenantId
  if (tenantId) localStorage.setItem('crm_tenantId', tenantId)
  else localStorage.removeItem('crm_tenantId')
}

/** Paths that must not send Bearer tokens — invalid JWTs break JWT middleware before [AllowAnonymous] runs. */
const authPathsWithoutBearer = ['/api/auth/login', '/api/auth/register-tenant']

/** Fired when 401 clears storage so AuthProvider can drop stale React state (must match auth listener). */
export const CRM_AUTH_CLEARED_EVENT = 'crm:auth-cleared'

export function clearStoredAuth() {
  memoryToken = null
  memoryTenantId = null
  localStorage.removeItem('crm_token')
  localStorage.removeItem('crm_tenantId')
  localStorage.removeItem('crm_email')
  localStorage.removeItem('crm_name')
  localStorage.removeItem('crm_userId')
  localStorage.removeItem('crm_tenantSubdomain')
  localStorage.removeItem('crm_role')
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const url = resolveApiUrl(path)
  const headers = new Headers(init?.headers)
  if (!(init?.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  const skipBearer = authPathsWithoutBearer.some((p) => path.startsWith(p))
  const t = getToken()
  if (t && !skipBearer) headers.set('Authorization', `Bearer ${t}`)
  const tid = getTenantId()
  if (tid && !skipBearer) headers.set('X-Tenant-ID', tid)
  const res = await fetch(url, { ...init, headers })
  const text = await res.text()
  if (!res.ok) {
    if (res.status === 401 && !skipBearer) {
      clearStoredAuth()
      window.dispatchEvent(new Event(CRM_AUTH_CLEARED_EVENT))
      const pathname = window.location.pathname
      const onLogin = pathname === '/login' || pathname.endsWith('/login')
      if (!onLogin) window.location.assign('/login')
    }
    let msg = res.statusText
    if (text) {
      try {
        const parsed = JSON.parse(text) as unknown
        if (typeof parsed === 'string') {
          msg = parsed
        } else if (parsed && typeof parsed === 'object') {
          const j = parsed as { title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }
          msg =
            j.detail ||
            j.title ||
            j.message ||
            (j.errors && Object.values(j.errors).flat().filter(Boolean).join(' ')) ||
            text
        }
      } catch {
        msg = text
      }
    }
    if (res.status === 502 || res.status === 504) {
      msg =
        'Cannot reach the API (bad gateway). Start the backend and ensure VITE_API_PROXY_TARGET (Vite) or VITE_API_URL matches your API URL.'
    }
    if (res.status === 401 && !skipBearer) {
      msg = 'Session expired or invalid. Please sign in again.'
    }
    throw new Error(msg)
  }
  if (res.status === 204 || !text) return undefined as T
  return JSON.parse(text) as T
}
