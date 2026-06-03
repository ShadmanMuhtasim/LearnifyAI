import { useEffect, useState, type CSSProperties } from 'react';
import apiClient from '../../services/api';

type ProviderOption = 'Gemini' | 'OpenAI' | 'Claude' | 'Ollama';

interface UserAiSettingsResponse {
  activeProvider: ProviderOption;
  model: string;
  customModel?: string | null;
  ollamaBaseUrl?: string | null;
  hasApiKey: boolean;
  isDefault: boolean;
}

interface ProviderTestResponse {
  success: boolean;
  provider: string;
  baseUrl: string;
  compatibleApi: string;
  message: string;
}

type MessageState = {
  tone: 'success' | 'error';
  text: string;
} | null;

const LOCAL_LLAMA_BASE_URL = 'http://127.0.0.1:8080';
const LOCAL_LLAMA_MODEL = 'llama3';

const providerOptions: Array<{ value: ProviderOption; label: string }> = [
  { value: 'Gemini', label: 'Gemini' },
  { value: 'OpenAI', label: 'OpenAI' },
  { value: 'Claude', label: 'Claude' },
  { value: 'Ollama', label: 'Ollama / Local LLaMA' },
];

const rootStyle: CSSProperties = {
  background: '#F7F8FC',
  borderRadius: '8px',
  padding: '1.5rem',
  boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
  color: '#1A202C',
};

const inputStyle: CSSProperties = {
  width: '100%',
  border: '1px solid #E2E8F0',
  borderRadius: '8px',
  padding: '0.75rem',
  outline: 'none',
  background: 'white',
};

const primaryButtonStyle: CSSProperties = {
  background: '#2563EB',
  color: 'white',
  border: 'none',
  borderRadius: '8px',
  padding: '0.75rem 1.2rem',
  cursor: 'pointer',
  fontWeight: 600,
};

const secondaryButtonStyle: CSSProperties = {
  background: 'white',
  color: '#2D3748',
  border: '1px solid #CBD5E0',
  borderRadius: '8px',
  padding: '0.75rem 1.2rem',
  cursor: 'pointer',
  fontWeight: 600,
};

export default function AiProviderSettings() {
  const [settings, setSettings] = useState<UserAiSettingsResponse | null>(null);
  const [selectedProvider, setSelectedProvider] = useState<ProviderOption>('Gemini');
  const [apiKey, setApiKey] = useState('');
  const [model, setModel] = useState('gemini-3.5-flash');
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState(LOCAL_LLAMA_BASE_URL);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState<MessageState>(null);
  const [testResult, setTestResult] = useState<ProviderTestResponse | null>(null);

  const isLocalProvider = selectedProvider === 'Ollama';

  const applySettingsToForm = (nextSettings: UserAiSettingsResponse) => {
    setSettings(nextSettings);
    setSelectedProvider(nextSettings.activeProvider || 'Gemini');
    setModel(nextSettings.customModel || nextSettings.model || LOCAL_LLAMA_MODEL);
    setOllamaBaseUrl(nextSettings.ollamaBaseUrl || LOCAL_LLAMA_BASE_URL);
    setApiKey('');
  };

  const loadSettings = async () => {
    try {
      setLoading(true);
      setMessage(null);
      const response = await apiClient.get<{ data: UserAiSettingsResponse }>('/api/user/ai-settings');
      applySettingsToForm(response.data.data);
    } catch (error: any) {
      setMessage({
        tone: 'error',
        text: error?.response?.data?.message || 'Failed to load AI settings.',
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadSettings();
  }, []);

  useEffect(() => {
    if (selectedProvider === 'Ollama' && !model.trim()) {
      setModel(LOCAL_LLAMA_MODEL);
    }

    if (selectedProvider === 'Ollama' && !ollamaBaseUrl.trim()) {
      setOllamaBaseUrl(LOCAL_LLAMA_BASE_URL);
    }
  }, [selectedProvider, model, ollamaBaseUrl]);

  const handleSave = async () => {
    if (!model.trim()) {
      setMessage({ tone: 'error', text: 'Enter a model name.' });
      return;
    }

    if (isLocalProvider && !ollamaBaseUrl.trim()) {
      setMessage({ tone: 'error', text: 'Enter the local LLaMA Base URL.' });
      return;
    }

    try {
      setSaving(true);
      setMessage(null);

      const response = await apiClient.put<{ data: UserAiSettingsResponse }>('/api/user/ai-settings', {
        activeProvider: selectedProvider,
        apiKey: isLocalProvider ? null : apiKey.trim() || null,
        customModel: model.trim(),
        ollamaBaseUrl: isLocalProvider ? ollamaBaseUrl.trim() : null,
      });

      applySettingsToForm(response.data.data);
      setMessage({ tone: 'success', text: 'AI provider settings saved.' });
    } catch (error: any) {
      setMessage({
        tone: 'error',
        text: error?.response?.data?.message || 'Failed to save AI settings.',
      });
    } finally {
      setSaving(false);
    }
  };

  const handleTestLocal = async () => {
    if (!ollamaBaseUrl.trim()) {
      setMessage({ tone: 'error', text: 'Enter a local server URL first.' });
      return;
    }

    try {
      setTesting(true);
      setMessage(null);
      setTestResult(null);

      const response = await apiClient.post<ProviderTestResponse>('/api/ai/provider/test', {
        provider: 'Ollama',
        baseUrl: ollamaBaseUrl.trim(),
        model: model.trim() || LOCAL_LLAMA_MODEL,
      });

      setTestResult(response.data);
      setMessage({
        tone: response.data.success ? 'success' : 'error',
        text: response.data.message,
      });
    } catch (error: any) {
      setMessage({
        tone: 'error',
        text: error?.response?.data?.message || `Local LLaMA server not reachable at ${ollamaBaseUrl}.`,
      });
    } finally {
      setTesting(false);
    }
  };

  if (loading) {
    return (
      <div style={rootStyle}>
        <p style={{ margin: 0, color: '#718096' }}>Loading AI provider settings...</p>
      </div>
    );
  }

  return (
    <div style={rootStyle}>
      {message && (
        <div style={{
          marginBottom: '1rem',
          padding: '0.85rem 1rem',
          borderRadius: '8px',
          background: message.tone === 'success' ? '#F0FFF4' : '#FFF5F5',
          color: message.tone === 'success' ? '#2F855A' : '#C53030',
          border: message.tone === 'success' ? '1px solid #9AE6B4' : '1px solid #FED7D7',
        }}>
          {message.text}
        </div>
      )}

      <div style={{ display: 'grid', gap: '1rem' }}>
        <div>
          <label htmlFor="ai-provider" style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
            Provider
          </label>
          <select
            id="ai-provider"
            value={selectedProvider}
            onChange={(event) => {
              const nextProvider = event.target.value as ProviderOption;
              setSelectedProvider(nextProvider);
              setTestResult(null);
              if (nextProvider === 'Ollama') {
                setModel(model || LOCAL_LLAMA_MODEL);
                setOllamaBaseUrl(ollamaBaseUrl || LOCAL_LLAMA_BASE_URL);
              }
            }}
            style={inputStyle}
          >
            {providerOptions.map((provider) => (
              <option key={provider.value} value={provider.value}>
                {provider.label}
              </option>
            ))}
          </select>
        </div>

        {!isLocalProvider && (
          <div>
            <label htmlFor="ai-api-key" style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
              API Key
            </label>
            <input
              id="ai-api-key"
              type="password"
              value={apiKey}
              onChange={(event) => setApiKey(event.target.value)}
              placeholder={settings?.hasApiKey ? 'Saved key exists. Leave blank to keep it.' : 'Enter your API key'}
              style={inputStyle}
            />
          </div>
        )}

        {isLocalProvider && (
          <div>
            <label htmlFor="ollama-base-url" style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
              Base URL
            </label>
            <input
              id="ollama-base-url"
              type="text"
              value={ollamaBaseUrl}
              onChange={(event) => setOllamaBaseUrl(event.target.value)}
              placeholder={LOCAL_LLAMA_BASE_URL}
              style={inputStyle}
            />
          </div>
        )}

        <div>
          <label htmlFor="ai-model" style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
            Model
          </label>
          <input
            id="ai-model"
            type="text"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            placeholder={isLocalProvider ? LOCAL_LLAMA_MODEL : 'gemini-3.5-flash'}
            style={inputStyle}
          />
        </div>

        {testResult && (
          <div style={{
            padding: '0.85rem 1rem',
            borderRadius: '8px',
            background: testResult.success ? '#F0FFF4' : '#FFF5F5',
            color: testResult.success ? '#2F855A' : '#C53030',
            border: testResult.success ? '1px solid #9AE6B4' : '1px solid #FED7D7',
          }}>
            {testResult.message}
          </div>
        )}

        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
          {isLocalProvider && (
            <button
              type="button"
              onClick={() => void handleTestLocal()}
              disabled={testing}
              style={{
                ...secondaryButtonStyle,
                opacity: testing ? 0.7 : 1,
                cursor: testing ? 'not-allowed' : 'pointer',
              }}
            >
              {testing ? 'Testing...' : 'Test Connection'}
            </button>
          )}

          <button
            type="button"
            onClick={() => void handleSave()}
            disabled={saving}
            style={{
              ...primaryButtonStyle,
              opacity: saving ? 0.7 : 1,
              cursor: saving ? 'not-allowed' : 'pointer',
            }}
          >
            {saving ? 'Saving...' : 'Save and Use Provider'}
          </button>
        </div>
      </div>
    </div>
  );
}
