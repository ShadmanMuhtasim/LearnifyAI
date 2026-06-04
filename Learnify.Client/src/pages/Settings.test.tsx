import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Settings from './Settings';
import apiClient from '../services/api';

vi.mock('../services/api', () => ({
  default: {
    get: vi.fn(),
    put: vi.fn(),
    post: vi.fn(),
  },
}));

const mockedApi = vi.mocked(apiClient);

describe('Settings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedApi.get.mockResolvedValue({
      data: {
        data: {
          activeProvider: 'Gemini',
          model: 'gemini-3.5-flash',
          customModel: null,
          ollamaBaseUrl: 'http://127.0.0.1:8080',
          localOpenAiBaseUrl: 'http://127.0.0.1:8080',
          hasApiKey: true,
          isDefault: true,
        },
      },
    });
  });

  it('loads provider settings', async () => {
    render(<Settings />);

    expect(await screen.findByRole('heading', { name: /settings/i })).toBeInTheDocument();
    expect(await screen.findByLabelText(/provider/i)).toHaveValue('Gemini');
    expect(screen.getByLabelText(/model/i)).toHaveValue('gemini-3.5-flash');
  });

  it('saves Gemini settings', async () => {
    mockedApi.put.mockResolvedValue({
      data: {
        data: {
          activeProvider: 'Gemini',
          model: 'gemini-3.5-flash',
          customModel: 'gemini-3.5-flash',
          hasApiKey: true,
          isDefault: false,
        },
      },
    });

    render(<Settings />);

    fireEvent.change(await screen.findByLabelText(/api key/i), {
      target: { value: 'gemini-test-key' },
    });
    fireEvent.click(screen.getByRole('button', { name: /save and use provider/i }));

    await waitFor(() => expect(mockedApi.put).toHaveBeenCalledWith('/api/user/ai-settings', expect.objectContaining({
      activeProvider: 'Gemini',
      apiKey: 'gemini-test-key',
      customModel: 'gemini-3.5-flash',
    })));
  });

  it('saves LocalOpenAI without confusing it with Ollama', async () => {
    mockedApi.put.mockResolvedValue({
      data: {
        data: {
          activeProvider: 'LocalOpenAI',
          model: 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf',
          customModel: 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf',
          ollamaBaseUrl: null,
          localOpenAiBaseUrl: 'http://127.0.0.1:8080',
          hasApiKey: false,
          isDefault: false,
        },
      },
    });

    render(<Settings />);

    fireEvent.change(await screen.findByLabelText(/provider/i), {
      target: { value: 'LocalOpenAI' },
    });
    fireEvent.click(screen.getByRole('button', { name: /save and use provider/i }));

    await waitFor(() => expect(mockedApi.put).toHaveBeenCalledWith('/api/user/ai-settings', expect.objectContaining({
      activeProvider: 'LocalOpenAI',
      localOpenAiBaseUrl: 'http://127.0.0.1:8080',
      ollamaBaseUrl: null,
    })));
    expect(mockedApi.put).not.toHaveBeenCalledWith('/api/user/ai-settings', expect.objectContaining({
      activeProvider: 'Ollama',
    }));
  });
});
