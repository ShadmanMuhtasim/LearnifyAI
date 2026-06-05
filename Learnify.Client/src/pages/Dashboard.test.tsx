import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
}));

const plannerMocks = vi.hoisted(() => ({
  getSummary: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: apiMocks,
}));

vi.mock('../services/studyPlannerService', () => ({
  studyPlannerService: plannerMocks,
}));

vi.mock('../store/authStore', () => ({
  useAuthStore: (selector?: any) => {
    const state = { user: { name: 'Shadman', role: 'Student' } };
    return selector ? selector(state) : state;
  },
}));

describe('Dashboard study planner summary', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    apiMocks.get.mockImplementation((url: string) => {
      if (url === '/api/courses') return Promise.resolve({ data: { data: [] } });
      if (url === '/api/notes') return Promise.resolve({ data: { data: [] } });
      if (url === '/api/quizzes') return Promise.resolve({ data: { data: [] } });
      return Promise.reject(new Error(`Unhandled GET ${url}`));
    });
    plannerMocks.getSummary.mockResolvedValue({
      pendingCount: 0,
      completedCount: 0,
      todayCount: 0,
      overdueCount: 0,
      totalEstimatedMinutesToday: 0,
      nextItem: null,
      suggestions: [],
    });
  });

  it('shows the real study planner summary and CTA', async () => {
    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>
    );

    expect(await screen.findByText("Today's Study Plan")).toBeInTheDocument();
    expect(screen.getByText('No study tasks yet. Create your first plan item.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Open Study Planner' })).toHaveAttribute('href', '/study-planner');
  });
});
