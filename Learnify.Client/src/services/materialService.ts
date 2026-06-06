import apiClient from './api';

export interface ExtractedMaterialText {
  fileName: string;
  contentType: string;
  extractedText: string;
  characterCount: number;
  warning?: string | null;
}

export async function extractText(file: File): Promise<ExtractedMaterialText> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await apiClient.post<ExtractedMaterialText>(
    '/api/materials/extract-text',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  return response.data;
}
