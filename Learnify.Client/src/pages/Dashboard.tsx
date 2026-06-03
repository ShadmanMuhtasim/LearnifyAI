import { useEffect, useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { Link } from 'react-router-dom';
import apiClient from '../services/api';

interface Course {
  id: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
}

export default function Dashboard() {
  const { user, isAuthenticated } = useAuthStore();
  const [courseCount, setCourseCount] = useState(0);

  useEffect(() => {
    if (!isAuthenticated) {
      setCourseCount(0);
      return;
    }

    let isMounted = true;

    const fetchCourseCount = async () => {
      try {
        const response = await apiClient.get<ApiResponse<Course[]>>('/api/courses');
        if (isMounted && response.data.success) {
          setCourseCount(response.data.data.length);
        }
      } catch {
        if (isMounted) {
          setCourseCount(0);
        }
      }
    };

    void fetchCourseCount();

    return () => {
      isMounted = false;
    };
  }, [isAuthenticated]);

  return (
    <div style={{
      minHeight: '100vh',
      background: 'linear-gradient(135deg, #f5f7fa 0%, #e8ecf1 100%)',
      padding: '2rem 1rem'
    }}>
      <div style={{ maxWidth: '1100px', margin: '0 auto' }}>
        {/* Welcome Header */}
        <div style={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          borderRadius: '16px',
          padding: '2.5rem',
          color: 'white',
          marginBottom: '2rem',
          boxShadow: '0 10px 40px rgba(102, 126, 234, 0.3)'
        }}>
          <h1 style={{ margin: 0, fontSize: '2rem', fontWeight: 700 }}>
            Welcome back, {user?.name || 'User'}! 👋
          </h1>
          <p style={{ margin: '0.5rem 0 0', opacity: 0.9, fontSize: '1.1rem' }}>
            Role: <span style={{
              display: 'inline-block',
              background: 'rgba(255,255,255,0.2)',
              padding: '0.25rem 0.75rem',
              borderRadius: '20px',
              fontSize: '0.9rem',
              fontWeight: 600
            }}>
              {user?.role || 'Student'}
            </span>
          </p>
        </div>

        {/* Stats Cards */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
          gap: '1.5rem',
          marginBottom: '2rem'
        }}>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '1.5rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            borderLeft: '4px solid #667eea'
          }}>
            <div style={{ color: '#666', fontSize: '0.875rem', fontWeight: 500, marginBottom: '0.5rem' }}>
              Total Courses
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 700, color: '#333' }}>
              {courseCount}
            </div>
          </div>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '1.5rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            borderLeft: '4px solid #764ba2'
          }}>
            <div style={{ color: '#666', fontSize: '0.875rem', fontWeight: 500, marginBottom: '0.5rem' }}>
              Total Notes
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 700, color: '#333' }}>
              0
            </div>
          </div>
          <div style={{
            background: 'white',
            borderRadius: '12px',
            padding: '1.5rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            borderLeft: '4px solid #f093fb'
          }}>
            <div style={{ color: '#666', fontSize: '0.875rem', fontWeight: 500, marginBottom: '0.5rem' }}>
              AI Features
            </div>
            <div style={{ fontSize: '2rem', fontWeight: 700, color: '#333' }}>
              4
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <h2 style={{ color: '#333', marginBottom: '1rem' }}>Quick Actions</h2>
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '1rem'
        }}>
          <Link to="/courses" style={{
            display: 'block',
            padding: '1.25rem',
            background: 'white',
            borderRadius: '12px',
            textDecoration: 'none',
            color: '#333',
            fontWeight: 600,
            fontSize: '1rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            transition: 'transform 0.2s, box-shadow 0.2s',
            textAlign: 'center'
          }}>
            📚 Browse Courses
          </Link>
          <Link to="/notes" style={{
            display: 'block',
            padding: '1.25rem',
            background: 'white',
            borderRadius: '12px',
            textDecoration: 'none',
            color: '#333',
            fontWeight: 600,
            fontSize: '1rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            transition: 'transform 0.2s, box-shadow 0.2s',
            textAlign: 'center'
          }}>
            📝 My Notes
          </Link>
          <Link to="/flashcards" style={{
            display: 'block',
            padding: '1.25rem',
            background: 'white',
            borderRadius: '12px',
            textDecoration: 'none',
            color: '#333',
            fontWeight: 600,
            fontSize: '1rem',
            boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            transition: 'transform 0.2s, box-shadow 0.2s',
            textAlign: 'center'
          }}>
            🤖 AI Tools
          </Link>
        </div>
      </div>
    </div>
  );
}
