import { useEffect, useState } from 'react'
import { api } from '../api'
import type { FireOpsDashboardDto } from '../types'

export function Dashboard() {
  const [data, setData] = useState<FireOpsDashboardDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const s = await api<FireOpsDashboardDto>('/api/dashboard/summary')
        if (!cancelled) setData(s)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  if (error) return <div className="error-banner">{error}</div>
  if (!data) return <div className="muted">Loading…</div>

  const fmt = (n: number) =>
    new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(n)

  return (
    <div className="page">
      <header className="page-head">
        <h1>Dashboard</h1>
        <p className="muted">Fire safety — AMC, services, and pipeline at a glance.</p>
      </header>
      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-label">Leads</div>
          <div className="stat-value">{data.totalLeads}</div>
        </div>
        <div className="stat-card accent">
          <div className="stat-label">Active AMC contracts</div>
          <div className="stat-value">{data.activeAmcContracts}</div>
        </div>
        <div className="stat-card warn">
          <div className="stat-label">Open service requests</div>
          <div className="stat-value">{data.openServiceRequests}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Pending quotations</div>
          <div className="stat-value">{data.pendingQuotations}</div>
        </div>
        <div className="stat-card accent">
          <div className="stat-label">AMC revenue (active)</div>
          <div className="stat-value">{fmt(data.amcRevenueActive)}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">AMC visits (30 days)</div>
          <div className="stat-value">{data.upcomingAmcVisitsNext30Days}</div>
        </div>
      </div>
    </div>
  )
}
