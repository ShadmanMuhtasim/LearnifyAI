import { useState, useCallback, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import NoteSummarizer from '../../components/AI/NoteSummarizer';
import FlashcardViewer from '../../components/AI/FlashcardViewer';
import StudyTips from '../../components/AI/StudyTips';
import AiProviderBadge from '../../components/AI/AiProviderBadge';

interface NoteData {
  id: string;
  title: string;
  content: string;
  subject: string;
  createdAt: string;
  updatedAt: string;
}

const NoteDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [note, setNote] = useState<NoteData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchNote = useCallback(async () => {
    if (!id) return;

    setLoading(true);
    setError(null);

    try {
      // TODO: Replace with actual API call to fetch note by ID
      // const response = await apiClient.get(`/api/notes/${id}`);
      // setNote(response.data);

      // Mock data for demonstration
      await new Promise(resolve => setTimeout(resolve, 500));
      setNote({
        id,
        title: 'Sample Note Title',
        content: 'This is a sample note content. In a real application, this would be fetched from the API based on the note ID.\n\nEducational content about the subject matter would appear here. This could include lecture notes, study materials, or any other educational content that the user has saved.',
        subject: 'Sample Subject',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch note.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchNote();
  }, [fetchNote]);

  if (loading) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '60vh',
        flexDirection: 'column',
        gap: '16px'
      }}>
        <div style={{
          width: '40px',
          height: '40px',
          border: '4px solid #e5e7eb',
          borderTopColor: '#3b82f6',
          borderRadius: '50%',
          animation: 'spin 1s linear infinite'
        }} />
        <p style={{ color: '#6b7280' }}>Loading note...</p>
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{
        padding: '16px',
        borderRadius: '8px',
        backgroundColor: '#fff5f5',
        border: '1px solid #fed7d7',
        color: '#c53030',
        maxWidth: '800px',
        margin: '24px auto'
      }}>
        <h3>Error</h3>
        <p>{error}</p>
        <button
          onClick={() => navigate('/notes')}
          style={{
            padding: '8px 16px',
            backgroundColor: '#c53030',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            marginTop: '12px'
          }}
        >
          ← Back to Notes
        </button>
      </div>
    );
  }

  if (!note) {
    return (
      <div style={{
        padding: '16px',
        textAlign: 'center',
        maxWidth: '800px',
        margin: '24px auto'
      }}>
        <h3>Note not found</h3>
        <button
          onClick={() => navigate('/notes')}
          style={{
            padding: '8px 16px',
            backgroundColor: '#3b82f6',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            marginTop: '12px'
          }}
        >
          ← Back to Notes
        </button>
      </div>
    );
  }

  return (
    <div style={{
      maxWidth: '800px',
      margin: '0 auto',
      padding: '24px'
    }}>
      {/* Header */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        marginBottom: '24px'
      }}>
        <div>
          <button
            onClick={() => navigate('/notes')}
            style={{
              padding: '6px 12px',
              backgroundColor: '#f3f4f6',
              color: '#4b5563',
              border: '1px solid #e5e7eb',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '13px',
              marginBottom: '12px'
            }}
          >
            ← Back to Notes
          </button>
          <h1 style={{ margin: '0 0 8px 0', fontSize: '24px', color: '#111827' }}>
            {note.title}
          </h1>
          <div style={{ display: 'flex', gap: '12px', fontSize: '13px', color: '#6b7280' }}>
            <span>📚 {note.subject}</span>
            <span>📅 Updated: {new Date(note.updatedAt).toLocaleDateString()}</span>
          </div>
        </div>
        <AiProviderBadge />
      </div>

      {/* Note Content */}
      <div style={{
        padding: '24px',
        borderRadius: '12px',
        backgroundColor: 'white',
        border: '1px solid #e5e7eb',
        marginBottom: '24px',
        whiteSpace: 'pre-wrap',
        lineHeight: '1.7',
        color: '#374151'
      }}>
        {note.content}
      </div>

      {/* AI Features */}
      <div style={{
        padding: '24px',
        borderRadius: '12px',
        backgroundColor: '#f9fafb',
        border: '1px solid #e5e7eb'
      }}>
        <h2 style={{ margin: '0 0 20px 0', fontSize: '18px', color: '#374151' }}>
          🤖 AI Study Tools
        </h2>

        {/* Note Summarizer */}
        <div style={{ marginBottom: '24px' }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: '16px', color: '#4b5563' }}>
            📝 Note Summarizer
          </h3>
          <NoteSummarizer noteId={note.id} content={note.content} />
        </div>

        {/* Flashcard Viewer */}
        <div style={{ marginBottom: '24px' }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: '16px', color: '#4b5563' }}>
            🃏 Flashcard Generator
          </h3>
          <FlashcardViewer noteId={note.id} content={note.content} />
        </div>

        {/* Study Tips */}
        <div>
          <h3 style={{ margin: '0 0 12px 0', fontSize: '16px', color: '#4b5563' }}>
            📚 AI Study Tips
          </h3>
          <StudyTips topic={note.subject} />
        </div>
      </div>
    </div>
  );
};

export default NoteDetail;