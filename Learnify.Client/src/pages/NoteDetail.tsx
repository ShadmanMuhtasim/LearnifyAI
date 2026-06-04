import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';
import AttachmentViewer from '../components/Notes/AttachmentViewer';
import FileUploader from '../components/Notes/FileUploader';
import StudyTips from '../components/AI/StudyTips';
import NoteSummarizer from '../components/AI/NoteSummarizer';

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

export default function NoteDetail() {
  const { id } = useParams<{ id: string }>();
  const { isAuthenticated } = useAuthStore();
  const [note, setNote] = useState<Note | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState('');
  const [showAttachmentViewer, setShowAttachmentViewer] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    const fetchNote = async () => {
      try {
        const response = await apiClient.get<any>(`/api/notes/${id}`);
        if (response.data.success) {
          setNote(response.data.data);
        }
      } catch {
        // ignore
      } finally {
        setLoading(false);
      }
    };

    fetchNote();
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
      // ignore
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
      // ignore
    }
  };

  const handleFileUploadSuccess = async (attachment: { name: string; type: string; base64: string }) => {
    // Convert base64 back to include the prefix for the UI
    const newAttachment: NoteAttachment = {
      id: crypto.randomUUID(),
      name: attachment.name,
      type: attachment.type,
      base64: attachment.base64
    };

    // Save note with new attachment
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
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  if (!note) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem', color: '#666' }}>
        <h2>Note not found</h2>
        <Link to="/notes" style={{ color: '#667eea' }}>← Back to Notes</Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '2rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <Link to="/notes" style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '0.5rem',
          color: '#667eea',
          textDecoration: 'none',
          fontWeight: 500,
          fontSize: '1rem'
        }}>
          <span>←</span> Back to Notes
        </Link>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            onClick={() => {
              setIsEditing(true);
              setEditContent(note.content);
            }}
            style={{
              padding: '0.5rem 1rem',
              background: '#667eea',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              cursor: 'pointer',
              fontWeight: 500
            }}
          >
            ✏️ Edit
          </button>
          <button
            onClick={() => {
              if (window.confirm('Are you sure you want to delete this note?')) {
                // Delete logic would go here
              }
            }}
            style={{
              padding: '0.5rem 1rem',
              background: '#e74c3c',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              cursor: 'pointer',
              fontWeight: 500
            }}
          >
            🗑️ Delete
          </button>
        </div>
      </div>

      {/* Note Content */}
      <div style={{
        background: 'white',
        borderRadius: '16px',
        padding: '2.5rem',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        marginBottom: '2rem'
      }}>
        <h1 style={{ margin: '0 0 0.5rem', color: '#333', fontSize: '2rem', fontWeight: 700 }}>
          {note.title || 'Untitled Note'}
        </h1>
        <p style={{ color: '#888', fontSize: '0.9rem', marginBottom: '1.5rem' }}>
          📅 Created: {new Date(note.createdAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
          })}
          {note.courseTitle && <> | 📚 Course: <strong>{note.courseTitle}</strong></>}
        </p>

        {/* Edit Mode */}
        {isEditing ? (
          <div>
            <textarea
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
              style={{
                width: '100%',
                minHeight: '300px',
                padding: '1rem',
                border: '2px solid #667eea',
                borderRadius: '12px',
                fontSize: '1rem',
                lineHeight: 1.7,
                resize: 'vertical',
                fontFamily: 'inherit'
              }}
            />
            <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
              <button
                onClick={handleSaveEdit}
                disabled={isSaving}
                style={{
                  padding: '0.75rem 1.5rem',
                  background: '#27ae60',
                  color: 'white',
                  border: 'none',
                  borderRadius: '8px',
                  cursor: isSaving ? 'not-allowed' : 'pointer',
                  fontWeight: 600
                }}
              >
                {isSaving ? 'Saving...' : '💾 Save'}
              </button>
              <button
                onClick={() => setIsEditing(false)}
                style={{
                  padding: '0.75rem 1.5rem',
                  background: '#95a5a6',
                  color: 'white',
                  border: 'none',
                  borderRadius: '8px',
                  cursor: 'pointer',
                  fontWeight: 600
                }}
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div style={{
            whiteSpace: 'pre-wrap',
            lineHeight: 1.8,
            color: '#444',
            fontSize: '1.05rem'
          }}>
            {note.content}
          </div>
        )}
      </div>

      {/* Attachments Section */}
      {note.attachments.length > 0 && (
        <div style={{
          background: 'white',
          borderRadius: '16px',
          padding: '2rem',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          marginBottom: '2rem'
        }}>
          <h3 style={{ margin: '0 0 1.5rem', color: '#333', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            📎 Attachments ({note.attachments.length})
          </h3>
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
            gap: '1rem'
          }}>
            {note.attachments.map((attachment) => (
              <div
                key={attachment.id}
                style={{
                  border: '1px solid #e0e0e0',
                  borderRadius: '12px',
                  padding: '1rem',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '0.75rem',
                  transition: 'transform 0.2s, box-shadow 0.2s'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = 'none';
                }}
              >
                <div style={{
                  width: '40px',
                  height: '40px',
                  borderRadius: '8px',
                  background: '#f0f0f0',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '1.25rem'
                }}>
                  {attachment.type.startsWith('image/') ? '🖼️' :
                   attachment.type.startsWith('video/') ? '🎥' :
                   attachment.type.startsWith('audio/') ? '🎵' :
                   attachment.type.includes('pdf') ? '📄' : '📎'}
                </div>
                <div style={{
                  fontSize: '0.85rem',
                  color: '#555',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                  fontWeight: 500
                }}>
                  {attachment.name}
                </div>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button
                    onClick={() => setShowAttachmentViewer(attachment.id)}
                    style={{
                      flex: 1,
                      padding: '0.4rem',
                      background: '#667eea',
                      color: 'white',
                      border: 'none',
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontSize: '0.8rem',
                      fontWeight: 500
                    }}
                  >
                    👁️ View
                  </button>
                  <button
                    onClick={() => handleDeleteAttachment(attachment.id)}
                    style={{
                      padding: '0.4rem 0.75rem',
                      background: '#e74c3c',
                      color: 'white',
                      border: 'none',
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontSize: '0.8rem'
                    }}
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* File Uploader */}
      <div style={{
        background: 'white',
        borderRadius: '16px',
        padding: '2rem',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        marginBottom: '2rem'
      }}>
        <h3 style={{ margin: '0 0 1rem', color: '#333' }}>📤 Upload Files</h3>
        <FileUploader onUploadSuccess={handleFileUploadSuccess} />
      </div>

      {/* AI Features */}
      <div style={{ marginBottom: '2rem' }}>
        <NoteSummarizer noteId={note.id} content={note.content} />
      </div>

      <div style={{ marginBottom: '2rem' }}>
        <StudyTips topic={note.title || note.content.substring(0, 100)} />
      </div>

      {/* Attachment Viewer Modal */}
      {showAttachmentViewer && (
        <AttachmentViewer
          attachments={[note.attachments.find(a => a.id === showAttachmentViewer)!].filter(Boolean)}
        />
      )}
    </div>
  );
}