import { Link, useNavigate } from 'react-router-dom';
import AiProviderBadge from '../AI/AiProviderBadge';
import { useAuthStore } from '../../store/authStore';

export default function Navbar() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav style={{
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      padding: '1rem 2rem',
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
      position: 'sticky',
      top: 0,
      zIndex: 100
    }}>
      <Link to="/dashboard" style={{
        color: 'white',
        textDecoration: 'none',
        fontSize: '1.5rem',
        fontWeight: 700
      }}>
        LearnifyAI
      </Link>

      <div style={{ display: 'flex', gap: '2rem', alignItems: 'center' }}>
        <Link to="/dashboard" style={{
          color: 'white',
          textDecoration: 'none',
          fontWeight: 500,
          opacity: 0.9
        }}>
          Dashboard
        </Link>
        <Link to="/courses" style={{
          color: 'white',
          textDecoration: 'none',
          fontWeight: 500,
          opacity: 0.9
        }}>
          Courses
        </Link>
        <Link to="/notes" style={{
          color: 'white',
          textDecoration: 'none',
          fontWeight: 500,
          opacity: 0.9
        }}>
          Notes
        </Link>
        <Link to="/settings" style={{
          color: 'white',
          textDecoration: 'none',
          fontWeight: 500,
          opacity: 0.9
        }}>
          Settings
        </Link>

        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
          color: 'white',
          marginLeft: '1rem',
          paddingLeft: '1rem',
          borderLeft: '1px solid rgba(255,255,255,0.3)'
        }}>
          <AiProviderBadge />
          <div>
            <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>
              {user?.name}
            </div>
            <div style={{
              fontSize: '0.75rem',
              opacity: 0.8,
              background: 'rgba(255,255,255,0.2)',
              padding: '0.15rem 0.5rem',
              borderRadius: '10px',
              display: 'inline-block'
            }}>
              {user?.role}
            </div>
          </div>
          <button
            onClick={handleLogout}
            style={{
              background: 'rgba(255,255,255,0.2)',
              border: '1px solid rgba(255,255,255,0.4)',
              color: 'white',
              padding: '0.4rem 1rem',
              borderRadius: '6px',
              cursor: 'pointer',
              fontWeight: 500,
              fontSize: '0.85rem'
            }}
          >
            Logout
          </button>
        </div>
      </div>
    </nav>
  );
}
