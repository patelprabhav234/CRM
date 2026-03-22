function getToken(): string | null {
  return localStorage.getItem('crm_token')
}

/** Exported for route guards — same as Bearer source. */
export function getStoredToken(): string | null {
  return localStorage.getItem('crm_token')
}

function getTenantId(): string | null {
  return localStorage.getItem('crm_tenantId')
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem('crm_token', token)
  else localStorage.removeItem('crm_token')
}

export function setTenantId(tenantId: string | null) {
  if (tenantId) localStorage.setItem('crm_tenantId', tenantId)
  else localStorage.removeItem('crm_tenantId')
}

/** Paths that must not send Bearer tokens — invalid JWTs break JWT middleware before [AllowAnonymous] runs. */
const authPathsWithoutBearer = ['/api/auth/login', '/api/auth/register-tenant']

/** Fired when 401 clears storage so AuthProvider can drop stale React state (must match auth listener). */
export const CRM_AUTH_CLEARED_EVENT = 'crm:auth-cleared'

export function clearStoredAuth() {
  localStorage.removeItem('crm_token')
  localStorage.removeItem('crm_tenantId')
  localStorage.removeItem('crm_email')
  localStorage.removeItem('crm_name')
  localStorage.removeItem('crm_userId')
  localStorage.removeItem('crm_tenantSubdomain')
  localStorage.removeItem('crm_role')
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!(init?.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  const skipBearer = authPathsWithoutBearer.some((p) => path.startsWith(p))
  const t = getToken()
  if (t && !skipBearer) headers.set('Authorization', `Bearer ${t}`)
  const tid = getTenantId()
  if (tid && !skipBearer) headers.set('X-Tenant-ID', tid)
  const res = await fetch(path, { ...init, headers })
  const text = await res.text()
  if (!res.ok) {
    if (res.status === 401 && !skipBearer) {
      clearStoredAuth()
      window.dispatchEvent(new Event(CRM_AUTH_CLEARED_EVENT))
      const path = window.location.pathname
      const onLogin = path === '/login' || path.endsWith('/login')
      if (!onLogin) window.location.assign('/login')
    }
    let msg = res.statusText
    if (text) {
      try {
        const j = JSON.parse(text) as { title?: string; detail?: string; message?: string }
        msg = j.detail || j.title || j.message || text
      } catch {
        msg = text
      }
    }
    if (res.status === 502 || res.status === 504) {
      msg =
        'Cannot reach the API (bad gateway). Start the backend: dotnet run --project be/CRM.Api (http://127.0.0.1:5254). Start PostgreSQL: docker compose up -d from the repo root.'
    }
    if (res.status === 401 && !skipBearer) {
      msg = 'Session expired or invalid. Please sign in again.'
    }
    throw new Error(msg)
  }
  if (res.status === 204 || !text) return undefined as T
  return JSON.parse(text) as T
}
