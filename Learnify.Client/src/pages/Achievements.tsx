import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { analyticsService, type AchievementStatus } from '../services/analyticsService';
import { Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader, StatCard } from '../components/UI/Primitives';

function progressText(achievement: AchievementStatus) {
  return `${achievement.progress}/${achievement.requiredValue}`;
}

function progressPercent(achievement: AchievementStatus) {
  if (achievement.requiredValue <= 0) {
    return 0;
  }

  return Math.min(100, Math.round((achievement.progress / achievement.requiredValue) * 100));
}

export default function Achievements() {
  const [achievements, setAchievements] = useState<AchievementStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadAchievements = async () => {
      try {
        setLoading(true);
        setError(null);
        const nextAchievements = await analyticsService.getAchievements();
        if (isMounted) {
          setAchievements(nextAchievements);
        }
      } catch {
        if (isMounted) {
          setError('Unable to load achievements right now.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    void loadAchievements();

    return () => {
      isMounted = false;
    };
  }, []);

  const unlocked = useMemo(() => achievements.filter((achievement) => achievement.isUnlocked), [achievements]);
  const locked = useMemo(() => achievements.filter((achievement) => !achievement.isUnlocked), [achievements]);
  const totalXp = unlocked.reduce((sum, achievement) => sum + achievement.pointsReward, 0);

  if (loading) {
    return <LoadingState label="Loading achievements..." />;
  }

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Achievements"
        title="Achievements"
        subtitle="Earned from real learning activity: courses, notes, quizzes, AI study tools, and streaks."
        actions={<Link to="/analytics" className="ui-button ui-button-secondary">View Analytics</Link>}
      />

      {error && <ErrorState message={error} />}

      <div className="grid grid-4">
        <StatCard label="Unlocked" value={unlocked.length} detail={`${achievements.length} available`} tone="success" />
        <StatCard label="Achievement XP" value={totalXp} detail="reward points" tone="primary" />
        <StatCard label="In Progress" value={locked.length} detail="still locked" tone="warning" />
        <StatCard label="Completion" value={achievements.length ? `${Math.round((unlocked.length / achievements.length) * 100)}%` : '0%'} detail="of catalog" />
      </div>

      {achievements.length === 0 ? (
        <EmptyState title="No achievements configured" message="Achievement definitions are not available yet." />
      ) : (
        <div className="grid grid-3">
          {achievements.map((achievement) => (
            <Card key={achievement.id} className={`stack achievement-card${achievement.isUnlocked ? ' achievement-card-unlocked' : ''}`}>
              <div className="split">
                <span className="achievement-icon" aria-hidden="true">{achievement.icon || 'XP'}</span>
                <Badge tone={achievement.isUnlocked ? 'success' : 'muted'}>
                  {achievement.isUnlocked ? 'Unlocked' : 'Locked'}
                </Badge>
              </div>
              <div>
                <h2>{achievement.title}</h2>
                <p className="muted mt-3">{achievement.description}</p>
              </div>
              <div className="achievement-progress" aria-label={`${achievement.title} progress ${progressText(achievement)}`}>
                <span style={{ width: `${progressPercent(achievement)}%` }} />
              </div>
              <div className="split">
                <span className="muted text-small">{progressText(achievement)}</span>
                <strong>{achievement.pointsReward} XP</strong>
              </div>
              {achievement.unlockedAt && (
                <p className="muted text-small">
                  Unlocked {new Date(achievement.unlockedAt).toLocaleDateString()}
                </p>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
