import apiClient from './api';

export interface ExtractedMaterial {
  text: string;
  warning?: string | null;
  fileName?: string;
}

export async function extractText(file: File): Promise<ExtractedMaterial> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await apiClient.post<ExtractedMaterial>('/api/materials/extract-text', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return response.data;
}
