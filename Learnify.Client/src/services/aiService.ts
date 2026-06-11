// ============================================================
// AI Service - Frontend client for LearnifyAI AI endpoints
// ============================================================

import apiClient from './api';

// ─── Request Types ───────────────────────────────────────────

export interface SummarizeNoteRequest {
  noteId: string;
  content: string;
  generationMode?: GenerationMode;
}

export interface FlashcardRequest {
  noteId: string;
  content: string;
  count?: number;
  generationMode?: GenerationMode;
}

export interface StudyTipsRequest {
  topic: string;
  generationMode?: GenerationMode;
}

export type GenerationMode = 'Auto' | 'AIProvider' | 'FreeLocal';

// ─── Response Types ──────────────────────────────────────────

export interface SummarizeNoteResponse {
  noteId: string;
  summary: string;
  generatedAt: string;
  generationModeUsed?: GenerationMode;
  providerUsed?: string | null;
  notice?: string | null;
  fromCache?: boolean;
}

export interface FlashcardItem {
  question: string;
  answer: string;
}

export interface FlashcardResponse {
  noteId: string;
  flashcards: FlashcardItem[];
  generatedAt: string;
  generationModeUsed?: GenerationMode;
  providerUsed?: string | null;
  notice?: string | null;
  fromCache?: boolean;
}

export interface StudyTipsResponse {
  tips: string;
  generatedAt: string;
  generationModeUsed?: GenerationMode;
  providerUsed?: string | null;
  notice?: string | null;
  fromCache?: boolean;
}

export interface ActiveProviderResponse {
  activeProvider: string;
  provider?: string;
  model: string;
  mode?: string;
  baseUrl?: string;
  remainingDefaultRequests?: number;
}

// ─── API Functions ───────────────────────────────────────────

/**
 * Summarize note content using AI.
 */
export async function summarizeNote(
  request: SummarizeNoteRequest
): Promise<SummarizeNoteResponse> {
  const response = await apiClient.post<SummarizeNoteResponse>(
    '/api/ai/summarize',
    request
  );
  return response.data;
}

/**
 * Generate flashcards from educational content using AI.
 */
export async function generateFlashcards(
  request: FlashcardRequest
): Promise<FlashcardResponse> {
  const response = await apiClient.post<FlashcardResponse>(
    '/api/ai/flashcards',
    request
  );
  return response.data;
}

/**
 * Generate study tips for a given topic using AI.
 */
export async function getStudyTips(
  request: StudyTipsRequest
): Promise<StudyTipsResponse> {
  const response = await apiClient.post<StudyTipsResponse>(
    '/api/ai/study-tips',
    request
  );
  return response.data;
}

/**
 * Get the currently active AI provider and model.
 */
export async function getActiveProvider(): Promise<ActiveProviderResponse> {
  const response = await apiClient.get<ActiveProviderResponse>(
    '/api/ai/provider'
  );
  const data = response.data;
  return {
    ...data,
    activeProvider: data.activeProvider || data.provider || 'Unknown',
  };
}
