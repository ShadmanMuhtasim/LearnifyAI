import { useRef, useState, type CSSProperties } from 'react';
import { useNavigate } from 'react-router-dom';
import { AppButton, Badge, ErrorState } from '../UI/Primitives';
import { uploadMaterial, type UploadMaterialResponse, type UploadMode } from '../../services/noteUploadService';

type CourseOption = {
  id: string;
  title: string;
};

type SmartUploadProps = {
  courses: CourseOption[];
  onUploaded?: () => void | Promise<void>;
};

const uploadZoneStyle: CSSProperties = {
  border: '2px dashed var(--border)',
  borderRadius: '12px',
  padding: '2rem',
  textAlign: 'center',
  cursor: 'pointer',
  background: 'var(--surface-subtle)',
  transition: 'all 0.2s ease',
};

const modeCopy: Record<UploadMode, { title: string; description: string }> = {
  SaveOnly: {
    title: 'Save only',
    description: 'Store the file now. Text and AI can be generated later from the note.',
  },
  ExtractAndSave: {
    title: 'Extract text',
    description: 'Recover readable text from .txt, .md, .docx, or text-based PDF files.',
  },
  AiAnalyzeAndSave: {
    title: 'Analyze with AI',
    description: 'Extract text first, then generate a saved AI analysis from that text.',
  },
};

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
  return apiError.response?.data?.message ?? apiError.response?.data?.errors?.[0] ?? apiError.message ?? fallback;
};

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
};

const isAllowedFile = (file: File): boolean => {
  const fileName = file.name.toLowerCase();
  return fileName.endsWith('.txt') || fileName.endsWith('.md') || fileName.endsWith('.pdf') || fileName.endsWith('.docx');
};

export default function SmartUpload({ courses, onUploaded }: SmartUploadProps) {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [courseId, setCourseId] = useState('');
  const [title, setTitle] = useState('');
  const [mode, setMode] = useState<UploadMode>('SaveOnly');
  const [isDragging, setIsDragging] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<UploadMaterialResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectFile = (file: File | null) => {
    setError(null);
    setResult(null);

    if (!file) {
      return;
    }

    if (!isAllowedFile(file)) {
      setError('Supported files: .txt, .md, .pdf, and .docx.');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('File too large. Maximum 5MB.');
      return;
    }

    setSelectedFile(file);
    if (!title.trim()) {
      setTitle(file.name.replace(/\.[^.]+$/, ''));
    }
  };

  const submitUpload = async () => {
    if (!courseId) {
      setError('Select a course before uploading.');
      return;
    }

    if (!selectedFile) {
      setError('Choose a file before uploading.');
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);

      const uploadResult = await uploadMaterial({
        courseId,
        file: selectedFile,
        mode,
        title,
        generationMode: mode === 'AiAnalyzeAndSave' ? 'summary' : undefined,
        summaryDepth: mode === 'AiAnalyzeAndSave' ? 'standard' : undefined,
      });

      setResult(uploadResult);
      await onUploaded?.();
    } catch (uploadError) {
      setError(getApiErrorMessage(uploadError, 'Upload failed.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="stack">
      <div
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            inputRef.current?.click();
          }
        }}
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={(event) => {
          event.preventDefault();
          setIsDragging(false);
        }}
        onDrop={(event) => {
          event.preventDefault();
          setIsDragging(false);
          selectFile(event.dataTransfer.files?.[0] ?? null);
        }}
        style={{
          ...uploadZoneStyle,
          background: isDragging ? 'rgba(79, 70, 229, 0.08)' : uploadZoneStyle.background,
          borderColor: isDragging ? 'var(--primary)' : undefined,
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".txt,.md,.pdf,.docx,text/plain,text/markdown,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          style={{ display: 'none' }}
          onChange={(event) => selectFile(event.target.files?.[0] ?? null)}
        />
        <h3 style={{ marginTop: 0 }}>Drop a study file here</h3>
        <p className="muted mb-0">Supports .txt, .md, .pdf, and .docx up to 5MB.</p>
      </div>

      {selectedFile && (
        <div className="ui-card split">
          <div>
            <strong>{selectedFile.name}</strong>
            <p className="muted text-small mb-0">{formatFileSize(selectedFile.size)}</p>
          </div>
          <AppButton
            type="button"
            variant="secondary"
            onClick={() => {
              setSelectedFile(null);
              setResult(null);
            }}
          >
            Remove
          </AppButton>
        </div>
      )}

      <div className="field-grid">
        <label>
          <span className="form-label">Course</span>
          <select value={courseId} onChange={(event) => setCourseId(event.target.value)} disabled={courses.length === 0}>
            <option value="">Select a course</option>
            {courses.map((course) => (
              <option key={course.id} value={course.id}>
                {course.title}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="form-label">Note title</span>
          <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Optional title" />
        </label>
      </div>

      <div>
        <span className="form-label">Upload mode</span>
        <div className="grid grid-3">
          {(Object.keys(modeCopy) as UploadMode[]).map((option) => (
            <button
              key={option}
              type="button"
              className={`ui-card text-left ${mode === option ? 'selected-card' : ''}`}
              onClick={() => setMode(option)}
              style={{
                cursor: 'pointer',
                borderColor: mode === option ? 'var(--primary)' : undefined,
                boxShadow: mode === option ? '0 0 0 2px rgba(79, 70, 229, 0.12)' : undefined,
              }}
            >
              <strong>{modeCopy[option].title}</strong>
              <p className="muted text-small mb-0">{modeCopy[option].description}</p>
            </button>
          ))}
        </div>
      </div>

      {courses.length === 0 && (
        <div className="alert alert-warning">Create a course before uploading study material.</div>
      )}

      {error && <ErrorState message={error} />}

      {result && (
        <div className="alert alert-success">
          <div className="split">
            <div>
              <strong>{result.message}</strong>
              <p className="mb-0 text-small">
                {result.characterCount} readable characters, {result.attachmentCount} saved attachment{result.attachmentCount === 1 ? '' : 's'}.
              </p>
              {result.warning && <p className="mb-0 text-small">{result.warning}</p>}
            </div>
            <Badge tone={result.aiUsed ? 'primary' : result.characterCount > 0 ? 'success' : 'muted'}>
              {result.aiUsed ? 'AI analyzed' : result.characterCount > 0 ? 'Text ready' : 'File saved'}
            </Badge>
          </div>
        </div>
      )}

      <div className="cluster" style={{ justifyContent: 'flex-end' }}>
        {result && (
          <AppButton type="button" variant="secondary" onClick={() => navigate(`/notes/${result.noteId}`)}>
            View Note
          </AppButton>
        )}
        <AppButton type="button" onClick={() => void submitUpload()} disabled={isSubmitting || courses.length === 0}>
          {isSubmitting ? 'Uploading...' : modeCopy[mode].title}
        </AppButton>
      </div>
    </div>
  );
}
