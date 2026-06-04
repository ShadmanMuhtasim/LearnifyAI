import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import AiProviderBadge from './AiProviderBadge';
import { getActiveProvider } from '../../services/aiService';

vi.mock('../../services/aiService', () => ({
  getActiveProvider: vi.fn(),
}));

describe('AiProviderBadge', () => {
  it('labels LocalOpenAI as Local OpenAI / llama.cpp instead of Ollama', async () => {
    vi.mocked(getActiveProvider).mockResolvedValue({
      activeProvider: 'LocalOpenAI',
      provider: 'LocalOpenAI',
      model: 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf',
      baseUrl: 'http://127.0.0.1:8080',
    });

    render(<AiProviderBadge />);

    expect(await screen.findByText(/powered by local openai \/ llama\.cpp/i)).toBeInTheDocument();
    expect(screen.getByText(/Qwen3\.6-35B-A3B-UD-Q4_K_M\.gguf/)).toBeInTheDocument();
    expect(screen.getByText('http://127.0.0.1:8080')).toBeInTheDocument();
    expect(screen.queryByText(/Ollama \/ Local LLaMA/i)).not.toBeInTheDocument();
  });
});
