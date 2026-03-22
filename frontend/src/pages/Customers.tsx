import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { CustomerDto, SiteDto } from '../types'

export function Customers() {
  const [customers, setCustomers] = useState<CustomerDto[]>([])
  const [sites, setSites] = useState<SiteDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      const [c, s] = await Promise.all([
        api<CustomerDto[]>('/api/customers'),
        api<SiteDto[]>('/api/sites'),
      ])
      setCustomers(c)
      setSites(s)
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
        <h1>Customers &amp; sites</h1>
        <p className="muted">One customer — multiple sites. Compliance per site.</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      {customers.map((c) => (
        <section key={c.id} className="card-section">
          <h2>{c.name}</h2>
          <p className="muted small">
            {c.contactPerson} · {c.phone} · {c.email}
          </p>
          <p className="small">{c.address}</p>
          <h3 className="sub-heading">Sites</h3>
          <ul className="simple-list">
            {sites
              .filter((s) => s.customerId === c.id)
              .map((s) => (
                <li key={s.id}>
                  <strong>{s.name}</strong> — {s.city}, {s.state} ({s.siteType})
                  {s.complianceStatus && <span className="muted"> · {s.complianceStatus}</span>}
                </li>
              ))}
            {sites.filter((s) => s.customerId === c.id).length === 0 && (
              <li className="muted">No sites yet</li>
            )}
          </ul>
        </section>
      ))}
      {customers.length === 0 && <p className="muted">No customers (seed creates Shah Fire Safety).</p>}
    </div>
  )
}
