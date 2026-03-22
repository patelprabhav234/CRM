import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { OpsTaskDto } from '../types'

export function OpsTasks() {
  const [items, setItems] = useState<OpsTaskDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await api<OpsTaskDto[]>('/api/opstasks'))
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
        <h1>Tasks</h1>
        <p className="muted">Installation, AMC, service — assigned to technicians.</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      <ul className="task-list">
        {items.map((t) => (
          <li key={t.id} className="task-item">
            <strong>{t.title}</strong>
            <div className="muted small">
              {t.taskType} · {t.status} · Due {new Date(t.dueDate).toLocaleString()}
            </div>
            <div className="muted small">{t.assignedToName ?? t.assignedToUserId}</div>
          </li>
        ))}
      </ul>
      {items.length === 0 && <p className="muted">No tasks.</p>}
    </div>
  )
}
