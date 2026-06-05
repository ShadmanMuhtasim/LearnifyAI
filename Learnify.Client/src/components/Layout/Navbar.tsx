import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import AiProviderBadge from '../AI/AiProviderBadge';
import { useAuthStore } from '../../store/authStore';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: 'DB' },
  { to: '/courses', label: 'Courses', icon: 'CR' },
  { to: '/notes', label: 'Notes', icon: 'NT' },
  { to: '/flashcards', label: 'Flashcards', icon: 'FC' },
  { to: '/quizzes', label: 'Quizzes', icon: 'QZ' },
  { to: '/study-planner', label: 'Study Planner', icon: 'SP' },
  { to: '/settings', label: 'Settings', icon: 'ST' },
];

const futureItems = [
  { label: 'AI Tutor', icon: 'AI' },
  { label: 'Analytics', icon: 'AN' },
  { label: 'Achievements', icon: 'XP' },
];

function getInitial(name?: string | null) {
  return name?.trim().charAt(0).toUpperCase() || 'U';
}

export default function Navbar() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const currentItem = navItems.find((item) => location.pathname.startsWith(item.to));

  return (
    <>
      <aside className="app-sidebar" aria-label="Primary navigation">
        <Link to="/dashboard" className="brand-link">
          <span className="brand-mark" aria-hidden="true">L</span>
          <span>
            <span className="brand-title">LearnifyAI</span>
            <span className="brand-subtitle">Premium Learning</span>
          </span>
        </Link>

        <Link to="/notes" className="ui-button ui-button-primary sidebar-upload">
          Upload
        </Link>

        <nav className="sidebar-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
            >
              <span className="nav-icon" aria-hidden="true">{item.icon}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}

          {futureItems.map((item) => (
            <span key={item.label} className="sidebar-link disabled" aria-disabled="true">
              <span className="nav-icon" aria-hidden="true">{item.icon}</span>
              <span>{item.label}</span>
              <span className="coming-soon">Soon</span>
            </span>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-card">
            <span className="user-avatar" aria-hidden="true">{getInitial(user?.name)}</span>
            <span>
              <span className="user-name">{user?.name || 'Learner'}</span>
              <span className="user-role">{user?.role || 'Student'}</span>
            </span>
          </div>
          <button type="button" className="ui-button ui-button-secondary" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </aside>

      <header className="app-topbar">
        <div className="cluster">
          <span className="mobile-brand">LearnifyAI</span>
          <div className="topbar-search" role="search" aria-label="Search placeholder">
            <span aria-hidden="true">Search</span>
            <span className="muted">across all materials</span>
          </div>
          {currentItem && <span className="ui-badge ui-badge-primary">{currentItem.label}</span>}
        </div>

        <div className="topbar-actions">
          <AiProviderBadge />
          <span className="topbar-icon" aria-label="Notifications">N</span>
          <button type="button" className="topbar-icon" aria-label="Log out" onClick={handleLogout}>
            {getInitial(user?.name)}
          </button>
        </div>
      </header>
    </>
  );
}
