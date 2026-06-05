import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import StudyPlanner from './StudyPlanner';
import type { StudyPlanItem, StudyPlanSummary } from '../services/studyPlannerService';

const plannerMocks = vi.hoisted(() => ({
  getItems: vi.fn(),
  getSummary: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  complete: vi.fn(),
  delete: vi.fn(),
}));

const courseMocks = vi.hoisted(() => ({
  getAll: vi.fn(),
}));

const quizMocks = vi.hoisted(() => ({
  getQuizzes: vi.fn(),
}));

const apiMocks = vi.hoisted(() => ({
  get: vi.fn(),
}));

vi.mock('../services/studyPlannerService', () => ({
  studyPlannerService: plannerMocks,
}));

vi.mock('../services/courseService', () => ({
  courseService: courseMocks,
}));

vi.mock('../services/quizService', () => ({
  quizService: quizMocks,
}));

vi.mock('../services/api', () => ({
  default: apiMocks,
}));

const emptySummary: StudyPlanSummary = {
  pendingCount: 0,
  completedCount: 0,
  todayCount: 0,
  overdueCount: 0,
  totalEstimatedMinutesToday: 0,
  nextItem: null,
  suggestions: [],
};

const pendingItem: StudyPlanItem = {
  id: 'plan-1',
  userId: 'user-1',
  courseId: 'course-1',
  courseTitle: 'Data Structures',
  noteId: 'note-1',
  notePreview: 'Graph traversal notes',
  quizId: null,
  quizTitle: null,
  title: 'Review graph notes',
  description: 'Focus on BFS vs DFS',
  planType: 'ReviewNote',
  scheduledFor: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  estimatedMinutes: 25,
  status: 'Pending',
  completedAt: null,
  createdAt: new Date().toISOString(),
  updatedAt: null,
  priority: 'High',
  source: 'Manual',
};

function renderPlanner() {
  return render(
    <MemoryRouter>
      <StudyPlanner />
    </MemoryRouter>
  );
}

describe('StudyPlanner', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    plannerMocks.getItems.mockResolvedValue([]);
    plannerMocks.getSummary.mockResolvedValue(emptySummary);
    plannerMocks.create.mockResolvedValue(pendingItem);
    plannerMocks.update.mockResolvedValue(pendingItem);
    plannerMocks.complete.mockResolvedValue({ ...pendingItem, status: 'Completed', completedAt: new Date().toISOString() });
    plannerMocks.delete.mockResolvedValue({});
    courseMocks.getAll.mockResolvedValue([{ id: 'course-1', title: 'Data Structures', createdAt: new Date().toISOString() }]);
    quizMocks.getQuizzes.mockResolvedValue([]);
    apiMocks.get.mockResolvedValue({ data: { success: true, data: [] } });
  });

  it('renders empty state for fresh users', async () => {
    renderPlanner();

    expect(await screen.findByText('Create your first study plan item.')).toBeInTheDocument();
    expect(screen.getByText('0 pending')).toBeInTheDocument();
  });

  it('renders planner items from the service', async () => {
    plannerMocks.getItems.mockResolvedValue([pendingItem]);
    plannerMocks.getSummary.mockResolvedValue({ ...emptySummary, pendingCount: 1, todayCount: 1, nextItem: pendingItem });

    renderPlanner();

    expect(await screen.findAllByText('Review graph notes')).toHaveLength(2);
    expect(screen.getAllByText('Data Structures').length).toBeGreaterThan(0);
    expect(screen.getAllByText('25 min').length).toBeGreaterThan(0);
  });

  it('creates a plan item from the form', async () => {
    renderPlanner();

    await screen.findByText('Create your first study plan item.');
    fireEvent.change(screen.getByPlaceholderText('Review graph traversal notes'), {
      target: { value: 'Practice dynamic programming' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Add to planner' }));

    await waitFor(() => expect(plannerMocks.create).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Practice dynamic programming',
      estimatedMinutes: 30,
      priority: 'Medium',
    })));
  });

  it('completes an item and reloads completed state', async () => {
    plannerMocks.getItems
      .mockResolvedValueOnce([pendingItem])
      .mockResolvedValueOnce([{ ...pendingItem, status: 'Completed', completedAt: new Date().toISOString() }]);
    plannerMocks.getSummary.mockResolvedValue({ ...emptySummary, pendingCount: 1, nextItem: pendingItem });

    renderPlanner();

    await screen.findAllByText('Review graph notes');
    fireEvent.click(screen.getByRole('button', { name: 'Complete' }));

    await waitFor(() => expect(plannerMocks.complete).toHaveBeenCalledWith('plan-1'));
    expect((await screen.findAllByText('Completed')).length).toBeGreaterThan(0);
  });
});
