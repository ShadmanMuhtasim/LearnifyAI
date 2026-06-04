import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import QuizResult from './QuizResult';
import type { QuizResult as QuizResultData } from '../services/quizService';

const result: QuizResultData = {
  attemptId: 'attempt-1',
  quizId: 'quiz-1',
  score: 1,
  totalPoints: 2,
  percentage: 50,
  completedAt: '2026-06-04T00:00:00Z',
  answers: [
    {
      questionId: 'question-1',
      type: 'MultipleChoice',
      questionText: 'Which organelle produces ATP?',
      options: ['Nucleus', 'Mitochondria'],
      userAnswer: 'Mitochondria',
      correctAnswer: 'Mitochondria',
      explanation: 'Mitochondria produce ATP through cellular respiration.',
      isCorrect: true,
      pointsAwarded: 1,
      points: 1,
    },
    {
      questionId: 'question-2',
      type: 'FillInTheBlank',
      questionText: 'Glycolysis produces ____.',
      options: [],
      userAnswer: 'ATP',
      correctAnswer: 'pyruvate',
      explanation: 'Glycolysis breaks glucose into pyruvate.',
      isCorrect: false,
      pointsAwarded: 0,
      points: 1,
    },
  ],
};

describe('QuizResult', () => {
  it('shows score, explanations, and retry incorrect flow', () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: '/quizzes/quiz-1/result', state: { result, quizTitle: 'Runtime Quiz' } }]}>
        <Routes>
          <Route path="/quizzes/:id/result" element={<QuizResult />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByRole('heading', { name: /runtime quiz/i })).toBeInTheDocument();
    expect(screen.getByText('1/2')).toBeInTheDocument();
    expect(screen.getByText('50%')).toBeInTheDocument();
    expect(screen.getByText(/Mitochondria produce ATP/)).toBeInTheDocument();
    expect(screen.getByText(/Glycolysis breaks glucose into pyruvate/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /retry incorrect questions/i }));

    expect(screen.getByRole('heading', { name: /retry incorrect questions/i })).toBeInTheDocument();
    expect(screen.getAllByText(/Glycolysis produces/).length).toBeGreaterThan(1);
  });
});
