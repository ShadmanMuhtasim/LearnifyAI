import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import NoteDetail from './NoteDetail';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
  summarizeNote: vi.fn(),
  generateFlashcards: vi.fn(),
  getStudyTips: vi.fn(),
  generateQuiz: vi.fn(),
  extractAttachmentText: vi.fn(),
  analyzeExistingNote: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: {
    get: mocks.get,
    put: mocks.put,
    delete: mocks.delete,
  },
}));

vi.mock('../services/aiService', () => ({
  summarizeNote: mocks.summarizeNote,
  generateFlashcards: mocks.generateFlashcards,
  getStudyTips: mocks.getStudyTips,
}));

vi.mock('../services/noteUploadService', () => ({
  extractAttachmentText: mocks.extractAttachmentText,
  analyzeExistingNote: mocks.analyzeExistingNote,
}));

vi.mock('../services/quizService', () => ({
  quizService: {
    generateQuiz: mocks.generateQuiz,
  },
}));

const note = {
  id: 'note-1',
  title: 'Hash Maps',
  content: 'Hash maps resolve collisions with chaining or open addressing.',
  courseId: 'course-1',
  courseName: 'Data Structures',
  tags: ['hashing'],
  createdAt: '2026-06-01T00:00:00Z',
};

const fileOnlyPdfNote = {
  ...note,
  content:
    'This PDF was uploaded without text extraction. Use AI Analyze on a text-based PDF or upload .txt/.md content to generate AI study tools.',
  attachments: [
    {
      id: 'attachment-1',
      name: 'lecture.pdf',
      type: 'application/pdf',
      base64: 'JVBERi0xLjQ=',
    },
  ],
};

const renderNoteDetail = () =>
  render(
    <MemoryRouter initialEntries={['/notes/note-1']}>
      <Routes>
        <Route path="/notes/:id" element={<NoteDetail />} />
        <Route path="/quizzes/:id" element={<div>Generated quiz route</div>} />
      </Routes>
    </MemoryRouter>
  );

describe('NoteDetail AI actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    mocks.get.mockResolvedValue({ data: { data: note } });
    mocks.put.mockResolvedValue({ data: { data: note } });
    mocks.delete.mockResolvedValue({ data: { success: true } });
    mocks.summarizeNote.mockResolvedValue({ data: { summary: 'Use chaining when multiple keys share a bucket.' } });
    mocks.generateFlashcards.mockResolvedValue({
      data: {
        flashcards: [{ front: 'Collision strategy?', back: 'Chaining or open addressing.' }],
      },
    });
    mocks.getStudyTips.mockResolvedValue({ data: { tips: 'Practice tracing inserts through buckets.' } });
    mocks.generateQuiz.mockResolvedValue({ data: { id: 'quiz-1' } });
    mocks.extractAttachmentText.mockResolvedValue({
      noteId: 'note-1',
      extractionStatus: 'Extracted',
      characterCount: 80,
      warning: null,
      message: 'Extracted text and saved the note.',
    });
    mocks.analyzeExistingNote.mockResolvedValue({
      noteId: 'note-1',
      title: 'Hash Maps',
      extractionStatus: 'Analyzed',
      characterCount: 80,
      attachmentCount: 1,
      warning: null,
      aiUsed: true,
      generationModeUsed: 'summary',
      fromCache: false,
      message: 'Analyzed extracted text and saved the note.',
    });
  });

  it('calls summary generation from the sidebar button', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate summary/i }));

    await waitFor(() =>
      expect(mocks.summarizeNote).toHaveBeenCalledWith({
        noteId: note.id,
        content: note.content,
        generationMode: 'Auto',
      })
    );
    expect(await screen.findByText(/use chaining/i)).toBeInTheDocument();
  });

  it('calls flashcard generation from the sidebar button', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate flashcards/i }));

    await waitFor(() =>
      expect(mocks.generateFlashcards).toHaveBeenCalledWith({
        noteId: note.id,
        content: note.content,
        generationMode: 'Auto',
      })
    );
    expect(await screen.findByText(/collision strategy/i)).toBeInTheDocument();
    expect(screen.getAllByText(/chaining or open addressing/i).length).toBeGreaterThanOrEqual(2);
  });

  it('calls study tips with note content instead of a generic topic', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate study tips/i }));

    await waitFor(() =>
      expect(mocks.getStudyTips).toHaveBeenCalledWith({
        topic: `${note.title}\n\n${note.content}`,
        generationMode: 'Auto',
      })
    );
    expect(await screen.findByText(/practice tracing inserts/i)).toBeInTheDocument();
  });

  it('generates a quiz from the current note and routes to it', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate quiz/i }));

    await waitFor(() =>
      expect(mocks.generateQuiz).toHaveBeenCalledWith({
        noteId: note.id,
        numberOfQuestions: 5,
        difficulty: 'Medium',
        questionTypes: ['MultipleChoice'],
        timeLimitMinutes: null,
      })
    );
    expect(await screen.findByText(/generated quiz route/i)).toBeInTheDocument();
  });

  it('shows PDF attachments and disables AI actions for file-only PDF notes', async () => {
    mocks.get.mockResolvedValueOnce({ data: { data: fileOnlyPdfNote } });

    renderNoteDetail();

    expect(await screen.findByText('lecture.pdf')).toBeInTheDocument();
    expect(screen.getByText(/AI summary, flashcards, quizzes, and study tips require readable extracted text/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /generate summary/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /generate flashcards/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /generate study tips/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /generate quiz/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /Extract Text/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /Analyze Existing File/i })).toBeEnabled();
  });

  it('extracts saved attachment text and enables AI actions after reload', async () => {
    const extractedNote = {
      ...fileOnlyPdfNote,
      content: '# lecture\n\n## Extracted Text\nReadable PDF text after extraction.',
    };
    mocks.get
      .mockResolvedValueOnce({ data: { data: fileOnlyPdfNote } })
      .mockResolvedValueOnce({ data: { data: extractedNote } });

    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /Extract Text/i }));

    await waitFor(() => expect(mocks.extractAttachmentText).toHaveBeenCalledWith('note-1'));
    expect(await screen.findByText(/Extracted text and saved the note/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /generate summary/i })).toBeEnabled();
  });

  it('renders the generation mode selector', async () => {
    renderNoteDetail();

    expect(await screen.findByRole('button', { name: 'Auto' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'AI' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Free Local' })).toBeInTheDocument();
  });

  it('sends FreeLocal when generating from the selector', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: 'Free Local' }));
    fireEvent.click(screen.getByRole('button', { name: /generate summary/i }));

    await waitFor(() =>
      expect(mocks.summarizeNote).toHaveBeenCalledWith({
        noteId: note.id,
        content: note.content,
        generationMode: 'FreeLocal',
      })
    );

    fireEvent.click(screen.getByRole('button', { name: /generate flashcards/i }));
    await waitFor(() =>
      expect(mocks.generateFlashcards).toHaveBeenCalledWith({
        noteId: note.id,
        content: note.content,
        generationMode: 'FreeLocal',
      })
    );

    fireEvent.click(screen.getByRole('button', { name: /generate study tips/i }));
    await waitFor(() =>
      expect(mocks.getStudyTips).toHaveBeenCalledWith({
        topic: `${note.title}\n\n${note.content}`,
        generationMode: 'FreeLocal',
      })
    );
  });

  it('shows fallback notices returned by Auto mode', async () => {
    mocks.summarizeNote.mockResolvedValueOnce({
      data: {
        summary: 'Local fallback summary.',
        notice: 'AI provider was unavailable, so Learnify generated this locally with Free Local study tools.',
        generationModeUsed: 'FreeLocal',
      },
    });
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate summary/i }));

    expect(await screen.findByText(/generated this locally with Free Local/i)).toBeInTheDocument();
    expect(await screen.findByText(/local fallback summary/i)).toBeInTheDocument();
  });

  it('shows provider errors when AI mode fails', async () => {
    mocks.summarizeNote.mockRejectedValueOnce({
      response: { data: { message: 'AI provider failed while summarizing this note.' } },
    });
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: 'AI' }));
    fireEvent.click(screen.getByRole('button', { name: /generate summary/i }));

    expect(await screen.findByText(/AI provider failed while summarizing/i)).toBeInTheDocument();
  });
});
