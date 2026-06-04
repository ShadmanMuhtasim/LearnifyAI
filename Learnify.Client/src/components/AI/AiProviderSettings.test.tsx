import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import AiProviderSettings from './AiProviderSettings';
import apiClient from '../../services/api';

vi.mock('../../services/api', () => ({
  default: {
    get: vi.fn(),
    put: vi.fn(),
    post: vi.fn(),
  },
}));

const mockedApi = vi.mocked(apiClient);

describe('AiProviderSettings', () => {
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

  it('includes LocalOpenAI in the provider dropdown', async () => {
    render(<AiProviderSettings />);

    const providerSelect = await screen.findByLabelText(/provider/i);

    expect(providerSelect).toHaveTextContent('Gemini');
    expect(providerSelect).toHaveTextContent('OpenAI');
    expect(providerSelect).toHaveTextContent('Claude');
    expect(providerSelect).toHaveTextContent('Ollama');
    expect(providerSelect).toHaveTextContent('Local OpenAI-Compatible / llama.cpp');
  });

  it('shows LocalOpenAI base URL and model defaults when selected', async () => {
    render(<AiProviderSettings />);

    fireEvent.change(await screen.findByLabelText(/provider/i), {
      target: { value: 'LocalOpenAI' },
    });

    expect(screen.getByLabelText(/base url/i)).toHaveValue('http://127.0.0.1:8080');
    expect(screen.getByLabelText(/model/i)).toHaveValue('Qwen3.6-35B-A3B-UD-Q4_K_M.gguf');
    expect(screen.getByLabelText(/api key/i)).toBeInTheDocument();
  });

  it('saves LocalOpenAI as LocalOpenAI, not Ollama', async () => {
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

    render(<AiProviderSettings />);

    fireEvent.change(await screen.findByLabelText(/provider/i), {
      target: { value: 'LocalOpenAI' },
    });
    fireEvent.click(screen.getByRole('button', { name: /save and use provider/i }));

    await waitFor(() => expect(mockedApi.put).toHaveBeenCalled());
    expect(mockedApi.put).toHaveBeenCalledWith('/api/user/ai-settings', expect.objectContaining({
      activeProvider: 'LocalOpenAI',
      customModel: 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf',
      localOpenAiBaseUrl: 'http://127.0.0.1:8080',
      ollamaBaseUrl: null,
    }));
  });

  it('tests LocalOpenAI using the selected provider and base URL', async () => {
    mockedApi.post.mockResolvedValue({
      data: {
        success: true,
        provider: 'LocalOpenAI',
        baseUrl: 'http://127.0.0.1:8080',
        compatibleApi: 'openai',
        message: 'Connected to an OpenAI-compatible local server.',
      },
    });

    render(<AiProviderSettings />);

    fireEvent.change(await screen.findByLabelText(/provider/i), {
      target: { value: 'LocalOpenAI' },
    });
    fireEvent.click(screen.getByRole('button', { name: /test connection/i }));

    await waitFor(() => expect(mockedApi.post).toHaveBeenCalled());
    expect(mockedApi.post).toHaveBeenCalledWith('/api/ai/provider/test', expect.objectContaining({
      provider: 'LocalOpenAI',
      baseUrl: 'http://127.0.0.1:8080',
      model: 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf',
    }));
  });
});
