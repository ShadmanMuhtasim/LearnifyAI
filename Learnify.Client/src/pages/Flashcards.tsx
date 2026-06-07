import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  generateFlashcards,
  getStudyTips,
  summarizeNote,
  type FlashcardItem,
  type GenerationMode,
  type SummaryDepth,
} from '../services/aiService';
import { toast } from 'react-hot-toast';
import AiProviderSettings from '../components/AI/AiProviderSettings';
import { AppButton, Badge, Card, PageHeader } from '../components/UI/Primitives';
import StudyMaterialSourceSelector from '../components/AI/StudyMaterialSourceSelector';

interface LocalFlashcard {
  question: string;
  answer: string;
}

type GenerationMeta = {
  generationModeUsed?: string;
  providerUsed?: string | null;
  fromCache?: boolean;
  notice?: string | null;
};

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
  return apiError.response?.data?.message ?? apiError.response?.data?.errors?.[0] ?? apiError.message ?? fallback;
};

const getApiErrorCode = (error: unknown) => {
  const apiError = error as { response?: { data?: { errorCode?: string } } };
  return apiError.response?.data?.errorCode;
};

function GenerationModeControl({
  value,
  onChange,
}: {
  value: GenerationMode;
  onChange: (mode: GenerationMode) => void;
}) {
  const helper = {
    Auto: 'Best balance. Uses AI when available and falls back if quota is reached.',
    AIProvider: 'Best quality. Uses selected provider.',
    FreeLocal: 'Always free. No API tokens. Simpler output.',
  }[value];

  return (
    <div className="stack">
      <div className="cluster" role="group" aria-label="Generation mode">
        {[
          ['Auto', 'Auto'],
          ['AIProvider', 'AI'],
          ['FreeLocal', 'Free Local'],
        ].map(([mode, label]) => (
          <button
            key={mode}
            type="button"
            className={`ui-button ${value === mode ? 'ui-button-primary' : 'ui-button-ghost'}`}
            onClick={() => onChange(mode as GenerationMode)}
          >
            {label}
          </button>
        ))}
      </div>
      <p className="muted text-small">{helper}</p>
    </div>
  );
}

function ResultBadges({ meta }: { meta: GenerationMeta | null }) {
  if (!meta) {
    return null;
  }

  return (
    <div className="cluster">
      {meta.generationModeUsed === 'FreeLocal' && <Badge tone="success">Free Local</Badge>}
      {meta.providerUsed && <Badge tone="primary">{meta.providerUsed}</Badge>}
      {meta.fromCache && <Badge tone="muted">From cache</Badge>}
    </div>
  );
}

export default function Flashcards() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'generate' | 'summarize' | 'tips'>('generate');
  const [showSettings, setShowSettings] = useState(false);
  const [flashcardContent, setFlashcardContent] = useState('');
  const [flashcardTitle, setFlashcardTitle] = useState('');
  const [generatedFlashcards, setGeneratedFlashcards] = useState<LocalFlashcard[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [flashcardError, setFlashcardError] = useState<string | null>(null);
  const [currentCardIndex, setCurrentCardIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [notesText, setNotesText] = useState('');
  const [summary, setSummary] = useState('');
  const [isSummarizing, setIsSummarizing] = useState(false);
  const [summaryError, setSummaryError] = useState<string | null>(null);
  const [tipsTopic, setTipsTopic] = useState('');
  const [tipsContent, setTipsContent] = useState('');
  const [studyTips, setStudyTips] = useState('');
  const [isGeneratingTips, setIsGeneratingTips] = useState(false);
  const [tipsError, setTipsError] = useState<string | null>(null);
  const [generationMode, setGenerationModeState] = useState<GenerationMode>(() => {
    const saved = window.localStorage.getItem('learnify-generation-mode');
    return saved === 'AIProvider' || saved === 'FreeLocal' || saved === 'Auto' ? saved : 'Auto';
  });
  const [summaryDepth, setSummaryDepth] = useState<SummaryDepth>('Balanced');
  const [flashcardMeta, setFlashcardMeta] = useState<GenerationMeta | null>(null);
  const [summaryMeta, setSummaryMeta] = useState<GenerationMeta | null>(null);
  const [tipsMeta, setTipsMeta] = useState<GenerationMeta | null>(null);

  const setGenerationMode = (mode: GenerationMode) => {
    setGenerationModeState(mode);
    window.localStorage.setItem('learnify-generation-mode', mode);
  };

  const showRateLimitToast = (message: string) => {
    toast((toastInstance) => (
      <div className="stack">
        <strong>AI provider limit reached</strong>
        <span>{message}</span>
        <div className="cluster">
          <button
            type="button"
            className="ui-button ui-button-primary"
            onClick={() => {
              setGenerationMode('FreeLocal');
              toast.dismiss(toastInstance.id);
            }}
          >
            Use Free Local
          </button>
          <button
            type="button"
            className="ui-button ui-button-secondary"
            onClick={() => {
              toast.dismiss(toastInstance.id);
              navigate('/settings');
            }}
          >
            Open AI Settings
          </button>
          <button
            type="button"
            className="ui-button ui-button-secondary"
            onClick={() => {
              setShowSettings(true);
              toast.dismiss(toastInstance.id);
            }}
          >
            Switch Provider
          </button>
        </div>
      </div>
    ), { duration: 9000 });
  };

  const handleGenerationError = (error: unknown, fallback: string, setError: (message: string) => void) => {
    const message = getApiErrorMessage(error, fallback);
    setError(message);
    if (getApiErrorCode(error) === 'AI_RATE_LIMIT' || message.toLowerCase().includes('quota') || message.toLowerCase().includes('rate limit')) {
      showRateLimitToast('Gemini quota or rate limit was reached. You can wait, switch to Free Local mode, add your own API key, or use your local LLM server.');
    } else {
      toast.error(message);
    }
  };

  const handleGenerateFlashcards = async () => {
    if (!flashcardContent.trim()) {
      toast.error('Please enter lesson content');
      return;
    }

    setIsGenerating(true);
    setFlashcardError(null);
    try {
      const result = await generateFlashcards({
        noteId: `temp-${Date.now()}`,
        content: flashcardContent,
        generationMode,
      });
      const cards: LocalFlashcard[] = result.flashcards.map((f: FlashcardItem) => ({
        question: f.question,
        answer: f.answer,
      }));
      setGeneratedFlashcards(cards);
      setCurrentCardIndex(0);
      setIsFlipped(false);
      setFlashcardMeta(result);
      if (result.notice) {
        toast(result.notice);
      }
      toast.success(`Generated ${cards.length} flashcards!`);
    } catch (error: unknown) {
      handleGenerationError(error, 'Failed to generate flashcards. Please try again.', setFlashcardError);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleSummarizeNotes = async () => {
    if (!notesText.trim()) {
      toast.error('Please enter notes to summarize');
      return;
    }

    setIsSummarizing(true);
    setSummaryError(null);
    try {
      const result = await summarizeNote({
        noteId: `temp-${Date.now()}`,
        content: notesText,
        generationMode,
        summaryDepth,
      });
      setSummary(result.summary);
      setSummaryMeta(result);
      if (result.notice) {
        toast(result.notice);
      }
      toast.success('Notes summarized successfully!');
    } catch (error: unknown) {
      handleGenerationError(error, 'Failed to summarize notes. Please try again.', setSummaryError);
    } finally {
      setIsSummarizing(false);
    }
  };

  const handleGenerateTips = async () => {
    const tipsSource = tipsContent.trim() || tipsTopic.trim();
    if (!tipsSource) {
      toast.error('Paste text, select a note, upload a file, or enter a topic first.');
      return;
    }

    setIsGeneratingTips(true);
    setTipsError(null);
    try {
      const result = await getStudyTips({ topic: tipsSource, generationMode });
      setStudyTips(result.tips);
      setTipsMeta(result);
      if (result.notice) {
        toast(result.notice);
      }
      toast.success('Study tips generated successfully!');
    } catch (error: unknown) {
      handleGenerationError(error, 'Failed to generate study tips. Please try again.', setTipsError);
    } finally {
      setIsGeneratingTips(false);
    }
  };

  const nextCard = () => {
    setIsFlipped(false);
    setCurrentCardIndex(prev => Math.min(prev + 1, generatedFlashcards.length - 1));
  };

  const prevCard = () => {
    setIsFlipped(false);
    setCurrentCardIndex(prev => Math.max(prev - 1, 0));
  };

  const currentCard = generatedFlashcards[currentCardIndex];

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Flashcards"
        title="AI Learning Tools"
        subtitle="Generate flashcards, summaries, and study tips from your own material."
        actions={<AppButton type="button" variant="secondary" onClick={() => setShowSettings((current) => !current)}>AI Provider Settings</AppButton>}
      />

      {showSettings && <AiProviderSettings />}

      <Card>
        <div className="cluster">
          {[
            ['generate', 'Flashcard Generator'],
            ['summarize', 'Note Summarizer'],
            ['tips', 'Study Tips'],
          ].map(([value, label]) => (
            <button
              key={value}
              type="button"
              className={`ui-button ${activeTab === value ? 'ui-button-primary' : 'ui-button-ghost'}`}
              onClick={() => setActiveTab(value as typeof activeTab)}
            >
              {label}
            </button>
          ))}
        </div>
      </Card>

      {activeTab === 'generate' && (
        <div className="page-two-column">
          <Card className="stack">
            <div>
              <h2>Generate Flashcards from Lesson Content</h2>
              <p className="muted mt-3">Paste study material and let the active AI provider create a focused deck.</p>
            </div>
            <GenerationModeControl value={generationMode} onChange={setGenerationMode} />
            <label>
              <span className="form-label">Lesson Title (optional)</span>
              <input
                type="text"
                value={flashcardTitle}
                onChange={(event) => setFlashcardTitle(event.target.value)}
                placeholder="e.g., Introduction to React"
              />
            </label>
            <StudyMaterialSourceSelector
              value={flashcardContent}
              onChange={setFlashcardContent}
              label="Lesson Content"
              placeholder="Paste your lesson notes, textbook content, or any study material here..."
            />
            <AppButton type="button" onClick={() => void handleGenerateFlashcards()} disabled={isGenerating || !flashcardContent.trim()}>
              {isGenerating ? 'Generating...' : 'Generate Flashcards'}
            </AppButton>
            {flashcardError && <div className="alert alert-danger">{flashcardError}</div>}
          </Card>

          <Card className="stack">
            <div className="split">
              <h2>Study Session</h2>
              {generatedFlashcards.length > 0 && (
                <Badge tone="primary">{currentCardIndex + 1}/{generatedFlashcards.length}</Badge>
              )}
            </div>
            <ResultBadges meta={flashcardMeta} />
            {flashcardMeta?.notice && <div className="alert alert-info">{flashcardMeta.notice}</div>}
            {currentCard ? (
              <>
                <button
                  type="button"
                  className="flashcard-surface"
                  onClick={() => setIsFlipped((current) => !current)}
                >
                  <div>
                    <div className="eyebrow mb-3">{isFlipped ? 'Answer' : 'Question'}</div>
                    <p className="ai-output-text">{isFlipped ? currentCard.answer : currentCard.question}</p>
                    <p className="muted text-small mt-3">Click to flip</p>
                  </div>
                </button>
                <div className="cluster" style={{ justifyContent: 'center' }}>
                  <AppButton type="button" variant="secondary" onClick={prevCard} disabled={currentCardIndex === 0}>Previous</AppButton>
                  <AppButton type="button" variant="secondary" onClick={() => setGeneratedFlashcards((cards) => [...cards].sort(() => Math.random() - 0.5))}>Shuffle</AppButton>
                  <AppButton type="button" onClick={nextCard} disabled={currentCardIndex === generatedFlashcards.length - 1}>Next</AppButton>
                </div>
                <div className="cluster" style={{ justifyContent: 'center' }}>
                  <AppButton type="button" variant="secondary">Got it</AppButton>
                  <AppButton type="button" variant="secondary">Review again</AppButton>
                </div>
              </>
            ) : (
              <p className="muted">Generated flashcards will appear here.</p>
            )}
          </Card>
        </div>
      )}

      {activeTab === 'summarize' && (
        <div className="grid grid-2">
          <Card className="stack">
            <h2>Summarize Your Notes</h2>
            <GenerationModeControl value={generationMode} onChange={setGenerationMode} />
            <label>
              <span className="form-label">Summary depth</span>
              <select value={summaryDepth} onChange={(event) => setSummaryDepth(event.target.value as SummaryDepth)}>
                <option value="Quick">Quick</option>
                <option value="Balanced">Balanced</option>
                <option value="Detailed">Detailed</option>
              </select>
            </label>
            <StudyMaterialSourceSelector
              value={notesText}
              onChange={setNotesText}
              label="Your Notes"
              placeholder="Paste your raw notes here..."
            />
            <AppButton type="button" onClick={() => void handleSummarizeNotes()} disabled={isSummarizing || !notesText.trim()}>
              {isSummarizing ? 'Summarizing...' : 'Summarize Notes'}
            </AppButton>
          </Card>
          <Card className="stack">
            <h2>Summary</h2>
            <ResultBadges meta={summaryMeta} />
            {summaryMeta?.notice && <div className="alert alert-info">{summaryMeta.notice}</div>}
            {summaryError && <div className="alert alert-danger">{summaryError}</div>}
            <p className="muted ai-output-text">{summary || 'Your summary will appear here.'}</p>
          </Card>
        </div>
      )}

      {activeTab === 'tips' && (
        <div className="grid grid-2">
          <Card className="stack">
            <h2>Get AI Study Tips</h2>
            <GenerationModeControl value={generationMode} onChange={setGenerationMode} />
            <label>
              <span className="form-label">Topic</span>
              <input
                type="text"
                value={tipsTopic}
                onChange={(event) => setTipsTopic(event.target.value)}
                placeholder="e.g., Machine Learning, World War II, Organic Chemistry"
              />
            </label>
            <StudyMaterialSourceSelector
              value={tipsContent}
              onChange={setTipsContent}
              label="Study Material"
              placeholder="Paste notes for content-specific study tips, or use only the topic above..."
            />
            <AppButton type="button" onClick={() => void handleGenerateTips()} disabled={isGeneratingTips || (!tipsTopic.trim() && !tipsContent.trim())}>
              {isGeneratingTips ? 'Generating...' : 'Get Study Tips'}
            </AppButton>
          </Card>
          <Card className="stack">
            <h2>{tipsTopic ? `Study Tips for "${tipsTopic}"` : 'Study Tips'}</h2>
            <ResultBadges meta={tipsMeta} />
            {tipsMeta?.notice && <div className="alert alert-info">{tipsMeta.notice}</div>}
            {tipsError && <div className="alert alert-danger">{tipsError}</div>}
            <p className="muted ai-output-text">{studyTips || 'Tips will appear here.'}</p>
          </Card>
        </div>
      )}
    </div>
  );
}
