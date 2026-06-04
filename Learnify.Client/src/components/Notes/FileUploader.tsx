import { useState, useRef, useCallback } from 'react';

interface FileUploaderProps {
  onUploadSuccess: (attachment: { name: string; type: string; base64: string }) => void;
  maxSizeMB?: number;
}

interface UploadedFile {
  name: string;
  type: string;
  size: number;
}

const FileUploader: React.FC<FileUploaderProps> = ({ onUploadSuccess, maxSizeMB = 5 }) => {
  const [dragging, setDragging] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [files, setFiles] = useState<UploadedFile[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const validateFile = (file: File): boolean => {
    if (file.size > maxSizeMB * 1024 * 1024) {
      setError(`File size exceeds ${maxSizeMB}MB limit.`);
      return false;
    }
    return true;
  };

  const readFileAsBase64 = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  };

  const handleFiles = useCallback(async (selectedFiles: FileList | null) => {
    if (!selectedFiles || selectedFiles.length === 0) return;

    setError(null);
    const newFiles: UploadedFile[] = [];

    for (let i = 0; i < selectedFiles.length; i++) {
      const file = selectedFiles[i];

      if (!validateFile(file)) {
        continue;
      }

      try {
        setUploading(true);
        const base64 = await readFileAsBase64(file);
        
        // Extract the base64 data without the prefix for storage
        const base64Data = base64.split(',')[1];
        const mimeType = file.type || 'application/octet-stream';

        const attachment = {
          name: file.name,
          type: mimeType,
          base64: base64Data
        };

        onUploadSuccess(attachment);

        newFiles.push({
          name: file.name,
          type: mimeType,
          size: file.size
        });

        setFiles(prev => [...prev, newFiles[newFiles.length - 1]]);
      } catch {
        setError(`Failed to read file: ${file.name}`);
      } finally {
        setUploading(false);
      }
    }
  }, [onUploadSuccess, maxSizeMB]);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    handleFiles(e.dataTransfer.files);
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    handleFiles(e.target.files);
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  const handleRemove = (index: number) => {
    setFiles(prev => prev.filter((_, i) => i !== index));
  };

  return (
    <div style={{ marginTop: '16px' }}>
      <h4 style={{ margin: '0 0 12px 0', color: '#374151', fontSize: '14px' }}>
        📎 Attachments ({files.length})
      </h4>

      {/* Drop zone */}
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={handleClick}
        style={{
          border: `2px dashed ${dragging ? '#3b82f6' : '#d1d5db'}`,
          borderRadius: '8px',
          padding: '24px',
          textAlign: 'center',
          cursor: 'pointer',
          backgroundColor: dragging ? '#eff6ff' : '#f9fafb',
          transition: 'all 0.2s ease',
          marginBottom: '12px'
        }}
      >
        <input
          ref={fileInputRef}
          type="file"
          multiple
          style={{ display: 'none' }}
          onChange={handleFileSelect}
        />
        <div style={{ fontSize: '32px', marginBottom: '8px' }}>
          {uploading ? '⏳' : '📁'}
        </div>
        <p style={{ margin: '0 0 4px 0', color: '#6b7280', fontSize: '14px' }}>
          {uploading ? 'Processing file...' : 'Drag & drop files here, or click to browse'}
        </p>
        <p style={{ margin: 0, color: '#9ca3af', fontSize: '12px' }}>
          Max {maxSizeMB}MB per file
        </p>
      </div>

      {/* Error message */}
      {error && (
        <div style={{
          padding: '8px 12px',
          backgroundColor: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: '4px',
          color: '#dc2626',
          fontSize: '13px',
          marginBottom: '8px'
        }}>
          {error}
        </div>
      )}

      {/* File list */}
      {files.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {files.map((file, index) => (
            <div
              key={index}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '8px 12px',
                backgroundColor: '#f3f4f6',
                borderRadius: '6px',
                fontSize: '13px'
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flex: 1, minWidth: 0 }}>
                <span style={{ fontSize: '16px' }}>📄</span>
                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {file.name}
                </span>
                <span style={{ color: '#9ca3af', fontSize: '11px' }}>
                  ({formatFileSize(file.size)})
                </span>
              </div>
              <button
                onClick={(e) => { e.stopPropagation(); handleRemove(index); }}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#ef4444',
                  cursor: 'pointer',
                  padding: '4px',
                  fontSize: '16px',
                  lineHeight: 1
                }}
                title="Remove attachment"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default FileUploader;