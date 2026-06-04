import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';

interface Lesson {
  id: string;
  title: string;
  content: string;
  courseId: string;
  courseTitle?: string;
  order?: number;
  createdAt: string;
}

export default function Lessons() {
  const { courseId } = useParams<{ courseId: string }>();
  const { isAuthenticated } = useAuthStore();
  const [lessons, setLessons] = useState<Lesson[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchLessons = async () => {
      try {
        const response = await apiClient.get<any>(`/api/courses/${courseId}/lessons`);
        if (response.data.success) {
          setLessons(response.data.data);
        } else {
          setError(response.data.errors?.join(', ') || 'Failed to fetch lessons');
        }
      } catch (err: unknown) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError('Failed to fetch lessons');
        }
      } finally {
        setLoading(false);
      }
    };

    if (courseId) fetchLessons();
  }, [courseId]);

  if (!isAuthenticated) return null;

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '2rem 1rem' }}>
      <Link to="/courses" style={{
        display: 'inline-block',
        color: '#667eea',
        textDecoration: 'none',
        marginBottom: '1rem',
        fontWeight: 500
      }}>
        ← Back to Courses
      </Link>

      <h1 style={{ marginBottom: '1.5rem', color: '#333' }}>📚 Course Lessons</h1>

      {error && (
        <div style={{
          background: '#fee',
          border: '1px solid #fcc',
          borderRadius: '8px',
          padding: '1rem',
          color: '#c00',
          marginBottom: '1rem'
        }}>
          {error}
        </div>
      )}

      {lessons.length === 0 ? (
        <div style={{
          background: 'white',
          borderRadius: '12px',
          padding: '2rem',
          textAlign: 'center',
          boxShadow: '0 2px 10px rgba(0,0,0,0.05)'
        }}>
          <p style={{ color: '#666', fontSize: '1.1rem' }}>
            No lessons available for this course yet.
          </p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {lessons.map((lesson) => (
            <Link
              to={`/courses/${courseId}/lessons/${lesson.id}`}
              key={lesson.id}
              style={{
                display: 'block',
                background: 'white',
                borderRadius: '12px',
                padding: '1.25rem',
                textDecoration: 'none',
                color: '#333',
                boxShadow: '0 2px 10px rgba(0,0,0,0.05)',
                border: '1px solid #eee'
              }}
            >
              <h3 style={{ margin: '0 0 0.5rem', color: '#667eea' }}>
                {lesson.order && <span style={{ color: '#888', marginRight: '0.5rem' }}>#{lesson.order}</span>}
                {lesson.title}
              </h3>
              <p style={{ margin: 0, color: '#888', fontSize: '0.85rem' }}>
                Created: {new Date(lesson.createdAt).toLocaleDateString()}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}