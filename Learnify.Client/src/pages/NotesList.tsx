import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import SmartUpload from '../components/Notes/SmartUpload';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';

interface Note {
  id: string;
  title: string;
  content: string;
  courseId: string;
  courseTitle?: string;
  createdAt: string;
}

interface CourseOption {
  id: string;
  title: string;
}

const primaryButtonStyle: React.CSSProperties = {
  background: 'linear-gradient(135deg, #6B46C1, #4299E1)',
  color: 'white',
  border: 'none',
  borderRadius: '10px',
  padding: '0.6rem 1.2rem',
  cursor: 'pointer',
  transition: 'all 0.2s ease',
  fontWeight: 600,
  boxShadow: '0 10px 24px rgba(66, 153, 225, 0.22)',
};

export default function NotesList() {
  const { isAuthenticated } = useAuthStore();
  const [notes, setNotes] = useState<Note[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [courses, setCourses] = useState<CourseOption[]>([]);
  const [creating, setCreating] = useState(false);
  const [uploadingText, setUploadingText] = useState(false);
  const [createForm, setCreateForm] = useState({ courseId: '', content: '' });
  const [uploadForm, setUploadForm] = useState<{ courseId: string; file: File | null }>({ courseId: '', file: null });
  const [createError, setCreateError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const fetchNotes = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await apiClient.get<any>('/api/notes');
      if (response.data.success) {
        setNotes(response.data.data);
      } else {
        setError(response.data.errors?.join(', ') || 'Failed to fetch notes');
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to fetch notes');
      }
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = async () => {
    setCreateError(null);

    try {
      const response = await apiClient.get<any>('/api/courses');
      if (response.data.success) {
        setCourses(response.data.data);
        setShowCreateModal(true);
      } else {
        setCreateError(response.data.errors?.join(', ') || 'Failed to load courses.');
      }
    } catch {
      setCreateError('Failed to load courses.');
    }
  };

  const openUploadModal = async () => {
    setCreateError(null);
    setUploadError(null);
    setUploadForm({ courseId: '', file: null });
    setShowUploadModal(true);

    try {
      const response = await apiClient.get<any>('/api/courses');
      if (response.data.success) {
        setCourses(response.data.data);
      } else {
        setUploadError(response.data.errors?.join(', ') || 'Failed to load courses.');
      }
    } catch {
      setUploadError('Failed to load courses.');
    }
  };

  const closeUploadModal = () => {
    setShowUploadModal(false);
    setUploadError(null);
    setUploadForm({ courseId: '', file: null });
  };

  const handleCreateNote = async () => {
    if (!createForm.courseId || !createForm.content.trim()) {
      setCreateError('Please select a course and enter content.');
      return;
    }

    setCreating(true);
    setCreateError(null);

    try {
      await apiClient.post('/api/notes', {
        courseId: createForm.courseId,
        content: createForm.content,
        attachments: [],
      });

      setShowCreateModal(false);
      setCreateForm({ courseId: '', content: '' });
      await fetchNotes();
    } catch {
      setCreateError('Failed to create note.');
    } finally {
      setCreating(false);
    }
  };

  const handleTextUpload = async () => {
    if (!uploadForm.courseId || !uploadForm.file) {
      setUploadError('Please select a course and a .txt or .md file.');
      return;
    }

    const fileName = uploadForm.file.name.toLowerCase();
    if (!fileName.endsWith('.txt') && !fileName.endsWith('.md')) {
      setUploadError('Only .txt and .md files are supported for direct upload.');
      return;
    }

    if (uploadForm.file.size > 2 * 1024 * 1024) {
      setUploadError('File too large. Maximum 2MB.');
      return;
    }

    try {
      setUploadingText(true);
      setUploadError(null);

      const formData = new FormData();
      formData.append('courseId', uploadForm.courseId);
      formData.append('file', uploadForm.file);

      await apiClient.post('/api/notes/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });

      setUploadForm({ courseId: '', file: null });
      setShowUploadModal(false);
      await fetchNotes();
    } catch (error: any) {
      setUploadError(error?.response?.data?.message || 'Failed to upload text note.');
    } finally {
      setUploadingText(false);
    }
  };

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    void fetchNotes();
  }, [isAuthenticated]);

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
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '1rem',
        marginBottom: '1.5rem',
      }}>
        <h1 style={{ margin: 0, color: '#333' }}>My Notes</h1>
        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', justifyContent: 'flex-end' }}>
          <button
            type="button"
            onClick={() => void openCreateModal()}
            style={primaryButtonStyle}
          >
            {'➕ New Note'}
          </button>
          <button
            type="button"
            onClick={() => void openUploadModal()}
            style={primaryButtonStyle}
          >
            Upload
          </button>
        </div>
      </div>

      {error && (
        <div style={{
          background: '#fee',
          border: '1px solid #fcc',
          borderRadius: '8px',
          padding: '1rem',
          color: '#c00',
        }}>
          {error}
        </div>
      )}

      {notes.length === 0 ? (
        <div style={{
          background: 'white',
          borderRadius: '12px',
          padding: '2rem',
          textAlign: 'center',
          boxShadow: '0 2px 10px rgba(0,0,0,0.05)',
        }}>
          <p style={{ color: '#666', fontSize: '1.1rem' }}>
            No notes found. Create your first note from a course page!
          </p>
          <button
            type="button"
            onClick={() => void openCreateModal()}
            style={{ ...primaryButtonStyle, marginTop: '1rem' }}
          >
            {'➕ New Note'}
          </button>
          <button
            type="button"
            onClick={() => void openUploadModal()}
            style={{ ...primaryButtonStyle, marginTop: '0.75rem' }}
          >
            Upload
          </button>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {notes.map((note) => (
            <Link
              to={`/notes/${note.id}`}
              key={note.id}
              style={{
                display: 'block',
                background: 'white',
                borderRadius: '12px',
                padding: '1.25rem',
                textDecoration: 'none',
                color: '#333',
                boxShadow: '0 2px 10px rgba(0,0,0,0.05)',
                border: '1px solid #eee',
              }}
            >
              <h3 style={{ margin: '0 0 0.5rem', color: '#667eea' }}>
                {note.title || 'Untitled Note'}
              </h3>
              <p style={{ margin: '0 0 0.5rem', color: '#666', fontSize: '0.9rem' }}>
                Course: {note.courseTitle || note.courseId}
              </p>
              <p style={{
                margin: 0,
                color: '#888',
                fontSize: '0.85rem',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}>
                {note.content}
              </p>
            </Link>
          ))}
        </div>
      )}

      {showCreateModal && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.5)',
          zIndex: 1000,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '1rem',
        }}>
          <div style={{
            background: 'white',
            borderRadius: '16px',
            padding: '2rem',
            maxWidth: '500px',
            width: '100%',
            boxShadow: '0 20px 60px rgba(0,0,0,0.3)',
          }}>
            <h2 style={{ marginTop: 0, marginBottom: '1rem', color: '#1A202C' }}>Create Note</h2>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <label style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
                  Course
                </label>
                <select
                  value={createForm.courseId}
                  onChange={(event) => setCreateForm((current) => ({ ...current, courseId: event.target.value }))}
                  style={{
                    width: '100%',
                    padding: '0.75rem',
                    border: '1px solid #E2E8F0',
                    borderRadius: '10px',
                    outline: 'none',
                  }}
                >
                  <option value="">Select a course</option>
                  {courses.map((course) => (
                    <option key={course.id} value={course.id}>
                      {course.title}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
                  Content
                </label>
                <textarea
                  value={createForm.content}
                  onChange={(event) => setCreateForm((current) => ({ ...current, content: event.target.value }))}
                  rows={6}
                  placeholder="Write your note here..."
                  style={{
                    width: '100%',
                    padding: '0.75rem',
                    border: '1px solid #E2E8F0',
                    borderRadius: '10px',
                    outline: 'none',
                    resize: 'vertical',
                  }}
                />
              </div>

              {createError && (
                <div style={{
                  padding: '0.75rem 1rem',
                  borderRadius: '10px',
                  background: '#FFF5F5',
                  color: '#C53030',
                  border: '1px solid #FED7D7',
                }}>
                  {createError}
                </div>
              )}

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateModal(false);
                    setCreateError(null);
                  }}
                  style={{
                    padding: '0.6rem 1.2rem',
                    borderRadius: '10px',
                    border: '1px solid #CBD5E0',
                    background: 'white',
                    color: '#4A5568',
                    cursor: 'pointer',
                  }}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={() => void handleCreateNote()}
                  disabled={creating}
                  style={{
                    ...primaryButtonStyle,
                    opacity: creating ? 0.7 : 1,
                    cursor: creating ? 'not-allowed' : 'pointer',
                  }}
                >
                  {creating ? 'Creating...' : 'Create Note'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showUploadModal && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.5)',
          zIndex: 1000,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '1rem',
        }}>
          <div style={{
            background: 'white',
            borderRadius: '16px',
            padding: '2rem',
            maxWidth: '600px',
            width: '100%',
            maxHeight: '90vh',
            overflowY: 'auto',
            boxShadow: '0 20px 60px rgba(0,0,0,0.3)',
          }}>
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              gap: '1rem',
              marginBottom: '1rem',
            }}>
              <div>
                <h2 style={{ margin: 0, color: '#1A202C' }}>Upload Notes</h2>
                <p style={{ margin: '0.35rem 0 0', color: '#718096' }}>
                  Save a text note directly, or let AI analyze an uploaded file for you.
                </p>
              </div>
              <button
                type="button"
                onClick={closeUploadModal}
                style={{
                  border: '1px solid #CBD5E0',
                  borderRadius: '10px',
                  background: 'white',
                  padding: '0.6rem 1rem',
                  color: '#4A5568',
                  cursor: 'pointer',
                }}
              >
                Close
              </button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <section>
                <h3 style={{ margin: '0 0 0.75rem', color: '#2D3748', fontSize: '1rem' }}>
                  Direct text upload
                </h3>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
                  <div>
                    <label style={{ display: 'block', marginBottom: '0.4rem', color: '#4A5568', fontWeight: 600 }}>
                      Course
                    </label>
                    <select
                      value={uploadForm.courseId}
                      onChange={(event) => setUploadForm((current) => ({ ...current, courseId: event.target.value }))}
                      disabled={courses.length === 0}
                      style={{
                        width: '100%',
                        padding: '0.75rem',
                        border: '1px solid #E2E8F0',
                        borderRadius: '10px',
                        outline: 'none',
                        background: courses.length === 0 ? '#F7FAFC' : 'white',
                      }}
                    >
                      <option value="">Select a course</option>
                      {courses.map((course) => (
                        <option key={course.id} value={course.id}>
                          {course.title}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label style={{ display: 'block', marginBottom: '0.4rem', color: '#4A5568', fontWeight: 600 }}>
                      Text or Markdown file
                    </label>
                    <input
                      type="file"
                      accept=".txt,.md,text/plain,text/markdown"
                      onChange={(event) => setUploadForm((current) => ({
                        ...current,
                        file: event.target.files?.[0] ?? null,
                      }))}
                      style={{
                        width: '100%',
                        padding: '0.75rem',
                        border: '1px solid #E2E8F0',
                        borderRadius: '10px',
                      }}
                    />
                    <p style={{ margin: '0.4rem 0 0', color: '#718096', fontSize: '0.85rem' }}>
                      Supports .txt and .md files up to 2MB.
                    </p>
                  </div>

                  {courses.length === 0 && (
                    <div style={{
                      padding: '0.75rem 1rem',
                      borderRadius: '10px',
                      background: '#FFFBEB',
                      color: '#92400E',
                      border: '1px solid #FDE68A',
                    }}>
                      Create a course before uploading a note.
                    </div>
                  )}

                  {uploadError && (
                    <div style={{
                      padding: '0.75rem 1rem',
                      borderRadius: '10px',
                      background: '#FFF5F5',
                      color: '#C53030',
                      border: '1px solid #FED7D7',
                    }}>
                      {uploadError}
                    </div>
                  )}

                  <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <button
                      type="button"
                      onClick={() => void handleTextUpload()}
                      disabled={uploadingText || courses.length === 0}
                      style={{
                        ...primaryButtonStyle,
                        opacity: uploadingText || courses.length === 0 ? 0.7 : 1,
                        cursor: uploadingText || courses.length === 0 ? 'not-allowed' : 'pointer',
                      }}
                    >
                      {uploadingText ? 'Uploading...' : 'Upload Text/Markdown'}
                    </button>
                  </div>
                </div>
              </section>

              <section style={{
                borderTop: '1px solid #E2E8F0',
                paddingTop: '1rem',
              }}>
                <h3 style={{ margin: '0 0 0.75rem', color: '#2D3748', fontSize: '1rem' }}>
                  AI upload and analysis
                </h3>
                <SmartUpload />
              </section>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
