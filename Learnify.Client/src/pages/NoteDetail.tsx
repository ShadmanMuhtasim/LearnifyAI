import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';
import AttachmentViewer from '../components/Notes/AttachmentViewer';
import FileUploader from '../components/Notes/FileUploader';
import StudyTips from '../components/AI/StudyTips';
import NoteSummarizer from '../components/AI/NoteSummarizer';
import FlashcardViewer from '../components/AI/FlashcardViewer';
import { AppButton, Badge, Card, LoadingState } from '../components/UI/Primitives';

interface NoteAttachment {
  id: string;
  name: string;
  type: string;
  base64: string;
}

interface Note {
  id: string;
  title: string;
  content: string;
  courseId: string;
  courseTitle?: string;
  attachments: NoteAttachment[];
  createdAt: string;
}

const conceptHints = [
  'Key Terms',
  'Definitions',
  'Examples',
  'Open Questions',
];

export default function NoteDetail() {
  const { id } = useParams<{ id: string }>();
  const { isAuthenticated } = useAuthStore();
  const [note, setNote] = useState<Note | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState('');
  const [showAttachmentViewer, setShowAttachmentViewer] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [activeTool, setActiveTool] = useState<'summary' | 'flashcards' | 'tips' | null>('summary');

  useEffect(() => {
    const fetchNote = async () => {
      try {
        const response = await apiClient.get<any>(`/api/notes/${id}`);
        if (response.data.success) {
          setNote(response.data.data);
        }
      } catch {
        // Keep the existing quiet failure behavior.
      } finally {
        setLoading(false);
      }
    };

    void fetchNote();
  }, [id]);

  const handleSaveEdit = async () => {
    try {
      setIsSaving(true);
      const response = await apiClient.put<any>(`/api/notes/${id}`, {
        content: editContent,
        attachments: []
      });
      if (response.data.success) {
        setNote(prev => prev ? { ...prev, content: editContent } : null);
        setIsEditing(false);
      }
    } catch {
      // Keep the existing quiet failure behavior.
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeleteAttachment = async (attachmentId: string) => {
    try {
      const response = await apiClient.delete<any>(`/api/notes/${id}/attachments/${attachmentId}`);
      if (response.data.success) {
        setNote(prev => prev ? {
          ...prev,
          attachments: prev.attachments.filter(a => a.id !== attachmentId)
        } : null);
      }
    } catch {
      // Keep the existing quiet failure behavior.
    }
  };

  const handleFileUploadSuccess = async (attachment: { name: string; type: string; base64: string }) => {
    const newAttachment: NoteAttachment = {
      id: crypto.randomUUID(),
      name: attachment.name,
      type: attachment.type,
      base64: attachment.base64
    };

    const currentAttachments = note?.attachments || [];
    const response = await apiClient.put<any>(`/api/notes/${id}`, {
      content: note?.content || '',
      attachments: [...currentAttachments, newAttachment]
    });

    if (response.data.success) {
      setNote(prev => prev ? {
        ...prev,
        attachments: [...currentAttachments, newAttachment]
      } : null);
    }
  };

  if (!isAuthenticated) return null;

  if (loading) {
    return <LoadingState label="Loading note..." />;
  }

  if (!note) {
    return (
      <div className="empty-state">
        <h2>Note not found</h2>
        <Link to="/notes" className="btn btn-outline-primary">Back to Notes</Link>
      </div>
    );
  }

  const attachments = note.attachments || [];
  const hasContent = note.content.trim().length > 0;

  return (
    <div className="stack">
      <Link to="/notes" className="btn btn-link" style={{ justifySelf: 'start' }}>Back to Notes</Link>

      <div className="page-two-column">
        <Card className="note-reader">
          <div className="note-reader-header">
            <div className="split">
              <div>
                <h1>{note.title || 'Untitled Note'}</h1>
                <div className="note-meta mt-3">
                  <span>Created {new Date(note.createdAt).toLocaleString()}</span>
                  {note.courseTitle && <span>Course: {note.courseTitle}</span>}
                  <Badge tone={hasContent ? 'success' : 'muted'}>{hasContent ? 'AI Ready' : 'Empty'}</Badge>
                </div>
              </div>
              <div className="cluster">
                <AppButton
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setIsEditing(true);
                    setEditContent(note.content);
                  }}
                >
                  Edit
                </AppButton>
                <AppButton
                  type="button"
                  variant="danger"
                  onClick={() => {
                    window.confirm('Are you sure you want to delete this note?');
                  }}
                >
                  Delete
                </AppButton>
              </div>
            </div>
          </div>

          <div className="note-content">
            {isEditing ? (
              <div className="stack">
                <label>
                  <span className="form-label">Note content</span>
                  <textarea
                    value={editContent}
                    onChange={(event) => setEditContent(event.target.value)}
                    rows={14}
                    style={{ lineHeight: 1.7 }}
                  />
                </label>
                <div className="cluster">
                  <AppButton type="button" onClick={() => void handleSaveEdit()} disabled={isSaving}>
                    {isSaving ? 'Saving...' : 'Save'}
                  </AppButton>
                  <AppButton type="button" variant="secondary" onClick={() => setIsEditing(false)}>
                    Cancel
                  </AppButton>
                </div>
              </div>
            ) : (
              <>
                {note.content || 'This note is empty.'}
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

            <AppButton type="button" disabled title="AI Tutor backend is not part of M8.3">
              Ask AI Tutor - Coming soon
            </AppButton>

            <div className="ai-action-list">
              <AppButton type="button" variant="secondary" onClick={() => setActiveTool('summary')}>
                Generate Summary
              </AppButton>
              <AppButton type="button" variant="secondary" onClick={() => setActiveTool('flashcards')}>
                Generate Flashcards
              </AppButton>
              <Link to="/quizzes" className="btn btn-outline-secondary">
                Generate Quiz
              </Link>
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
          <div className="grid grid-3">
            {attachments.map((attachment) => (
              <Card key={attachment.id} className="stack">
                <strong>{attachment.name}</strong>
                <span className="muted text-small">{attachment.type}</span>
                <div className="cluster">
                  <AppButton type="button" variant="secondary" onClick={() => setShowAttachmentViewer(attachment.id)}>
                    View
                  </AppButton>
                  <AppButton type="button" variant="danger" onClick={() => void handleDeleteAttachment(attachment.id)}>
                    Delete
                  </AppButton>
                </div>
              </Card>
            ))}
          </div>
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
          {activeTool === 'summary' && <NoteSummarizer noteId={note.id} content={note.content} />}
          {activeTool === 'flashcards' && <FlashcardViewer noteId={note.id} content={note.content} />}
          {activeTool === 'tips' && <StudyTips topic={note.title || note.content.substring(0, 100)} />}
          {!activeTool && <p className="muted">Choose an AI action from the analysis panel.</p>}
          <AppButton type="button" variant="secondary" onClick={() => setActiveTool('tips')}>
            Generate Study Tips
          </AppButton>
        </Card>
      </div>

      {showAttachmentViewer && (
        <AttachmentViewer
          attachments={[attachments.find(a => a.id === showAttachmentViewer)!].filter(Boolean)}
        />
      )}
    </div>
  );
}
