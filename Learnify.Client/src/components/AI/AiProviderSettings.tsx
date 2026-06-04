import { useEffect, useState } from 'react';
import apiClient from '../../services/api';
import { AppButton, Badge, ErrorState, LoadingState } from '../UI/Primitives';

type ProviderOption = 'Gemini' | 'OpenAI' | 'Claude' | 'Ollama' | 'LocalOpenAI';

interface UserAiSettingsResponse {
  activeProvider: ProviderOption;
  model: string;
  customModel?: string | null;
  ollamaBaseUrl?: string | null;
  localOpenAiBaseUrl?: string | null;
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

const LOCAL_BASE_URL = 'http://127.0.0.1:8080';
const OLLAMA_MODEL = 'llama3';
const LOCAL_OPENAI_MODEL = 'Qwen3.6-35B-A3B-UD-Q4_K_M.gguf';

const providerOptions: Array<{ value: ProviderOption; label: string; help: string }> = [
  { value: 'Gemini', label: 'Gemini', help: 'Default cloud model: gemini-3.5-flash.' },
  { value: 'OpenAI', label: 'OpenAI', help: 'OpenAI API-compatible hosted models.' },
  { value: 'Claude', label: 'Claude', help: 'Claude API provider.' },
  { value: 'Ollama', label: 'Ollama', help: 'Local Ollama server using /api/* endpoints.' },
  { value: 'LocalOpenAI', label: 'Local OpenAI-Compatible / llama.cpp', help: 'OpenAI-compatible local server using /v1/* endpoints.' },
];

const defaultModelForProvider = (provider: ProviderOption) => {
  switch (provider) {
    case 'Ollama':
      return OLLAMA_MODEL;
    case 'LocalOpenAI':
      return LOCAL_OPENAI_MODEL;
    case 'OpenAI':
      return 'gpt-4o-mini';
    case 'Claude':
      return 'claude-sonnet-4-20250514';
    default:
      return 'gemini-3.5-flash';
  }
};

export default function AiProviderSettings() {
  const [settings, setSettings] = useState<UserAiSettingsResponse | null>(null);
  const [selectedProvider, setSelectedProvider] = useState<ProviderOption>('Gemini');
  const [apiKey, setApiKey] = useState('');
  const [model, setModel] = useState('gemini-3.5-flash');
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState(LOCAL_BASE_URL);
  const [localOpenAiBaseUrl, setLocalOpenAiBaseUrl] = useState(LOCAL_BASE_URL);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState<MessageState>(null);
  const [testResult, setTestResult] = useState<ProviderTestResponse | null>(null);

  const isOllamaProvider = selectedProvider === 'Ollama';
  const isLocalOpenAiProvider = selectedProvider === 'LocalOpenAI';
  const isLocalProvider = isOllamaProvider || isLocalOpenAiProvider;
  const selectedBaseUrl = isLocalOpenAiProvider ? localOpenAiBaseUrl : ollamaBaseUrl;
  const setSelectedBaseUrl = isLocalOpenAiProvider ? setLocalOpenAiBaseUrl : setOllamaBaseUrl;

  const applySettingsToForm = (nextSettings: UserAiSettingsResponse) => {
    setSettings(nextSettings);
    setSelectedProvider(nextSettings.activeProvider || 'Gemini');
    setModel(nextSettings.customModel || nextSettings.model || OLLAMA_MODEL);
    setOllamaBaseUrl(nextSettings.ollamaBaseUrl || LOCAL_BASE_URL);
    setLocalOpenAiBaseUrl(nextSettings.localOpenAiBaseUrl || LOCAL_BASE_URL);
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
      setModel(OLLAMA_MODEL);
    }

    if (selectedProvider === 'Ollama' && !ollamaBaseUrl.trim()) {
      setOllamaBaseUrl(LOCAL_BASE_URL);
    }

    if (selectedProvider === 'LocalOpenAI' && !model.trim()) {
      setModel(LOCAL_OPENAI_MODEL);
    }

    if (selectedProvider === 'LocalOpenAI' && !localOpenAiBaseUrl.trim()) {
      setLocalOpenAiBaseUrl(LOCAL_BASE_URL);
    }
  }, [selectedProvider, model, ollamaBaseUrl, localOpenAiBaseUrl]);

  const selectProvider = (nextProvider: ProviderOption) => {
    setSelectedProvider(nextProvider);
    setTestResult(null);
    setModel(defaultModelForProvider(nextProvider));
    if (nextProvider === 'Ollama') {
      setOllamaBaseUrl(ollamaBaseUrl || LOCAL_BASE_URL);
    }
    if (nextProvider === 'LocalOpenAI') {
      setLocalOpenAiBaseUrl(localOpenAiBaseUrl || LOCAL_BASE_URL);
    }
  };

  const handleSave = async () => {
    if (!model.trim()) {
      setMessage({ tone: 'error', text: 'Enter a model name.' });
      return;
    }

    if (isLocalProvider && !selectedBaseUrl.trim()) {
      setMessage({ tone: 'error', text: 'Enter the local server Base URL.' });
      return;
    }

    try {
      setSaving(true);
      setMessage(null);

      const response = await apiClient.put<{ data: UserAiSettingsResponse }>('/api/user/ai-settings', {
        activeProvider: selectedProvider,
        apiKey: isOllamaProvider ? null : apiKey.trim() || null,
        customModel: model.trim(),
        ollamaBaseUrl: isOllamaProvider ? ollamaBaseUrl.trim() : null,
        localOpenAiBaseUrl: isLocalOpenAiProvider ? localOpenAiBaseUrl.trim() : null,
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
    if (!selectedBaseUrl.trim()) {
      setMessage({ tone: 'error', text: 'Enter a local server URL first.' });
      return;
    }

    try {
      setTesting(true);
      setMessage(null);
      setTestResult(null);

      const response = await apiClient.post<ProviderTestResponse>('/api/ai/provider/test', {
        provider: selectedProvider,
        baseUrl: selectedBaseUrl.trim(),
        model: model.trim() || (isLocalOpenAiProvider ? LOCAL_OPENAI_MODEL : OLLAMA_MODEL),
      });

      setTestResult(response.data);
      setMessage({
        tone: response.data.success ? 'success' : 'error',
        text: response.data.message,
      });
    } catch (error: any) {
      setMessage({
        tone: 'error',
        text: error?.response?.data?.message || `Local server not reachable at ${selectedBaseUrl}.`,
      });
    } finally {
      setTesting(false);
    }
  };

  if (loading) {
    return <LoadingState label="Loading AI provider settings..." />;
  }

  return (
    <div className="stack">
      {message && (
        message.tone === 'success'
          ? <div className="alert alert-success">{message.text}</div>
          : <ErrorState message={message.text} />
      )}

      <div className="provider-grid" aria-label="AI choices">
        {providerOptions.map((provider) => (
          <button
            key={provider.value}
            type="button"
            className={`provider-card ${selectedProvider === provider.value ? 'active' : ''}`}
            onClick={() => selectProvider(provider.value)}
          >
            <strong>{provider.label}</strong>
            <span>{provider.help}</span>
            {selectedProvider === provider.value && <Badge tone="primary">Selected</Badge>}
          </button>
        ))}
      </div>

      <div className="grid grid-2">
        <label>
          <span className="form-label">Provider</span>
          <select
            id="ai-provider"
            value={selectedProvider}
            onChange={(event) => selectProvider(event.target.value as ProviderOption)}
          >
            {providerOptions.map((provider) => (
              <option key={provider.value} value={provider.value}>
                {provider.label}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span className="form-label">Model</span>
          <input
            id="ai-model"
            type="text"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            placeholder={isLocalOpenAiProvider ? LOCAL_OPENAI_MODEL : isOllamaProvider ? OLLAMA_MODEL : 'gemini-3.5-flash'}
          />
        </label>
      </div>

      {!isOllamaProvider && (
        <label>
          <span className="form-label">{isLocalOpenAiProvider ? 'API Key (optional)' : 'API Key'}</span>
          <input
            id="ai-api-key"
            type="password"
            value={apiKey}
            onChange={(event) => setApiKey(event.target.value)}
            placeholder={settings?.hasApiKey ? 'Saved key exists. Leave blank to keep it.' : isLocalOpenAiProvider ? 'Optional bearer token' : 'Enter your API key'}
          />
        </label>
      )}

      {isLocalProvider && (
        <div className="grid grid-2">
          <label>
            <span className="form-label">Base URL</span>
            <input
              id="local-base-url"
              type="text"
              value={selectedBaseUrl}
              onChange={(event) => setSelectedBaseUrl(event.target.value)}
              placeholder={LOCAL_BASE_URL}
            />
          </label>
          <div className="alert alert-info">
            {isOllamaProvider
              ? 'Ollama uses the native /api/* protocol.'
              : 'LocalOpenAI / llama.cpp uses the OpenAI-compatible /v1/* protocol.'}
          </div>
        </div>
      )}

      {testResult && (
        <div className={testResult.success ? 'alert alert-success' : 'alert alert-danger'}>
          {testResult.message}
        </div>
      )}

      <div className="cluster">
        {isLocalProvider && (
          <AppButton type="button" variant="secondary" onClick={() => void handleTestLocal()} disabled={testing}>
            {testing ? 'Testing...' : 'Test Connection'}
          </AppButton>
        )}

        <AppButton type="button" onClick={() => void handleSave()} disabled={saving}>
          {saving ? 'Saving...' : 'Save and Use Provider'}
        </AppButton>
      </div>
    </div>
  );
}
