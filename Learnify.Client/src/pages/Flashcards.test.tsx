import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { toast, Toaster } from 'react-hot-toast';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Flashcards from './Flashcards';
import apiClient from '../services/api';
import { extractText } from '../services/materialService';
import { generateFlashcards, getStudyTips, summarizeNote } from '../services/aiService';

vi.mock('../services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

vi.mock('../services/materialService', () => ({
  extractText: vi.fn(),
}));

vi.mock('../services/aiService', () => ({
  generateFlashcards: vi.fn(),
  summarizeNote: vi.fn(),
  getStudyTips: vi.fn(),
}));

const mockedApi = vi.mocked(apiClient);
const mockedExtractText = vi.mocked(extractText);
const mockedGenerateFlashcards = vi.mocked(generateFlashcards);
const mockedSummarizeNote = vi.mocked(summarizeNote);
const mockedGetStudyTips = vi.mocked(getStudyTips);

function renderFlashcards() {
  return render(
    <MemoryRouter>
      <Toaster />
      <Flashcards />
    </MemoryRouter>
  );
}

describe('Flashcards M11 tools', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    toast.remove();
    mockedApi.get.mockResolvedValue({ data: { data: [] } });
  });

  afterEach(() => {
    toast.remove();
  });

  it('renders generation mode toggle and sends Free Local mode for flashcards', async () => {
    mockedGenerateFlashcards.mockResolvedValue({
      noteId: 'temp',
      flashcards: [{ question: 'What is photosynthesis?', answer: 'Conversion of light to chemical energy.' }],
      generatedAt: new Date().toISOString(),
      generationModeUsed: 'FreeLocal',
      fromCache: false,
      notice: 'Generated locally without AI. Quality may be simpler than AI-generated output.',
    });

    renderFlashcards();

    fireEvent.click(screen.getByRole('button', { name: /free local/i }));
    fireEvent.change(screen.getByPlaceholderText(/paste your lesson notes/i), {
      target: { value: 'Photosynthesis is the conversion of light to chemical energy.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /generate flashcards/i }));

    await waitFor(() => expect(mockedGenerateFlashcards).toHaveBeenCalledWith(expect.objectContaining({
      generationMode: 'FreeLocal',
    })));
    expect(await screen.findByText(/what is photosynthesis/i)).toBeInTheDocument();
    expect(screen.getAllByText(/free local/i).length).toBeGreaterThan(0);
  });

  it('shows summary cache badge when backend returns cached output', async () => {
    mockedSummarizeNote.mockResolvedValue({
      noteId: 'temp',
      summary: '## Overview\nCached summary',
      generatedAt: new Date().toISOString(),
      generationModeUsed: 'AIProvider',
      providerUsed: 'Gemini',
      fromCache: true,
    });

    renderFlashcards();

    fireEvent.click(screen.getByRole('button', { name: /note summarizer/i }));
    fireEvent.change(screen.getByPlaceholderText(/paste your raw notes/i), {
      target: { value: 'Gradient descent minimizes a loss function.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /summarize notes/i }));

    expect(await screen.findByText(/cached summary/i)).toBeInTheDocument();
    expect(screen.getByText(/from cache/i)).toBeInTheDocument();
  });

  it('uploads material through the source selector', async () => {
    mockedExtractText.mockResolvedValue({
      text: 'Extracted markdown content for flashcards.',
      fileName: 'study.md',
    });

    renderFlashcards();

    const file = new File(['# Study'], 'study.md', { type: 'text/markdown' });
    fireEvent.change(screen.getByLabelText(/upload material/i), {
      target: { files: [file] },
    });

    expect(await screen.findByDisplayValue(/extracted markdown content/i)).toBeInTheDocument();
    expect(mockedExtractText).toHaveBeenCalledWith(file);
  });

  it('shows rate-limit snackbar actions and switches to Free Local', async () => {
    mockedSummarizeNote.mockRejectedValue({
      response: {
        data: {
          errorCode: 'AI_RATE_LIMIT',
          message: 'Gemini quota or rate limit was reached.',
        },
      },
    });

    renderFlashcards();

    fireEvent.click(screen.getByRole('button', { name: /note summarizer/i }));
    fireEvent.change(screen.getByPlaceholderText(/paste your raw notes/i), {
      target: { value: 'Large notes about contract exceptions.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /summarize notes/i }));

    expect(await screen.findByText(/ai provider limit reached/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /use free local/i }));
    expect(await screen.findByText(/always free/i)).toBeInTheDocument();
  });

  it('generates content-specific study tips in selected mode', async () => {
    mockedGetStudyTips.mockResolvedValue({
      tips: '## Active Recall Questions\n- Explain Hash Map collisions.',
      generatedAt: new Date().toISOString(),
      generationModeUsed: 'FreeLocal',
    });

    renderFlashcards();

    fireEvent.click(screen.getByRole('button', { name: /study tips/i }));
    fireEvent.click(within(screen.getByRole('group', { name: /generation mode/i })).getByRole('button', { name: /^free local$/i }));
    fireEvent.change(screen.getByPlaceholderText(/paste notes for content-specific/i), {
      target: { value: 'Hash Map collisions use chaining.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /get study tips/i }));

    await waitFor(() => expect(mockedGetStudyTips).toHaveBeenCalledWith(expect.objectContaining({
      generationMode: 'FreeLocal',
      topic: 'Hash Map collisions use chaining.',
    })));
    expect(await screen.findByText(/explain hash map collisions/i)).toBeInTheDocument();
  });
});
