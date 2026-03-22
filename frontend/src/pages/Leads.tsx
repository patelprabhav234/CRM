import { useCallback, useEffect, useState } from 'react'
import { api } from '../api'
import type { LeadDto } from '../types'

const statuses = ['New', 'Contacted', 'SiteVisit', 'Quoted', 'Won', 'Lost'] as const

export function Leads() {
  const [items, setItems] = useState<LeadDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<LeadDto | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      const data = await api<LeadDto[]>('/api/leads')
      setItems(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <div className="page">
      <header className="page-head row">
        <div>
          <h1>Leads &amp; enquiries</h1>
          <p className="muted">Requirements: extinguishers, hydrant, audit, training, AMC.</p>
        </div>
        <button type="button" className="btn-primary" onClick={() => setOpen(true)}>
          New lead
        </button>
      </header>
      {error && <div className="error-banner">{error}</div>}
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Name / company</th>
              <th>Location</th>
              <th>Requirement</th>
              <th>Source</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((l) => (
              <tr key={l.id}>
                <td>
                  <strong>{l.name}</strong>
                  <div className="muted small">{l.company}</div>
                  <div className="muted small">{l.email}</div>
                </td>
                <td>
                  <div className="small">{l.city ?? '—'}{l.state ? `, ${l.state}` : ''}</div>
                  <div className="muted small">{l.location}</div>
                </td>
                <td className="small">{l.requirement ?? '—'}</td>
                <td>{l.source}</td>
                <td>
                  <span className="pill">{l.status}</span>
                </td>
                <td>
                  <button type="button" className="btn-link" onClick={() => setEditing(l)}>
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {open && (
        <LeadModal
          onClose={() => setOpen(false)}
          onSaved={() => {
            setOpen(false)
            void load()
          }}
        />
      )}
      {editing && (
        <LeadModal
          initial={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            void load()
          }}
        />
      )}
    </div>
  )
}

function LeadModal({
  initial,
  onClose,
  onSaved,
}: {
  initial?: LeadDto
  onClose: () => void
  onSaved: () => void
}) {
  const [name, setName] = useState(initial?.name ?? '')
  const [company, setCompany] = useState(initial?.company ?? '')
  const [email, setEmail] = useState(initial?.email ?? '')
  const [phone, setPhone] = useState(initial?.phone ?? '')
  const [location, setLocation] = useState(initial?.location ?? '')
  const [city, setCity] = useState(initial?.city ?? '')
  const [state, setState] = useState(initial?.state ?? '')
  const [requirement, setRequirement] = useState(initial?.requirement ?? '')
  const [source, setSource] = useState(initial?.source ?? 'Call')
  const [status, setStatus] = useState(initial?.status ?? 'New')
  const [assignedToUserId, setAssignedToUserId] = useState(initial?.assignedToUserId ?? '')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    const body = {
      name,
      company: company || null,
      email: email || null,
      phone: phone || null,
      location: location || null,
      city: city || null,
      state: state || null,
      requirement: requirement || null,
      source,
      status,
      assignedToUserId: assignedToUserId ? assignedToUserId : null,
    }
    try {
      if (initial) {
        await api(`/api/leads/${initial.id}`, {
          method: 'PUT',
          body: JSON.stringify(body),
        })
      } else {
        await api('/api/leads', { method: 'POST', body: JSON.stringify(body) })
      }
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal" role="dialog" onClick={(e) => e.stopPropagation()}>
        <h2>{initial ? 'Edit lead' : 'New lead'}</h2>
        <form onSubmit={submit} className="form grid-form">
          <label>
            Name *
            <input required value={name} onChange={(e) => setName(e.target.value)} />
          </label>
          <label>
            Company
            <input value={company} onChange={(e) => setCompany(e.target.value)} />
          </label>
          <label>
            Email
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label>
            Phone
            <input value={phone} onChange={(e) => setPhone(e.target.value)} />
          </label>
          <label>
            City
            <input value={city} onChange={(e) => setCity(e.target.value)} />
          </label>
          <label>
            State
            <input value={state} onChange={(e) => setState(e.target.value)} />
          </label>
          <label className="full">
            Location / area
            <input value={location} onChange={(e) => setLocation(e.target.value)} />
          </label>
          <label className="full">
            Requirement
            <textarea value={requirement} onChange={(e) => setRequirement(e.target.value)} rows={2} placeholder="e.g. CO2 refill, hydrant audit" />
          </label>
          <label>
            Source
            <input value={source} onChange={(e) => setSource(e.target.value)} placeholder="Call, WhatsApp, referral…" />
          </label>
          <label>
            Status
            <select value={status} onChange={(e) => setStatus(e.target.value)}>
              {statuses.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
          <label className="full">
            Assigned user id (optional)
            <input value={assignedToUserId} onChange={(e) => setAssignedToUserId(e.target.value)} placeholder="GUID of sales user" />
          </label>
          {error && <div className="error-banner full">{error}</div>}
          <div className="modal-actions full">
            <button type="button" className="btn-ghost" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={busy}>
              {busy ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
