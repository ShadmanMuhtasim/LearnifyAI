import { useEffect, useState } from 'react';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';

interface Course {
  id: string;
  title: string;
  description: string;
  userId: string;
  instructorName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  errors: string[];
  message?: string;
}

export default function Courses() {
  const { user, isAuthenticated } = useAuthStore();
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchCourses = async () => {
      try {
        const response = await apiClient.get<ApiResponse<Course[]>>('/api/courses');
        if (response.data.success) {
          setCourses(response.data.data);
        } else {
          setError(response.data.errors?.join(', ') || 'Failed to fetch courses');
        }
      } catch (err: unknown) {
        if (err instanceof Error) {
          setError(err.message);
        } else {
          setError('Failed to fetch courses');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchCourses();
  }, [isAuthenticated]);

  if (!isAuthenticated) {
    return null;
  }

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="container mt-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1>My Courses</h1>
        <span className="text-muted">Welcome, {user?.name}</span>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {courses.length === 0 ? (
        <div className="alert alert-info">
          No courses found. Start by enrolling in a course!
        </div>
      ) : (
        <div className="row">
          {courses.map((course) => (
            <div className="col-md-4 mb-4" key={course.id}>
              <div className="card h-100">
                <div className="card-body">
                  <h5 className="card-title">{course.title}</h5>
                  <p className="card-text">{course.description}</p>
                  {course.instructorName && (
                    <p className="text-muted">
                      Instructor: {course.instructorName}
                    </p>
                  )}
                  <small className="text-muted">
                    Created: {new Date(course.createdAt).toLocaleDateString()}
                  </small>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}