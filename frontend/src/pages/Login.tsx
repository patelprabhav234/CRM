import { useEffect, useRef, useState } from 'react'
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { getStoredToken } from '../api'
import { useAuth } from '../auth'

export function Login() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { login, registerTenant, logout } = useAuth()
  const reauthDone = useRef(false)
  const autoSubmitDone = useRef(false)
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

  const hasSession = !!getStoredToken()
  const urlRequestsLoginPage =
    searchParams.get('reauth') === '1' ||
    searchParams.has('email') ||
    searchParams.has('tenant') ||
    searchParams.has('subdomain') ||
    searchParams.get('auto') === '1'

  useEffect(() => {
    if (reauthDone.current) return
    if (searchParams.get('reauth') !== '1') return
    reauthDone.current = true
    logout()
    const next = new URLSearchParams(searchParams)
    next.delete('reauth')
    setSearchParams(next, { replace: true })
  }, [searchParams, logout, setSearchParams])

  useEffect(() => {
    const tenant = searchParams.get('tenant') ?? searchParams.get('subdomain')
    const em = searchParams.get('email')
    if (tenant) setTenantSubdomain(tenant)
    if (em) setEmail(em)
    const pw = searchParams.get('password')
    if (pw) setPassword(pw)
  }, [searchParams])

  useEffect(() => {
    if (autoSubmitDone.current) return
    if (searchParams.get('auto') !== '1') return
    const tenant = (searchParams.get('tenant') ?? searchParams.get('subdomain'))?.trim()
    const em = searchParams.get('email')?.trim()
    const pw = searchParams.get('password') ?? ''
    if (!tenant || !em || !pw) return
    autoSubmitDone.current = true
    void (async () => {
      setBusy(true)
      setError(null)
      try {
        await login(em, pw, tenant)
        navigate('/', { replace: true })
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Sign-in failed')
      } finally {
        setBusy(false)
      }
    })()
  }, [searchParams, login, navigate])

  if (hasSession && !urlRequestsLoginPage) return <Navigate to="/" replace />

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      if (mode === 'login') {
        await login(
          email.trim().toLowerCase(),
          password.trim(),
          tenantSubdomain.trim().toLowerCase(),
        )
        navigate('/', { replace: true })
      } else {
        const sub = subdomain.trim().toLowerCase()
        const userEmail = email.trim().toLowerCase()
        await registerTenant(
          companyName.trim(),
          sub,
          userEmail,
          password.trim(),
          name.trim(),
        )
        navigate('/', { replace: true })
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
            onClick={() => {
              setMode('login')
              setError(null)
            }}
          >
            Sign in
          </button>
          <button
            type="button"
            className={mode === 'register' ? 'tab active' : 'tab'}
            onClick={() => {
              setMode('register')
              setError(null)
            }}
          >
            New organization
          </button>
        </div>
        <form className="form" onSubmit={onSubmit}>
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
          <button type="submit" className="btn-primary" disabled={busy}>
            {busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create organization'}
          </button>
        </form>
        <p className="hint muted">
          Dev · <code>shah-fire</code> · Admin <code>crm@shahfiresafety.in</code> /{' '}
          <code>ShahFire#MaX-2025</code> · Tech <code>field@shahfiresafety.in</code> or{' '}
          <code>tech@shahfire.com</code> / <code>FieldTech#MaX-2025</code>
        </p>
        <p className="hint muted small">
          If Chrome warns “password in a data breach,” it’s flagging common demo passwords—use the strings above
          after a DB update, or set your own in PostgreSQL.
        </p>
      </div>
    </div>
  )
}
