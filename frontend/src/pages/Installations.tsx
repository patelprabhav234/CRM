import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { InstallationDto } from '../types'

export function Installations() {
  const [items, setItems] = useState<InstallationDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await api<InstallationDto[]>('/api/installations'))
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
        <h1>Installations</h1>
        <p className="muted">Technician, schedule, checklist, photos (URLs).</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Customer / site</th>
              <th>Technician</th>
              <th>Scheduled</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map((j) => (
              <tr key={j.id}>
                <td>
                  {j.customerName}
                  <div className="muted small">{j.siteName}</div>
                </td>
                <td>{j.technicianName ?? '—'}</td>
                <td>{j.scheduledDate ? new Date(j.scheduledDate).toLocaleString() : '—'}</td>
                <td>{j.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
