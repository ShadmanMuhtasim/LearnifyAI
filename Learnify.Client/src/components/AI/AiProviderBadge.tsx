import { useState, useEffect, useCallback } from 'react';
import { getActiveProvider, type ActiveProviderResponse } from '../../services/aiService';

interface AiProviderBadgeProps {
  onProviderChange?: (provider: ActiveProviderResponse) => void;
}

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
    fetchProvider();
  }, [fetchProvider]);

  if (loading) {
    return (
      <div style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '8px',
        padding: '6px 12px',
        backgroundColor: '#f1f5f9',
        borderRadius: '20px',
        fontSize: '13px',
        color: '#94a3b8'
      }}>
        <div style={{
          width: '8px',
          height: '8px',
          borderRadius: '50%',
          backgroundColor: '#cbd5e1',
          animation: 'pulse 1s ease-in-out infinite'
        }} />
        Loading AI Provider...
        <style>{`@keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }`}</style>
      </div>
    );
  }

  if (error || !provider) {
    return (
      <div style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '8px',
        padding: '6px 12px',
        backgroundColor: '#fff5f5',
        borderRadius: '20px',
        fontSize: '13px',
        color: '#c53030',
        border: '1px solid #fed7d7'
      }}>
        ⚠️ Provider unavailable
      </div>
    );
  }

  const getProviderConfig = (name: string): {
    icon: string;
    label: string;
    color: string;
    bgColor: string;
    borderColor: string;
  } => {
    switch (name.toLowerCase()) {
      case 'gemini':
        return {
          icon: '✨',
          label: 'Gemini 3.5 Flash',
          color: '#1e40af',
          bgColor: '#eff6ff',
          borderColor: '#93c5fd'
        };
      case 'openai':
        return {
          icon: '🤖',
          label: 'OpenAI',
          color: '#166534',
          bgColor: '#f0fdf4',
          borderColor: '#86efac'
        };
      case 'ollama':
        return {
          icon: '🏠',
          label: 'Ollama / Local LLaMA',
          color: '#7c2d12',
          bgColor: '#fff7ed',
          borderColor: '#fdba74'
        };
      case 'claude':
        return {
          icon: '⚡',
          label: 'Claude',
          color: '#581c87',
          bgColor: '#faf5ff',
          borderColor: '#d8b4fe'
        };
      default:
        return {
          icon: '🔮',
          label: name,
          color: '#4b5563',
          bgColor: '#f9fafb',
          borderColor: '#e5e7eb'
        };
    }
  };

  const config = getProviderConfig(provider.activeProvider);
  const isLocalProvider = provider.activeProvider.toLowerCase() === 'ollama';

  return (
    <div style={{
      display: 'inline-flex',
      alignItems: 'center',
      gap: '8px',
      padding: '6px 14px',
      backgroundColor: config.bgColor,
      borderRadius: '20px',
      fontSize: '13px',
      color: config.color,
      border: `1px solid ${config.borderColor}`,
      fontWeight: 500,
      boxShadow: '0 1px 2px rgba(0,0,0,0.05)'
    }}>
      <span>{config.icon}</span>
      <span>Powered by {config.label}</span>
      <span style={{
        opacity: 0.7,
        fontWeight: 400
      }}>
        ({provider.model})
      </span>
      {isLocalProvider && provider.baseUrl && (
        <span style={{
          opacity: 0.7,
          fontWeight: 400
        }}>
          {provider.baseUrl}
        </span>
      )}
    </div>
  );
};

export default AiProviderBadge;
