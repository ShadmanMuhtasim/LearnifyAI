import { Link, useLocation, useParams } from 'react-router-dom';
import { useMemo, useState } from 'react';
import type { QuizResult as QuizResultData } from '../services/quizService';

const panelStyle: React.CSSProperties = {
  background: 'white',
  border: '1px solid #E2E8F0',
  borderRadius: '8px',
  padding: '1.25rem',
  boxShadow: '0 4px 18px rgba(15, 23, 42, 0.06)',
};

export default function QuizResult() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const state = location.state as { result?: QuizResultData; quizTitle?: string } | null;
  const result = state?.result;
  const incorrectAnswers = useMemo(
    () => result?.answers.filter((answer) => !answer.isCorrect) || [],
    [result]
  );
  const [retryActive, setRetryActive] = useState(false);
  const [retryAnswers, setRetryAnswers] = useState<Record<string, string>>({});
  const [retrySubmitted, setRetrySubmitted] = useState(false);

  const normalizeAnswer = (value: string) =>
    value.trim().split(/\s+/).filter(Boolean).join(' ').toLowerCase();

  const retryScore = incorrectAnswers.filter((answer) =>
    normalizeAnswer(retryAnswers[answer.questionId] || '') === normalizeAnswer(answer.correctAnswer)
  ).length;

  if (!result || !id) {
    return (
      <div className="container mt-4">
        <div className="alert alert-info">
          No quiz result is loaded. Submit a quiz attempt to view detailed results.
        </div>
        <Link to={id ? `/quizzes/${id}` : '/quizzes'} className="btn btn-outline-primary">
          Back to Quiz
        </Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '920px', margin: '0 auto', padding: '2rem 1rem' }}>
      <div style={{ marginBottom: '1.25rem' }}>
        <h1 style={{ margin: 0, color: '#1A202C' }}>{state?.quizTitle || 'Quiz Result'}</h1>
        <p style={{ margin: '0.35rem 0 0', color: '#64748B' }}>
          Completed {new Date(result.completedAt).toLocaleString()}
        </p>
      </div>

      <section style={{ ...panelStyle, marginBottom: '1rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
          <div>
            <div style={{ color: '#64748B', fontWeight: 600 }}>Score</div>
            <div style={{ fontSize: '2rem', fontWeight: 700, color: '#1E293B' }}>
              {result.score}/{result.totalPoints}
            </div>
          </div>
          <div>
            <div style={{ color: '#64748B', fontWeight: 600 }}>Percentage</div>
            <div style={{ fontSize: '2rem', fontWeight: 700, color: result.percentage >= 70 ? '#047857' : '#B45309' }}>
              {result.percentage}%
            </div>
          </div>
          <div>
            <div style={{ color: '#64748B', fontWeight: 600 }}>Breakdown</div>
            <div style={{ fontSize: '1.1rem', fontWeight: 700, color: '#1E293B' }}>
              {result.answers.filter((answer) => answer.isCorrect).length} correct / {result.answers.length} total
            </div>
          </div>
        </div>
      </section>

      <div style={{ display: 'grid', gap: '1rem' }}>
        {result.answers.map((answer, index) => (
          <section
            key={answer.questionId}
            style={{
              ...panelStyle,
              borderColor: answer.isCorrect ? '#A7F3D0' : '#FECACA',
              background: answer.isCorrect ? '#F0FDF4' : '#FEF2F2',
            }}
          >
            <h2 style={{ margin: '0 0 0.75rem', fontSize: '1rem', color: '#1E293B' }}>
              {index + 1}. {answer.questionText}
            </h2>
            <div style={{ display: 'grid', gap: '0.45rem', color: '#334155' }}>
              <div><strong>Your answer:</strong> {answer.userAnswer || 'No answer'}</div>
              <div><strong>Correct answer:</strong> {answer.correctAnswer}</div>
              <div><strong>Result:</strong> {answer.isCorrect ? 'Correct' : 'Incorrect'} ({answer.pointsAwarded}/{answer.points} pts)</div>
              {answer.explanation && <div><strong>Explanation:</strong> {answer.explanation}</div>}
            </div>
          </section>
        ))}
      </div>

      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', marginTop: '1.25rem' }}>
        <Link to={`/quizzes/${id}`} className="btn btn-primary">Retake Quiz</Link>
        {incorrectAnswers.length > 0 && (
          <button
            type="button"
            className="btn btn-outline-primary"
            onClick={() => {
              setRetryActive(true);
              setRetrySubmitted(false);
              setRetryAnswers({});
            }}
          >
            Retry Incorrect Questions
          </button>
        )}
        <Link to="/quizzes" className="btn btn-outline-secondary">Back to Quizzes</Link>
      </div>

      {retryActive && (
        <section style={{ ...panelStyle, marginTop: '1.25rem' }}>
          <h2 style={{ margin: '0 0 1rem', fontSize: '1.1rem', color: '#1E293B' }}>
            Retry Incorrect Questions
          </h2>
          {retrySubmitted && (
            <div className="alert alert-info">
              Retry score: {retryScore}/{incorrectAnswers.length}
            </div>
          )}
          <div style={{ display: 'grid', gap: '1rem' }}>
            {incorrectAnswers.map((answer, index) => (
              <div key={answer.questionId} style={{ borderTop: index === 0 ? 'none' : '1px solid #E2E8F0', paddingTop: index === 0 ? 0 : '1rem' }}>
                <div style={{ marginBottom: '0.55rem', fontWeight: 700 }}>
                  {index + 1}. {answer.questionText}
                </div>
                {answer.type === 'MultipleChoice' || answer.type === 'TrueFalse' ? (
                  <div style={{ display: 'grid', gap: '0.45rem' }}>
                    {(answer.type === 'TrueFalse' ? ['True', 'False'] : answer.options).map((option) => (
                      <label key={option} style={{ display: 'flex', gap: '0.45rem', alignItems: 'center' }}>
                        <input
                          type="radio"
                          name={`retry-${answer.questionId}`}
                          checked={(retryAnswers[answer.questionId] || '') === option}
                          onChange={() => setRetryAnswers((current) => ({ ...current, [answer.questionId]: option }))}
                          disabled={retrySubmitted}
                        />
                        {option}
                      </label>
                    ))}
                  </div>
                ) : (
                  <input
                    type="text"
                    value={retryAnswers[answer.questionId] || ''}
                    onChange={(event) => setRetryAnswers((current) => ({ ...current, [answer.questionId]: event.target.value }))}
                    disabled={retrySubmitted}
                    placeholder="Try again..."
                    style={{ width: '100%', padding: '0.65rem', border: '1px solid #CBD5E1', borderRadius: '8px' }}
                  />
                )}
                {retrySubmitted && (
                  <div style={{ marginTop: '0.55rem', color: '#334155' }}>
                    <strong>
                      {normalizeAnswer(retryAnswers[answer.questionId] || '') === normalizeAnswer(answer.correctAnswer)
                        ? 'Correct'
                        : 'Still review'}
                    </strong>
                    {' - '}Correct answer: {answer.correctAnswer}
                    {answer.explanation && <div>{answer.explanation}</div>}
                  </div>
                )}
              </div>
            ))}
          </div>
          <div style={{ marginTop: '1rem' }}>
            <button
              type="button"
              className="btn btn-primary"
              onClick={() => setRetrySubmitted(true)}
              disabled={retrySubmitted}
            >
              Score Retry
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
