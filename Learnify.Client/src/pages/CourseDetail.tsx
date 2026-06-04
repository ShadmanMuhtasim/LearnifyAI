import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import axios from 'axios';
import { Badge, Card, ErrorState, LoadingState, PageHeader, StatCard } from '../components/UI/Primitives';
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
    return <LoadingState label="Loading course..." />;
  }

  if (unauthorized) {
    return (
      <div className="stack">
        <ErrorState message="You are not allowed to view this course." />
        <Link to="/courses" className="btn btn-outline-primary">Back to Courses</Link>
      </div>
    );
  }

  if (notFound || !course) {
    return (
      <div className="stack">
        <div className="alert alert-info">Course not found.</div>
        <Link to="/courses" className="btn btn-outline-primary">Back to Courses</Link>
      </div>
    );
  }

  return (
    <div className="stack">
      <Link to="/courses" className="btn btn-link" style={{ justifySelf: 'start' }}>Back to Courses</Link>

      {error && <ErrorState message={error} />}

      <PageHeader
        eyebrow="Course"
        title={course.title}
        subtitle={course.description || 'No description added yet.'}
        actions={<Badge tone="success">Active</Badge>}
      />

      <div className="grid grid-3">
        <StatCard label="Created" value={new Date(course.createdAt).toLocaleDateString()} detail="course record" />
        <StatCard label="Lessons" value="Open" detail="available from lesson view" tone="primary" />
        <StatCard label="AI Actions" value="Notes" detail="generate quizzes from note detail" tone="success" />
      </div>

      <Card className="stack">
        <h2>Learning Path</h2>
        <p className="muted">
          Use lessons and notes to build study material, then generate summaries, flashcards, and quizzes from your saved notes.
        </p>
        <div className="cluster">
          <Link to={`/courses/${course.id}/lessons`} className="btn btn-primary">
            View Lessons
          </Link>
          <Link to="/notes" className="btn btn-outline-secondary">
            View Notes
          </Link>
        </div>
      </Card>
    </div>
  );
}
