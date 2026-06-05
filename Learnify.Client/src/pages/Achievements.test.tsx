import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Achievements from './Achievements';
import { analyticsService } from '../services/analyticsService';

vi.mock('../services/analyticsService', () => ({
  analyticsService: {
    getAchievements: vi.fn(),
  },
}));

const mockedAnalyticsService = vi.mocked(analyticsService);

describe('Achievements page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders locked and unlocked achievement progress', async () => {
    mockedAnalyticsService.getAchievements.mockResolvedValue([
      {
        id: 'achievement-1',
        code: 'first-course',
        title: 'First Course',
        description: 'Create your first course.',
        icon: 'CR',
        requiredValue: 1,
        achievementType: 'CourseCreated',
        pointsReward: 25,
        isUnlocked: true,
        unlockedAt: '2026-06-05T00:00:00Z',
        progress: 1,
      },
      {
        id: 'achievement-2',
        code: 'productive-learner',
        title: 'Productive Learner',
        description: 'Complete several learning actions.',
        icon: 'XP',
        requiredValue: 10,
        achievementType: 'ActivityCount',
        pointsReward: 75,
        isUnlocked: false,
        unlockedAt: null,
        progress: 4,
      },
    ]);

    render(
      <MemoryRouter>
        <Achievements />
      </MemoryRouter>
    );

    expect(await screen.findByRole('heading', { name: /^achievements$/i })).toBeInTheDocument();
    expect(screen.getByText('First Course')).toBeInTheDocument();
    expect(screen.getByText('Productive Learner')).toBeInTheDocument();
    expect(screen.getByText('1/1')).toBeInTheDocument();
    expect(screen.getByText('4/10')).toBeInTheDocument();
    expect(screen.getByText('25 XP')).toBeInTheDocument();
  });

  it('shows a configured-empty state when no definitions are returned', async () => {
    mockedAnalyticsService.getAchievements.mockResolvedValue([]);

    render(
      <MemoryRouter>
        <Achievements />
      </MemoryRouter>
    );

    expect(await screen.findByRole('heading', { name: /no achievements configured/i })).toBeInTheDocument();
  });
});
