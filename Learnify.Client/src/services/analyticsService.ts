import api from './api';

export interface RecentActivity {
  id: string;
  activityType: string;
  entityType?: string | null;
  entityId?: string | null;
  points: number;
  occurredAt: string;
}

export interface DashboardAnalytics {
  totalCourses: number;
  totalNotes: number;
  totalQuizzes: number;
  totalQuizAttempts: number;
  averageQuizScore: number;
  bestQuizScore: number;
  totalFlashcardsGenerated: number;
  totalSummariesGenerated: number;
  totalStudyTipsGenerated: number;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  unlockedAchievements: number;
  availableAchievements: number;
  recentActivity: RecentActivity[];
}

export interface RecentQuizAttempt {
  attemptId: string;
  quizId: string;
  quizTitle: string;
  score: number;
  totalPoints: number;
  percentage: number;
  completedAt: string;
}

export interface QuizSummary {
  quizId: string;
  title: string;
  attemptsCount: number;
  bestScore: number;
  averageScore: number;
}

export interface QuizPerformance {
  attemptsCount: number;
  averageScore: number;
  bestScore: number;
  recentAttempts: RecentQuizAttempt[];
  quizSummaries: QuizSummary[];
}

export interface AchievementStatus {
  id: string;
  code: string;
  title: string;
  description: string;
  icon?: string | null;
  requiredValue: number;
  achievementType: string;
  pointsReward: number;
  isUnlocked: boolean;
  unlockedAt?: string | null;
  progress: number;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

export const analyticsService = {
  getDashboard: () =>
    api.get<ApiResponse<DashboardAnalytics>>('/api/analytics/dashboard').then((r) => r.data.data),
  getQuizPerformance: () =>
    api.get<ApiResponse<QuizPerformance>>('/api/analytics/quiz-performance').then((r) => r.data.data),
  getRecentActivity: () =>
    api.get<ApiResponse<RecentActivity[]>>('/api/analytics/activity').then((r) => r.data.data),
  getAchievements: () =>
    api.get<ApiResponse<AchievementStatus[]>>('/api/achievements').then((r) => r.data.data),
};
