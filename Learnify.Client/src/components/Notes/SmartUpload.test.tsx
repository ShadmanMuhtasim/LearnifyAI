import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SmartUpload from './SmartUpload';

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
}));

vi.mock('../../services/api', () => ({
  default: {
    post: mocks.post,
  },
}));

const courses = [{ id: 'course-1', title: 'Data Structures' }];

const renderSmartUpload = () =>
  render(
    <MemoryRouter>
      <SmartUpload courses={courses} />
    </MemoryRouter>
  );

const selectFile = (container: HTMLElement, file: File) => {
  const input = container.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
  return input;
};

describe('SmartUpload', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.post.mockResolvedValue({
      data: {
        success: true,
        data: {
          noteId: 'note-1',
          title: 'Workflow Note',
          extractionStatus: 'Extracted',
          characterCount: 42,
          attachmentCount: 1,
          warning: null,
          aiUsed: false,
          generationModeUsed: null,
          fromCache: false,
          message: 'Extracted text and saved the note.',
        },
      },
    });
  });

  it('accepts supported material files and shows the unified mode choices', () => {
    const { container } = renderSmartUpload();
    const input = container.querySelector('input[type="file"]') as HTMLInputElement;

    expect(input.accept).toContain('.txt');
    expect(input.accept).toContain('.md');
    expect(input.accept).toContain('.pdf');
    expect(input.accept).toContain('.docx');
    expect(screen.getByText(/Supports \.txt, \.md, \.pdf, and \.docx/i)).toBeInTheDocument();
    expect(screen.getAllByText('Save only').length).toBeGreaterThan(0);
    expect(screen.getByText('Extract text')).toBeInTheDocument();
    expect(screen.getByText('Analyze with AI')).toBeInTheDocument();

    selectFile(container, new File(['%PDF tiny fixture'], 'lecture.pdf', { type: 'application/pdf' }));

    expect(screen.getByText('lecture.pdf')).toBeInTheDocument();
  });

  it('uploads selected files through upload-material as multipart form data', async () => {
    const { container } = renderSmartUpload();

    selectFile(container, new File(['# Notes'], 'notes.md', { type: 'text/markdown' }));
    fireEvent.change(screen.getByLabelText(/Course/i), { target: { value: 'course-1' } });
    fireEvent.click(screen.getAllByRole('button', { name: /Extract text/i })[0]);
    const extractButtons = screen.getAllByRole('button', { name: /Extract text/i });
    fireEvent.click(extractButtons[extractButtons.length - 1]);

    await waitFor(() => expect(mocks.post).toHaveBeenCalledWith(
      '/api/notes/upload-material',
      expect.any(FormData),
      { headers: { 'Content-Type': 'multipart/form-data' } }
    ));
    expect(await screen.findByText(/Extracted text and saved the note/i)).toBeInTheDocument();
  });

  it('shows backend extraction errors clearly', async () => {
    mocks.post.mockRejectedValueOnce({
      response: {
        status: 400,
        data: {
          message: 'This PDF appears to be scanned or image-only. OCR is not available or could not extract readable text.',
        },
      },
    });
    const { container } = renderSmartUpload();

    selectFile(container, new File(['not really a pdf'], 'scan.pdf', { type: 'application/pdf' }));
    fireEvent.change(screen.getByLabelText(/Course/i), { target: { value: 'course-1' } });
    fireEvent.click(screen.getAllByRole('button', { name: /Extract text/i })[0]);
    const extractButtons = screen.getAllByRole('button', { name: /Extract text/i });
    fireEvent.click(extractButtons[extractButtons.length - 1]);

    expect(await screen.findByText(/OCR is not available/i)).toBeInTheDocument();
    expect(screen.queryByText(/Failed to analyze and save the document/i)).not.toBeInTheDocument();
  });
});
