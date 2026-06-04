import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { quizService, type Quiz } from '../services/quizService';

const panelStyle: React.CSSProperties = {
  background: 'white',
  border: '1px solid #E2E8F0',
  borderRadius: '8px',
  padding: '1.25rem',
  boxShadow: '0 4px 18px rgba(15, 23, 42, 0.06)',
};

export default function QuizTaking() {
  const { id } = useParams<{ id: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const initialMode = searchParams.get('mode') === 'practice' ? 'practice' : 'exam';
  const [quiz, setQuiz] = useState<Quiz | null>(null);
  const [mode, setMode] = useState<'practice' | 'exam'>(initialMode);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [timeRemaining, setTimeRemaining] = useState<number | null>(null);
  const [timeExpired, setTimeExpired] = useState(false);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const answeredCount = useMemo(
    () => Object.values(answers).filter((answer) => answer.trim()).length,
    [answers]
  );

  useEffect(() => {
    const loadQuiz = async () => {
      if (!id) {
        setError('Quiz id is missing.');
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const loadedQuiz = await quizService.getQuiz(id, mode === 'practice');
        setQuiz(loadedQuiz);
        setTimeRemaining(loadedQuiz.timeLimitMinutes ? loadedQuiz.timeLimitMinutes * 60 : null);
        setTimeExpired(false);
        setSubmitted(false);
      } catch (err: any) {
        setError(err?.response?.data?.message || 'Failed to load quiz.');
      } finally {
        setLoading(false);
      }
    };

    void loadQuiz();
  }, [id, mode]);

  const setAnswer = (questionId: string, value: string) => {
    setAnswers((current) => ({ ...current, [questionId]: value }));
  };

  const handleModeChange = (nextMode: 'practice' | 'exam') => {
    setMode(nextMode);
    setSearchParams({ mode: nextMode });
    setAnswers({});
  };

  const normalizeAnswer = (value: string) =>
    value.trim().split(/\s+/).filter(Boolean).join(' ').toLowerCase();

  const handleSubmit = useCallback(async () => {
    if (!quiz) {
      return;
    }

    if (answeredCount === 0) {
      setError('Answer at least one question before submitting.');
      return;
    }

    try {
      setSubmitting(true);
      setSubmitted(true);
      setError(null);
      const result = await quizService.submitAttempt(
        quiz.id,
        quiz.questions.map((question) => ({
          questionId: question.id,
          userAnswer: answers[question.id] || '',
        }))
      );

      navigate(`/quizzes/${quiz.id}/result`, { state: { result, quizTitle: quiz.title } });
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to submit quiz.');
      setSubmitted(false);
    } finally {
      setSubmitting(false);
    }
  }, [answeredCount, answers, navigate, quiz]);

  useEffect(() => {
    if (timeRemaining === null || timeExpired || submitted || submitting) {
      return;
    }

    if (timeRemaining <= 0) {
      setTimeExpired(true);
      void handleSubmit();
      return;
    }

    const timerId = window.setTimeout(() => {
      setTimeRemaining((current) => current === null ? null : Math.max(current - 1, 0));
    }, 1000);

    return () => window.clearTimeout(timerId);
  }, [handleSubmit, submitted, submitting, timeExpired, timeRemaining]);

  const formatTime = (seconds: number) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  if (!quiz) {
    return (
      <div className="container mt-4">
        {error && <div className="alert alert-danger">{error}</div>}
        <Link to="/quizzes" className="btn btn-outline-primary">Back to Quizzes</Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '920px', margin: '0 auto', padding: '2rem 1rem' }}>
      <Link to="/quizzes" className="btn btn-link px-0">Back to Quizzes</Link>
      <div style={{ marginBottom: '1.25rem' }}>
        <h1 style={{ margin: 0, color: '#1A202C' }}>{quiz.title}</h1>
        <p style={{ margin: '0.35rem 0 0', color: '#64748B' }}>
          {quiz.difficulty} - {quiz.questions.length} questions - {answeredCount}/{quiz.questions.length} answered
          {timeRemaining !== null ? ` - ${formatTime(timeRemaining)} left` : ''}
        </p>
      </div>

      <div style={{ ...panelStyle, marginBottom: '1rem', display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
        <strong>Mode</strong>
        <label style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}>
          <input
            type="radio"
            checked={mode === 'practice'}
            onChange={() => handleModeChange('practice')}
          />
          Practice
        </label>
        <label style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}>
          <input
            type="radio"
            checked={mode === 'exam'}
            onChange={() => handleModeChange('exam')}
          />
          Exam
        </label>
        {timeRemaining !== null && (
          <span style={{ marginLeft: 'auto', color: timeRemaining <= 60 ? '#B45309' : '#334155', fontWeight: 700 }}>
            Timer: {formatTime(timeRemaining)}
          </span>
        )}
      </div>

      {error && <div className="alert alert-danger">{error}</div>}
      {timeExpired && <div className="alert alert-warning">Time expired. Submitting your quiz...</div>}

      <div style={{ display: 'grid', gap: '1rem' }}>
        {quiz.questions.map((question, index) => (
          <section key={question.id} style={panelStyle}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', marginBottom: '0.75rem' }}>
              <h2 style={{ margin: 0, fontSize: '1rem', color: '#1E293B' }}>
                {index + 1}. {question.questionText}
              </h2>
              <span style={{ color: '#64748B', whiteSpace: 'nowrap' }}>{question.points} pt</span>
            </div>

            {question.type === 'ShortAnswer' || question.type === 'FillInTheBlank' ? (
              <textarea
                value={answers[question.id] || ''}
                onChange={(event) => setAnswer(question.id, event.target.value)}
                rows={3}
                disabled={timeExpired || submitting}
                placeholder={question.type === 'FillInTheBlank' ? 'Fill in the blank...' : 'Type your answer...'}
                style={{ width: '100%', padding: '0.75rem', border: '1px solid #CBD5E1', borderRadius: '8px' }}
              />
            ) : (
              <div style={{ display: 'grid', gap: '0.6rem' }}>
                {(question.type === 'TrueFalse' ? ['True', 'False'] : question.options).map((option) => (
                  <label
                    key={option}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.55rem',
                      padding: '0.65rem 0.75rem',
                      border: '1px solid #CBD5E1',
                      borderRadius: '8px',
                      cursor: 'pointer',
                    }}
                  >
                    <input
                      type="radio"
                      name={question.id}
                      value={option}
                      checked={(answers[question.id] || '') === option}
                      disabled={timeExpired || submitting}
                      onChange={() => setAnswer(question.id, option)}
                    />
                    {option}
                  </label>
                ))}
              </div>
            )}

            {mode === 'practice' && answers[question.id]?.trim() && question.correctAnswer && (
              <div style={{
                marginTop: '0.85rem',
                padding: '0.75rem',
                borderRadius: '8px',
                background: normalizeAnswer(answers[question.id]) === normalizeAnswer(question.correctAnswer)
                  ? '#F0FDF4'
                  : '#FEF2F2',
                border: normalizeAnswer(answers[question.id]) === normalizeAnswer(question.correctAnswer)
                  ? '1px solid #A7F3D0'
                  : '1px solid #FECACA',
                color: '#334155',
              }}>
                <div>
                  <strong>
                    {normalizeAnswer(answers[question.id]) === normalizeAnswer(question.correctAnswer)
                      ? 'Correct'
                      : 'Review'}
                  </strong>
                  {normalizeAnswer(answers[question.id]) !== normalizeAnswer(question.correctAnswer) &&
                    ` - Correct answer: ${question.correctAnswer}`}
                </div>
                {question.explanation && <div style={{ marginTop: '0.35rem' }}>{question.explanation}</div>}
              </div>
            )}
          </section>
        ))}
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.25rem' }}>
        <button
          type="button"
          onClick={() => void handleSubmit()}
          disabled={submitting || timeExpired}
          style={{
            background: '#2563EB',
            color: 'white',
            border: 'none',
            borderRadius: '8px',
            padding: '0.75rem 1.25rem',
            fontWeight: 600,
            opacity: submitting ? 0.7 : 1,
            cursor: submitting ? 'not-allowed' : 'pointer',
          }}
        >
          {submitting ? 'Submitting...' : 'Submit Quiz'}
        </button>
      </div>
    </div>
  );
}
