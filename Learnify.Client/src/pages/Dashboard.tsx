import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { analyticsService, type DashboardAnalytics, type RecentActivity } from '../services/analyticsService';
import { Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader, StatCard } from '../components/UI/Primitives';

const emptyDashboard: DashboardAnalytics = {
  totalCourses: 0,
  totalNotes: 0,
  totalQuizzes: 0,
  totalQuizAttempts: 0,
  averageQuizScore: 0,
  bestQuizScore: 0,
  totalFlashcardsGenerated: 0,
  totalSummariesGenerated: 0,
  totalStudyTipsGenerated: 0,
  totalXp: 0,
  currentStreak: 0,
  longestStreak: 0,
  unlockedAchievements: 0,
  availableAchievements: 0,
  recentActivity: [],
};

const activityLabels: Record<string, string> = {
  CourseCreated: 'Created a course',
  NoteUploaded: 'Uploaded a note',
  PdfUploaded: 'Uploaded a PDF',
  SummaryGenerated: 'Generated a summary',
  FlashcardsGenerated: 'Generated flashcards',
  StudyTipsGenerated: 'Generated study tips',
  QuizGenerated: 'Generated a quiz',
  QuizAttemptSubmitted: 'Submitted a quiz attempt',
};

function formatPercent(value: number) {
  return `${Math.round(value)}%`;
}

function ActivityRow({ activity }: { activity: RecentActivity }) {
  return (
    <div className="list-row">
      <span>{activityLabels[activity.activityType] ?? activity.activityType}</span>
      <span className="muted text-small">
        +{activity.points} XP · {new Date(activity.occurredAt).toLocaleDateString()}
      </span>
    </div>
  );
}

export default function Dashboard() {
  const user = useAuthStore((state) => state.user);
  const [dashboard, setDashboard] = useState<DashboardAnalytics>(emptyDashboard);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadDashboard = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const nextDashboard = await analyticsService.getDashboard();
        if (isMounted) {
          setDashboard(nextDashboard);
        }
      } catch {
        if (isMounted) {
          setError('Unable to load your analytics right now.');
          setDashboard(emptyDashboard);
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

  if (isLoading) {
    return <LoadingState label="Loading dashboard..." />;
  }

  const hasLearningData = dashboard.totalCourses + dashboard.totalNotes + dashboard.totalQuizzes > 0;
  const aiActions = dashboard.totalSummariesGenerated + dashboard.totalFlashcardsGenerated + dashboard.totalStudyTipsGenerated;

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Dashboard"
        title={`Good morning, ${user?.name || 'Learner'}.`}
        subtitle="Your learning progress is calculated from your real courses, notes, quizzes, and AI study actions."
        actions={<Badge tone="primary">{dashboard.totalXp} XP</Badge>}
      />

      {error && <ErrorState message={error} />}

      <div className="grid grid-4">
        <StatCard label="Courses" value={dashboard.totalCourses} detail="active learning paths" tone="success" />
        <StatCard label="Notes" value={dashboard.totalNotes} detail="saved study notes" tone="primary" />
        <StatCard label="Quizzes" value={dashboard.totalQuizzes} detail="generated quizzes" tone="default" />
        <StatCard label="Current Streak" value={dashboard.currentStreak} detail="activity days" tone="warning" />
        <StatCard label="Average Quiz Score" value={formatPercent(dashboard.averageQuizScore)} detail="submitted attempts" tone="primary" />
        <StatCard label="Best Quiz Score" value={formatPercent(dashboard.bestQuizScore)} detail={`${dashboard.totalQuizAttempts} attempts`} tone="success" />
        <StatCard label="AI Study Actions" value={aiActions} detail="summary, cards, and tips" tone="default" />
        <StatCard
          label="Achievements"
          value={`${dashboard.unlockedAchievements}/${dashboard.availableAchievements}`}
          detail="unlocked"
          tone="warning"
        />
      </div>

      {!hasLearningData && (
        <EmptyState
          title="No learning activity yet"
          message="Create a course or upload a note to start building analytics. New accounts stay empty until you add your own material."
          action={<Link to="/courses" className="ui-button ui-button-primary">Create Course</Link>}
        />
      )}

      <div className="page-two-column">
        <Card className="stack">
          <div className="split">
            <div>
              <h2>Recent Activity</h2>
              <p className="muted text-small mt-3">Tracked from your own learning actions.</p>
            </div>
            <Link to="/analytics" className="ui-button ui-button-secondary">View Analytics</Link>
          </div>

          {dashboard.recentActivity.length === 0 ? (
            <p className="muted">No activity has been recorded yet.</p>
          ) : (
            <div className="stack">
              {dashboard.recentActivity.map((activity) => (
                <ActivityRow key={activity.id} activity={activity} />
              ))}
            </div>
          )}
        </Card>

        <div className="stack">
          <Card>
            <div className="eyebrow mb-3">Quick Actions</div>
            <div className="grid grid-2">
              <Link to="/quizzes" className="ui-button ui-button-secondary">New Quiz</Link>
              <Link to="/notes" className="ui-button ui-button-secondary">Upload Note</Link>
              <Link to="/flashcards" className="ui-button ui-button-secondary">Review Flashcards</Link>
              <Link to="/achievements" className="ui-button ui-button-secondary">Achievements</Link>
            </div>
          </Card>

          <Card style={{ background: 'linear-gradient(135deg, #4f46e5, #3525cd)', color: '#ffffff' }}>
            <div className="eyebrow" style={{ color: 'rgba(255,255,255,0.72)' }}>Next Step</div>
            {dashboard.totalNotes > 0 ? (
              <>
                <h2 style={{ color: '#ffffff', marginTop: 10 }}>Practice from your notes</h2>
                <p style={{ color: 'rgba(255,255,255,0.82)', marginTop: 8 }}>
                  Generate flashcards, study tips, or a quiz from a real note.
                </p>
                <Link to="/notes" className="ui-button" style={{ marginTop: 16, background: '#ffffff', color: '#3525cd' }}>
                  Open Notes
                </Link>
              </>
            ) : (
              <>
                <h2 style={{ color: '#ffffff', marginTop: 10 }}>Add your first note</h2>
                <p style={{ color: 'rgba(255,255,255,0.82)', marginTop: 8 }}>
                  Analytics and recommendations appear after you add learning material.
                </p>
                <Link to="/notes" className="ui-button" style={{ marginTop: 16, background: '#ffffff', color: '#3525cd' }}>
                  Add Note
                </Link>
              </>
            )}
          </Card>

          <Card>
            <div className="split">
              <span className="eyebrow">Study Totals</span>
              <Badge tone="muted">{dashboard.longestStreak} day best streak</Badge>
            </div>
            <div className="stack mt-3">
              <div className="list-row">
                <span>Summaries</span>
                <strong>{dashboard.totalSummariesGenerated}</strong>
              </div>
              <div className="list-row">
                <span>Flashcard sets</span>
                <strong>{dashboard.totalFlashcardsGenerated}</strong>
              </div>
              <div className="list-row">
                <span>Study tips</span>
                <strong>{dashboard.totalStudyTipsGenerated}</strong>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
