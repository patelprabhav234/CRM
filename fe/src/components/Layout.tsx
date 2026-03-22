import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth'

const nav = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/leads', label: 'Leads' },
  { to: '/customers', label: 'Customers' },
  { to: '/products', label: 'Products' },
  { to: '/quotations', label: 'Quotations' },
  { to: '/installations', label: 'Installations' },
  { to: '/amc', label: 'AMC' },
  { to: '/services', label: 'Service requests' },
  { to: '/ops-tasks', label: 'Tasks' },
]

export function Layout() {
  const { auth, logout } = useAuth()
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">F</span>
          <div>
            <div className="brand-title">FireOps CRM</div>
            <div className="brand-sub">MaX · Fire safety</div>
          </div>
        </div>
        <nav className="nav">
          {nav.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="user-line">
            {auth.name ?? auth.email}
            {auth.role && <span className="muted small"> · {auth.role}</span>}
            {auth.tenantSubdomain && (
              <div className="muted small">Tenant · {auth.tenantSubdomain}</div>
            )}
          </div>
          <button type="button" className="btn-ghost" onClick={logout}>
            Sign out
          </button>
        </div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
