import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import QuizTaking from './QuizTaking';
import { quizService, type Quiz, type QuizResult } from '../services/quizService';

vi.mock('../services/quizService', () => ({
  quizService: {
    getQuiz: vi.fn(),
    submitAttempt: vi.fn(),
  },
}));

const mockedQuizService = vi.mocked(quizService);

const practiceQuiz: Quiz = {
  id: 'quiz-1',
  userId: 'user-1',
  noteId: 'note-1',
  title: 'Photosynthesis Quiz',
  difficulty: 'Easy',
  timeLimitMinutes: 1,
  questionTypes: ['MultipleChoice'],
  createdAt: '2026-06-04T00:00:00Z',
  questions: [
    {
      id: 'question-1',
      type: 'MultipleChoice',
      questionText: 'What pigment absorbs light?',
      options: ['Chlorophyll', 'Glucose', 'ATP', 'Oxygen'],
      correctAnswer: 'Chlorophyll',
      explanation: 'Chlorophyll captures light energy.',
      points: 1,
      orderIndex: 0,
    },
  ],
};

const examQuiz: Quiz = {
  ...practiceQuiz,
  questions: [
    {
      ...practiceQuiz.questions[0],
      correctAnswer: null,
      explanation: null,
    },
  ],
};

const result: QuizResult = {
  attemptId: 'attempt-1',
  quizId: 'quiz-1',
  score: 1,
  totalPoints: 1,
  percentage: 100,
  completedAt: '2026-06-04T00:00:00Z',
  answers: [],
};

function renderQuizTaking(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/quizzes/:id" element={<QuizTaking />} />
        <Route path="/quizzes/:id/result" element={<div>Result route</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('QuizTaking', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedQuizService.getQuiz.mockImplementation((_id, includeAnswers) =>
      Promise.resolve(includeAnswers ? practiceQuiz : examQuiz)
    );
    mockedQuizService.submitAttempt.mockResolvedValue(result);
  });

  it('renders the question and timer', async () => {
    renderQuizTaking('/quizzes/quiz-1');

    expect(await screen.findByText(/what pigment absorbs light/i)).toBeInTheDocument();
    expect(screen.getByText(/timer: 1:00/i)).toBeInTheDocument();
    expect(mockedQuizService.getQuiz).toHaveBeenCalledWith('quiz-1', false);
  });

  it('shows feedback in practice mode', async () => {
    renderQuizTaking('/quizzes/quiz-1?mode=practice');

    fireEvent.click(await screen.findByLabelText(/chlorophyll/i));

    expect(await screen.findByText(/correct/i)).toBeInTheDocument();
    expect(screen.getByText(/chlorophyll captures light energy/i)).toBeInTheDocument();
    expect(mockedQuizService.getQuiz).toHaveBeenCalledWith('quiz-1', true);
  });

  it('hides correct answers in exam mode before submit', async () => {
    renderQuizTaking('/quizzes/quiz-1');

    fireEvent.click(await screen.findByLabelText(/chlorophyll/i));

    expect(screen.queryByText(/correct answer/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/chlorophyll captures light energy/i)).not.toBeInTheDocument();
  });

  it('submits answers through the quiz service', async () => {
    renderQuizTaking('/quizzes/quiz-1');

    fireEvent.click(await screen.findByLabelText(/chlorophyll/i));
    fireEvent.click(screen.getByRole('button', { name: /submit quiz/i }));

    await waitFor(() => expect(mockedQuizService.submitAttempt).toHaveBeenCalledWith('quiz-1', [
      { questionId: 'question-1', userAnswer: 'Chlorophyll' },
    ]));
    expect(await screen.findByText(/result route/i)).toBeInTheDocument();
  });
});
