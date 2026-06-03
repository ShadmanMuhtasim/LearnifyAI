import { type CSSProperties, type FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { useAuthStore } from '../store/authStore';
import { courseService, type Course, type CreateCourseDto, type UpdateCourseDto } from '../services/courseService';

interface ApiErrorPayload {
  errors?: string[];
  message?: string;
}

const primaryButtonStyle: CSSProperties = {
  background: 'linear-gradient(135deg, #2563EB, #16A34A)',
  color: 'white',
  border: 'none',
  borderRadius: '8px',
  padding: '0.6rem 1rem',
  cursor: 'pointer',
  fontWeight: 600,
  boxShadow: '0 10px 22px rgba(37, 99, 235, 0.18)',
};

const modalBackdropStyle: CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(15, 23, 42, 0.55)',
  zIndex: 1000,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  padding: '1rem',
};

const modalPanelStyle: CSSProperties = {
  background: 'white',
  borderRadius: '8px',
  padding: '1.5rem',
  maxWidth: '520px',
  width: '100%',
  boxShadow: '0 24px 70px rgba(15, 23, 42, 0.28)',
};

function isApiErrorPayload(value: unknown): value is ApiErrorPayload {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const payload = value as Record<string, unknown>;
  return (
    (payload.errors === undefined || Array.isArray(payload.errors)) &&
    (payload.message === undefined || typeof payload.message === 'string')
  );
}

function getApiError(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data: unknown = error.response.data;
    if (!isApiErrorPayload(data)) {
      return fallback;
    }

    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors[0];
    }

    if (data.message) {
      return data.message;
    }
  }

  return error instanceof Error ? error.message : fallback;
}

export default function Courses() {
  const { user, isAuthenticated } = useAuthStore();
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [updating, setUpdating] = useState(false);
  const [deletingCourseId, setDeletingCourseId] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [createError, setCreateError] = useState<string | null>(null);
  const [editError, setEditError] = useState<string | null>(null);

  const fetchCourses = async () => {
    try {
      setLoading(true);
      setError(null);
      const courseList = await courseService.getAll();
      setCourses(courseList);
    } catch (err: unknown) {
      setError(getApiError(err, 'Failed to fetch courses'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    void fetchCourses();
  }, [isAuthenticated]);

  const resetCreateForm = () => {
    setTitle('');
    setDescription('');
    setCreateError(null);
    setShowCreateModal(false);
  };

  const openCreateModal = () => {
    setTitle('');
    setDescription('');
    setCreateError(null);
    setShowCreateModal(true);
  };

  const openEditModal = (course: Course) => {
    setEditingCourse(course);
    setEditTitle(course.title);
    setEditDescription(course.description || '');
    setEditError(null);
  };

  const resetEditForm = () => {
    setEditingCourse(null);
    setEditTitle('');
    setEditDescription('');
    setEditError(null);
  };

  const handleCreateCourse = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!title.trim()) {
      setCreateError('Course title is required.');
      return;
    }

    try {
      setCreating(true);
      setCreateError(null);

      const dto: CreateCourseDto = {
        title: title.trim(),
      };

      const trimmedDescription = description.trim();
      if (trimmedDescription) {
        dto.description = trimmedDescription;
      }

      const createdCourse = await courseService.create(dto);
      setCourses((currentCourses) => [createdCourse, ...currentCourses]);
      resetCreateForm();
    } catch (err: unknown) {
      setCreateError(getApiError(err, 'Failed to create course'));
    } finally {
      setCreating(false);
    }
  };

  const handleUpdateCourse = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!editingCourse) {
      return;
    }

    if (!editTitle.trim()) {
      setEditError('Course title is required.');
      return;
    }

    try {
      setUpdating(true);
      setEditError(null);

      const dto: UpdateCourseDto = {
        title: editTitle.trim(),
        description: editDescription.trim(),
      };

      const updatedCourse = await courseService.update(editingCourse.id, dto);
      setCourses((currentCourses) =>
        currentCourses.map((course) => (course.id === updatedCourse.id ? updatedCourse : course))
      );
      resetEditForm();
    } catch (err: unknown) {
      setEditError(getApiError(err, 'Failed to update course'));
    } finally {
      setUpdating(false);
    }
  };

  const handleDeleteCourse = async (course: Course) => {
    const shouldDelete = window.confirm(`Delete "${course.title}"? This action cannot be undone.`);
    if (!shouldDelete) {
      return;
    }

    try {
      setDeletingCourseId(course.id);
      setError(null);
      await courseService.delete(course.id);
      setCourses((currentCourses) => currentCourses.filter((currentCourse) => currentCourse.id !== course.id));
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        window.alert("You don't have permission to delete this course.");
        return;
      }

      setError(getApiError(err, 'Failed to delete course'));
    } finally {
      setDeletingCourseId(null);
    }
  };

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
        <div>
          <h1>My Courses</h1>
          <span className="text-muted">Welcome, {user?.name}</span>
        </div>
        <button
          type="button"
          style={primaryButtonStyle}
          onClick={openCreateModal}
        >
          New Course
        </button>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {courses.length === 0 ? (
        <div className="alert alert-info">
          <h2 className="h4">No Courses Yet</h2>
          <p>You have no courses yet. Click 'New Course' to get started.</p>
          <button
            type="button"
            style={primaryButtonStyle}
            onClick={openCreateModal}
          >
            New Course
          </button>
        </div>
      ) : (
        <div className="row">
          {courses.map((course) => (
            <div className="col-md-4 mb-4" key={course.id}>
              <div className="card h-100">
                <div className="card-body d-flex flex-column">
                  <Link
                    to={`/courses/${course.id}`}
                    style={{ color: 'inherit', textDecoration: 'none' }}
                    aria-label={`Open ${course.title}`}
                  >
                    <h5 className="card-title">{course.title}</h5>
                    {course.description && <p className="card-text">{course.description}</p>}
                  </Link>
                  <small className="text-muted mt-auto">
                    Created: {new Date(course.createdAt).toLocaleDateString()}
                  </small>
                  <button
                    type="button"
                    className="btn btn-outline-primary mt-3"
                    onClick={() => openEditModal(course)}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-danger mt-3"
                    disabled={deletingCourseId === course.id}
                    onClick={() => void handleDeleteCourse(course)}
                  >
                    {deletingCourseId === course.id ? 'Deleting...' : 'Delete'}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {showCreateModal && (
        <div style={modalBackdropStyle} role="presentation">
          <div style={modalPanelStyle} role="dialog" aria-modal="true" aria-labelledby="create-course-title">
            <h2 id="create-course-title" className="h4 mb-3">
              New Course
            </h2>

            <form onSubmit={(event) => void handleCreateCourse(event)}>
              <div className="mb-3">
                <label htmlFor="course-title" className="form-label">
                  Course Title
                </label>
                <input
                  id="course-title"
                  type="text"
                  className="form-control"
                  maxLength={100}
                  required
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                />
              </div>

              <div className="mb-3">
                <label htmlFor="course-description" className="form-label">
                  Description
                </label>
                <textarea
                  id="course-description"
                  className="form-control"
                  maxLength={500}
                  rows={4}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              </div>

              {createError && (
                <div className="alert alert-danger" role="alert">
                  {createError}
                </div>
              )}

              <div className="d-flex justify-content-end gap-2">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  disabled={creating}
                  onClick={resetCreateForm}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  style={{
                    ...primaryButtonStyle,
                    opacity: creating ? 0.75 : 1,
                    cursor: creating ? 'not-allowed' : 'pointer',
                  }}
                  disabled={creating}
                >
                  {creating && (
                    <span
                      className="spinner-border spinner-border-sm me-2"
                      role="status"
                      aria-hidden="true"
                    />
                  )}
                  {creating ? 'Creating...' : 'Create Course'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editingCourse && (
        <div style={modalBackdropStyle} role="presentation">
          <div style={modalPanelStyle} role="dialog" aria-modal="true" aria-labelledby="edit-course-title">
            <h2 id="edit-course-title" className="h4 mb-3">
              Edit Course
            </h2>

            <form onSubmit={(event) => void handleUpdateCourse(event)}>
              <div className="mb-3">
                <label htmlFor="edit-course-title-input" className="form-label">
                  Course Title
                </label>
                <input
                  id="edit-course-title-input"
                  type="text"
                  className="form-control"
                  maxLength={100}
                  required
                  value={editTitle}
                  onChange={(event) => setEditTitle(event.target.value)}
                />
              </div>

              <div className="mb-3">
                <label htmlFor="edit-course-description" className="form-label">
                  Description
                </label>
                <textarea
                  id="edit-course-description"
                  className="form-control"
                  maxLength={500}
                  rows={4}
                  value={editDescription}
                  onChange={(event) => setEditDescription(event.target.value)}
                />
              </div>

              {editError && (
                <div className="alert alert-danger" role="alert">
                  {editError}
                </div>
              )}

              <div className="d-flex justify-content-end gap-2">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  disabled={updating}
                  onClick={resetEditForm}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  style={{
                    ...primaryButtonStyle,
                    opacity: updating ? 0.75 : 1,
                    cursor: updating ? 'not-allowed' : 'pointer',
                  }}
                  disabled={updating}
                >
                  {updating && (
                    <span
                      className="spinner-border spinner-border-sm me-2"
                      role="status"
                      aria-hidden="true"
                    />
                  )}
                  {updating ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
