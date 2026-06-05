import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SmartUpload from './SmartUpload';

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock('../../services/api', () => ({
  default: {
    post: mocks.post,
    put: mocks.put,
  },
}));

const renderSmartUpload = () =>
  render(
    <MemoryRouter>
      <SmartUpload />
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
          courseId: 'course-1',
          courseName: 'Extracted PDF Course',
          courseWasCreated: true,
          summary: 'Summary from extracted text.',
          detectedTopics: ['PDF Topic'],
          message: 'Saved',
        },
      },
    });
  });

  it('accepts text-based PDFs and shows the supported PDF copy', () => {
    const { container } = renderSmartUpload();
    const input = container.querySelector('input[type="file"]') as HTMLInputElement;

    expect(input.accept).toBe('.txt,.md,.pdf');
    expect(screen.getByText(/Supports \.txt, \.md, and text-based \.pdf files/i)).toBeInTheDocument();

    selectFile(container, new File(['%PDF tiny fixture'], 'lecture.pdf', { type: 'application/pdf' }));

    expect(screen.getByText('lecture.pdf')).toBeInTheDocument();
    expect(screen.getByText(/17 B\s+•\s+PDF/i)).toBeInTheDocument();
  });

  it('shows backend PDF extraction errors clearly', async () => {
    mocks.post.mockRejectedValueOnce({
      response: {
        status: 400,
        data: {
          message:
            'Could not extract readable text from this PDF. Scanned/image-only PDFs are not supported yet. Please upload a text-based PDF, .txt, or .md file.',
        },
      },
    });
    const { container } = renderSmartUpload();

    selectFile(container, new File(['not really a pdf'], 'scan.pdf', { type: 'application/pdf' }));
    fireEvent.click(screen.getByRole('button', { name: /Analyze & Save/i }));

    expect(await screen.findByText(/Could not extract readable text from this PDF/i)).toBeInTheDocument();
    expect(screen.queryByText(/Failed to analyze and save the document/i)).not.toBeInTheDocument();
  });

  it('keeps text and markdown files supported', async () => {
    const { container } = renderSmartUpload();

    selectFile(container, new File(['# Notes'], 'notes.md', { type: 'text/markdown' }));
    fireEvent.click(screen.getByRole('button', { name: /Analyze & Save/i }));

    await waitFor(() => expect(mocks.post).toHaveBeenCalled());
    expect(mocks.post.mock.calls[0][1]).toMatchObject({
      fileName: 'notes.md',
      fileType: 'text',
    });
  });
});
