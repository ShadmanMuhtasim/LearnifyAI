import { useEffect, useState } from 'react';
import apiClient from '../../services/api';
import { extractText } from '../../services/materialService';
import { Badge } from '../UI/Primitives';

type NoteOption = {
  id: string;
  content: string;
  title?: string;
  courseTitle?: string;
};

type Props = {
  value: string;
  onChange: (value: string) => void;
  label: string;
  placeholder: string;
  rows?: number;
};

const unwrapNotes = (payload: unknown): NoteOption[] => {
  const outer = payload as { data?: unknown };
  const maybeData = outer.data as { data?: unknown } | NoteOption[] | undefined;
  const notes = Array.isArray((maybeData as { data?: unknown })?.data)
    ? (maybeData as { data: NoteOption[] }).data
    : Array.isArray(maybeData)
      ? maybeData
      : [];
  return notes;
};

export default function StudyMaterialSourceSelector({ value, onChange, label, placeholder, rows = 10 }: Props) {
  const [notes, setNotes] = useState<NoteOption[]>([]);
  const [selectedNoteId, setSelectedNoteId] = useState('');
  const [fileName, setFileName] = useState('');
  const [warning, setWarning] = useState<string | null>(null);
  const [isExtracting, setIsExtracting] = useState(false);

  useEffect(() => {
    let isMounted = true;
    apiClient.get('/api/notes')
      .then((response) => {
        if (isMounted) {
          setNotes(unwrapNotes(response.data));
        }
      })
      .catch(() => {
        if (isMounted) {
          setNotes([]);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const handleSelectNote = (noteId: string) => {
    setSelectedNoteId(noteId);
    const note = notes.find((item) => item.id === noteId);
    if (note) {
      onChange(note.content);
      setFileName('');
      setWarning(null);
    }
  };

  const handleUpload = async (file?: File) => {
    if (!file) {
      return;
    }

    setIsExtracting(true);
    setWarning(null);
    try {
      const result = await extractText(file);
      onChange(result.text);
      setFileName(result.fileName || file.name);
      setWarning(result.warning || null);
      setSelectedNoteId('');
    } catch (error: unknown) {
      const apiError = error as { response?: { data?: { message?: string } }; message?: string };
      setWarning(apiError.response?.data?.message || apiError.message || 'Could not extract text from this file.');
    } finally {
      setIsExtracting(false);
    }
  };

  return (
    <div className="stack">
      <label>
        <span className="form-label">Saved note</span>
        <select value={selectedNoteId} onChange={(event) => handleSelectNote(event.target.value)}>
          <option value="">Paste text or choose a saved note</option>
          {notes.map((note) => (
            <option key={note.id} value={note.id}>
              {note.title || note.courseTitle || `Note ${note.id.slice(0, 8)}`}
            </option>
          ))}
        </select>
      </label>

      <label>
        <span className="form-label">Upload material</span>
        <input
          type="file"
          accept=".txt,.md,.pdf,.docx,text/plain,text/markdown,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          onChange={(event) => void handleUpload(event.target.files?.[0])}
          disabled={isExtracting}
        />
      </label>

      <div className="cluster">
        <Badge tone="muted">TXT</Badge>
        <Badge tone="muted">MD</Badge>
        <Badge tone="muted">Text PDF</Badge>
        <Badge tone="muted">DOCX</Badge>
        {fileName && <Badge tone="primary">{fileName}</Badge>}
        <Badge tone="muted">{value.length.toLocaleString()} chars</Badge>
      </div>

      {warning && <div className="alert alert-warning">{warning}</div>}
      <p className="muted text-small">PDF support requires selectable text. Scanned/image-only PDFs show a clear OCR unavailable message.</p>

      <label>
        <span className="form-label">{label}</span>
        <textarea
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          rows={rows}
        />
      </label>
    </div>
  );
}
