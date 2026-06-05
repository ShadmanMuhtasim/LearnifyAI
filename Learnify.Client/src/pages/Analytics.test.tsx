import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Analytics from './Analytics';
import { analyticsService } from '../services/analyticsService';

vi.mock('../services/analyticsService', () => ({
  analyticsService: {
    getDashboard: vi.fn(),
    getQuizPerformance: vi.fn(),
  },
}));

const mockedAnalyticsService = vi.mocked(analyticsService);

const dashboard = {
  totalCourses: 1,
  totalNotes: 2,
  totalQuizzes: 1,
  totalQuizAttempts: 1,
  averageQuizScore: 80,
  bestQuizScore: 100,
  totalFlashcardsGenerated: 1,
  totalSummariesGenerated: 1,
  totalStudyTipsGenerated: 1,
  totalXp: 120,
  currentStreak: 2,
  longestStreak: 3,
  unlockedAchievements: 2,
  availableAchievements: 8,
  recentActivity: [
    {
      id: 'activity-1',
      activityType: 'SummaryGenerated',
      entityType: 'Note',
      entityId: 'note-1',
      points: 5,
      occurredAt: '2026-06-05T00:00:00Z',
    },
  ],
};

const performance = {
  attemptsCount: 1,
  averageScore: 80,
  bestScore: 100,
  recentAttempts: [
    {
      attemptId: 'attempt-1',
      quizId: 'quiz-1',
      quizTitle: 'Hash Map Quiz',
      score: 4,
      totalPoints: 5,
      percentage: 80,
      completedAt: '2026-06-05T00:00:00Z',
    },
  ],
  quizSummaries: [
    {
      quizId: 'quiz-1',
      title: 'Hash Map Quiz',
      attemptsCount: 1,
      bestScore: 100,
      averageScore: 80,
    },
  ],
};

describe('Analytics page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders dashboard and quiz performance data', async () => {
    mockedAnalyticsService.getDashboard.mockResolvedValue(dashboard);
    mockedAnalyticsService.getQuizPerformance.mockResolvedValue(performance);

    render(
      <MemoryRouter>
        <Analytics />
      </MemoryRouter>
    );

    expect(await screen.findByRole('heading', { name: /learning analytics/i })).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getAllByText('Hash Map Quiz').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/4\/5/i)).toBeInTheDocument();
    expect(screen.getByText('Generated a summary')).toBeInTheDocument();
  });

  it('shows an empty analytics state when no activity exists', async () => {
    mockedAnalyticsService.getDashboard.mockResolvedValue({
      ...dashboard,
      totalCourses: 0,
      totalNotes: 0,
      totalQuizzes: 0,
      totalQuizAttempts: 0,
      totalXp: 0,
      currentStreak: 0,
      unlockedAchievements: 0,
      recentActivity: [],
    });
    mockedAnalyticsService.getQuizPerformance.mockResolvedValue({
      attemptsCount: 0,
      averageScore: 0,
      bestScore: 0,
      recentAttempts: [],
      quizSummaries: [],
    });

    render(
      <MemoryRouter>
        <Analytics />
      </MemoryRouter>
    );

    expect(await screen.findByRole('heading', { name: /no analytics yet/i })).toBeInTheDocument();
    expect(screen.getByText(/no submitted quiz attempts yet/i)).toBeInTheDocument();
  });
});
