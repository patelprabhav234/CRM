function getToken(): string | null {
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

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (!(init?.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  const t = getToken()
  if (t) headers.set('Authorization', `Bearer ${t}`)
  const tid = getTenantId()
  if (tid) headers.set('X-Tenant-ID', tid)
  const res = await fetch(path, { ...init, headers })
  const text = await res.text()
  if (!res.ok) {
    let msg = res.statusText
    if (text) {
      try {
        const j = JSON.parse(text) as { title?: string; detail?: string; message?: string }
        msg = j.detail || j.title || j.message || text
      } catch {
        msg = text
      }
    }
    throw new Error(msg)
  }
  if (res.status === 204 || !text) return undefined as T
  return JSON.parse(text) as T
}
