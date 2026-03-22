import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { ServiceRequestDto } from '../types'

export function ServiceRequests() {
  const [items, setItems] = useState<ServiceRequestDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await api<ServiceRequestDto[]>('/api/servicerequests'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div className="page">
      <header className="page-head">
        <h1>Service requests</h1>
        <p className="muted">Complaints and maintenance tickets.</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      <ul className="activity-list">
        {items.map((s) => (
          <li key={s.id} className="activity-item">
            <div className="row-between">
              <span className="pill">{s.status}</span>
              {s.priority && <span className="muted small">Priority: {s.priority}</span>}
            </div>
            <p>{s.description}</p>
            <p className="muted small">
              {s.customerName} {s.siteName && `· ${s.siteName}`} ·{' '}
              {s.assignedToName ?? 'Unassigned'} · {new Date(s.createdAt).toLocaleString()}
            </p>
          </li>
        ))}
      </ul>
      {items.length === 0 && <p className="muted">No service requests.</p>}
    </div>
  )
}
