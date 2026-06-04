import { useRef, useState, type CSSProperties } from 'react';
import { useNavigate } from 'react-router-dom';
import apiClient from '../../services/api';

interface AnalyzeUploadResult {
  noteId: string;
  courseId: string;
  courseName: string;
  courseWasCreated: boolean;
  summary: string;
  detectedTopics: string[];
  message: string;
}

const uploadZoneStyle: CSSProperties = {
  border: '2px dashed #805AD5',
  borderRadius: '16px',
  padding: '3rem',
  textAlign: 'center',
  cursor: 'pointer',
  background: 'rgba(128,90,213,0.03)',
  transition: 'all 0.2s ease',
};

const inputStyle: CSSProperties = {
  width: '100%',
  border: '1px solid #E2E8F0',
  borderRadius: '10px',
  padding: '0.75rem',
  outline: 'none',
  transition: 'all 0.2s ease',
  background: 'white',
};

const primaryButtonStyle: CSSProperties = {
  background: 'linear-gradient(135deg, #6B46C1, #4299E1)',
  color: 'white',
  border: 'none',
  borderRadius: '10px',
  padding: '0.75rem 1.2rem',
  cursor: 'pointer',
  fontWeight: 600,
  transition: 'all 0.2s ease',
  boxShadow: '0 8px 24px rgba(66, 153, 225, 0.2)',
};

const readFileAsBase64 = (file: File): Promise<string> =>
  new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      resolve(result.split(',')[1]);
    };
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });

const getFileType = (file: File): string => {
  if (file.type === 'application/pdf') return 'pdf';
  if (file.type.includes('word') || file.name.endsWith('.docx')) return 'docx';
  if (file.type.startsWith('image/')) return 'image';
  return 'text';
};

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
};

const getFileIcon = (file: File | null): string => {
  if (!file) return '📄';
  const fileType = getFileType(file);
  if (fileType === 'pdf') return '📕';
  if (fileType === 'docx') return '📝';
  if (fileType === 'image') return '🖼️';
  return '📄';
};

export default function SmartUpload() {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [courseOverride, setCourseOverride] = useState('');
  const [isDragging, setIsDragging] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<AnalyzeUploadResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [renameValue, setRenameValue] = useState('');
  const [isRenaming, setIsRenaming] = useState(false);
  const [renameMessage, setRenameMessage] = useState<string | null>(null);

  const resetMessages = () => {
    setError(null);
    setRenameMessage(null);
  };

  const validateFile = (file: File) => {
    const isAllowed = ['pdf', 'docx', 'image', 'text'].includes(getFileType(file));
    if (!isAllowed) {
      setError('Only PDF, DOCX, PNG, JPG, and TXT files are supported.');
      return false;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('File too large. Maximum 5MB.');
      return false;
    }

    return true;
  };

  const selectFile = (file: File | null) => {
    if (!file) {
      return;
    }

    resetMessages();
    if (!validateFile(file)) {
      return;
    }

    setSelectedFile(file);
    setResult(null);
    setRenameValue('');
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    selectFile(event.target.files?.[0] ?? null);
  };

  const handleDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    selectFile(event.dataTransfer.files?.[0] ?? null);
  };

  const handleAnalyze = async () => {
    if (!selectedFile) {
      setError('Choose a file before starting the analysis.');
      return;
    }

    try {
      setIsSubmitting(true);
      resetMessages();

      const base64String = await readFileAsBase64(selectedFile);
      const response = await apiClient.post<any>('/api/notes/analyze-upload', {
        fileName: selectedFile.name,
        fileBase64: base64String,
        fileType: getFileType(selectedFile),
        preferredCourseName: courseOverride.trim() || null,
      });

      if (response.data.success) {
        const analysisResult: AnalyzeUploadResult = response.data.data;
        setResult(analysisResult);
        setRenameValue(analysisResult.courseName);
      } else {
        setError(response.data.message || 'Failed to analyze the document.');
      }
    } catch (requestError: any) {
      if (requestError?.response?.status === 429) {
        setError("You've hit the hourly limit on the default key. Switch to your own key in AI Settings.");
      } else {
        setError(requestError?.response?.data?.message || 'Failed to analyze and save the document.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRenameCourse = async () => {
    if (!result || !renameValue.trim()) {
      setRenameMessage('Enter a new course name before saving.');
      return;
    }

    try {
      setIsRenaming(true);
      setRenameMessage(null);

      await apiClient.put(`/api/courses/${result.courseId}`, {
        title: renameValue.trim(),
        description: '',
      });

      setResult({ ...result, courseName: renameValue.trim() });
      setRenameMessage('Course name updated.');
    } catch {
      setRenameMessage('Failed to rename the course.');
    } finally {
      setIsRenaming(false);
    }
  };

  return (
    <div>
      <div
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            inputRef.current?.click();
          }
        }}
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={(event) => {
          event.preventDefault();
          setIsDragging(false);
        }}
        onDrop={handleDrop}
        style={{
          ...uploadZoneStyle,
          background: isDragging ? 'rgba(128,90,213,0.1)' : uploadZoneStyle.background,
          borderStyle: isDragging ? 'solid' : 'dashed',
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,.docx,.png,.jpg,.jpeg,.txt"
          style={{ display: 'none' }}
          onChange={handleFileChange}
        />
        <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>{getFileIcon(selectedFile)}</div>
        <h3 style={{ marginTop: 0, marginBottom: '0.5rem', color: '#1A202C' }}>
          Drag and drop a file, or click to browse
        </h3>
        <p style={{ margin: 0, color: '#718096' }}>
          PDF, DOCX, PNG, JPG, or TXT up to 5MB
        </p>
      </div>

      {selectedFile && (
        <div style={{
          marginTop: '1rem',
          background: 'white',
          borderRadius: '16px',
          padding: '1rem 1.25rem',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          display: 'flex',
          justifyContent: 'space-between',
          gap: '1rem',
          flexWrap: 'wrap',
          alignItems: 'center',
        }}>
          <div>
            <div style={{ fontWeight: 700, color: '#1A202C' }}>{selectedFile.name}</div>
            <div style={{ color: '#718096', marginTop: '0.25rem' }}>
              {formatFileSize(selectedFile.size)} • {getFileType(selectedFile).toUpperCase()}
            </div>
          </div>
          <button
            type="button"
            onClick={() => {
              setSelectedFile(null);
              setResult(null);
              setCourseOverride('');
              setRenameValue('');
              resetMessages();
            }}
            style={{
              border: '1px solid #E2E8F0',
              borderRadius: '10px',
              background: 'white',
              padding: '0.65rem 1rem',
              color: '#4A5568',
              cursor: 'pointer',
            }}
          >
            Remove
          </button>
        </div>
      )}

      <div style={{ marginTop: '1rem' }}>
        <label style={{ display: 'block', marginBottom: '0.5rem', color: '#4A5568', fontWeight: 600 }}>
          Course name override (optional)
        </label>
        <input
          type="text"
          value={courseOverride}
          onChange={(event) => setCourseOverride(event.target.value)}
          placeholder="Leave blank to let the AI decide"
          style={inputStyle}
        />
      </div>

      <div style={{ marginTop: '1rem', display: 'flex', justifyContent: 'flex-start' }}>
        <button
          type="button"
          onClick={() => void handleAnalyze()}
          disabled={isSubmitting}
          style={{
            ...primaryButtonStyle,
            opacity: isSubmitting ? 0.7 : 1,
            cursor: isSubmitting ? 'not-allowed' : 'pointer',
          }}
        >
          {isSubmitting ? '🤖 AI is analyzing your document...' : 'Analyze & Save'}
        </button>
      </div>

      {error && (
        <div style={{
          marginTop: '1rem',
          padding: '1rem',
          borderRadius: '12px',
          background: '#FFF5F5',
          color: '#C53030',
          border: '1px solid #FED7D7',
        }}>
          <div>{error}</div>
          {selectedFile && (
            <button
              type="button"
              onClick={() => void handleAnalyze()}
              style={{ ...primaryButtonStyle, marginTop: '0.75rem' }}
            >
              Retry
            </button>
          )}
        </div>
      )}

      {result && (
        <div style={{
          background: 'white',
          borderRadius: '16px',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          padding: '1.5rem',
          marginTop: '1rem',
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
            <div>
              <h3 style={{ marginTop: 0, marginBottom: '0.5rem', color: '#1A202C' }}>{result.courseName}</h3>
              <span style={{
                display: 'inline-block',
                padding: '0.35rem 0.75rem',
                borderRadius: '999px',
                background: result.courseWasCreated ? '#E6FFFA' : '#EBF8FF',
                color: result.courseWasCreated ? '#2C7A7B' : '#2B6CB0',
                fontWeight: 600,
              }}>
                {result.courseWasCreated ? '✓ Created new course' : '✓ Saved to existing course'}
              </span>
            </div>
            <button
              type="button"
              onClick={() => navigate(`/notes/${result.noteId}`)}
              style={primaryButtonStyle}
            >
              View Note
            </button>
          </div>

          <div style={{ marginTop: '1rem' }}>
            <h4 style={{ marginBottom: '0.5rem', color: '#1A202C' }}>Summary</h4>
            <p style={{ margin: 0, color: '#4A5568', lineHeight: 1.7 }}>{result.summary}</p>
          </div>

          {result.detectedTopics.length > 0 && (
            <div style={{ marginTop: '1rem' }}>
              <h4 style={{ marginBottom: '0.75rem', color: '#1A202C' }}>Detected Topics</h4>
              <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                {result.detectedTopics.map((topic) => (
                  <span
                    key={topic}
                    style={{
                      background: '#EDE9FE',
                      color: '#6B46C1',
                      borderRadius: '20px',
                      padding: '0.25rem 0.75rem',
                      fontSize: '0.8rem',
                      fontWeight: 600,
                    }}
                  >
                    {topic}
                  </span>
                ))}
              </div>
            </div>
          )}

          <div style={{ marginTop: '1.25rem', display: 'grid', gap: '0.75rem' }}>
            <label style={{ color: '#4A5568', fontWeight: 600 }}>Rename Course</label>
            <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
              <input
                type="text"
                value={renameValue}
                onChange={(event) => setRenameValue(event.target.value)}
                style={{ ...inputStyle, flex: '1 1 260px' }}
              />
              <button
                type="button"
                onClick={() => void handleRenameCourse()}
                disabled={isRenaming}
                style={{
                  ...primaryButtonStyle,
                  opacity: isRenaming ? 0.7 : 1,
                  cursor: isRenaming ? 'not-allowed' : 'pointer',
                }}
              >
                {isRenaming ? 'Saving...' : 'Save'}
              </button>
            </div>
            {renameMessage && (
              <div style={{ color: renameMessage.includes('updated') ? '#2F855A' : '#C53030' }}>
                {renameMessage}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
