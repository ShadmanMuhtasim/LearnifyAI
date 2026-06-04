import { useState, useCallback, useEffect, useMemo } from 'react';
import { generateFlashcards, getActiveProvider, type FlashcardItem as ApiFlashcardItem, type ActiveProviderResponse } from '../../services/aiService';

interface FlashcardViewerProps {
  noteId: string;
  content: string;
}

type ConfidenceRating = 'got' | 'review';

const FlashcardViewer: React.FC<FlashcardViewerProps> = ({ noteId, content }) => {
  const [flashcards, setFlashcards] = useState<ApiFlashcardItem[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [count, setCount] = useState(5);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isFlipped, setIsFlipped] = useState(false);
  const [activeProvider, setActiveProvider] = useState<ActiveProviderResponse | null>(null);
  const [ratings, setRatings] = useState<Record<number, ConfidenceRating>>({});

  useEffect(() => {
    const loadProvider = async () => {
      try {
        const provider = await getActiveProvider();
        setActiveProvider(provider);
      } catch {
        // Provider info is helpful context, but the viewer still works without it.
      }
    };

    void loadProvider();
  }, []);

  const resetDeck = useCallback(() => {
    setFlashcards([]);
    setRatings({});
    setIsFlipped(false);
    setCurrentIndex(0);
  }, []);

  const handleGenerate = useCallback(async () => {
    if (!content.trim()) {
      setError('Note content is empty.');
      return;
    }

    setLoading(true);
    setError(null);
    resetDeck();

    try {
      const result = await generateFlashcards({ noteId, content, count });
      setFlashcards(result.flashcards);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to generate flashcards.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [noteId, content, count, resetDeck]);

  const handlePrevious = useCallback(() => {
    setIsFlipped(false);
    setCurrentIndex((prev) => Math.max(0, prev - 1));
  }, []);

  const handleNext = useCallback(() => {
    setIsFlipped(false);
    setCurrentIndex((prev) => Math.min(flashcards.length - 1, prev + 1));
  }, [flashcards.length]);

  const handleFlip = useCallback(() => {
    setIsFlipped((prev) => !prev);
  }, []);

  const handleShuffle = useCallback(() => {
    setFlashcards((currentDeck) => {
      const deckWithRatings = currentDeck.map((card, index) => ({
        card,
        rating: ratings[index],
      }));

      for (let index = deckWithRatings.length - 1; index > 0; index -= 1) {
        const swapIndex = Math.floor(Math.random() * (index + 1));
        [deckWithRatings[index], deckWithRatings[swapIndex]] = [deckWithRatings[swapIndex], deckWithRatings[index]];
      }

      const nextRatings: Record<number, ConfidenceRating> = {};
      deckWithRatings.forEach((item, index) => {
        if (item.rating) {
          nextRatings[index] = item.rating;
        }
      });

      setRatings(nextRatings);
      return deckWithRatings.map((item) => item.card);
    });

    setCurrentIndex(0);
    setIsFlipped(false);
  }, [ratings]);

  const handleRate = useCallback((rating: ConfidenceRating) => {
    setRatings((current) => ({
      ...current,
      [currentIndex]: rating,
    }));
  }, [currentIndex]);

  useEffect(() => {
    if (flashcards.length === 0) {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      const tagName = target?.tagName;
      const isTypingTarget = tagName === 'INPUT' || tagName === 'TEXTAREA' || tagName === 'SELECT' || target?.isContentEditable;

      if (isTypingTarget) {
        return;
      }

      if (event.key === 'ArrowLeft') {
        handlePrevious();
      }

      if (event.key === 'ArrowRight') {
        handleNext();
      }

      if (event.code === 'Space') {
        event.preventDefault();
        handleFlip();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [flashcards.length, handleFlip, handleNext, handlePrevious]);

  const scoreSummary = useMemo(() => {
    const values = Object.values(ratings);
    const got = values.filter((rating) => rating === 'got').length;
    const review = values.filter((rating) => rating === 'review').length;
    const score = flashcards.length > 0 ? Math.round((got / flashcards.length) * 100) : 0;

    return {
      got,
      review,
      answered: values.length,
      score,
    };
  }, [flashcards.length, ratings]);

  if (loading) {
    return (
      <div style={{
        padding: '16px',
        borderRadius: '8px',
        backgroundColor: '#f8f9fa',
        border: '1px solid #e9ecef',
        textAlign: 'center',
      }}>
        <div style={{
          display: 'inline-block',
          width: '32px',
          height: '32px',
          border: '3px solid #e9ecef',
          borderTopColor: '#7c3aed',
          borderRadius: '50%',
          animation: 'spin 0.8s linear infinite',
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
        marginBottom: '12px',
      }}>
        <strong>Error:</strong> {error}
        <button
          type="button"
          onClick={() => setError(null)}
          style={{
            marginLeft: '12px',
            padding: '4px 12px',
            border: '1px solid #fc8181',
            borderRadius: '4px',
            backgroundColor: 'white',
            cursor: 'pointer',
            color: '#c53030',
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
            onChange={(event) => setCount(Number(event.target.value))}
            style={{
              padding: '8px 12px',
              borderRadius: '6px',
              border: '1px solid #d1d5db',
              fontSize: '14px',
              backgroundColor: 'white',
            }}
          >
            <option value={3}>3</option>
            <option value={5}>5</option>
            <option value={10}>10</option>
          </select>
          <button
            type="button"
            onClick={() => void handleGenerate()}
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
            }}
          >
            Generate Flashcards
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
  const currentRating = ratings[currentIndex];

  return (
    <div style={{ marginTop: '16px' }}>
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '1rem',
        marginBottom: '12px',
        flexWrap: 'wrap',
      }}>
        <span style={{ fontSize: '14px', color: '#4b5563', fontWeight: 500 }}>
          Card {currentIndex + 1} of {flashcards.length}
        </span>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
          <span style={{
            fontSize: '12px',
            color: '#475569',
            backgroundColor: '#f1f5f9',
            padding: '4px 8px',
            borderRadius: '8px',
          }}>
            Score {scoreSummary.score}%
          </span>
          <span style={{
            fontSize: '12px',
            color: '#475569',
            backgroundColor: '#f1f5f9',
            padding: '4px 8px',
            borderRadius: '8px',
          }}>
            {scoreSummary.answered}/{flashcards.length} marked
          </span>
          <span style={{
            fontSize: '12px',
            color: '#64748b',
            backgroundColor: '#f8fafc',
            padding: '4px 8px',
            borderRadius: '8px',
          }}>
            {activeProvider?.activeProvider ?? 'AI'}
          </span>
        </div>
      </div>

      <div style={{
        width: '100%',
        height: '4px',
        backgroundColor: '#e2e8f0',
        borderRadius: '2px',
        marginBottom: '16px',
        overflow: 'hidden',
      }}>
        <div style={{
          width: `${((currentIndex + 1) / flashcards.length) * 100}%`,
          height: '100%',
          backgroundColor: '#7c3aed',
          borderRadius: '2px',
          transition: 'width 0.3s ease',
        }} />
      </div>

      <button
        type="button"
        onClick={handleFlip}
        style={{
          display: 'block',
          width: '100%',
          minHeight: '220px',
          perspective: '1000px',
          cursor: 'pointer',
          marginBottom: '16px',
          padding: 0,
          border: 'none',
          background: 'transparent',
          textAlign: 'initial',
        }}
      >
        <div style={{
          position: 'relative',
          width: '100%',
          minHeight: '220px',
          transition: 'transform 0.6s',
          transformStyle: 'preserve-3d',
          transform: isFlipped ? 'rotateY(180deg)' : 'rotateY(0deg)',
        }}>
          <div style={{
            position: 'absolute',
            width: '100%',
            minHeight: '220px',
            backfaceVisibility: 'hidden',
            backgroundColor: '#faf5ff',
            border: '2px solid #d8b4fe',
            borderRadius: '12px',
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
          }}>
            <span style={{ fontSize: '12px', color: '#7e22ce', fontWeight: 700, marginBottom: '12px', textTransform: 'uppercase' }}>
              Question
            </span>
            <p style={{
              fontSize: '16px',
              color: '#1e1b4b',
              lineHeight: '1.6',
              textAlign: 'center',
              margin: 0,
              whiteSpace: 'pre-wrap',
            }}>
              {current.question}
            </p>
            <p style={{ fontSize: '11px', color: '#64748b', marginTop: '16px' }}>
              Click to reveal answer
            </p>
          </div>

          <div style={{
            position: 'absolute',
            width: '100%',
            minHeight: '220px',
            backfaceVisibility: 'hidden',
            backgroundColor: '#f0fdf4',
            border: '2px solid #86efac',
            borderRadius: '12px',
            padding: '24px',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            transform: 'rotateY(180deg)',
          }}>
            <span style={{ fontSize: '12px', color: '#15803d', fontWeight: 700, marginBottom: '12px', textTransform: 'uppercase' }}>
              Answer
            </span>
            <p style={{
              fontSize: '16px',
              color: '#14532d',
              lineHeight: '1.6',
              textAlign: 'center',
              margin: 0,
              whiteSpace: 'pre-wrap',
            }}>
              {current.answer}
            </p>
            <p style={{ fontSize: '11px', color: '#64748b', marginTop: '16px' }}>
              Click to show question
            </p>
          </div>
        </div>
      </button>

      <div style={{
        display: 'flex',
        gap: '0.75rem',
        alignItems: 'center',
        justifyContent: 'center',
        flexWrap: 'wrap',
        marginBottom: '16px',
      }}>
        <button
          type="button"
          onClick={() => handleRate('got')}
          style={{
            padding: '8px 14px',
            borderRadius: '6px',
            border: currentRating === 'got' ? '1px solid #15803d' : '1px solid #bbf7d0',
            backgroundColor: currentRating === 'got' ? '#dcfce7' : 'white',
            color: '#166534',
            cursor: 'pointer',
            fontSize: '14px',
            fontWeight: 600,
          }}
        >
          Got It
        </button>
        <button
          type="button"
          onClick={() => handleRate('review')}
          style={{
            padding: '8px 14px',
            borderRadius: '6px',
            border: currentRating === 'review' ? '1px solid #b45309' : '1px solid #fde68a',
            backgroundColor: currentRating === 'review' ? '#fef3c7' : 'white',
            color: '#92400e',
            cursor: 'pointer',
            fontSize: '14px',
            fontWeight: 600,
          }}
        >
          Review Again
        </button>
      </div>

      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '0.75rem',
        flexWrap: 'wrap',
      }}>
        <button
          type="button"
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
            fontWeight: 500,
          }}
        >
          Previous
        </button>

        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', justifyContent: 'center' }}>
          <button
            type="button"
            onClick={handleShuffle}
            style={{
              padding: '8px 16px',
              backgroundColor: '#fff7ed',
              color: '#9a3412',
              border: '1px solid #fed7aa',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 500,
            }}
          >
            Shuffle
          </button>
          <button
            type="button"
            onClick={resetDeck}
            style={{
              padding: '8px 16px',
              backgroundColor: '#f1f5f9',
              color: '#475569',
              border: '1px solid #e2e8f0',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 500,
            }}
          >
            New Cards
          </button>
        </div>

        <button
          type="button"
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
            fontWeight: 500,
          }}
        >
          Next
        </button>
      </div>

      <div style={{
        marginTop: '12px',
        color: '#64748b',
        fontSize: '12px',
        display: 'flex',
        gap: '0.75rem',
        flexWrap: 'wrap',
      }}>
        <span>Got it: {scoreSummary.got}</span>
        <span>Review again: {scoreSummary.review}</span>
      </div>
    </div>
  );
};

export default FlashcardViewer;
