import api from './api';

export type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'ShortAnswer' | 'FillInTheBlank';

export interface Question {
  id: string;
  type: QuestionType;
  questionText: string;
  options: string[];
  correctAnswer?: string | null;
  explanation?: string | null;
  points: number;
  orderIndex: number;
}

export interface Quiz {
  id: string;
  userId: string;
  courseId?: string | null;
  noteId?: string | null;
  title: string;
  description?: string | null;
  difficulty: string;
  timeLimitMinutes?: number | null;
  questionTypes: string[];
  questions: Question[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface GenerateQuizRequest {
  noteId: string;
  numberOfQuestions: number;
  difficulty: string;
  questionTypes: QuestionType[];
  timeLimitMinutes?: number | null;
}

export interface SubmitQuizAnswer {
  questionId: string;
  userAnswer: string;
}

export interface QuizAttempt {
  id: string;
  quizId: string;
  userId: string;
  score: number;
  totalPoints: number;
  percentage: number;
  startedAt: string;
  completedAt?: string | null;
}

export interface QuizResultAnswer {
  questionId: string;
  type: QuestionType;
  questionText: string;
  options: string[];
  userAnswer: string;
  correctAnswer: string;
  explanation?: string | null;
  isCorrect: boolean;
  pointsAwarded: number;
  points: number;
}

export interface QuizResult {
  attemptId: string;
  quizId: string;
  score: number;
  totalPoints: number;
  percentage: number;
  answers: QuizResultAnswer[];
  completedAt: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

export const quizService = {
  generateQuiz: (request: GenerateQuizRequest) =>
    api.post<ApiResponse<Quiz>>('/api/quizzes/generate', request).then((r) => r.data.data),
  getQuizzes: () =>
    api.get<ApiResponse<Quiz[]>>('/api/quizzes').then((r) => r.data.data),
  getQuiz: (id: string, includeAnswers = false) =>
    api.get<ApiResponse<Quiz>>(`/api/quizzes/${id}`, { params: { includeAnswers } }).then((r) => r.data.data),
  submitAttempt: (quizId: string, answers: SubmitQuizAnswer[]) =>
    api.post<ApiResponse<QuizResult>>(`/api/quizzes/${quizId}/attempts`, { answers }).then((r) => r.data.data),
  getAttempts: (quizId: string) =>
    api.get<ApiResponse<QuizAttempt[]>>(`/api/quizzes/${quizId}/attempts`).then((r) => r.data.data),
};
