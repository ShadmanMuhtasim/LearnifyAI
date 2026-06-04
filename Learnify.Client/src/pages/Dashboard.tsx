import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import apiClient from '../services/api';
import { Badge, Card, PageHeader, StatCard } from '../components/UI/Primitives';

interface Course {
  id: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
}

const heatmapRows = [
  { label: 'Morning', levels: [1, 2, 1, 2, 4, 1] },
  { label: 'Afternoon', levels: [1, 3, 2, 1, 1, 2] },
  { label: 'Evening', levels: [2, 1, 4, 3, 1, 1] },
];

export default function Dashboard() {
  const { user, isAuthenticated } = useAuthStore();
  const [courseCount, setCourseCount] = useState(0);

  useEffect(() => {
    if (!isAuthenticated) {
      setCourseCount(0);
      return;
    }

    let isMounted = true;

    const fetchCourseCount = async () => {
      try {
        const response = await apiClient.get<ApiResponse<Course[]>>('/api/courses');
        if (isMounted && response.data.success) {
          setCourseCount(response.data.data.length);
        }
      } catch {
        if (isMounted) {
          setCourseCount(0);
        }
      }
    };

    void fetchCourseCount();

    return () => {
      isMounted = false;
    };
  }, [isAuthenticated]);

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Dashboard"
        title={`Good morning, ${user?.name || 'Learner'}.`}
        subtitle="Ready to level up today?"
        actions={<Badge tone="primary">Based on available data</Badge>}
      />

      <div className="grid grid-4">
        <StatCard label="Daily Streak" value="5" detail="days, coming soon" tone="warning" />
        <StatCard label="Weekly Progress" value="82%" detail="visual placeholder" tone="primary" />
        <StatCard label="Courses" value={courseCount} detail="active learning paths" tone="success" />
        <StatCard label="AI Tools" value="4" detail="summary, cards, tips, quizzes" tone="default" />
      </div>

      <div className="page-two-column">
        <Card>
          <div className="split mb-4">
            <div>
              <h2>Weekly Learning Heatmap</h2>
              <p className="muted text-small">A visual study rhythm guide; backend analytics are still future work.</p>
            </div>
            <Badge tone="muted">Coming soon</Badge>
          </div>

          <div className="heatmap">
            <div className="heatmap-grid">
              <span />
              {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((day, index) => (
                <span key={day} className={`heatmap-label${index > 2 ? ' hide-mobile' : ''}`}>{day}</span>
              ))}
              {heatmapRows.map((row) => (
                <div key={row.label} style={{ display: 'contents' }}>
                  <span className="heatmap-label">{row.label}</span>
                  {row.levels.map((level, index) => (
                    <span
                      key={`${row.label}-${index}`}
                      className={`heatmap-cell level-${level}${index > 2 ? ' hide-mobile' : ''}`}
                      aria-label={`${row.label} level ${level}`}
                    />
                  ))}
                </div>
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
            <h2 style={{ color: '#ffffff', marginTop: 10 }}>Review your latest notes</h2>
            <p style={{ color: 'rgba(255,255,255,0.82)', marginTop: 8 }}>
              Use flashcards or a generated quiz when your note content is ready for AI actions.
            </p>
            <Link to="/flashcards" className="ui-button" style={{ marginTop: 16, background: '#ffffff', color: '#3525cd' }}>
              Start Session
            </Link>
          </Card>

          <Card>
            <div className="split">
              <span className="eyebrow">Recent Notes</span>
              <Link to="/notes" className="text-small" style={{ color: 'var(--primary)', textDecoration: 'none' }}>
                View all
              </Link>
            </div>
            <p className="muted mt-3">Recent-note analytics are not available yet. Open Notes to continue studying.</p>
          </Card>
        </div>
      </div>
    </div>
  );
}
