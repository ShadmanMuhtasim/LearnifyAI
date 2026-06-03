import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import axios from 'axios';
import { courseService, type Course } from '../services/courseService';

export default function CourseDetail() {
  const { id } = useParams<{ id: string }>();
  const [course, setCourse] = useState<Course | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);

  useEffect(() => {
    const loadCourse = async () => {
      if (!id) {
        setNotFound(true);
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        setNotFound(false);
        setUnauthorized(false);
        const result = await courseService.getById(id);
        setCourse(result);
      } catch (err: unknown) {
        if (axios.isAxiosError(err)) {
          if (err.response?.status === 401 || err.response?.status === 403) {
            setUnauthorized(true);
            return;
          }

          if (err.response?.status === 404) {
            setNotFound(true);
            return;
          }
        }

        setError(err instanceof Error ? err.message : 'Failed to load course.');
      } finally {
        setLoading(false);
      }
    };

    void loadCourse();
  }, [id]);

  if (loading) {
    return (
      <div className="container mt-4">
        <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '50vh' }}>
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
        </div>
      </div>
    );
  }

  if (unauthorized) {
    return (
      <div className="container mt-4">
        <div className="alert alert-warning">You are not allowed to view this course.</div>
        <Link to="/courses" className="btn btn-outline-primary">Back to Courses</Link>
      </div>
    );
  }

  if (notFound || !course) {
    return (
      <div className="container mt-4">
        <div className="alert alert-info">Course not found.</div>
        <Link to="/courses" className="btn btn-outline-primary">Back to Courses</Link>
      </div>
    );
  }

  return (
    <div className="container mt-4">
      <Link to="/courses" className="btn btn-link px-0">Back to Courses</Link>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      <div className="card">
        <div className="card-body">
          <h1 className="card-title h2">{course.title}</h1>
          <p className="card-text text-muted">
            Created: {new Date(course.createdAt).toLocaleDateString()}
          </p>
          {course.description ? (
            <p className="card-text">{course.description}</p>
          ) : (
            <p className="card-text text-muted">No description added yet.</p>
          )}

          <div className="d-flex gap-2 flex-wrap mt-4">
            <Link to={`/courses/${course.id}/lessons`} className="btn btn-primary">
              View Lessons
            </Link>
            <Link to="/notes" className="btn btn-outline-secondary">
              View Notes
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
