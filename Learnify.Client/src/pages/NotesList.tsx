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

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
  return apiError.response?.data?.message ?? apiError.response?.data?.errors?.[0] ?? apiError.message ?? fallback;
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
  const [uploadingPdf, setUploadingPdf] = useState(false);
  const [createForm, setCreateForm] = useState({ courseId: '', content: '' });
  const [uploadForm, setUploadForm] = useState<{ courseId: string; file: File | null }>({ courseId: '', file: null });
  const [pdfUploadForm, setPdfUploadForm] = useState<{ courseId: string; file: File | null }>({ courseId: '', file: null });
  const [createError, setCreateError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [pdfUploadError, setPdfUploadError] = useState<string | null>(null);
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
    setPdfUploadError(null);
    setUploadForm({ courseId: '', file: null });
    setPdfUploadForm({ courseId: '', file: null });
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
    setPdfUploadError(null);
    setUploadForm({ courseId: '', file: null });
    setPdfUploadForm({ courseId: '', file: null });
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
    } catch (uploadErrorResponse: unknown) {
      setUploadError(getApiErrorMessage(uploadErrorResponse, 'Failed to upload text note.'));
    } finally {
      setUploadingText(false);
    }
  };

  const handlePdfUpload = async () => {
    if (!pdfUploadForm.courseId || !pdfUploadForm.file) {
      setPdfUploadError('Please select a course and a PDF file.');
      return;
    }

    const fileName = pdfUploadForm.file.name.toLowerCase();
    if (!fileName.endsWith('.pdf')) {
      setPdfUploadError('Only PDF files are supported for simple file upload.');
      return;
    }

    if (pdfUploadForm.file.size > 5 * 1024 * 1024) {
      setPdfUploadError('File too large. Maximum 5MB.');
      return;
    }

    try {
      setUploadingPdf(true);
      setPdfUploadError(null);

      const formData = new FormData();
      formData.append('courseId', pdfUploadForm.courseId);
      formData.append('file', pdfUploadForm.file);

      await apiClient.post('/api/notes/upload-file', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });

      setPdfUploadForm({ courseId: '', file: null });
      setShowUploadModal(false);
      await fetchNotes();
    } catch (uploadErrorResponse: unknown) {
      setPdfUploadError(getApiErrorMessage(uploadErrorResponse, 'Failed to upload PDF.'));
    } finally {
      setUploadingPdf(false);
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
                <p className="muted">Save a text note directly, or let AI analyze an uploaded file.</p>
              </div>
              <AppButton type="button" variant="secondary" onClick={closeUploadModal}>Close</AppButton>
            </div>

            <div className="stack">
              <Card className="stack">
                <h3>Direct text upload</h3>
                <div>
                  <label htmlFor="upload-course" className="form-label">Course</label>
                  <select
                    id="upload-course"
                    value={uploadForm.courseId}
                    onChange={(event) => setUploadForm((current) => ({ ...current, courseId: event.target.value }))}
                    disabled={courses.length === 0}
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
                  <label htmlFor="upload-file" className="form-label">Text or Markdown file</label>
                  <input
                    id="upload-file"
                    type="file"
                    accept=".txt,.md,text/plain,text/markdown"
                    onChange={(event) => setUploadForm((current) => ({
                      ...current,
                      file: event.target.files?.[0] ?? null,
                    }))}
                  />
                  <p className="muted text-small mt-3">Supports .txt and .md files up to 2MB.</p>
                </div>

                {courses.length === 0 && (
                  <div className="alert alert-warning">Create a course before uploading a note.</div>
                )}

                {uploadError && <ErrorState message={uploadError} />}

                <div className="cluster" style={{ justifyContent: 'flex-end' }}>
                  <AppButton
                    type="button"
                    onClick={() => void handleTextUpload()}
                    disabled={uploadingText || courses.length === 0}
                  >
                    {uploadingText ? 'Uploading...' : 'Upload Text/Markdown'}
                  </AppButton>
                </div>
              </Card>

              <Card className="stack">
                <h3>Simple PDF Upload</h3>
                <p className="muted">Save a PDF to your notes without AI analysis.</p>
                <p className="muted text-small">AI summary, flashcards, quizzes, and study tips require readable extracted text.</p>

                <div>
                  <label htmlFor="pdf-upload-course" className="form-label">Course</label>
                  <select
                    id="pdf-upload-course"
                    value={pdfUploadForm.courseId}
                    onChange={(event) => setPdfUploadForm((current) => ({ ...current, courseId: event.target.value }))}
                    disabled={courses.length === 0}
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
                  <label htmlFor="pdf-upload-file" className="form-label">PDF file</label>
                  <input
                    id="pdf-upload-file"
                    type="file"
                    accept=".pdf,application/pdf"
                    onChange={(event) => setPdfUploadForm((current) => ({
                      ...current,
                      file: event.target.files?.[0] ?? null,
                    }))}
                  />
                  <p className="muted text-small mt-3">Stores the PDF as an attachment up to 5MB. It does not call AI.</p>
                </div>

                {pdfUploadError && <ErrorState message={pdfUploadError} />}

                <div className="cluster" style={{ justifyContent: 'flex-end' }}>
                  <AppButton
                    type="button"
                    onClick={() => void handlePdfUpload()}
                    disabled={uploadingPdf || courses.length === 0}
                  >
                    {uploadingPdf ? 'Uploading...' : 'Save PDF Without AI'}
                  </AppButton>
                </div>
              </Card>

              <Card className="stack">
                <h3>AI upload and analysis</h3>
                <SmartUpload />
              </Card>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
