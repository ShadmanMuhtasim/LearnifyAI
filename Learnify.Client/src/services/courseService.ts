import api from './api';

export interface Course {
  id: string;
  title: string;
  description?: string;
  createdAt: string;
}

export interface CreateCourseDto {
  title: string;
  description?: string;
}

export interface UpdateCourseDto {
  title: string;
  description?: string;
}

export const courseService = {
  getAll: () => api.get<{ data: Course[] }>('/api/courses').then(r => r.data.data),
  getById: (id: string) => api.get<{ data: Course }>(`/api/courses/${id}`).then(r => r.data.data),
  create: (dto: CreateCourseDto) => api.post<{ data: Course }>('/api/courses', dto).then(r => r.data.data),
  update: (id: string, dto: UpdateCourseDto) => api.put<{ data: Course }>(`/api/courses/${id}`, dto).then(r => r.data.data),
  delete: (id: string) => api.delete(`/api/courses/${id}`),
};
