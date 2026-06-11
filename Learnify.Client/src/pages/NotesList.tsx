import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import SmartUpload from '../components/Notes/SmartUpload';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';
import { AppButton, Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader } from '../components/UI/Primitives';

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

const FILE_ONLY_PDF_CONTENT =
  'This PDF was uploaded without text extraction. Use AI Analyze on a text-based PDF or upload .txt/.md content to generate AI study tools.';

export default function NotesList() {
  const { isAuthenticated } = useAuthStore();
  const [notes, setNotes] = useState<Note[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [courses, setCourses] = useState<CourseOption[]>([]);
  const [creating, setCreating] = useState(false);
  const [createForm, setCreateForm] = useState({ courseId: '', content: '' });
  const [createError, setCreateError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const [courseFilter, setCourseFilter] = useState('All notes');

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

  const loadCourses = async () => {
    const response = await apiClient.get<any>('/api/courses');
    if (response.data.success) {
      setCourses(response.data.data);
    }
    return response;
  };

  const openCreateModal = async () => {
    setCreateError(null);

    try {
      const response = await loadCourses();
      if (response.data.success) {
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
    setShowUploadModal(true);

    try {
      const response = await loadCourses();
      if (!response.data.success) {
        setUploadError(response.data.errors?.join(', ') || 'Failed to load courses.');
      }
    } catch {
      setUploadError('Failed to load courses.');
    }
  };

  const closeUploadModal = () => {
    setShowUploadModal(false);
    setUploadError(null);
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

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    void fetchNotes();
  }, [isAuthenticated]);

  const courseNames = useMemo(() => {
    const names = new Set(notes.map((note) => note.courseTitle || note.courseId).filter(Boolean));
    return ['All notes', ...Array.from(names)];
  }, [notes]);

  const visibleNotes = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return notes
      .filter((note) => courseFilter === 'All notes' || (note.courseTitle || note.courseId) === courseFilter)
      .filter((note) => {
        if (!normalizedQuery) return true;
        return `${note.title || ''} ${note.content} ${note.courseTitle || ''}`.toLowerCase().includes(normalizedQuery);
      });
  }, [courseFilter, notes, query]);

  if (!isAuthenticated) return null;

  if (loading) {
    return <LoadingState label="Loading notes..." />;
  }

  return (
    <div className="stack">
      <PageHeader
        title="Notes"
        subtitle="Upload, organize, and turn your study material into AI-powered practice."
        actions={(
          <>
            <AppButton type="button" variant="secondary" onClick={() => void openCreateModal()}>New Note</AppButton>
            <AppButton type="button" onClick={() => void openUploadModal()}>Upload</AppButton>
          </>
        )}
      />

      {error && <ErrorState message={error} />}

      <div className="page-two-column" style={{ gridTemplateColumns: '250px minmax(0, 1fr)' }}>
        <Card className="stack">
          <div>
            <div className="eyebrow mb-3">Folders</div>
            <div className="stack" style={{ gap: 8 }}>
              {courseNames.map((name) => (
                <button
                  key={name}
                  type="button"
                  className={`ui-button ${courseFilter === name ? 'ui-button-primary' : 'ui-button-ghost'}`}
                  onClick={() => setCourseFilter(name)}
                >
                  {name}
                </button>
              ))}
            </div>
          </div>
          <div>
            <div className="eyebrow mb-3">Tags</div>
            <div className="cluster">
              <Badge tone="primary">AI Ready</Badge>
              <Badge tone="warning">Uploads</Badge>
              <Badge tone="muted">PDF partial</Badge>
            </div>
          </div>
        </Card>

        <div className="stack">
          <Card className="split">
            <label style={{ flex: 1 }}>
              <span className="form-label">Search notes</span>
              <input
                type="text"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search title, content, or course"
              />
            </label>
            <div className="ui-card" style={{ borderStyle: 'dashed', textAlign: 'center', minWidth: 260 }}>
              <div className="eyebrow">Upload Dropzone</div>
              <p className="muted text-small mb-3">Supports .txt, .md, text-based PDF analysis, and simple PDF attachments.</p>
              <AppButton type="button" onClick={() => void openUploadModal()}>Choose File</AppButton>
            </div>
          </Card>

          {notes.length === 0 ? (
            <EmptyState
              title="No notes found"
              message="Create a note or upload a text/Markdown file to start studying."
              action={<AppButton type="button" onClick={() => void openCreateModal()}>New Note</AppButton>}
            />
          ) : visibleNotes.length === 0 ? (
            <EmptyState
              title="No matching notes"
              message="Try a different search term or folder."
              action={<AppButton type="button" variant="secondary" onClick={() => { setQuery(''); setCourseFilter('All notes'); }}>Clear filters</AppButton>}
            />
          ) : (
            <div className="grid grid-2">
              {visibleNotes.map((note) => {
                const isFileOnlyPdf = note.content.trim() === FILE_ONLY_PDF_CONTENT;
                const hasAiContent = note.content.trim().length > 0 && !isFileOnlyPdf;
                return (
                  <Link to={`/notes/${note.id}`} key={note.id} style={{ textDecoration: 'none' }}>
                    <Card className="stack">
                      <div className="split">
                        <h2 style={{ fontSize: '1.1rem' }}>{note.title || 'Untitled Note'}</h2>
                        <Badge tone={hasAiContent ? 'success' : 'muted'}>{hasAiContent ? 'AI Ready' : isFileOnlyPdf ? 'PDF attached' : 'Empty'}</Badge>
                      </div>
                      <p className="muted text-small">Course: {note.courseTitle || note.courseId}</p>
                      <p className="muted">
                        {note.content.slice(0, 170)}
                        {note.content.length > 170 ? '...' : ''}
                      </p>
                      <span className="text-small muted">
                        Created {new Date(note.createdAt).toLocaleDateString()}
                      </span>
                    </Card>
                  </Link>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {showCreateModal && (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="create-note-title">
            <h2 id="create-note-title" className="mb-3">Create Note</h2>

            <div className="field-grid">
              <div>
                <label htmlFor="note-course" className="form-label">Course</label>
                <select
                  id="note-course"
                  value={createForm.courseId}
                  onChange={(event) => setCreateForm((current) => ({ ...current, courseId: event.target.value }))}
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
                <label htmlFor="note-content" className="form-label">Content</label>
                <textarea
                  id="note-content"
                  value={createForm.content}
                  onChange={(event) => setCreateForm((current) => ({ ...current, content: event.target.value }))}
                  rows={7}
                  placeholder="Write your note here..."
                />
              </div>

              {createError && <ErrorState message={createError} />}

              <div className="cluster" style={{ justifyContent: 'flex-end' }}>
                <AppButton type="button" variant="secondary" onClick={() => { setShowCreateModal(false); setCreateError(null); }}>
                  Cancel
                </AppButton>
                <AppButton type="button" onClick={() => void handleCreateNote()} disabled={creating}>
                  {creating ? 'Creating...' : 'Create Note'}
                </AppButton>
              </div>
            </div>
          </div>
        </div>
      )}

      {showUploadModal && (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="upload-note-title" style={{ width: 'min(720px, 100%)' }}>
            <div className="split mb-4">
              <div>
                <h2 id="upload-note-title">Upload Notes</h2>
                <p className="muted">Choose one file and decide whether to save, extract text, or run AI analysis.</p>
              </div>
              <AppButton type="button" variant="secondary" onClick={closeUploadModal}>Close</AppButton>
            </div>

            <div className="stack">
              <Card className="stack">
                <h3>Unified upload</h3>
                <p className="muted text-small">
                  Save-only never calls AI. Extract text works locally for readable files. Analyze existing text uses AI only after extraction succeeds.
                </p>
                {uploadError && <ErrorState message={uploadError} />}
                <SmartUpload courses={courses} onUploaded={fetchNotes} />
              </Card>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
