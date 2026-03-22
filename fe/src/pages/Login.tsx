import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth'

export function Login() {
  const { auth, login, registerTenant } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [tenantSubdomain, setTenantSubdomain] = useState('demo')
  const [companyName, setCompanyName] = useState('')
  const [subdomain, setSubdomain] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (auth.token) return <Navigate to="/" replace />

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      if (mode === 'login') await login(email, password, tenantSubdomain.trim())
      else
        await registerTenant(
          companyName.trim(),
          subdomain.trim().toLowerCase(),
          email,
          password,
          name.trim(),
        )
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>FireOps CRM</h1>
        <p className="muted">Service + AMC + compliance for fire safety companies.</p>
        <div className="tabs">
          <button
            type="button"
            className={mode === 'login' ? 'tab active' : 'tab'}
            onClick={() => setMode('login')}
          >
            Sign in
          </button>
          <button
            type="button"
            className={mode === 'register' ? 'tab active' : 'tab'}
            onClick={() => setMode('register')}
          >
            New organization
          </button>
        </div>
        <form onSubmit={onSubmit} className="form">
          {mode === 'login' && (
            <label>
              Tenant subdomain
              <input
                value={tenantSubdomain}
                onChange={(e) => setTenantSubdomain(e.target.value)}
                autoComplete="organization"
                placeholder="demo"
                required
              />
            </label>
          )}
          {mode === 'register' && (
            <>
              <label>
                Company name
                <input
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  autoComplete="organization"
                  placeholder="Shah Fire Safety"
                  required
                />
              </label>
              <label>
                Subdomain (letters, digits, hyphens)
                <input
                  value={subdomain}
                  onChange={(e) => setSubdomain(e.target.value)}
                  autoComplete="off"
                  placeholder="your-company"
                  required
                />
              </label>
              <label>
                Admin name
                <input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  autoComplete="name"
                  placeholder="Your name"
                  required
                />
              </label>
            </>
          )}
          <label>
            Email
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
            />
          </label>
          <label>
            Password
            <input
              type="password"
              required
              minLength={6}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
          </label>
          {error && <div className="error-banner">{error}</div>}
          <button type="submit" className="btn-primary" disabled={busy}>
            {busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create organization'}
          </button>
        </form>
        <p className="hint muted">
          Seed tenant: subdomain <code>demo</code> · <code>admin@fireops.local</code> /{' '}
          <code>Admin123!</code> · <code>tech@fireops.local</code> / <code>Tech123!</code>
        </p>
      </div>
    </div>
  )
}
