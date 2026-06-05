import { useState, useEffect, useCallback } from 'react';
import { getStudyTips, type StudyTipsResponse } from '../../services/aiService';

interface StudyTipsProps {
  topic: string;
}

interface StudyTipsState {
  tips: string;
  loading: boolean;
  error: string | null;
}

const StudyTips: React.FC<StudyTipsProps> = ({ topic }) => {
  const [state, setState] = useState<StudyTipsState>({
    tips: '',
    loading: true,
    error: null
  });

  const fetchTips = useCallback(async (topicToFetch: string) => {
    if (!topicToFetch.trim()) {
      setState(prev => ({ ...prev, loading: false }));
      return;
    }

    setState(prev => ({ ...prev, loading: true, error: null }));

    try {
      const result: StudyTipsResponse = await getStudyTips({ topic: topicToFetch });
      setState({ tips: result.tips, loading: false, error: null });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch study tips.';
      setState({ tips: '', loading: false, error: message });
    }
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      fetchTips(topic);
    }, 300); // Debounce topic changes

    return () => clearTimeout(timeoutId);
  }, [topic, fetchTips]);

  if (state.loading && !state.tips) {
    return (
      <div style={{
        padding: '16px',
        borderRadius: '8px',
        backgroundColor: '#f8f9fa',
        border: '1px solid #e9ecef',
        marginTop: '12px'
      }}>
        <h4 style={{ margin: '0 0 12px 0', color: '#6c757d', fontSize: '14px' }}>
        AI Study Tips for "{topic}"
        </h4>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {[1, 2, 3, 4].map(i => (
            <div
              key={i}
              style={{
                height: '16px',
                backgroundColor: '#e9ecef',
                borderRadius: '4px',
                marginBottom: '8px',
                animation: `pulse ${1 + i * 0.2}s ease-in-out infinite alternate`
              }}
            />
          ))}
        </div>
        <style>{`
          @keyframes pulse {
            from { opacity: 0.5; }
            to { opacity: 1; }
          }
        `}</style>
      </div>
    );
  }

  if (state.error) {
    return (
      <div style={{
        padding: '12px 16px',
        borderRadius: '8px',
        backgroundColor: '#fff5f5',
        border: '1px solid #fed7d7',
        color: '#c53030',
        marginTop: '12px'
      }}>
        <strong>Error:</strong> {state.error}
        <button
          onClick={() => fetchTips(topic)}
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
          Retry
        </button>
      </div>
    );
  }

  if (!state.tips) {
    return (
      <div style={{
        padding: '16px',
        borderRadius: '8px',
        backgroundColor: '#f8f9fa',
        border: '1px solid #e9ecef',
        marginTop: '12px',
        textAlign: 'center',
        color: '#94a3b8'
      }}>
        Enter a topic to get AI-generated study tips.
      </div>
    );
  }

  return (
    <div style={{
      padding: '16px',
      borderRadius: '8px',
      backgroundColor: '#fffbeb',
      border: '1px solid #fde68a',
      marginTop: '12px',
      maxHeight: '60vh',
      overflowY: 'auto',
      overflowWrap: 'anywhere'
    }}>
      <h4 style={{ margin: '0 0 12px 0', color: '#92400e', fontSize: '14px' }}>
        AI Study Tips for "{topic}"
      </h4>
      <p style={{
        margin: 0,
        color: '#78350f',
        lineHeight: '1.8',
        whiteSpace: 'pre-wrap',
        overflowWrap: 'anywhere'
      }}>
        {state.tips}
      </p>
    </div>
  );
};

export default StudyTips;
