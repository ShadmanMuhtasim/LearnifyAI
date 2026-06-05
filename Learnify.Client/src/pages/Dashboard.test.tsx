import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';
import { analyticsService } from '../services/analyticsService';
import { useAuthStore } from '../store/authStore';

vi.mock('../services/analyticsService', () => ({
  analyticsService: {
    getDashboard: vi.fn(),
  },
}));

const mockedAnalyticsService = vi.mocked(analyticsService);

const emptyDashboard = {
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
  availableAchievements: 8,
  recentActivity: [],
};

function renderDashboard() {
  useAuthStore.setState({
    user: { id: 'user-1', name: 'Shadman', email: 'shadman@example.com', role: 'Student' },
    isAuthenticated: true,
    isLoading: false,
    error: null,
  });

  return render(
    <MemoryRouter>
      <Dashboard />
    </MemoryRouter>
  );
}

describe('Dashboard analytics', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows a real empty state for fresh users without fake progress', async () => {
    mockedAnalyticsService.getDashboard.mockResolvedValue(emptyDashboard);

    renderDashboard();

    expect(await screen.findByRole('heading', { name: /no learning activity yet/i })).toBeInTheDocument();
    expect(screen.getByText('0 XP')).toBeInTheDocument();
    expect(screen.getByText('0/8')).toBeInTheDocument();
    expect(screen.queryByText(/82%/)).not.toBeInTheDocument();
    expect(screen.queryByText(/cellular biology/i)).not.toBeInTheDocument();
  });

  it('renders tracked activity and quiz metrics from analytics data', async () => {
    mockedAnalyticsService.getDashboard.mockResolvedValue({
      ...emptyDashboard,
      totalCourses: 1,
      totalNotes: 2,
      totalQuizzes: 1,
      totalQuizAttempts: 1,
      averageQuizScore: 75,
      bestQuizScore: 90,
      totalSummariesGenerated: 1,
      totalFlashcardsGenerated: 2,
      totalStudyTipsGenerated: 1,
      totalXp: 85,
      currentStreak: 1,
      unlockedAchievements: 3,
      recentActivity: [
        {
          id: 'activity-1',
          activityType: 'QuizAttemptSubmitted',
          entityType: 'Quiz',
          entityId: 'quiz-1',
          points: 20,
          occurredAt: '2026-06-05T00:00:00Z',
        },
      ],
    });

    renderDashboard();

    expect(await screen.findByText('Submitted a quiz attempt')).toBeInTheDocument();
    expect(screen.getByText('85 XP')).toBeInTheDocument();
    expect(screen.getByText('90%')).toBeInTheDocument();
    expect(screen.getByText('3/8')).toBeInTheDocument();
  });
});
