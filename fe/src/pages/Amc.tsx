import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { AmcContractDto, AmcVisitDto } from '../types'

export function Amc() {
  const [contracts, setContracts] = useState<AmcContractDto[]>([])
  const [visits, setVisits] = useState<AmcVisitDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      const [c, v] = await Promise.all([
        api<AmcContractDto[]>('/api/amc/contracts'),
        api<AmcVisitDto[]>('/api/amc/visits'),
      ])
      setContracts(c)
      setVisits(v)
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
        <h1>AMC</h1>
        <p className="muted">Contracts and scheduled visits — core revenue engine.</p>
      </header>
      {error && <div className="error-banner">{error}</div>}
      <h2 className="sub-heading">Contracts</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Customer / site</th>
              <th>Period</th>
              <th>Visits / yr</th>
              <th>Value</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {contracts.map((c) => (
              <tr key={c.id}>
                <td>
                  {c.customerName}
                  <div className="muted small">{c.siteName}</div>
                </td>
                <td className="small">
                  {new Date(c.startDate).toLocaleDateString()} — {new Date(c.endDate).toLocaleDateString()}
                </td>
                <td>{c.visitFrequencyPerYear}</td>
                <td>{c.contractValue != null ? fmt(c.contractValue) : '—'}</td>
                <td>{c.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <h2 className="sub-heading" style={{ marginTop: '2rem' }}>
        Visits
      </h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Scheduled</th>
              <th>Technician</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {visits.map((v) => (
              <tr key={v.id}>
                <td>{new Date(v.scheduledDate).toLocaleString()}</td>
                <td>{v.technicianName ?? '—'}</td>
                <td>{v.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
