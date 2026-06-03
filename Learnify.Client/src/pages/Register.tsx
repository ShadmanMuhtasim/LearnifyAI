import { useState, type FormEvent } from 'react';
import axios from 'axios';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export default function Register() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { register, isLoading, error: authError, clearError } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    clearError();

    try {
      await register(name, email, password);
      navigate('/dashboard');
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && error.response?.data) {
        const data = error.response.data;
        if (data.errors && Array.isArray(data.errors) && data.errors.length > 0) {
          setSubmitError(data.errors[0]);
        } else if (data.message) {
          setSubmitError(data.message);
        } else {
          setSubmitError('Registration failed. Please try again.');
        }
      } else {
        setSubmitError('Unable to connect to server. Please check your connection.');
      }
    }
  };

  const visibleError = submitError || authError;

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      padding: '1rem'
    }}>
      <form
        onSubmit={handleSubmit}
        style={{
          background: 'white',
          borderRadius: '12px',
          padding: '2.5rem',
          width: '100%',
          maxWidth: '420px',
          boxShadow: '0 20px 60px rgba(0,0,0,0.3)'
        }}
      >
        <h1 style={{
          margin: '0 0 0.5rem',
          fontSize: '2rem',
          color: '#333',
          textAlign: 'center'
        }}>
          Create Account
        </h1>
        <p style={{
          margin: '0 0 2rem',
          color: '#666',
          textAlign: 'center'
        }}>
          Join Learnify today
        </p>

        <div style={{ marginBottom: '1.25rem' }}>
          <label htmlFor="name" style={{
            display: 'block',
            marginBottom: '0.5rem',
            fontWeight: 500,
            color: '#374151',
            fontSize: '0.875rem'
          }}>
            Full Name
          </label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            placeholder="John Doe"
            style={{
              width: '100%',
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              border: '1px solid #d1d5db',
              fontSize: '1rem',
              boxSizing: 'border-box',
              outline: 'none',
              transition: 'border-color 0.2s'
            }}
            onFocus={(e) => (e.target.style.borderColor = '#667eea')}
            onBlur={(e) => (e.target.style.borderColor = '#d1d5db')}
          />
        </div>

        <div style={{ marginBottom: '1.25rem' }}>
          <label htmlFor="email" style={{
            display: 'block',
            marginBottom: '0.5rem',
            fontWeight: 500,
            color: '#374151',
            fontSize: '0.875rem'
          }}>
            Email Address
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
            placeholder="you@example.com"
            style={{
              width: '100%',
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              border: '1px solid #d1d5db',
              fontSize: '1rem',
              boxSizing: 'border-box',
              outline: 'none',
              transition: 'border-color 0.2s'
            }}
            onFocus={(e) => (e.target.style.borderColor = '#667eea')}
            onBlur={(e) => (e.target.style.borderColor = '#d1d5db')}
          />
        </div>

        <div style={{ marginBottom: '1.25rem' }}>
          <label htmlFor="password" style={{
            display: 'block',
            marginBottom: '0.5rem',
            fontWeight: 500,
            color: '#374151',
            fontSize: '0.875rem'
          }}>
            Password
          </label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="new-password"
            placeholder="••••••••"
            style={{
              width: '100%',
              padding: '0.75rem 1rem',
              borderRadius: '8px',
              border: '1px solid #d1d5db',
              fontSize: '1rem',
              boxSizing: 'border-box',
              outline: 'none',
              transition: 'border-color 0.2s'
            }}
            onFocus={(e) => (e.target.style.borderColor = '#667eea')}
            onBlur={(e) => (e.target.style.borderColor = '#d1d5db')}
          />
        </div>

        {visibleError && (
          <div style={{
            color: 'red',
            marginBottom: '1rem',
            padding: '0.5rem',
            border: '1px solid red',
            borderRadius: '4px',
          }}>
            {visibleError}
          </div>
        )}

        <button
          type="submit"
          disabled={isLoading}
          style={{
            width: '100%',
            padding: '0.875rem',
            background: isLoading ? '#9ca3af' : 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            color: 'white',
            border: 'none',
            borderRadius: '8px',
            fontSize: '1rem',
            fontWeight: 600,
            cursor: isLoading ? 'not-allowed' : 'pointer',
            transition: 'transform 0.2s, box-shadow 0.2s',
            boxShadow: '0 4px 14px rgba(102, 126, 234, 0.4)'
          }}
        >
          {isLoading ? 'Creating account...' : 'Sign Up'}
        </button>

        <p style={{
          textAlign: 'center',
          marginTop: '1.5rem',
          color: '#666',
          fontSize: '0.875rem'
        }}>
          Already have an account?{' '}
          <Link to="/login" style={{
            color: '#667eea',
            fontWeight: 600,
            textDecoration: 'none'
          }}>
            Sign in
          </Link>
        </p>
      </form>
    </div>
  );
}
