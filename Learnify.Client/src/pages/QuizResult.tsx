import { Link, useLocation, useParams } from 'react-router-dom';
import { useMemo, useState } from 'react';
import type { QuizResult as QuizResultData } from '../services/quizService';
import { AppButton, Badge, Card, PageHeader, StatCard } from '../components/UI/Primitives';

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
      <div className="stack">
        <div className="alert alert-info">
          No quiz result is loaded. Submit a quiz attempt to view detailed results.
        </div>
        <Link to={id ? `/quizzes/${id}` : '/quizzes'} className="btn btn-outline-primary">
          Back to Quiz
        </Link>
      </div>
    );
  }

  const correctCount = result.answers.filter((answer) => answer.isCorrect).length;

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Quiz Result"
        title={state?.quizTitle || 'Quiz Result'}
        subtitle={`Completed ${new Date(result.completedAt).toLocaleString()}`}
        actions={<Badge tone={result.percentage >= 70 ? 'success' : 'warning'}>Result saved</Badge>}
      />

      <div className="grid grid-3">
        <StatCard label="Score" value={`${result.score}/${result.totalPoints}`} detail="points awarded" tone="primary" />
        <StatCard label="Percentage" value={`${result.percentage}%`} detail={result.percentage >= 70 ? 'strong pass' : 'needs review'} tone={result.percentage >= 70 ? 'success' : 'warning'} />
        <StatCard label="Breakdown" value={`${correctCount} correct`} detail={`${result.answers.length} total answers`} />
      </div>

      <div className="stack">
        {result.answers.map((answer, index) => (
          <Card
            key={answer.questionId}
            className={`stack ${answer.isCorrect ? 'result-correct' : 'result-incorrect'}`}
          >
            <div className="split">
              <h2 style={{ fontSize: '1rem' }}>{index + 1}. {answer.questionText}</h2>
              <Badge tone={answer.isCorrect ? 'success' : 'danger'}>
                {answer.isCorrect ? 'Correct' : 'Incorrect'}
              </Badge>
            </div>
            <div className="stack" style={{ gap: 8 }}>
              <div><strong>Your answer:</strong> {answer.userAnswer || 'No answer'}</div>
              <div><strong>Correct answer:</strong> {answer.correctAnswer}</div>
              <div><strong>Result:</strong> {answer.isCorrect ? 'Correct' : 'Incorrect'} ({answer.pointsAwarded}/{answer.points} pts)</div>
              {answer.explanation && <div><strong>Explanation:</strong> {answer.explanation}</div>}
            </div>
          </Card>
        ))}
      </div>

      <div className="cluster">
        <Link to={`/quizzes/${id}`} className="btn btn-primary">Retake Quiz</Link>
        {incorrectAnswers.length > 0 && (
          <AppButton
            type="button"
            variant="secondary"
            onClick={() => {
              setRetryActive(true);
              setRetrySubmitted(false);
              setRetryAnswers({});
            }}
          >
            Retry Incorrect Questions
          </AppButton>
        )}
        <Link to="/quizzes" className="btn btn-outline-secondary">Back to Quizzes</Link>
      </div>

      {retryActive && (
        <Card className="stack">
          <h2>Retry Incorrect Questions</h2>
          {retrySubmitted && (
            <div className="alert alert-info">
              Retry score: {retryScore}/{incorrectAnswers.length}
            </div>
          )}
          <div className="stack">
            {incorrectAnswers.map((answer, index) => (
              <div key={answer.questionId} className="stack" style={{ borderTop: index === 0 ? 'none' : '1px solid var(--border)', paddingTop: index === 0 ? 0 : '1rem' }}>
                <strong>{index + 1}. {answer.questionText}</strong>
                {answer.type === 'MultipleChoice' || answer.type === 'TrueFalse' ? (
                  <div className="stack" style={{ gap: 8 }}>
                    {(answer.type === 'TrueFalse' ? ['True', 'False'] : answer.options).map((option) => (
                      <label key={option} className="quiz-option">
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
                  />
                )}
                {retrySubmitted && (
                  <div className="alert alert-info">
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
          <AppButton type="button" onClick={() => setRetrySubmitted(true)} disabled={retrySubmitted} style={{ justifySelf: 'start' }}>
            Score Retry
          </AppButton>
        </Card>
      )}
    </div>
  );
}
