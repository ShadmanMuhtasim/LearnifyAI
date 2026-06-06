import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Flashcards from './Flashcards';

const mocks = vi.hoisted(() => ({
  apiGet: vi.fn(),
  extractText: vi.fn(),
  summarizeNote: vi.fn(),
  generateFlashcards: vi.fn(),
  getStudyTips: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: {
    get: mocks.apiGet,
  },
}));

vi.mock('../services/materialService', () => ({
  extractText: mocks.extractText,
}));

vi.mock('../services/aiService', () => ({
  summarizeNote: mocks.summarizeNote,
  generateFlashcards: mocks.generateFlashcards,
  getStudyTips: mocks.getStudyTips,
}));

vi.mock('../components/AI/AiProviderSettings', () => ({
  default: () => <div>AI Provider Settings Mock</div>,
}));

const noteResponse = {
  data: {
    data: [
      {
        id: 'note-1',
        content: 'Saved note content about hash maps.',
        courseTitle: 'Data Structures',
        createdAt: '2026-06-01T00:00:00Z',
      },
    ],
  },
};

const extracted = (text: string, fileName = 'source.txt') => ({
  fileName,
  contentType: 'text/plain',
  extractedText: text,
  characterCount: text.length,
  warning: null,
});

describe('Flashcards AI tools source selector', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.apiGet.mockResolvedValue(noteResponse);
    mocks.extractText.mockResolvedValue(extracted('Uploaded source content about collision handling.'));
    mocks.generateFlashcards.mockResolvedValue({
      flashcards: [{ question: 'Collision strategy?', answer: 'Separate chaining.' }],
    });
    mocks.summarizeNote.mockResolvedValue({
      summary: 'Uploaded notes summarized.',
    });
    mocks.getStudyTips.mockResolvedValue({
      tips: 'Use active recall.',
    });
  });

  it('uploads a file on Flashcard Generator and uses extracted text', async () => {
    render(<Flashcards />);

    const file = new File(['hash map notes'], 'hashmap.txt', { type: 'text/plain' });
    fireEvent.change(await screen.findByLabelText(/upload source file/i), {
      target: { files: [file] },
    });

    expect(await screen.findByDisplayValue(/uploaded source content/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /generate flashcards/i }));

    await waitFor(() =>
      expect(mocks.generateFlashcards).toHaveBeenCalledWith(expect.objectContaining({
        content: 'Uploaded source content about collision handling.',
      }))
    );
    expect(await screen.findByText(/collision strategy/i)).toBeInTheDocument();
  });

  it('uploads a file on Note Summarizer and uses extracted text', async () => {
    render(<Flashcards />);

    fireEvent.click(screen.getByRole('button', { name: /note summarizer/i }));
    const summarizerPanel = screen.getByRole('heading', { name: /summarize your notes/i }).closest('.stack')!;
    const file = new File(['markdown notes'], 'notes.md', { type: 'text/markdown' });
    fireEvent.change(within(summarizerPanel).getByLabelText(/upload source file/i), {
      target: { files: [file] },
    });

    expect(await screen.findByDisplayValue(/uploaded source content/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /summarize notes/i }));

    await waitFor(() =>
      expect(mocks.summarizeNote).toHaveBeenCalledWith(expect.objectContaining({
        content: 'Uploaded source content about collision handling.',
      }))
    );
    expect(await screen.findByText(/uploaded notes summarized/i)).toBeInTheDocument();
  });

  it('uploads a file on Study Tips and prefers extracted text over topic', async () => {
    render(<Flashcards />);

    fireEvent.click(screen.getByRole('button', { name: /^study tips$/i }));
    fireEvent.change(screen.getByLabelText(/^topic$/i), { target: { value: 'Generic topic' } });
    const file = new File(['pdf text'], 'lecture.pdf', { type: 'application/pdf' });
    fireEvent.change(screen.getByLabelText(/upload source file/i), {
      target: { files: [file] },
    });

    expect(await screen.findByDisplayValue(/uploaded source content/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /get study tips/i }));

    await waitFor(() =>
      expect(mocks.getStudyTips).toHaveBeenCalledWith({
        topic: 'Uploaded source content about collision handling.',
      })
    );
  });

  it('shows AI rate-limit messages clearly', async () => {
    mocks.generateFlashcards.mockRejectedValueOnce({
      response: {
        data: {
          code: 'AI_RATE_LIMIT',
          message: 'Gemini quota or rate limit was reached. Try again later, switch provider, or use local/free mode if available.',
        },
      },
    });

    render(<Flashcards />);
    fireEvent.change(await screen.findByLabelText(/extracted or pasted text/i), {
      target: { value: 'Some source content' },
    });
    fireEvent.click(screen.getByRole('button', { name: /generate flashcards/i }));

    expect(await screen.findByText(/Gemini quota or rate limit was reached/i)).toBeInTheDocument();
  });

  it('shows extraction errors clearly', async () => {
    mocks.extractText.mockRejectedValueOnce({
      response: {
        data: {
          message: 'No readable text was extracted. Please upload a text-based PDF, .txt, .md, or .docx file.',
        },
      },
    });

    render(<Flashcards />);
    const file = new File(['bad pdf'], 'scan.pdf', { type: 'application/pdf' });
    fireEvent.change(await screen.findByLabelText(/upload source file/i), {
      target: { files: [file] },
    });

    expect(await screen.findByText(/No readable text was extracted/i)).toBeInTheDocument();
  });
});
