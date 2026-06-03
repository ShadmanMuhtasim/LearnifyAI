import AiProviderSettings from '../components/AI/AiProviderSettings';

export default function Settings() {
  return (
    <div style={{ maxWidth: '960px', margin: '0 auto' }}>
      <h1 style={{ margin: '0 0 1.5rem', color: '#1A202C' }}>Settings</h1>
      <section>
        <h2 style={{ margin: '0 0 1rem', color: '#2D3748', fontSize: '1.25rem' }}>
          AI Provider
        </h2>
        <AiProviderSettings />
      </section>
    </div>
  );
}
