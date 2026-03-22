import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth'

export function Login() {
  const { auth, login, registerTenant } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [tenantSubdomain, setTenantSubdomain] = useState('shah-fire')
  const [companyName, setCompanyName] = useState('')
  const [subdomain, setSubdomain] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  if (auth.token) return <Navigate to="/" replace />

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(null)
    setBusy(true)
    try {
      if (mode === 'login') await login(email, password, tenantSubdomain.trim())
      else {
        const sub = subdomain.trim().toLowerCase()
        const userEmail = email.trim()
        await registerTenant(
          companyName.trim(),
          sub,
          userEmail,
          password,
          name.trim(),
        )
        setSuccess('Organization created successfully! You can now sign in.')
        setTenantSubdomain(sub)
        setEmail(userEmail)
        setPassword('')
        setMode('login')
      }
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
                placeholder="shah-fire"
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
          <label className="password-field">
            Password
            <div className="input-group">
              <input
                type={showPassword ? 'text' : 'password'}
                required
                minLength={6}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              />
              <button
                type="button"
                className="eye-toggle"
                onClick={() => setShowPassword(!showPassword)}
                tabIndex={-1}
              >
                {showPassword ? '🙈' : '👁️'}
              </button>
            </div>
          </label>
          {error && <div className="error-banner">{error}</div>}
          {success && <div className="success-banner">{success}</div>}
          <button type="submit" className="btn-primary" disabled={busy}>
            {busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create organization'}
          </button>
        </form>
        <p className="hint muted">
          SQL seed: subdomain <code>shah-fire</code> · <code>crm@shahfiresafety.in</code> /{' '}
          <code>Admin123!</code> (Admin) · <code>field@shahfiresafety.in</code> / <code>Tech123!</code> (Technician)
        </p>
      </div>
    </div>
  )
}
