import apiClient from './api';

export type UploadMode = 'SaveOnly' | 'ExtractAndSave' | 'AiAnalyzeAndSave';

export type UploadMaterialRequest = {
  courseId: string;
  file: File;
  mode: UploadMode;
  title?: string;
  generationMode?: string;
  summaryDepth?: string;
};

export type UploadMaterialResponse = {
  noteId: string;
  title: string;
  extractionStatus: string;
  characterCount: number;
  attachmentCount: number;
  warning?: string | null;
  aiUsed: boolean;
  generationModeUsed?: string | null;
  fromCache: boolean;
  message: string;
};

export type PostUploadExtractionResponse = {
  noteId: string;
  extractionStatus: string;
  characterCount: number;
  warning?: string | null;
  message: string;
};

const unwrap = <T>(response: { data: { data?: T } | T }): T =>
  ((response.data as { data?: T })?.data ?? response.data) as T;

export const uploadMaterial = async (request: UploadMaterialRequest): Promise<UploadMaterialResponse> => {
  const formData = new FormData();
  formData.append('courseId', request.courseId);
  formData.append('file', request.file);
  formData.append('mode', request.mode);

  if (request.title?.trim()) {
    formData.append('title', request.title.trim());
  }

  if (request.generationMode) {
    formData.append('generationMode', request.generationMode);
  }

  if (request.summaryDepth) {
    formData.append('summaryDepth', request.summaryDepth);
  }

  const response = await apiClient.post('/api/notes/upload-material', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return unwrap<UploadMaterialResponse>(response);
};

export const extractAttachmentText = async (noteId: string): Promise<PostUploadExtractionResponse> => {
  const response = await apiClient.post(`/api/notes/${noteId}/extract-attachment-text`);
  return unwrap<PostUploadExtractionResponse>(response);
};

export const analyzeExistingNote = async (noteId: string): Promise<UploadMaterialResponse> => {
  const response = await apiClient.post(`/api/notes/${noteId}/analyze-existing`, {});
  return unwrap<UploadMaterialResponse>(response);
};
