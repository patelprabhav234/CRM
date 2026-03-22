import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { QuotationDto } from '../types'

export function Quotations() {
  const [items, setItems] = useState<QuotationDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await api<QuotationDto[]>('/api/quotations'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const fmt = (n: number) =>
    new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(n)

  return (
    <div className="page">
      <header className="page-head">
        <h1>Quotations</h1>
        <p className="muted">Draft / sent / approved / rejected. PDF export can be added next.</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      {items.map((q) => (
        <div key={q.id} className="card-section">
          <div className="row-between">
            <strong>{q.customerName}</strong>
            <span className="pill">{q.status}</span>
          </div>
          <p className="muted small">
            {new Date(q.createdAt).toLocaleString()} · {q.siteName ?? 'All sites'}
          </p>
          <p className="stat-value" style={{ fontSize: '1.2rem' }}>
            {fmt(q.totalAmount)}
          </p>
          <ul className="simple-list">
            {q.items.map((i) => (
              <li key={i.id}>
                {i.productName} × {i.quantity} @ {fmt(i.unitPrice)} = {fmt(i.lineTotal)}
              </li>
            ))}
          </ul>
        </div>
      ))}
      {items.length === 0 && <p className="muted">No quotations yet.</p>}
    </div>
  )
}
