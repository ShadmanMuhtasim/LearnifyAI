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
    mocks.post.mockResolvedValue({ data: { success: true } });
  });

  it('uploads a simple PDF through upload-file without calling AI analysis', async () => {
    renderNotesList();

    await screen.findByText(/No notes found/i);
    fireEvent.click(screen.getAllByRole('button', { name: /^Upload$/i })[0]);

    expect(await screen.findByText('Simple PDF Upload')).toBeInTheDocument();
    expect(screen.getByText(/Save a PDF to your notes without AI analysis/i)).toBeInTheDocument();

    fireEvent.change(document.querySelector('#pdf-upload-course') as HTMLSelectElement, {
      target: { value: 'course-1' },
    });
    fireEvent.change(screen.getByLabelText(/PDF file/i), {
      target: {
        files: [new File(['%PDF-1.4'], 'lecture.pdf', { type: 'application/pdf' })],
      },
    });

    fireEvent.click(screen.getByRole('button', { name: /Save PDF Without AI/i }));

    await waitFor(() => expect(mocks.post).toHaveBeenCalledWith(
      '/api/notes/upload-file',
      expect.any(FormData),
      { headers: { 'Content-Type': 'multipart/form-data' } }
    ));
    expect(mocks.post).not.toHaveBeenCalledWith('/api/notes/analyze-upload', expect.anything());
  });
});
