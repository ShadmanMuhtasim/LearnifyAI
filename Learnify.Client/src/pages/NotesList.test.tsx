import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import NotesList from './NotesList';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: {
    get: mocks.get,
    post: mocks.post,
  },
}));

vi.mock('../store/authStore', () => ({
  useAuthStore: () => ({ isAuthenticated: true }),
}));

const renderNotesList = () =>
  render(
    <MemoryRouter>
      <NotesList />
    </MemoryRouter>
  );

describe('NotesList upload modal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.get.mockImplementation((url: string) => {
      if (url === '/api/notes') {
        return Promise.resolve({ data: { success: true, data: [] } });
      }

      if (url === '/api/courses') {
        return Promise.resolve({
          data: {
            success: true,
            data: [{ id: 'course-1', title: 'Data Structures' }],
          },
        });
      }

      return Promise.reject(new Error(`Unhandled GET ${url}`));
    });
    mocks.post.mockResolvedValue({
      data: {
        success: true,
        data: {
          noteId: 'note-1',
          title: 'Lecture',
          extractionStatus: 'Saved',
          characterCount: 0,
          attachmentCount: 1,
          warning: null,
          aiUsed: false,
          generationModeUsed: null,
          fromCache: false,
          message: 'Saved the file without AI analysis.',
        },
      },
    });
  });

  it('uses the unified upload-material workflow instead of legacy upload choices', async () => {
    renderNotesList();

    await screen.findByText(/No notes found/i);
    fireEvent.click(screen.getAllByRole('button', { name: /^Upload$/i })[0]);

    expect(await screen.findByText('Unified upload')).toBeInTheDocument();
    expect(screen.queryByText('Simple PDF Upload')).not.toBeInTheDocument();
    expect(screen.queryByText('AI upload and analysis')).not.toBeInTheDocument();

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['%PDF-1.4'], 'lecture.pdf', { type: 'application/pdf' })],
      },
    });
    fireEvent.change(screen.getByLabelText(/Course/i), {
      target: { value: 'course-1' },
    });
    const saveOnlyButtons = screen.getAllByRole('button', { name: /Save only/i });
    fireEvent.click(saveOnlyButtons[saveOnlyButtons.length - 1]);

    await waitFor(() => expect(mocks.post).toHaveBeenCalledWith(
      '/api/notes/upload-material',
      expect.any(FormData),
      { headers: { 'Content-Type': 'multipart/form-data' } }
    ));
    expect(mocks.post).not.toHaveBeenCalledWith('/api/notes/analyze-upload', expect.anything());
  });

  it('renders paged preview cards without requiring full note content', async () => {
    mocks.get.mockImplementation((url: string) => {
      if (url === '/api/notes') {
        return Promise.resolve({
          data: {
            success: true,
            data: {
              items: [
                {
                  id: 'note-1',
                  title: 'Preview Note',
                  preview: 'Short preview from the backend.',
                  courseId: 'course-1',
                  courseTitle: 'Data Structures',
                  hasAttachments: true,
                  extractionStatus: 'Extracted',
                  createdAt: '2026-06-01T00:00:00Z',
                },
              ],
              page: 1,
              pageSize: 20,
              totalCount: 1,
              totalPages: 1,
              hasPreviousPage: false,
              hasNextPage: false,
            },
          },
        });
      }

      return Promise.reject(new Error(`Unhandled GET ${url}`));
    });

    renderNotesList();

    expect(await screen.findByText('Preview Note')).toBeInTheDocument();
    expect(screen.getByText('Short preview from the backend.')).toBeInTheDocument();
    expect(screen.queryByText(/UNIQUE_FULL_CONTENT_SHOULD_NOT_BE_REQUIRED/i)).not.toBeInTheDocument();
  });
});
