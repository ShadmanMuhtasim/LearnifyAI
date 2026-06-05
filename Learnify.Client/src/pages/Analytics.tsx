import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  analyticsService,
  type DashboardAnalytics,
  type QuizPerformance,
  type RecentActivity,
} from '../services/analyticsService';
import { Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader, StatCard } from '../components/UI/Primitives';

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
        +{activity.points} XP · {new Date(activity.occurredAt).toLocaleString()}
      </span>
    </div>
  );
}

export default function Analytics() {
  const [dashboard, setDashboard] = useState<DashboardAnalytics | null>(null);
  const [performance, setPerformance] = useState<QuizPerformance | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadAnalytics = async () => {
      try {
        setLoading(true);
        setError(null);
        const [nextDashboard, nextPerformance] = await Promise.all([
          analyticsService.getDashboard(),
          analyticsService.getQuizPerformance(),
        ]);

        if (isMounted) {
          setDashboard(nextDashboard);
          setPerformance(nextPerformance);
        }
      } catch {
        if (isMounted) {
          setError('Unable to load analytics right now.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    void loadAnalytics();

    return () => {
      isMounted = false;
    };
  }, []);

  if (loading) {
    return <LoadingState label="Loading analytics..." />;
  }

  if (!dashboard || !performance) {
    return <ErrorState message={error || 'Analytics are unavailable right now.'} />;
  }

  const hasActivity = dashboard.recentActivity.length > 0 || performance.attemptsCount > 0;

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Analytics"
        title="Learning Analytics"
        subtitle="Real activity, quiz performance, streaks, and XP from your own account."
        actions={<Link to="/dashboard" className="ui-button ui-button-secondary">Back to Dashboard</Link>}
      />

      {error && <ErrorState message={error} />}

      <div className="grid grid-4">
        <StatCard label="XP" value={dashboard.totalXp} detail="earned from activity" tone="primary" />
        <StatCard label="Current Streak" value={dashboard.currentStreak} detail="activity days" tone="warning" />
        <StatCard label="Average Score" value={formatPercent(performance.averageScore)} detail="quiz attempts" tone="success" />
        <StatCard label="Best Score" value={formatPercent(performance.bestScore)} detail={`${performance.attemptsCount} attempts`} />
      </div>

      {!hasActivity && (
        <EmptyState
          title="No analytics yet"
          message="Create courses, upload notes, generate study aids, or submit quizzes to populate this page."
          action={<Link to="/notes" className="ui-button ui-button-primary">Upload Note</Link>}
        />
      )}

      <div className="page-two-column">
        <Card className="stack">
          <div className="split">
            <h2>Quiz Performance</h2>
            <Badge tone="muted">{performance.attemptsCount} attempts</Badge>
          </div>

          {performance.recentAttempts.length === 0 ? (
            <p className="muted">No submitted quiz attempts yet.</p>
          ) : (
            <div className="stack">
              {performance.recentAttempts.map((attempt) => (
                <div key={attempt.attemptId} className="list-row">
                  <span>{attempt.quizTitle}</span>
                  <span className="muted text-small">
                    {attempt.score}/{attempt.totalPoints} · {formatPercent(attempt.percentage)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </Card>

        <div className="stack">
          <Card>
            <div className="eyebrow mb-3">Learning Totals</div>
            <div className="stack">
              <div className="list-row"><span>Courses</span><strong>{dashboard.totalCourses}</strong></div>
              <div className="list-row"><span>Notes</span><strong>{dashboard.totalNotes}</strong></div>
              <div className="list-row"><span>Quizzes</span><strong>{dashboard.totalQuizzes}</strong></div>
              <div className="list-row"><span>Achievements</span><strong>{dashboard.unlockedAchievements}/{dashboard.availableAchievements}</strong></div>
            </div>
          </Card>

          <Card>
            <div className="eyebrow mb-3">Recent Activity</div>
            {dashboard.recentActivity.length === 0 ? (
              <p className="muted">No recent activity yet.</p>
            ) : (
              <div className="stack">
                {dashboard.recentActivity.slice(0, 6).map((activity) => (
                  <ActivityRow key={activity.id} activity={activity} />
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      <Card className="stack">
        <div className="split">
          <h2>Quiz Summaries</h2>
          <Badge tone="muted">{performance.quizSummaries.length} quizzes</Badge>
        </div>

        {performance.quizSummaries.length === 0 ? (
          <p className="muted">Quiz summaries appear after attempts are submitted.</p>
        ) : (
          <div className="grid grid-3">
            {performance.quizSummaries.map((summary) => (
              <Card key={summary.quizId} className="stack">
                <h3>{summary.title}</h3>
                <Badge tone="primary">{summary.attemptsCount} attempts</Badge>
                <p className="muted text-small">Best {formatPercent(summary.bestScore)} · Average {formatPercent(summary.averageScore)}</p>
              </Card>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
