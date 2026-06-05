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
  });

  it('calls summary generation from the sidebar button', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate summary/i }));

    await waitFor(() => expect(mocks.summarizeNote).toHaveBeenCalledWith({ noteId: note.id, content: note.content }));
    expect(await screen.findByText(/use chaining/i)).toBeInTheDocument();
  });

  it('calls flashcard generation from the sidebar button', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate flashcards/i }));

    await waitFor(() => expect(mocks.generateFlashcards).toHaveBeenCalledWith({ noteId: note.id, content: note.content }));
    expect(await screen.findByText(/collision strategy/i)).toBeInTheDocument();
    expect(screen.getAllByText(/chaining or open addressing/i).length).toBeGreaterThanOrEqual(2);
  });

  it('calls study tips with note content instead of a generic topic', async () => {
    renderNoteDetail();

    fireEvent.click(await screen.findByRole('button', { name: /generate study tips/i }));

    await waitFor(() =>
      expect(mocks.getStudyTips).toHaveBeenCalledWith({
        topic: `${note.title}\n\n${note.content}`,
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
  });
});
