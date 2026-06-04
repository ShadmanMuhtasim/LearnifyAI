import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../services/api';
import { quizService, type QuestionType, type Quiz } from '../services/quizService';

interface NoteOption {
  id: string;
  content: string;
  courseTitle?: string | null;
  createdAt: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
}

const questionTypeOptions: Array<{ value: QuestionType; label: string }> = [
  { value: 'MultipleChoice', label: 'Multiple choice' },
  { value: 'TrueFalse', label: 'True/false' },
  { value: 'ShortAnswer', label: 'Short answer' },
  { value: 'FillInTheBlank', label: 'Fill-in-the-blank' },
];

const panelStyle: React.CSSProperties = {
  background: 'white',
  border: '1px solid #E2E8F0',
  borderRadius: '8px',
  padding: '1.25rem',
  boxShadow: '0 4px 18px rgba(15, 23, 42, 0.06)',
};

const primaryButtonStyle: React.CSSProperties = {
  background: '#2563EB',
  color: 'white',
  border: 'none',
  borderRadius: '8px',
  padding: '0.7rem 1.1rem',
  cursor: 'pointer',
  fontWeight: 600,
};

export default function Quizzes() {
  const navigate = useNavigate();
  const [notes, setNotes] = useState<NoteOption[]>([]);
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [noteId, setNoteId] = useState('');
  const [numberOfQuestions, setNumberOfQuestions] = useState(5);
  const [difficulty, setDifficulty] = useState('Medium');
  const [timeLimitMinutes, setTimeLimitMinutes] = useState('');
  const [questionTypes, setQuestionTypes] = useState<QuestionType[]>(['MultipleChoice']);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generateError, setGenerateError] = useState<string | null>(null);

  const selectedNote = useMemo(
    () => notes.find((note) => note.id === noteId),
    [notes, noteId]
  );

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [notesResponse, quizList] = await Promise.all([
        apiClient.get<ApiResponse<NoteOption[]>>('/api/notes'),
        quizService.getQuizzes(),
      ]);

      const nextNotes = notesResponse.data.data || [];
      setNotes(nextNotes);
      setQuizzes(quizList);
      setNoteId((current) => current || nextNotes[0]?.id || '');
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to load quiz data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const toggleQuestionType = (type: QuestionType) => {
    setQuestionTypes((current) => {
      if (current.includes(type)) {
        return current.length === 1 ? current : current.filter((item) => item !== type);
      }

      return [...current, type];
    });
  };

  const handleGenerate = async () => {
    if (!noteId) {
      setGenerateError('Select a note before generating a quiz.');
      return;
    }

    if (questionTypes.length === 0) {
      setGenerateError('Select at least one question type.');
      return;
    }

    try {
      setGenerating(true);
      setGenerateError(null);
      const quiz = await quizService.generateQuiz({
        noteId,
        numberOfQuestions,
        difficulty,
        questionTypes,
        timeLimitMinutes: timeLimitMinutes.trim() ? Number(timeLimitMinutes) : null,
      });

      setQuizzes((current) => [quiz, ...current.filter((item) => item.id !== quiz.id)]);
      navigate(`/quizzes/${quiz.id}`);
    } catch (err: any) {
      setGenerateError(err?.response?.data?.message || 'Failed to generate quiz.');
    } finally {
      setGenerating(false);
    }
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

  return (
    <div style={{ maxWidth: '1080px', margin: '0 auto', padding: '2rem 1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ margin: 0, color: '#1A202C' }}>Quizzes</h1>
          <p style={{ margin: '0.35rem 0 0', color: '#64748B' }}>
            Generate and take quizzes from your saved notes.
          </p>
        </div>
        <Link to="/notes" className="btn btn-outline-primary">Manage Notes</Link>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <section style={{ ...panelStyle, marginBottom: '1.5rem' }}>
        <h2 style={{ margin: '0 0 1rem', fontSize: '1.25rem', color: '#1E293B' }}>Generate Quiz</h2>

        {notes.length === 0 ? (
          <div style={{ padding: '1rem', background: '#FFFBEB', border: '1px solid #FDE68A', borderRadius: '8px', color: '#92400E' }}>
            Add or upload a note before generating a quiz.
          </div>
        ) : (
          <div style={{ display: 'grid', gap: '1rem' }}>
            <div>
              <label htmlFor="quiz-note" style={{ display: 'block', marginBottom: '0.4rem', fontWeight: 600 }}>
                Note
              </label>
              <select
                id="quiz-note"
                value={noteId}
                onChange={(event) => setNoteId(event.target.value)}
                style={{ width: '100%', padding: '0.7rem', borderRadius: '8px', border: '1px solid #CBD5E1' }}
              >
                {notes.map((note) => (
                  <option key={note.id} value={note.id}>
                    {(note.courseTitle || 'Untitled course')} - {note.content.slice(0, 80)}
                  </option>
                ))}
              </select>
              {selectedNote && (
                <p style={{ margin: '0.45rem 0 0', color: '#64748B', fontSize: '0.9rem' }}>
                  {selectedNote.content.slice(0, 180)}
                  {selectedNote.content.length > 180 ? '...' : ''}
                </p>
              )}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem' }}>
              <div>
                <label htmlFor="quiz-count" style={{ display: 'block', marginBottom: '0.4rem', fontWeight: 600 }}>
                  Questions
                </label>
                <input
                  id="quiz-count"
                  type="number"
                  min={1}
                  max={20}
                  value={numberOfQuestions}
                  onChange={(event) => setNumberOfQuestions(Number(event.target.value))}
                  style={{ width: '100%', padding: '0.7rem', borderRadius: '8px', border: '1px solid #CBD5E1' }}
                />
              </div>
              <div>
                <label htmlFor="quiz-difficulty" style={{ display: 'block', marginBottom: '0.4rem', fontWeight: 600 }}>
                  Difficulty
                </label>
                <select
                  id="quiz-difficulty"
                  value={difficulty}
                  onChange={(event) => setDifficulty(event.target.value)}
                  style={{ width: '100%', padding: '0.7rem', borderRadius: '8px', border: '1px solid #CBD5E1' }}
                >
                  <option>Easy</option>
                  <option>Medium</option>
                  <option>Hard</option>
                </select>
              </div>
              <div>
                <label htmlFor="quiz-timer" style={{ display: 'block', marginBottom: '0.4rem', fontWeight: 600 }}>
                  Timer
                </label>
                <input
                  id="quiz-timer"
                  type="number"
                  min={1}
                  max={240}
                  value={timeLimitMinutes}
                  onChange={(event) => setTimeLimitMinutes(event.target.value)}
                  placeholder="Optional minutes"
                  style={{ width: '100%', padding: '0.7rem', borderRadius: '8px', border: '1px solid #CBD5E1' }}
                />
              </div>
            </div>

            <div>
              <div style={{ marginBottom: '0.5rem', fontWeight: 600 }}>Question types</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem' }}>
                {questionTypeOptions.map((option) => (
                  <label key={option.value} style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}>
                    <input
                      type="checkbox"
                      checked={questionTypes.includes(option.value)}
                      onChange={() => toggleQuestionType(option.value)}
                    />
                    {option.label}
                  </label>
                ))}
              </div>
            </div>

            {generateError && <div className="alert alert-danger">{generateError}</div>}

            <div>
              <button
                type="button"
                onClick={() => void handleGenerate()}
                disabled={generating}
                style={{
                  ...primaryButtonStyle,
                  opacity: generating ? 0.7 : 1,
                  cursor: generating ? 'not-allowed' : 'pointer',
                }}
              >
                {generating ? 'Generating...' : 'Generate Quiz'}
              </button>
            </div>
          </div>
        )}
      </section>

      <section>
        <h2 style={{ margin: '0 0 1rem', fontSize: '1.25rem', color: '#1E293B' }}>Saved Quizzes</h2>
        {quizzes.length === 0 ? (
          <div style={panelStyle}>No quizzes yet.</div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))', gap: '1rem' }}>
            {quizzes.map((quiz) => (
              <Link
                key={quiz.id}
                to={`/quizzes/${quiz.id}`}
                style={{ ...panelStyle, display: 'block', textDecoration: 'none', color: '#1E293B' }}
              >
                <h3 style={{ margin: '0 0 0.45rem', fontSize: '1.05rem' }}>{quiz.title}</h3>
                <p style={{ margin: '0 0 0.7rem', color: '#64748B' }}>{quiz.description || 'Generated quiz'}</p>
                <div style={{ display: 'flex', justifyContent: 'space-between', color: '#475569', fontSize: '0.9rem' }}>
                  <span>{quiz.difficulty}</span>
                  <span>{quiz.questions.length} questions{quiz.timeLimitMinutes ? ` - ${quiz.timeLimitMinutes} min` : ''}</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
