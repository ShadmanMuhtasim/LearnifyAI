import { useCallback, useEffect, useState } from 'react';
import { getActiveProvider, type ActiveProviderResponse } from '../../services/aiService';

interface AiProviderBadgeProps {
  onProviderChange?: (provider: ActiveProviderResponse) => void;
}

const providerLabel = (name: string) => {
  switch (name.toLowerCase()) {
    case 'gemini':
      return { icon: 'G', label: 'Gemini 3.5 Flash' };
    case 'openai':
      return { icon: 'O', label: 'OpenAI' };
    case 'claude':
      return { icon: 'C', label: 'Claude' };
    case 'ollama':
      return { icon: 'OL', label: 'Ollama' };
    case 'localopenai':
      return { icon: 'LC', label: 'Local OpenAI / llama.cpp' };
    default:
      return { icon: 'AI', label: name };
  }
};

const AiProviderBadge: React.FC<AiProviderBadgeProps> = ({ onProviderChange }) => {
  const [provider, setProvider] = useState<ActiveProviderResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProvider = useCallback(async () => {
    try {
      const result = await getActiveProvider();
      setProvider(result);
      setError(null);
      onProviderChange?.(result);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch provider info.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [onProviderChange]);

  useEffect(() => {
    void fetchProvider();
  }, [fetchProvider]);

  if (loading) {
    return (
      <div className="provider-badge">
        <span className="spinner" style={{ width: 12, height: 12, borderWidth: 2 }} aria-hidden="true" />
        Loading AI Provider...
      </div>
    );
  }

  if (error || !provider) {
    return <div className="provider-badge">Provider unavailable</div>;
  }

  const config = providerLabel(provider.activeProvider);
  const isLocalProvider = ['ollama', 'localopenai'].includes(provider.activeProvider.toLowerCase());

  return (
    <div className="provider-badge">
      <span className="nav-icon" style={{ width: 22, height: 22, flexBasis: 22 }} aria-hidden="true">
        {config.icon}
      </span>
      <span>Powered by {config.label}</span>
      <span className="provider-badge-model">({provider.model})</span>
      {isLocalProvider && provider.baseUrl && (
        <span className="provider-badge-model">{provider.baseUrl}</span>
      )}
    </div>
  );
};

export default AiProviderBadge;
