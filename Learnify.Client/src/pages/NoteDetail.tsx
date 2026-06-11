import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import apiClient from '../services/api';
import { generateFlashcards, getStudyTips, summarizeNote, type GenerationMode } from '../services/aiService';
import { analyzeExistingNote, extractAttachmentText } from '../services/noteUploadService';
import { quizService } from '../services/quizService';
import AttachmentViewer from '../components/Notes/AttachmentViewer';
import FileUploader from '../components/Notes/FileUploader';
import { AppButton, Badge, Card, LoadingState } from '../components/UI/Primitives';

type NoteAttachment = {
  id?: string;
  name: string;
  type: string;
  base64: string;
};

type Note = {
  id: string;
  title: string;
  content: string;
  courseId?: string | null;
  courseName?: string | null;
  courseTitle?: string | null;
  tags?: string[];
  attachments?: NoteAttachment[];
  createdAt?: string;
  updatedAt?: string;
};

type FlashcardResult = {
  front?: string;
  back?: string;
  question?: string;
  answer?: string;
};

type ActiveTool = 'summary' | 'flashcards' | 'tips' | 'quiz';

type GenerationMetadata = {
  notice?: string | null;
  generationModeUsed?: GenerationMode;
  fromCache?: boolean;
};

const conceptHints = ['Key Terms', 'Definitions', 'Examples', 'Open Questions'];
const FILE_ONLY_PDF_CONTENT =
  'This PDF was uploaded without text extraction. Use AI Analyze on a text-based PDF or upload .txt/.md content to generate AI study tools.';
const AI_UNAVAILABLE_MESSAGE =
  'AI summary, flashcards, quizzes, and study tips require readable extracted text. Extract text from the saved attachment or analyze the existing file first.';
const NOTE_DETAIL_GENERATION_MODE_KEY = 'learnify.noteDetail.generationMode';
const generationModes: { value: GenerationMode; label: string }[] = [
  { value: 'Auto', label: 'Auto' },
  { value: 'AIProvider', label: 'AI' },
  { value: 'FreeLocal', label: 'Free Local' },
];

const readGenerationModePreference = (): GenerationMode => {
  const saved = window.localStorage.getItem(NOTE_DETAIL_GENERATION_MODE_KEY);
  return generationModes.some((mode) => mode.value === saved) ? (saved as GenerationMode) : 'Auto';
};

const unwrap = <T,>(response: unknown): T => {
  const value = response as { data?: unknown };
  const data = value?.data as { data?: T } | T | undefined;
  return ((data as { data?: T })?.data ?? data ?? response) as T;
};

const normalizeText = (value: unknown): string => {
  if (typeof value === 'string') {
    return value;
  }

  if (value && typeof value === 'object') {
    const candidate = value as Record<string, unknown>;
    return String(candidate.summary ?? candidate.tips ?? candidate.content ?? '');
  }

  return '';
};

const normalizeFlashcards = (value: unknown): FlashcardResult[] => {
  const response = value as Record<string, unknown>;
  const cards = response?.flashcards ?? response?.cards ?? value;
  return Array.isArray(cards) ? (cards as FlashcardResult[]) : [];
};

export default function NoteDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [note, setNote] = useState<Note | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [draftTitle, setDraftTitle] = useState('');
  const [draftContent, setDraftContent] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [activeTool, setActiveTool] = useState<ActiveTool | null>(null);
  const [aiLoading, setAiLoading] = useState<ActiveTool | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [aiNotice, setAiNotice] = useState<string | null>(null);
  const [generationMode, setGenerationMode] = useState<GenerationMode>(() => readGenerationModePreference());
  const [summary, setSummary] = useState('');
  const [studyTips, setStudyTips] = useState('');
  const [flashcards, setFlashcards] = useState<FlashcardResult[]>([]);
  const [postUploadAction, setPostUploadAction] = useState<'extract' | 'analyze' | null>(null);

  const loadNote = useCallback(async () => {
    if (!id) {
      return;
    }

    setIsLoading(true);
    setMessage(null);

    try {
      const response = await apiClient.get(`/api/notes/${id}`);
      const loadedNote = unwrap<Note>(response);

      setNote(loadedNote);
      setDraftTitle(loadedNote.title);
      setDraftContent(loadedNote.content);
    } catch {
      setNote(null);
      setMessage('Unable to load this note.');
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void loadNote();
  }, [loadNote]);

  const isFileOnlyPdf = useMemo(() => {
    const content = note?.content?.trim() ?? '';
    return (
      content === FILE_ONLY_PDF_CONTENT ||
      content.toLowerCase().includes('uploaded without text extraction')
    );
  }, [note?.content]);
  const hasContent = useMemo(() => Boolean(note?.content?.trim()), [note?.content]);
  const hasUsableAiContent = hasContent && !isFileOnlyPdf;
  const attachments = note?.attachments ?? [];
  const canRecoverAttachmentText = !hasUsableAiContent && attachments.length > 0;
  const generatedFlashcards = flashcards.filter((card) => card.front || card.question || card.back || card.answer);

  const handleGenerationModeChange = (mode: GenerationMode) => {
    setGenerationMode(mode);
    window.localStorage.setItem(NOTE_DETAIL_GENERATION_MODE_KEY, mode);
  };

  const captureGenerationNotice = (value: GenerationMetadata) => {
    const cacheNotice = value.fromCache ? ' Served from cache.' : '';
    setAiNotice(value.notice ? `${value.notice}${cacheNotice}` : value.fromCache ? 'Served from cache.' : null);
  };

  const handleExtractAttachmentText = async () => {
    if (!note) {
      return;
    }

    try {
      setPostUploadAction('extract');
      setAiError(null);
      const result = await extractAttachmentText(note.id);
      await loadNote();
      setMessage(result.warning ?? result.message);
    } catch (error) {
      const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
      setAiError(
        apiError.response?.data?.message ??
          apiError.response?.data?.errors?.[0] ??
          apiError.message ??
          'Unable to extract text from this attachment.'
      );
    } finally {
      setPostUploadAction(null);
    }
  };

  const handleAnalyzeExisting = async () => {
    if (!note) {
      return;
    }

    try {
      setPostUploadAction('analyze');
      setAiError(null);
      const result = await analyzeExistingNote(note.id);
      await loadNote();
      setMessage(result.warning ?? result.message);
    } catch (error) {
      const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
      setAiError(
        apiError.response?.data?.message ??
          apiError.response?.data?.errors?.[0] ??
          apiError.message ??
          'Unable to analyze the saved attachment.'
      );
    } finally {
      setPostUploadAction(null);
    }
  };

  const runAiAction = async (tool: ActiveTool, action: () => Promise<void>) => {
    if (!note || !hasUsableAiContent) {
      setActiveTool(tool);
      setAiError(isFileOnlyPdf ? AI_UNAVAILABLE_MESSAGE : 'This note needs text content before AI actions can run.');
      setAiNotice(null);
      return;
    }

    setActiveTool(tool);
    setAiError(null);
    setAiNotice(null);
    setAiLoading(tool);

    try {
      await action();
    } catch (error) {
      const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
      setAiError(
        apiError.response?.data?.message ??
          apiError.response?.data?.errors?.[0] ??
          apiError.message ??
          'The AI action could not be completed.'
      );
    } finally {
      setAiLoading(null);
    }
  };

  const handleGenerateSummary = () =>
    runAiAction('summary', async () => {
      const result = await summarizeNote({ noteId: note!.id, content: note!.content, generationMode });
      const data = unwrap<GenerationMetadata>(result);
      setSummary(normalizeText(data));
      captureGenerationNotice(data);
    });

  const handleGenerateFlashcards = () =>
    runAiAction('flashcards', async () => {
      const result = await generateFlashcards({ noteId: note!.id, content: note!.content, generationMode });
      const data = unwrap<GenerationMetadata>(result);
      setFlashcards(normalizeFlashcards(data));
      captureGenerationNotice(data);
    });

  const handleGenerateStudyTips = () =>
    runAiAction('tips', async () => {
      const result = await getStudyTips({ topic: `${note!.title}\n\n${note!.content}`, generationMode });
      const data = unwrap<GenerationMetadata>(result);
      setStudyTips(normalizeText(data));
      captureGenerationNotice(data);
    });

  const handleGenerateQuiz = () =>
    runAiAction('quiz', async () => {
      const result = await quizService.generateQuiz({
        noteId: note!.id,
        numberOfQuestions: 5,
        difficulty: 'Medium',
        questionTypes: ['MultipleChoice'],
        timeLimitMinutes: null,
      });
      const quiz = unwrap<{ id?: string; quizId?: string }>(result);

      navigate(quiz.id || quiz.quizId ? `/quizzes/${quiz.id ?? quiz.quizId}` : '/quizzes');
    });

  const saveNote = async () => {
    if (!note) {
      return;
    }

    setIsSaving(true);
    setMessage(null);

    try {
      const response = await apiClient.put(`/api/notes/${note.id}`, {
        title: draftTitle,
        content: draftContent,
        courseId: note.courseId,
        tags: note.tags ?? [],
        attachments,
      });
      const updatedNote = unwrap<Note>(response);
      setNote(updatedNote);
      setDraftTitle(updatedNote.title);
      setDraftContent(updatedNote.content);
      setIsEditing(false);
      setMessage('Note saved.');
    } catch {
      setMessage('Unable to save this note.');
    } finally {
      setIsSaving(false);
    }
  };

  const deleteNote = async () => {
    if (!note || !window.confirm(`Delete ${note.title || 'this note'}? This cannot be undone.`)) {
      return;
    }

    try {
      await apiClient.delete(`/api/notes/${note.id}`);
      navigate('/notes');
    } catch {
      setMessage('Unable to delete this note.');
    }
  };

  const handleFileUploadSuccess = (attachment: { name: string; type: string; base64: string }) => {
    setNote((current) =>
      current
        ? {
            ...current,
            attachments: [...(current.attachments ?? []), { ...attachment, id: crypto.randomUUID() }],
          }
        : current
    );
    setMessage('Attachment added locally. Save the note to persist supported attachment metadata.');
  };

  const removeAttachment = (index: number) => {
    setNote((current) =>
      current
        ? {
            ...current,
            attachments: (current.attachments ?? []).filter((_, attachmentIndex) => attachmentIndex !== index),
          }
        : current
    );
  };

  if (isLoading) {
    return <LoadingState label="Loading note..." />;
  }

  if (!note) {
    return (
      <div className="empty-state">
        <h2>Note not found</h2>
        <p className="muted">{message ?? 'This note could not be found or is not available to your account.'}</p>
        <Link to="/notes" className="btn btn-outline-primary">Back to Notes</Link>
      </div>
    );
  }

  return (
    <div className="stack">
      <Link to="/notes" className="btn btn-link" style={{ justifySelf: 'start' }}>Back to Notes</Link>

      {message && <div className="alert alert-info">{message}</div>}

      <div className="page-two-column">
        <Card className="note-reader">
          <div className="note-reader-header">
            <div className="split">
              <div>
                <h1>{note.title || 'Untitled Note'}</h1>
                <div className="note-meta mt-3">
                  {note.createdAt && <span>Created {new Date(note.createdAt).toLocaleString()}</span>}
                  {(note.courseName || note.courseTitle) && <span>Course: {note.courseName || note.courseTitle}</span>}
                  {(note.tags ?? []).map((tag) => (
                    <Badge key={tag} tone="muted">#{tag}</Badge>
                  ))}
                  <Badge tone={hasUsableAiContent ? 'success' : 'muted'}>{hasUsableAiContent ? 'AI Ready' : isFileOnlyPdf ? 'PDF attached' : 'Empty'}</Badge>
                </div>
              </div>

              <div className="cluster">
                {isEditing ? (
                  <>
                    <AppButton type="button" onClick={() => void saveNote()} disabled={isSaving}>
                      {isSaving ? 'Saving...' : 'Save'}
                    </AppButton>
                    <AppButton type="button" variant="secondary" onClick={() => setIsEditing(false)}>
                      Cancel
                    </AppButton>
                  </>
                ) : (
                  <AppButton
                    type="button"
                    variant="secondary"
                    onClick={() => {
                      setDraftTitle(note.title);
                      setDraftContent(note.content);
                      setIsEditing(true);
                    }}
                  >
                    Edit
                  </AppButton>
                )}
                <AppButton type="button" variant="danger" onClick={() => void deleteNote()}>
                  Delete
                </AppButton>
              </div>
            </div>
          </div>

          <div className="note-content">
            {isEditing ? (
              <div className="stack">
                <label>
                  <span className="form-label">Note title</span>
                  <input value={draftTitle} onChange={(event) => setDraftTitle(event.target.value)} />
                </label>
                <label>
                  <span className="form-label">Note content</span>
                  <textarea
                    value={draftContent}
                    onChange={(event) => setDraftContent(event.target.value)}
                    rows={14}
                    style={{ lineHeight: 1.7 }}
                  />
                </label>
              </div>
            ) : (
              <>
                {note.content ? (
                  note.content.split('\n').map((paragraph, index) =>
                    paragraph.trim() ? <p key={`${paragraph.slice(0, 24)}-${index}`}>{paragraph}</p> : <br key={index} />
                  )
                ) : (
                  <p className="muted">This note is empty.</p>
                )}
                {note.content.includes('=') && (
                  <div className="note-code-block">
                    <pre><code>{note.content.split('\n').find((line) => line.includes('='))}</code></pre>
                  </div>
                )}
              </>
            )}
          </div>
        </Card>

        <aside className="ai-panel">
          <Card className="stack">
            <div className="cluster">
              <span className="nav-icon" aria-hidden="true">AI</span>
              <h2>AI Analysis</h2>
            </div>

            <AppButton type="button" disabled title="AI Tutor backend is not part of M8.4">
              Ask AI Tutor - Coming soon
            </AppButton>

            <div className="generation-mode-selector" aria-label="Generation mode">
              {generationModes.map((mode) => (
                <button
                  key={mode.value}
                  type="button"
                  className={`ui-button ${generationMode === mode.value ? 'ui-button-primary' : 'ui-button-ghost'}`}
                  onClick={() => handleGenerationModeChange(mode.value)}
                  disabled={aiLoading !== null}
                  aria-pressed={generationMode === mode.value}
                >
                  {mode.label}
                </button>
              ))}
            </div>

            {canRecoverAttachmentText && (
              <div className="alert alert-warning">
                <p className="mb-3">{AI_UNAVAILABLE_MESSAGE}</p>
                <div className="cluster">
                  <AppButton
                    type="button"
                    variant="secondary"
                    onClick={() => void handleExtractAttachmentText()}
                    disabled={postUploadAction !== null}
                  >
                    {postUploadAction === 'extract' ? 'Extracting...' : 'Extract Text'}
                  </AppButton>
                  <AppButton
                    type="button"
                    onClick={() => void handleAnalyzeExisting()}
                    disabled={postUploadAction !== null}
                  >
                    {postUploadAction === 'analyze' ? 'Analyzing...' : 'Analyze Existing File'}
                  </AppButton>
                </div>
              </div>
            )}

            <div className="ai-action-list">
              <AppButton type="button" variant="secondary" onClick={handleGenerateSummary} disabled={aiLoading !== null || !hasUsableAiContent}>
                {aiLoading === 'summary' ? 'Generating Summary...' : 'Generate Summary'}
              </AppButton>
              <AppButton type="button" variant="secondary" onClick={handleGenerateFlashcards} disabled={aiLoading !== null || !hasUsableAiContent}>
                {aiLoading === 'flashcards' ? 'Generating Flashcards...' : 'Generate Flashcards'}
              </AppButton>
              <AppButton type="button" variant="secondary" onClick={handleGenerateStudyTips} disabled={aiLoading !== null || !hasUsableAiContent}>
                {aiLoading === 'tips' ? 'Generating Study Tips...' : 'Generate Study Tips'}
              </AppButton>
              <AppButton type="button" variant="secondary" onClick={handleGenerateQuiz} disabled={aiLoading !== null || !hasUsableAiContent}>
                {aiLoading === 'quiz' ? 'Generating Quiz...' : 'Generate Quiz'}
              </AppButton>
            </div>
          </Card>

          <Card className="stack">
            <div className="split">
              <h2>Key Concepts</h2>
              <Badge tone="muted">Placeholder</Badge>
            </div>
            <p className="muted text-small">Concept extraction is a future enhancement. These are study prompts, not generated concepts.</p>
            {conceptHints.map((concept) => (
              <div className="concept-card" key={concept}>
                <strong>{concept}</strong>
                <span className="muted text-small">Review the note for this category.</span>
              </div>
            ))}
          </Card>
        </aside>
      </div>

      {attachments.length > 0 && (
        <Card className="stack">
          <div className="split">
            <h2>Attachments ({attachments.length})</h2>
            <Badge tone="primary">Saved files</Badge>
          </div>
          <AttachmentViewer attachments={attachments} onRemove={removeAttachment} />
        </Card>
      )}

      <div className="page-two-column">
        <Card className="stack">
          <h2>Upload Files</h2>
          <p className="muted">Attach supporting material to this note. PDF extraction remains partial.</p>
          <FileUploader onUploadSuccess={handleFileUploadSuccess} />
        </Card>

        <Card className="stack">
          <h2>Active AI Tool</h2>
          {aiError && <div className="alert alert-danger">{aiError}</div>}
          {aiNotice && !aiError && <div className="alert alert-info">{aiNotice}</div>}
          {!activeTool && <p className="muted">Choose an AI action from the analysis panel.</p>}
          {activeTool === 'summary' && !aiError && <p className="ai-output-text">{summary || 'Summary will appear here.'}</p>}
          {activeTool === 'tips' && !aiError && <p className="ai-output-text">{studyTips || 'Study tips will appear here.'}</p>}
          {activeTool === 'flashcards' && !aiError && (
            <div className="grid grid-2">
              {generatedFlashcards.length === 0 ? (
                <p className="muted">Flashcards will appear here.</p>
              ) : (
                generatedFlashcards.map((card, index) => (
                  <Card key={`${card.front ?? card.question ?? index}`} className="concept-card">
                    <strong>{card.front ?? card.question ?? `Card ${index + 1}`}</strong>
                    <span>{card.back ?? card.answer}</span>
                  </Card>
                ))
              )}
            </div>
          )}
          {activeTool === 'quiz' && !aiError && (
            <p className="muted">
              {aiLoading === 'quiz' ? 'Generating a quiz from this note...' : 'Opening your generated quiz...'}
            </p>
          )}
        </Card>
      </div>
    </div>
  );
}
