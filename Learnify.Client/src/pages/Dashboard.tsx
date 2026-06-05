import { Fragment, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';
import { Badge, Card, PageHeader, StatCard } from '../components/UI/Primitives';

type Course = {
  id: string;
  title: string;
};

type Note = {
  id: string;
  title: string;
  updatedAt?: string;
  createdAt?: string;
};

type Quiz = {
  id: string;
};

type DashboardTotals = {
  courses: number;
  notes: number;
  quizzes: number;
};

const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const periods = ['Morning', 'Afternoon', 'Evening'];

const unwrap = <T,>(response: unknown): T => {
  const value = response as { data?: unknown };
  const data = value?.data as { data?: T; items?: T } | T | undefined;
  return ((data as { data?: T })?.data ?? (data as { items?: T })?.items ?? data ?? response) as T;
};

const asArray = <T,>(value: unknown): T[] => {
  const unwrapped = unwrap<unknown>(value);
  return Array.isArray(unwrapped) ? (unwrapped as T[]) : [];
};

export default function Dashboard() {
  const user = useAuthStore((state) => state.user);
  const [totals, setTotals] = useState<DashboardTotals>({ courses: 0, notes: 0, quizzes: 0 });
  const [recentNotes, setRecentNotes] = useState<Note[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadDashboard = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const [coursesResponse, notesResponse, quizzesResponse] = await Promise.all([
          apiClient.get('/api/courses'),
          apiClient.get('/api/notes'),
          apiClient.get('/api/quizzes'),
        ]);

        const courses = asArray<Course>(coursesResponse);
        const notes = asArray<Note>(notesResponse);
        const quizzes = asArray<Quiz>(quizzesResponse);

        if (isMounted) {
          setTotals({
            courses: courses.length,
            notes: notes.length,
            quizzes: quizzes.length,
          });
          setRecentNotes(
            notes
              .slice()
              .sort((first, second) => {
                const firstDate = new Date(first.updatedAt ?? first.createdAt ?? 0).getTime();
                const secondDate = new Date(second.updatedAt ?? second.createdAt ?? 0).getTime();
                return secondDate - firstDate;
              })
              .slice(0, 3)
          );
        }
      } catch {
        if (isMounted) {
          setError('Unable to load your dashboard data right now.');
          setTotals({ courses: 0, notes: 0, quizzes: 0 });
          setRecentNotes([]);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void loadDashboard();

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Dashboard"
        title={`Good morning, ${user?.name || 'Learner'}.`}
        subtitle="Ready to level up today?"
        actions={<Badge tone="primary">{isLoading ? 'Syncing data' : 'Based on your data'}</Badge>}
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="grid grid-4">
        <StatCard label="Courses" value={totals.courses} detail="active learning paths" tone="success" />
        <StatCard label="Notes" value={totals.notes} detail="saved study notes" tone="primary" />
        <StatCard label="Quizzes" value={totals.quizzes} detail="generated quizzes" tone="default" />
        <StatCard label="Daily Streak" value="0" detail="coming soon" tone="warning" />
        <StatCard label="Weekly Progress" value="0%" detail="coming soon" tone="primary" />
        <StatCard label="AI Tools" value="4" detail="summary, cards, tips, quizzes" tone="default" />
      </div>

      <div className="page-two-column">
        <Card>
          <div className="split mb-4">
            <div>
              <h2>Weekly Learning Heatmap</h2>
              <p className="muted text-small">
                Activity analytics are not available yet, so new accounts start with an empty heatmap.
              </p>
            </div>
            <Badge tone="muted">Coming soon</Badge>
          </div>

          <div className="heatmap">
            <div className="heatmap-grid heatmap-grid--empty">
              <span />
              {days.map((day, index) => (
                <span key={day} className={`heatmap-label${index > 2 ? ' hide-mobile' : ''}`}>
                  {day}
                </span>
              ))}
              {periods.map((period) => (
                <Fragment key={period}>
                  <span className="heatmap-label">{period}</span>
                  {days.map((day, index) => (
                    <span
                      key={`${period}-${day}`}
                      className={`heatmap-cell level-0${index > 2 ? ' hide-mobile' : ''}`}
                      aria-label={`${period} ${day}: no activity recorded`}
                    />
                  ))}
                </Fragment>
              ))}
            </div>
          </div>
        </Card>

        <div className="stack">
          <Card>
            <div className="eyebrow mb-3">Quick Actions</div>
            <div className="grid grid-2">
              <Link to="/quizzes" className="ui-button ui-button-secondary">New Quiz</Link>
              <Link to="/notes" className="ui-button ui-button-secondary">Upload Note</Link>
              <Link to="/flashcards" className="ui-button ui-button-secondary">Review Flashcards</Link>
              <Link to="/settings" className="ui-button ui-button-secondary">Open Settings</Link>
            </div>
          </Card>

          <Card style={{ background: 'linear-gradient(135deg, #4f46e5, #3525cd)', color: '#ffffff' }}>
            <div className="eyebrow" style={{ color: 'rgba(255,255,255,0.72)' }}>AI Recommendation</div>
            {totals.notes > 0 ? (
              <>
                <h2 style={{ color: '#ffffff', marginTop: 10 }}>Review your latest notes</h2>
                <p style={{ color: 'rgba(255,255,255,0.82)', marginTop: 8 }}>
                  Use summary, flashcards, study tips, or a generated quiz from Note Detail.
                </p>
                <Link to="/notes" className="ui-button" style={{ marginTop: 16, background: '#ffffff', color: '#3525cd' }}>
                  Start Session
                </Link>
              </>
            ) : (
              <>
                <h2 style={{ color: '#ffffff', marginTop: 10 }}>Add your first note</h2>
                <p style={{ color: 'rgba(255,255,255,0.82)', marginTop: 8 }}>
                  Personalized recommendations appear after you add learning material.
                </p>
                <Link to="/notes" className="ui-button" style={{ marginTop: 16, background: '#ffffff', color: '#3525cd' }}>
                  Add Note
                </Link>
              </>
            )}
          </Card>

          <Card>
            <div className="split">
              <span className="eyebrow">Recent Notes</span>
              <Link to="/notes" className="text-small" style={{ color: 'var(--primary)', textDecoration: 'none' }}>
                View all
              </Link>
            </div>
            {recentNotes.length === 0 ? (
              <p className="muted mt-3">No notes yet. Open Notes to add your first study material.</p>
            ) : (
              <div className="stack mt-3">
                {recentNotes.map((note) => (
                  <Link key={note.id} to={`/notes/${note.id}`} className="list-row">
                    <span>{note.title}</span>
                    <span className="muted text-small">
                      {note.updatedAt || note.createdAt
                        ? new Date(note.updatedAt ?? note.createdAt ?? '').toLocaleDateString()
                        : 'Recently added'}
                    </span>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      <Card>
        <p className="muted text-small">
          AI features use your own notes and courses. New accounts start empty until you add materials.
        </p>
      </Card>
    </div>
  );
}
