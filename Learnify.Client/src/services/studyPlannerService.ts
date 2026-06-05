import api from './api';

export type StudyPlanStatus = 'Pending' | 'Completed' | 'Skipped';

export interface StudyPlanItem {
  id: string;
  userId: string;
  courseId?: string | null;
  courseTitle?: string | null;
  noteId?: string | null;
  notePreview?: string | null;
  quizId?: string | null;
  quizTitle?: string | null;
  title: string;
  description?: string | null;
  planType: string;
  scheduledFor: string;
  estimatedMinutes: number;
  status: StudyPlanStatus;
  completedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  priority: string;
  source: string;
}

export interface CreateStudyPlanItemRequest {
  courseId?: string | null;
  noteId?: string | null;
  quizId?: string | null;
  title: string;
  description?: string | null;
  planType: string;
  scheduledFor: string;
  estimatedMinutes: number;
  priority: string;
  source?: string;
}

export interface UpdateStudyPlanItemRequest extends CreateStudyPlanItemRequest {
  status: StudyPlanStatus;
}

export interface StudySuggestion {
  title: string;
  description?: string | null;
  planType: string;
  courseId?: string | null;
  noteId?: string | null;
  quizId?: string | null;
  estimatedMinutes: number;
  priority: string;
}

export interface StudyPlanSummary {
  pendingCount: number;
  completedCount: number;
  todayCount: number;
  overdueCount: number;
  totalEstimatedMinutesToday: number;
  nextItem?: StudyPlanItem | null;
  suggestions: StudySuggestion[];
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

export const studyPlannerService = {
  getItems: (params?: { from?: string; to?: string; status?: string }) =>
    api.get<ApiResponse<StudyPlanItem[]>>('/api/study-planner', { params }).then((r) => r.data.data),
  getUpcoming: () =>
    api.get<ApiResponse<StudyPlanItem[]>>('/api/study-planner/upcoming').then((r) => r.data.data),
  getSummary: () =>
    api.get<ApiResponse<StudyPlanSummary>>('/api/study-planner/summary').then((r) => r.data.data),
  create: (request: CreateStudyPlanItemRequest) =>
    api.post<ApiResponse<StudyPlanItem>>('/api/study-planner', request).then((r) => r.data.data),
  update: (id: string, request: UpdateStudyPlanItemRequest) =>
    api.put<ApiResponse<StudyPlanItem>>(`/api/study-planner/${id}`, request).then((r) => r.data.data),
  complete: (id: string) =>
    api.post<ApiResponse<StudyPlanItem>>(`/api/study-planner/${id}/complete`, {}).then((r) => r.data.data),
  delete: (id: string) => api.delete(`/api/study-planner/${id}`),
};
