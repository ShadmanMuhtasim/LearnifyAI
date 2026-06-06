import { useEffect, useRef, useState } from 'react';
import apiClient from '../../services/api';
import { extractText, type ExtractedMaterialText } from '../../services/materialService';
import { AppButton, Badge } from '../UI/Primitives';

interface NoteOption {
  id: string;
  content: string;
  courseTitle?: string | null;
  courseName?: string | null;
  createdAt?: string;
}

interface ApiResponse<T> {
  data?: T;
  success?: boolean;
  message?: string;
}

interface StudyMaterialSourceSelectorProps {
  value: string;
  onChange: (value: string) => void;
  label: string;
  placeholder: string;
  rows?: number;
  disabled?: boolean;
}

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
  return apiError.response?.data?.message ?? apiError.response?.data?.errors?.[0] ?? apiError.message ?? fallback;
};

export default function StudyMaterialSourceSelector({
  value,
  onChange,
  label,
  placeholder,
  rows = 10,
  disabled = false,
}: StudyMaterialSourceSelectorProps) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [notes, setNotes] = useState<NoteOption[]>([]);
  const [selectedNoteId, setSelectedNoteId] = useState('');
  const [isLoadingNotes, setIsLoadingNotes] = useState(false);
  const [isExtracting, setIsExtracting] = useState(false);
  const [sourceMeta, setSourceMeta] = useState<ExtractedMaterialText | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadNotes = async () => {
      setIsLoadingNotes(true);
      try {
        const response = await apiClient.get<ApiResponse<NoteOption[]> | NoteOption[]>('/api/notes');
        const raw = response.data;
        const nextNotes = Array.isArray(raw)
          ? raw
          : raw.data ?? [];
        if (isMounted) {
          setNotes(nextNotes);
        }
      } catch {
        if (isMounted) {
          setNotes([]);
        }
      } finally {
        if (isMounted) {
          setIsLoadingNotes(false);
        }
      }
    };

    void loadNotes();

    return () => {
      isMounted = false;
    };
  }, []);

  const applyNote = (noteId: string) => {
    setSelectedNoteId(noteId);
    const note = notes.find((item) => item.id === noteId);
    if (!note) {
      return;
    }

    onChange(note.content);
    setSourceMeta({
      fileName: note.courseTitle || note.courseName || 'Saved note',
      contentType: 'saved-note',
      extractedText: note.content,
      characterCount: note.content.length,
      warning: null,
    });
    setError(null);
  };

  const handleFileChange = async (file: File | undefined) => {
    if (!file) {
      return;
    }

    setIsExtracting(true);
    setError(null);
    try {
      const result = await extractText(file);
      onChange(result.extractedText);
      setSourceMeta(result);
      setSelectedNoteId('');
    } catch (nextError) {
      setError(getApiErrorMessage(nextError, 'Unable to extract readable text from this file.'));
    } finally {
      setIsExtracting(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  return (
    <div className="stack">
      <div>
        <span className="form-label">{label}</span>
        <p className="muted text-small mt-3">Paste text, select a saved note, or upload a file.</p>
        <p className="muted text-small">PDF support requires selectable text. Scanned/image-only PDFs are not supported.</p>
        <p className="muted text-small">DOCX support extracts plain text only.</p>
      </div>

      <div className="field-grid">
        <label>
          <span className="form-label">Saved note</span>
          <select
            value={selectedNoteId}
            onChange={(event) => applyNote(event.target.value)}
            disabled={disabled || isLoadingNotes || notes.length === 0}
          >
            <option value="">{isLoadingNotes ? 'Loading notes...' : 'Select a saved note'}</option>
            {notes.map((note) => (
              <option key={note.id} value={note.id}>
                {(note.courseTitle || note.courseName || 'Untitled course')} - {note.content.slice(0, 70)}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="form-label">Upload source file</span>
          <input
            ref={fileInputRef}
            type="file"
            accept=".txt,.md,.pdf,.docx,text/plain,text/markdown,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            disabled={disabled || isExtracting}
            onChange={(event) => void handleFileChange(event.target.files?.[0])}
          />
        </label>
      </div>

      {sourceMeta && (
        <div className="cluster">
          <Badge tone="primary">{sourceMeta.fileName}</Badge>
          <Badge tone="muted">{sourceMeta.characterCount.toLocaleString()} chars</Badge>
          {sourceMeta.warning && <Badge tone="warning">{sourceMeta.warning}</Badge>}
        </div>
      )}

      {error && <div className="alert alert-danger">{error}</div>}

      <label>
        <span className="form-label">Extracted or pasted text</span>
        <textarea
          value={value}
          onChange={(event) => {
            onChange(event.target.value);
            setSourceMeta(null);
          }}
          placeholder={placeholder}
          rows={rows}
          disabled={disabled}
        />
      </label>

      {value.trim() && (
        <AppButton type="button" variant="secondary" onClick={() => onChange('')} disabled={disabled}>
          Clear source
        </AppButton>
      )}
    </div>
  );
}
