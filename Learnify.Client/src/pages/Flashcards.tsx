import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { generateFlashcards, summarizeNotes, generateRecommendations } from '../services/aiService';
import { toast } from 'react-hot-toast';

interface Flashcard {
  question: string;
  answer: string;
  explanation?: string;
}

export default function Flashcards() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'generate' | 'summarize' | 'recommend'>('generate');
  
  // Flashcard generation state
  const [flashcardContent, setFlashcardContent] = useState('');
  const [flashcardTitle, setFlashcardTitle] = useState('');
  const [generatedFlashcards, setGeneratedFlashcards] = useState<Flashcard[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [currentCardIndex, setCurrentCardIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);

  // Note summarization state
  const [notesText, setNotesText] = useState('');
  const [summary, setSummary] = useState('');
  const [isSummarizing, setIsSummarizing] = useState(false);

  // Recommendations state
  const [recommendationCourseId, setRecommendationCourseId] = useState('');
  const [recommendationCourseTitle, setRecommendationCourseTitle] = useState('');
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [isGeneratingRecommendations, setIsGeneratingRecommendations] = useState(false);

  const handleGenerateFlashcards = async () => {
    if (!flashcardContent.trim()) {
      toast.error('Please enter lesson content');
      return;
    }

    setIsGenerating(true);
    try {
      const result = await generateFlashcards(flashcardContent, flashcardTitle || undefined);
      setGeneratedFlashcards(result);
      setCurrentCardIndex(0);
      setIsFlipped(false);
      toast.success(`Generated ${result.length} flashcards!`);
    } catch {
      toast.error('Failed to generate flashcards. Please try again.');
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
    try {
      const result = await summarizeNotes(notesText);
      setSummary(result);
      toast.success('Notes summarized successfully!');
    } catch {
      toast.error('Failed to summarize notes. Please try again.');
    } finally {
      setIsSummarizing(false);
    }
  };

  const handleGenerateRecommendations = async () => {
    if (!recommendationCourseId.trim() || !recommendationCourseTitle.trim()) {
      toast.error('Please enter both course ID and title');
      return;
    }

    setIsGeneratingRecommendations(true);
    try {
      const result = await generateRecommendations(recommendationCourseId, recommendationCourseTitle);
      setRecommendations(result);
      toast.success(`Generated ${result.length} recommendations!`);
    } catch {
      toast.error('Failed to generate recommendations. Please try again.');
    } finally {
      setIsGeneratingRecommendations(false);
    }
  };

  const flipCard = () => setIsFlipped(!isFlipped);
  
  const nextCard = () => {
    setIsFlipped(false);
    setCurrentCardIndex(prev => Math.min(prev + 1, generatedFlashcards.length - 1));
  };
  
  const prevCard = () => {
    setIsFlipped(false);
    setCurrentCardIndex(prev => Math.max(prev - 1, 0));
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <button
            onClick={() => navigate('/')}
            className="text-gray-600 hover:text-gray-900 flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back
          </button>
          <h1 className="text-3xl font-bold text-gray-900">AI Learning Tools</h1>
          <div className="w-20"></div>
        </div>

        {/* Tabs */}
        <div className="flex gap-2 mb-8 bg-white rounded-lg p-1 shadow-sm">
          <button
            onClick={() => setActiveTab('generate')}
            className={`flex-1 py-3 px-4 rounded-md font-medium transition-colors ${
              activeTab === 'generate'
                ? 'bg-blue-600 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            Flashcard Generator
          </button>
          <button
            onClick={() => setActiveTab('summarize')}
            className={`flex-1 py-3 px-4 rounded-md font-medium transition-colors ${
              activeTab === 'summarize'
                ? 'bg-blue-600 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            Note Summarizer
          </button>
          <button
            onClick={() => setActiveTab('recommend')}
            className={`flex-1 py-3 px-4 rounded-md font-medium transition-colors ${
              activeTab === 'recommend'
                ? 'bg-blue-600 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            Recommendations
          </button>
        </div>

        {/* Flashcard Generator Tab */}
        {activeTab === 'generate' && (
          <div className="space-y-6">
            <div className="bg-white rounded-lg shadow-sm p-6">
              <h2 className="text-xl font-semibold mb-4">Generate Flashcards from Lesson Content</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Lesson Title (optional)
                  </label>
                  <input
                    type="text"
                    value={flashcardTitle}
                    onChange={(e) => setFlashcardTitle(e.target.value)}
                    placeholder="e.g., Introduction to React"
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Lesson Content
                  </label>
                  <textarea
                    value={flashcardContent}
                    onChange={(e) => setFlashcardContent(e.target.value)}
                    placeholder="Paste your lesson notes, textbook content, or any study material here..."
                    rows={10}
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <button
                  onClick={handleGenerateFlashcards}
                  disabled={isGenerating}
                  className="w-full bg-blue-600 text-white py-3 rounded-md font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {isGenerating ? (
                    <span className="flex items-center justify-center gap-2">
                      <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                      Generating...
                    </span>
                  ) : (
                    'Generate Flashcards'
                  )}
                </button>
              </div>
            </div>

            {/* Flashcard Display */}
            {generatedFlashcards.length > 0 && (
              <div className="bg-white rounded-lg shadow-sm p-6">
                <h3 className="text-lg font-semibold mb-4">
                  Generated Flashcards ({currentCardIndex + 1}/{generatedFlashcards.length})
                </h3>
                
                <div className="flex justify-center mb-6">
                  <div
                    onClick={flipCard}
                    className="w-full max-w-2xl min-h-[250px] cursor-pointer transition-transform duration-300 transform-style-3d relative"
                  >
                    <div className={`w-full min-h-[250px] bg-gradient-to-br from-blue-50 to-indigo-50 rounded-xl p-8 border-2 border-blue-200 flex items-center justify-center transition-all duration-300 ${isFlipped ? 'rotate-y-180' : ''}`}>
                      <div className="text-center">
                        {isFlipped ? (
                          <>
                            <p className="text-sm text-gray-500 mb-2">Answer</p>
                            <p className="text-lg font-medium text-gray-900">{generatedFlashcards[currentCardIndex].answer}</p>
                            {generatedFlashcards[currentCardIndex].explanation && (
                              <p className="text-sm text-gray-600 mt-4 italic">
                                {generatedFlashcards[currentCardIndex].explanation}
                              </p>
                            )}
                          </>
                        ) : (
                          <>
                            <p className="text-sm text-gray-500 mb-2">Question</p>
                            <p className="text-lg font-medium text-gray-900">{generatedFlashcards[currentCardIndex].question}</p>
                            <p className="text-sm text-gray-400 mt-4">Click to reveal answer</p>
                          </>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Navigation */}
                <div className="flex justify-between items-center">
                  <button
                    onClick={prevCard}
                    disabled={currentCardIndex === 0}
                    className="px-6 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Previous
                  </button>
                  <span className="text-gray-600">
                    {currentCardIndex + 1} / {generatedFlashcards.length}
                  </span>
                  <button
                    onClick={nextCard}
                    disabled={currentCardIndex === generatedFlashcards.length - 1}
                    className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Note Summarizer Tab */}
        {activeTab === 'summarize' && (
          <div className="space-y-6">
            <div className="bg-white rounded-lg shadow-sm p-6">
              <h2 className="text-xl font-semibold mb-4">Summarize Your Notes</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Your Notes
                  </label>
                  <textarea
                    value={notesText}
                    onChange={(e) => setNotesText(e.target.value)}
                    placeholder="Paste your raw notes here..."
                    rows={10}
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <button
                  onClick={handleSummarizeNotes}
                  disabled={isSummarizing}
                  className="w-full bg-blue-600 text-white py-3 rounded-md font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {isSummarizing ? (
                    <span className="flex items-center justify-center gap-2">
                      <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                      Summarizing...
                    </span>
                  ) : (
                    'Summarize Notes'
                  )}
                </button>
              </div>
            </div>

            {summary && (
              <div className="bg-white rounded-lg shadow-sm p-6">
                <h3 className="text-lg font-semibold mb-4">Summary</h3>
                <div className="prose max-w-none">
                  <p className="text-gray-700 whitespace-pre-wrap">{summary}</p>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Recommendations Tab */}
        {activeTab === 'recommend' && (
          <div className="space-y-6">
            <div className="bg-white rounded-lg shadow-sm p-6">
              <h2 className="text-xl font-semibold mb-4">Get Course Recommendations</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Course ID
                  </label>
                  <input
                    type="text"
                    value={recommendationCourseId}
                    onChange={(e) => setRecommendationCourseId(e.target.value)}
                    placeholder="e.g., course-123"
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Course Title
                  </label>
                  <input
                    type="text"
                    value={recommendationCourseTitle}
                    onChange={(e) => setRecommendationCourseTitle(e.target.value)}
                    placeholder="e.g., Introduction to Computer Science"
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <button
                  onClick={handleGenerateRecommendations}
                  disabled={isGeneratingRecommendations}
                  className="w-full bg-blue-600 text-white py-3 rounded-md font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {isGeneratingRecommendations ? (
                    <span className="flex items-center justify-center gap-2">
                      <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                      Generating...
                    </span>
                  ) : (
                    'Get Recommendations'
                  )}
                </button>
              </div>
            </div>

            {recommendations.length > 0 && (
              <div className="bg-white rounded-lg shadow-sm p-6">
                <h3 className="text-lg font-semibold mb-4">
                  Recommended Resources ({recommendations.length})
                </h3>
                <ul className="space-y-3">
                  {recommendations.map((rec, index) => (
                    <li key={index} className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
                      <span className="flex-shrink-0 w-8 h-8 bg-blue-100 text-blue-600 rounded-full flex items-center justify-center text-sm font-medium">
                        {index + 1}
                      </span>
                      <span className="text-gray-700">{rec}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}