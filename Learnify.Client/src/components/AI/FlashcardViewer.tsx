import { useState, useCallback } from 'react';
import { generateFlashcards, getActiveProvider, type FlashcardItem as ApiFlashcardItem, type ActiveProviderResponse } from '../../services/aiService';

interface FlashcardViewerProps {
  noteId: string;
  content: string;
}

const FlashcardViewer: React.FC<FlashcardViewerProps> = ({ noteId, content }) => {
  const [flashcards, setFlashcards] = useState<ApiFlashcardItem[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [count, setCount] = useState(5);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isFlipped, setIsFlipped] = useState(false);
  const [activeProvider, setActiveProvider] = useState<ActiveProviderResponse | null>(null);

  const fetchActiveProvider = useCallback(async () => {
    try {
      const provider = await getActiveProvider();
      setActiveProvider(provider);
    } catch {
      // Silently fail
    }
  }, []);

  const handleGenerate = useCallback(async () => {
    if (!content.trim()) {
      setError('Note content is empty.');
      return;
    }

    setLoading(true);
    setError(null);
    setFlashcards([]);
    setIsFlipped(false);
    setCurrentIndex(0);

    try {
      const result = await generateFlashcards({ noteId, content, count });
      setFlashcards(result.flashcards);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to generate flashcards.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [noteId, content, count]);

  const handlePrevious = useCallback(() => {
    setIsFlipped(false);
    setCurrentIndex(prev => Math.max(0, prev - 1));
  }, []);

  const handleNext = useCallback(() => {
    setIsFlipped(false);
    setCurrentIndex(prev => Math.min(flashcards.length - 1, prev + 1));
  }, [flashcards.length]);

  const handleFlip = useCallback(() => {
    setIsFlipped(prev => !prev);
  }, []);

  if (loading) {
    return (
      <div style={{
        padding: '16px',
        borderRadius: '8px',
        backgroundColor: '#f8f9fa',
        border: '1px solid #e9ecef',
        textAlign: 'center'
      }}>
        <div style={{
          display: 'inline-block',
          width: '32px',
          height: '32px',
          border: '3px solid #e9ecef',
          borderTopColor: '#7c3aed',
          borderRadius: '50%',
          animation: 'spin 0.8s linear infinite'
        }} />
        <p style={{ marginTop: '12px', color: '#6c757d' }}>Generating flashcards with AI...</p>
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{
        padding: '12px 16px',
        borderRadius: '8px',
        backgroundColor: '#fff5f5',
        border: '1px solid #fed7d7',
        color: '#c53030',
        marginBottom: '12px'
      }}>
        <strong>Error:</strong> {error}
        <button
          onClick={() => setError(null)}
          style={{
            marginLeft: '12px',
            padding: '4px 12px',
            border: '1px solid #fc8181',
            borderRadius: '4px',
            backgroundColor: 'white',
            cursor: 'pointer',
            color: '#c53030'
          }}
        >
          Dismiss
        </button>
      </div>
    );
  }

  if (flashcards.length === 0) {
    return (
      <div style={{ marginTop: '16px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '12px', flexWrap: 'wrap' }}>
          <label style={{ fontSize: '14px', color: '#4b5563', fontWeight: 500 }}>Card Count:</label>
          <select
            value={count}
            onChange={e => setCount(Number(e.target.value))}
            style={{
              padding: '8px 12px',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '14px',
              backgroundColor: 'white'
            }}
          >
            <option value={3}>3</option>
            <option value={5}>5</option>
            <option value={10}>10</option>
          </select>
          <button
            onClick={handleGenerate}
            disabled={!content.trim()}
            style={{
              padding: '10px 20px',
              backgroundColor: content.trim() ? '#7c3aed' : '#94a3b8',
              color: 'white',
              border: 'none',
              borderRadius: '6px',
              cursor: content.trim() ? 'pointer' : 'not-allowed',
              fontSize: '14px',
              fontWeight: 500,
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            🃏 Generate Flashcards
          </button>
        </div>
        {activeProvider && (
          <p style={{ fontSize: '12px', color: '#94a3b8' }}>
            Powered by {activeProvider.activeProvider} ({activeProvider.model})
          </p>
        )}
      </div>
    );
  }

  const current = flashcards[currentIndex];

  return (
    <div style={{ marginTop: '16px' }}>
      {/* Progress indicator */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '12px'
      }}>
        <span style={{ fontSize: '14px', color: '#4b5563', fontWeight: 500 }}>
          Card {currentIndex + 1} of {flashcards.length}
        </span>
        <span style={{
          fontSize: '11px',
          color: '#64748b',
          backgroundColor: '#f1f5f9',
          padding: '2px 8px',
          borderRadius: '12px'
        }}>
          {activeProvider?.activeProvider ?? 'AI'}
        </span>
      </div>

      {/* Progress bar */}
      <div style={{
        width: '100%',
        height: '4px',
        backgroundColor: '#e2e8f0',
        borderRadius: '2px',
        marginBottom: '16px',
        overflow: 'hidden'
      }}>
        <div style={{
          width: `${((currentIndex + 1) / flashcards.length) * 100}%`,
          height: '100%',
          backgroundColor: '#7c3aed',
          borderRadius: '2px',
          transition: 'width 0.3s ease'
        }} />
      </div>

      {/* Flip card */}
      <div
        onClick={handleFlip}
        style={{
          width: '100%',
          minHeight: '200px',
          perspective: '1000px',
          cursor: 'pointer',
          marginBottom: '16px'
        }}
      >
        <div style={{
          position: 'relative',
          width: '100%',
          minHeight: '200px',
          transition: 'transform 0.6s',
          transformStyle: 'preserve-3d',
          transform: isFlipped ? 'rotateY(180deg)' : 'rotateY(0deg)'
        }}>
          {/* Front (Question) */}
          <div style={{
            position: 'absolute',
            width: '100%',
            minHeight: '200px',
            backfaceVisibility: 'hidden',
            backgroundColor: '#faf5ff',
            border: '2px solid #d8b4fe',
            borderRadius: '12px',
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center'
          }}>
            <span style={{ fontSize: '12px', color: '#a855f7', fontWeight: 600, marginBottom: '12px', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Question
            </span>
            <p style={{
              fontSize: '16px',
              color: '#1e1b4b',
              lineHeight: '1.6',
              textAlign: 'center',
              margin: 0,
              whiteSpace: 'pre-wrap'
            }}>
              {current.question}
            </p>
            <p style={{ fontSize: '11px', color: '#94a3b8', marginTop: '16px' }}>
              Click to reveal answer
            </p>
          </div>

          {/* Back (Answer) */}
          <div style={{
            position: 'absolute',
            width: '100%',
            minHeight: '200px',
            backfaceVisibility: 'hidden',
            backgroundColor: '#f0fdf4',
            border: '2px solid #86efac',
            borderRadius: '12px',
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            transform: 'rotateY(180deg)'
          }}>
            <span style={{ fontSize: '12px', color: '#16a34a', fontWeight: 600, marginBottom: '12px', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Answer
            </span>
            <p style={{
              fontSize: '16px',
              color: '#14532d',
              lineHeight: '1.6',
              textAlign: 'center',
              margin: 0,
              whiteSpace: 'pre-wrap'
            }}>
              {current.answer}
            </p>
            <p style={{ fontSize: '11px', color: '#94a3b8', marginTop: '16px' }}>
              Click to show question
            </p>
          </div>
        </div>
      </div>

      {/* Navigation buttons */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }}>
        <button
          onClick={handlePrevious}
          disabled={currentIndex === 0}
          style={{
            padding: '8px 16px',
            backgroundColor: currentIndex === 0 ? '#e2e8f0' : '#7c3aed',
            color: currentIndex === 0 ? '#94a3b8' : 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: currentIndex === 0 ? 'not-allowed' : 'pointer',
            fontSize: '14px',
            fontWeight: 500
          }}
        >
          ← Previous
        </button>

        <button
          onClick={() => {
            setIsFlipped(false);
            setFlashcards([]);
            setCurrentIndex(0);
          }}
          style={{
            padding: '8px 16px',
            backgroundColor: '#f1f5f9',
            color: '#475569',
            border: '1px solid #e2e8f0',
            borderRadius: '6px',
            cursor: 'pointer',
            fontSize: '14px',
            fontWeight: 500
          }}
        >
          🔄 New Cards
        </button>

        <button
          onClick={handleNext}
          disabled={currentIndex === flashcards.length - 1}
          style={{
            padding: '8px 16px',
            backgroundColor: currentIndex === flashcards.length - 1 ? '#e2e8f0' : '#7c3aed',
            color: currentIndex === flashcards.length - 1 ? '#94a3b8' : 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: currentIndex === flashcards.length - 1 ? 'not-allowed' : 'pointer',
            fontSize: '14px',
            fontWeight: 500
          }}
        >
          Next →
        </button>
      </div>
    </div>
  );
};

export default FlashcardViewer;