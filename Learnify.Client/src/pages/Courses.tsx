import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { useAuthStore } from '../store/authStore';
import { courseService, type Course, type CreateCourseDto, type UpdateCourseDto } from '../services/courseService';
import { AppButton, Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader } from '../components/UI/Primitives';

interface ApiErrorPayload {
  errors?: string[];
  message?: string;
}

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

function courseTag(course: Course) {
  const text = `${course.title} ${course.description || ''}`.toLowerCase();
  if (text.includes('science') || text.includes('biology') || text.includes('physics')) return 'Science';
  if (text.includes('code') || text.includes('computer') || text.includes('data')) return 'Computer Science';
  if (text.includes('economic') || text.includes('market')) return 'Economics';
  return 'General';
}

export default function Courses() {
  const { isAuthenticated } = useAuthStore();
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
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('All Subjects');

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

  const filters = useMemo(() => {
    const tags = new Set(courses.map(courseTag));
    return ['All Subjects', ...Array.from(tags)];
  }, [courses]);

  const visibleCourses = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return courses
      .filter((course) => filter === 'All Subjects' || courseTag(course) === filter)
      .filter((course) => {
        if (!normalizedQuery) return true;
        return `${course.title} ${course.description || ''}`.toLowerCase().includes(normalizedQuery);
      });
  }, [courses, filter, query]);

  if (!isAuthenticated) {
    return null;
  }

  if (loading) {
    return <LoadingState label="Loading courses..." />;
  }

  return (
    <div className="stack">
      <PageHeader
        title="Your Courses"
        subtitle="Manage and track your active learning paths."
        actions={<AppButton type="button" onClick={openCreateModal}>New Course</AppButton>}
      />

      {error && <ErrorState message={error} />}

      <Card>
        <div className="split">
          <div className="cluster">
            {filters.map((item) => (
              <button
                type="button"
                key={item}
                className={`ui-button ${filter === item ? 'ui-button-primary' : 'ui-button-ghost'}`}
                onClick={() => setFilter(item)}
              >
                {item}
              </button>
            ))}
          </div>
          <label style={{ minWidth: 260 }}>
            <span className="form-label">Search courses</span>
            <input
              type="text"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search title or description"
            />
          </label>
        </div>
      </Card>

      {courses.length === 0 ? (
        <EmptyState
          title="No Courses Yet"
          message="You have no courses yet. Click 'New Course' to get started."
          action={<AppButton type="button" onClick={openCreateModal}>New Course</AppButton>}
        />
      ) : visibleCourses.length === 0 ? (
        <EmptyState
          title="No matching courses"
          message="Try a different search term or subject filter."
          action={<AppButton type="button" variant="secondary" onClick={() => { setQuery(''); setFilter('All Subjects'); }}>Clear filters</AppButton>}
        />
      ) : (
        <div className="grid grid-3">
          {visibleCourses.map((course) => (
            <Card className="course-card" key={course.id}>
              <div className="course-art">
                <Badge tone="primary">{courseTag(course)}</Badge>
              </div>
              <div className="course-body">
                <Link to={`/courses/${course.id}`} aria-label={`Open ${course.title}`}>
                  <h2>{course.title}</h2>
                </Link>
                <p className="muted">
                  {course.description || 'No description added yet.'}
                </p>
                <div className="split">
                  <span className="text-small muted">
                    Created {new Date(course.createdAt).toLocaleDateString()}
                  </span>
                  <Badge tone="success">Active</Badge>
                </div>
                <div className="cluster">
                  <AppButton type="button" variant="secondary" onClick={() => openEditModal(course)}>
                    Edit
                  </AppButton>
                  <AppButton
                    type="button"
                    variant="danger"
                    disabled={deletingCourseId === course.id}
                    onClick={() => void handleDeleteCourse(course)}
                  >
                    {deletingCourseId === course.id ? 'Deleting...' : 'Delete'}
                  </AppButton>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {showCreateModal && (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="create-course-title">
            <h2 id="create-course-title" className="mb-3">New Course</h2>

            <form onSubmit={(event) => void handleCreateCourse(event)} className="field-grid">
              <div>
                <label htmlFor="course-title" className="form-label">
                  Course Title
                </label>
                <input
                  id="course-title"
                  type="text"
                  maxLength={100}
                  required
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                />
              </div>

              <div>
                <label htmlFor="course-description" className="form-label">
                  Description
                </label>
                <textarea
                  id="course-description"
                  maxLength={500}
                  rows={4}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              </div>

              {createError && <ErrorState message={createError} />}

              <div className="cluster" style={{ justifyContent: 'flex-end' }}>
                <AppButton type="button" variant="secondary" disabled={creating} onClick={resetCreateForm}>
                  Cancel
                </AppButton>
                <AppButton type="submit" disabled={creating}>
                  {creating ? 'Creating...' : 'Create Course'}
                </AppButton>
              </div>
            </form>
          </div>
        </div>
      )}

      {editingCourse && (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="edit-course-title">
            <h2 id="edit-course-title" className="mb-3">Edit Course</h2>

            <form onSubmit={(event) => void handleUpdateCourse(event)} className="field-grid">
              <div>
                <label htmlFor="edit-course-title-input" className="form-label">
                  Course Title
                </label>
                <input
                  id="edit-course-title-input"
                  type="text"
                  maxLength={100}
                  required
                  value={editTitle}
                  onChange={(event) => setEditTitle(event.target.value)}
                />
              </div>

              <div>
                <label htmlFor="edit-course-description" className="form-label">
                  Description
                </label>
                <textarea
                  id="edit-course-description"
                  maxLength={500}
                  rows={4}
                  value={editDescription}
                  onChange={(event) => setEditDescription(event.target.value)}
                />
              </div>

              {editError && <ErrorState message={editError} />}

              <div className="cluster" style={{ justifyContent: 'flex-end' }}>
                <AppButton type="button" variant="secondary" disabled={updating} onClick={resetEditForm}>
                  Cancel
                </AppButton>
                <AppButton type="submit" disabled={updating}>
                  {updating ? 'Saving...' : 'Save Changes'}
                </AppButton>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
