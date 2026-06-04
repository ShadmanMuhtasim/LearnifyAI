import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import Courses from './Courses';
import { courseService } from '../services/courseService';
import { useAuthStore } from '../store/authStore';

vi.mock('../services/courseService', () => ({
  courseService: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

const mockedCourseService = vi.mocked(courseService);

function renderCourses() {
  useAuthStore.setState({
    user: { id: 'user-1', name: 'Test User', email: 'test@example.com', role: 'Student' },
    isAuthenticated: true,
    isLoading: false,
    error: null,
  });

  return render(
    <MemoryRouter>
      <Courses />
    </MemoryRouter>
  );
}

describe('Courses', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.spyOn(window, 'alert').mockImplementation(() => undefined);
  });

  it('shows the empty state', async () => {
    mockedCourseService.getAll.mockResolvedValue([]);

    renderCourses();

    expect(await screen.findByRole('heading', { name: /no courses yet/i })).toBeInTheDocument();
    expect(screen.getByText(/click 'new course' to get started/i)).toBeInTheDocument();
  });

  it('creates a course from the modal', async () => {
    mockedCourseService.getAll.mockResolvedValue([]);
    mockedCourseService.create.mockResolvedValue({
      id: 'course-1',
      title: 'Biology',
      description: 'Cells and systems',
      createdAt: '2026-06-04T00:00:00Z',
    });

    renderCourses();

    const newCourseButtons = await screen.findAllByRole('button', { name: /new course/i });
    fireEvent.click(newCourseButtons[0]);
    fireEvent.change(screen.getByLabelText(/course title/i), { target: { value: 'Biology' } });
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'Cells and systems' } });
    fireEvent.click(screen.getByRole('button', { name: /create course/i }));

    await waitFor(() => expect(mockedCourseService.create).toHaveBeenCalledWith({
      title: 'Biology',
      description: 'Cells and systems',
    }));
    expect(await screen.findByText('Biology')).toBeInTheDocument();
  });

  it('updates and deletes an existing course', async () => {
    mockedCourseService.getAll.mockResolvedValue([
      {
        id: 'course-1',
        title: 'Old Biology',
        description: 'Old description',
        createdAt: '2026-06-04T00:00:00Z',
      },
    ]);
    mockedCourseService.update.mockResolvedValue({
      id: 'course-1',
      title: 'Updated Biology',
      description: 'New description',
      createdAt: '2026-06-04T00:00:00Z',
    });
    mockedCourseService.delete.mockResolvedValue({} as never);

    renderCourses();

    fireEvent.click(await screen.findByRole('button', { name: /edit/i }));
    fireEvent.change(screen.getByLabelText(/course title/i), { target: { value: 'Updated Biology' } });
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'New description' } });
    fireEvent.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(mockedCourseService.update).toHaveBeenCalledWith('course-1', {
      title: 'Updated Biology',
      description: 'New description',
    }));
    expect(await screen.findByText('Updated Biology')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /delete/i }));

    await waitFor(() => expect(mockedCourseService.delete).toHaveBeenCalledWith('course-1'));
    expect(screen.queryByText('Updated Biology')).not.toBeInTheDocument();
  });
});
