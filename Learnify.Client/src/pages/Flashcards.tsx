import { useState } from 'react';
import { generateFlashcards, getStudyTips, summarizeNote, type FlashcardItem } from '../services/aiService';
import { toast } from 'react-hot-toast';
import AiProviderSettings from '../components/AI/AiProviderSettings';
import StudyMaterialSourceSelector from '../components/AI/StudyMaterialSourceSelector';
import { AppButton, Badge, Card, PageHeader } from '../components/UI/Primitives';

interface LocalFlashcard {
  question: string;
  answer: string;
}

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as { response?: { data?: { message?: string; errors?: string[] } }; message?: string };
  return apiError.response?.data?.message ?? apiError.response?.data?.errors?.[0] ?? apiError.message ?? fallback;
};

export default function Flashcards() {
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
  const [tipsSourceText, setTipsSourceText] = useState('');
  const [studyTips, setStudyTips] = useState('');
  const [isGeneratingTips, setIsGeneratingTips] = useState(false);
  const [tipsError, setTipsError] = useState<string | null>(null);

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
      });
      const cards: LocalFlashcard[] = result.flashcards.map((f: FlashcardItem) => ({
        question: f.question,
        answer: f.answer,
      }));
      setGeneratedFlashcards(cards);
      setCurrentCardIndex(0);
      setIsFlipped(false);
      toast.success(`Generated ${cards.length} flashcards!`);
    } catch (error: unknown) {
      const message = getApiErrorMessage(error, 'Failed to generate flashcards. Please try again.');
      setFlashcardError(message);
      toast.error(message);
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
      });
      setSummary(result.summary);
      toast.success('Notes summarized successfully!');
    } catch (error: unknown) {
      const message = getApiErrorMessage(error, 'Failed to summarize notes. Please try again.');
      setSummaryError(message);
      toast.error(message);
    } finally {
      setIsSummarizing(false);
    }
  };

  const handleGenerateTips = async () => {
    const source = tipsSourceText.trim() || tipsTopic.trim();
    if (!source) {
      toast.error('Please enter a topic or provide study material');
      return;
    }

    setIsGeneratingTips(true);
    setTipsError(null);
    try {
      const result = await getStudyTips({ topic: source });
      setStudyTips(result.tips);
      toast.success('Study tips generated successfully!');
    } catch (error: unknown) {
      const message = getApiErrorMessage(error, 'Failed to generate study tips. Please try again.');
      setTipsError(message);
      toast.error(message);
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
              label="Lesson Content"
              value={flashcardContent}
              onChange={setFlashcardContent}
              placeholder="Paste your lesson notes, textbook content, or any study material here..."
              rows={10}
              disabled={isGenerating}
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
            <StudyMaterialSourceSelector
              label="Your Notes"
              value={notesText}
              onChange={setNotesText}
              placeholder="Paste your raw notes here..."
              rows={10}
              disabled={isSummarizing}
            />
            <AppButton type="button" onClick={() => void handleSummarizeNotes()} disabled={isSummarizing || !notesText.trim()}>
              {isSummarizing ? 'Summarizing...' : 'Summarize Notes'}
            </AppButton>
          </Card>
          <Card className="stack">
            <h2>Summary</h2>
            {summaryError && <div className="alert alert-danger">{summaryError}</div>}
            <p className="muted ai-output-text">{summary || 'Your summary will appear here.'}</p>
          </Card>
        </div>
      )}

      {activeTab === 'tips' && (
        <div className="grid grid-2">
          <Card className="stack">
            <h2>Get AI Study Tips</h2>
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
              label="Study material"
              value={tipsSourceText}
              onChange={setTipsSourceText}
              placeholder="Paste source material for content-specific study tips..."
              rows={8}
              disabled={isGeneratingTips}
            />
            <AppButton type="button" onClick={() => void handleGenerateTips()} disabled={isGeneratingTips || (!tipsTopic.trim() && !tipsSourceText.trim())}>
              {isGeneratingTips ? 'Generating...' : 'Get Study Tips'}
            </AppButton>
          </Card>
          <Card className="stack">
            <h2>{tipsTopic ? `Study Tips for "${tipsTopic}"` : 'Study Tips'}</h2>
            {tipsError && <div className="alert alert-danger">{tipsError}</div>}
            <p className="muted ai-output-text">{studyTips || 'Tips will appear here.'}</p>
          </Card>
        </div>
      )}
    </div>
  );
}
