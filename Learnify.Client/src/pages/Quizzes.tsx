import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../services/api';
import { quizService, type QuestionType, type Quiz } from '../services/quizService';
import { AppButton, Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader } from '../components/UI/Primitives';

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
    return <LoadingState label="Loading quizzes..." />;
  }

  return (
    <div className="stack">
      <PageHeader
        title="Quizzes"
        subtitle="Generate practice or exam-style quizzes from your saved notes."
        actions={<Link to="/notes" className="btn btn-outline-primary">Manage Notes</Link>}
      />

      {error && <ErrorState message={error} />}

      <Card className="stack">
        <div className="split">
          <div>
            <h2>Generate Quiz</h2>
            <p className="muted mt-3">Choose a source note, difficulty, question mix, and optional timer.</p>
          </div>
          <Badge tone="primary">AI generated</Badge>
        </div>

        {notes.length === 0 ? (
          <div className="alert alert-warning">Add or upload a note before generating a quiz.</div>
        ) : (
          <div className="field-grid">
            <label>
              <span className="form-label">Note</span>
              <select id="quiz-note" value={noteId} onChange={(event) => setNoteId(event.target.value)}>
                {notes.map((note) => (
                  <option key={note.id} value={note.id}>
                    {(note.courseTitle || 'Untitled course')} - {note.content.slice(0, 80)}
                  </option>
                ))}
              </select>
              {selectedNote && (
                <p className="muted text-small mt-3">
                  {selectedNote.content.slice(0, 180)}
                  {selectedNote.content.length > 180 ? '...' : ''}
                </p>
              )}
            </label>

            <div className="grid grid-3">
              <label>
                <span className="form-label">Questions</span>
                <input
                  id="quiz-count"
                  type="number"
                  min={1}
                  max={20}
                  value={numberOfQuestions}
                  onChange={(event) => setNumberOfQuestions(Number(event.target.value))}
                />
              </label>
              <label>
                <span className="form-label">Difficulty</span>
                <select id="quiz-difficulty" value={difficulty} onChange={(event) => setDifficulty(event.target.value)}>
                  <option>Easy</option>
                  <option>Medium</option>
                  <option>Hard</option>
                </select>
              </label>
              <label>
                <span className="form-label">Timer</span>
                <input
                  id="quiz-timer"
                  type="number"
                  min={1}
                  max={240}
                  value={timeLimitMinutes}
                  onChange={(event) => setTimeLimitMinutes(event.target.value)}
                  placeholder="Optional minutes"
                />
              </label>
            </div>

            <div>
              <div className="form-label">Question types</div>
              <div className="cluster">
                {questionTypeOptions.map((option) => (
                  <label key={option.value} className="quiz-option" style={{ width: 'auto' }}>
                    <input
                      type="checkbox"
                      checked={questionTypes.includes(option.value)}
                      onChange={() => toggleQuestionType(option.value)}
                    />
                    <span>{option.label}</span>
                  </label>
                ))}
              </div>
            </div>

            {generateError && <ErrorState message={generateError} />}

            <AppButton type="button" onClick={() => void handleGenerate()} disabled={generating} style={{ justifySelf: 'start' }}>
              {generating ? 'Generating...' : 'Generate Quiz'}
            </AppButton>
          </div>
        )}
      </Card>

      <section className="stack">
        <div className="split">
          <h2>Saved Quizzes</h2>
          <Badge tone="muted">{quizzes.length} total</Badge>
        </div>
        {quizzes.length === 0 ? (
          <EmptyState title="No quizzes yet" message="Generate a quiz from a saved note to start practicing." />
        ) : (
          <div className="grid grid-3">
            {quizzes.map((quiz) => (
              <Link key={quiz.id} to={`/quizzes/${quiz.id}`} style={{ textDecoration: 'none' }}>
                <Card className="stack">
                  <div className="split">
                    <h3>{quiz.title}</h3>
                    <Badge tone={quiz.difficulty === 'Hard' ? 'warning' : 'primary'}>{quiz.difficulty}</Badge>
                  </div>
                  <p className="muted">{quiz.description || 'Generated quiz'}</p>
                  <div className="cluster">
                    <Badge tone="muted">{quiz.questions.length} questions</Badge>
                    {quiz.timeLimitMinutes && <Badge tone="warning">{quiz.timeLimitMinutes} min</Badge>}
                    <Badge tone="success">Practice or exam</Badge>
                  </div>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
