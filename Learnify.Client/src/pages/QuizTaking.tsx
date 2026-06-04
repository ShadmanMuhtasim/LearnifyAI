import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { quizService, type Quiz } from '../services/quizService';
import { AppButton, Badge, Card, ErrorState, LoadingState, PageHeader } from '../components/UI/Primitives';

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
    return <LoadingState label="Loading quiz..." />;
  }

  if (!quiz) {
    return (
      <div className="stack">
        {error && <ErrorState message={error} />}
        <Link to="/quizzes" className="btn btn-outline-primary">Back to Quizzes</Link>
      </div>
    );
  }

  return (
    <div className="stack">
      <Link to="/quizzes" className="btn btn-link" style={{ justifySelf: 'start' }}>Back to Quizzes</Link>

      <PageHeader
        title={quiz.title}
        subtitle={`${quiz.difficulty} - ${quiz.questions.length} questions - ${answeredCount}/${quiz.questions.length} answered`}
        actions={timeRemaining !== null ? <Badge tone={timeRemaining <= 60 ? 'warning' : 'primary'}>{formatTime(timeRemaining)} left</Badge> : <Badge tone="muted">No timer</Badge>}
      />

      <Card className="split">
        <div className="cluster">
          <strong>Mode</strong>
          <label className="quiz-option" style={{ width: 'auto' }}>
            <input type="radio" checked={mode === 'practice'} onChange={() => handleModeChange('practice')} />
            Practice
          </label>
          <label className="quiz-option" style={{ width: 'auto' }}>
            <input type="radio" checked={mode === 'exam'} onChange={() => handleModeChange('exam')} />
            Exam
          </label>
        </div>
        {timeRemaining !== null && <strong>Timer: {formatTime(timeRemaining)}</strong>}
      </Card>

      {error && <ErrorState message={error} />}
      {timeExpired && <div className="alert alert-warning">Time expired. Submitting your quiz...</div>}

      <div className="stack">
        {quiz.questions.map((question, index) => (
          <Card key={question.id} className="stack">
            <div className="split">
              <h2 style={{ fontSize: '1rem' }}>{index + 1}. {question.questionText}</h2>
              <div className="cluster">
                <Badge tone="primary">{question.type}</Badge>
                <Badge tone="muted">{question.points} pt</Badge>
              </div>
            </div>

            {question.type === 'ShortAnswer' || question.type === 'FillInTheBlank' ? (
              <textarea
                value={answers[question.id] || ''}
                onChange={(event) => setAnswer(question.id, event.target.value)}
                rows={3}
                disabled={timeExpired || submitting}
                placeholder={question.type === 'FillInTheBlank' ? 'Fill in the blank...' : 'Type your answer...'}
              />
            ) : (
              <div className="stack" style={{ gap: 10 }}>
                {(question.type === 'TrueFalse' ? ['True', 'False'] : question.options).map((option) => (
                  <label key={option} className="quiz-option">
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
              <div className={
                normalizeAnswer(answers[question.id]) === normalizeAnswer(question.correctAnswer)
                  ? 'alert alert-success'
                  : 'alert alert-danger'
              }>
                <div>
                  <strong>
                    {normalizeAnswer(answers[question.id]) === normalizeAnswer(question.correctAnswer)
                      ? 'Correct'
                      : 'Review'}
                  </strong>
                  {normalizeAnswer(answers[question.id]) !== normalizeAnswer(question.correctAnswer) &&
                    ` - Correct answer: ${question.correctAnswer}`}
                </div>
                {question.explanation && <div className="mt-3">{question.explanation}</div>}
              </div>
            )}
          </Card>
        ))}
      </div>

      <div className="cluster" style={{ justifyContent: 'flex-end' }}>
        <AppButton type="button" onClick={() => void handleSubmit()} disabled={submitting || timeExpired}>
          {submitting ? 'Submitting...' : 'Submit Quiz'}
        </AppButton>
      </div>
    </div>
  );
}
