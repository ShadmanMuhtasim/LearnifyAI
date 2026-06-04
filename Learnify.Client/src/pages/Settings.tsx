import AiProviderSettings from '../components/AI/AiProviderSettings';
import { Card, PageHeader } from '../components/UI/Primitives';

export default function Settings() {
  return (
    <div className="stack">
      <PageHeader
        eyebrow="Settings"
        title="Settings"
        subtitle="Manage AI provider routing and local model connection settings."
      />

      <Card className="stack">
        <div>
          <h2>AI Provider</h2>
          <p className="muted mt-3">
            Keep Gemini, OpenAI, Claude, Ollama, and LocalOpenAI clearly separated.
          </p>
        </div>
        <AiProviderSettings />
      </Card>
    </div>
  );
}
