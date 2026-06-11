import { useState } from 'react';
import apiClient from '../../services/api';

interface Attachment {
  id?: string;
  name: string;
  fileName?: string;
  type: string;
  contentType?: string;
  base64?: string;
  sizeBytes?: number;
  createdAt?: string;
}

interface AttachmentViewerProps {
  noteId?: string;
  attachments: Attachment[];
  onRemove?: (index: number) => void;
}

const AttachmentViewer: React.FC<AttachmentViewerProps> = ({ noteId, attachments, onRemove }) => {
  const [preview, setPreview] = useState<Attachment | null>(null);

  const getName = (attachment: Attachment) => attachment.fileName || attachment.name;
  const getType = (attachment: Attachment) => attachment.contentType || attachment.type || 'application/octet-stream';

  const getFileIcon = (type: string): string => {
    if (type.startsWith('image/')) return '🖼️';
    if (type.includes('pdf')) return '📕';
    if (type.includes('word') || type.includes('document')) return '📘';
    if (type.includes('excel') || type.includes('spreadsheet')) return '📗';
    if (type.includes('powerpoint') || type.includes('presentation')) return '📙';
    if (type.includes('text')) return '📄';
    if (type.includes('zip') || type.includes('rar') || type.includes('archive')) return '🗜️';
    if (type.includes('video')) return '🎥';
    if (type.includes('audio')) return '🎵';
    return '📎';
  };

  const formatFileSize = (attachment: Attachment): string => {
    const bytes = attachment.sizeBytes ?? (attachment.base64 ? Math.round((attachment.base64.length * 3) / 4) : 0);
    if (bytes <= 0) return 'Stored file';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const getDownloadUrl = (attachment: Attachment): string | null => {
    if (!attachment.base64) {
      return null;
    }

    return `data:${getType(attachment)};base64,${attachment.base64}`;
  };

  const handleDownload = async (attachment: Attachment) => {
    if (noteId && attachment.id && !attachment.base64) {
      const response = await apiClient.get(`/api/notes/${noteId}/attachments/${attachment.id}/download`, {
        responseType: 'blob',
      });
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = getName(attachment);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      return;
    }

    const downloadUrl = getDownloadUrl(attachment);
    if (!downloadUrl) {
      return;
    }

    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = getName(attachment);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const isImage = (type: string): boolean => {
    return type.startsWith('image/');
  };

  if (attachments.length === 0) return null;

  return (
    <div style={{ marginTop: '16px' }}>
      <h4 style={{ margin: '0 0 12px 0', color: '#374151', fontSize: '14px' }}>
        📎 Attachments ({attachments.length})
      </h4>

      {/* Attachment grid */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
        gap: '12px',
        marginBottom: '12px'
      }}>
        {attachments.map((attachment, index) => (
          <div
            key={index}
            onClick={() => setPreview(attachment)}
            style={{
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              padding: '12px',
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              backgroundColor: '#f9fafb',
              position: 'relative'
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLDivElement).style.borderColor = '#3b82f6';
              (e.currentTarget as HTMLDivElement).style.boxShadow = '0 2px 8px rgba(59, 130, 246, 0.15)';
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLDivElement).style.borderColor = '#e5e7eb';
              (e.currentTarget as HTMLDivElement).style.boxShadow = 'none';
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
              <span style={{ fontSize: '24px' }}>{getFileIcon(getType(attachment))}</span>
              <div style={{ flex: 1, minWidth: 0 }}>
                <p style={{
                  margin: 0,
                  fontSize: '13px',
                  fontWeight: 500,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                  color: '#1f2937'
                }}>
                  {getName(attachment)}
                </p>
                <p style={{
                  margin: 0,
                  fontSize: '11px',
                  color: '#9ca3af'
                }}>
                  {formatFileSize(attachment)}
                </p>
              </div>
            </div>
            <div style={{ display: 'flex', gap: '4px' }}>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  void handleDownload(attachment);
                }}
                style={{
                  flex: 1,
                  padding: '4px 8px',
                  fontSize: '11px',
                  backgroundColor: '#3b82f6',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}
              >
                Download
              </button>
              {onRemove && (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onRemove(index);
                  }}
                  style={{
                    padding: '4px 8px',
                    fontSize: '11px',
                    backgroundColor: '#ef4444',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer'
                  }}
                >
                  Remove
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Preview modal */}
      {preview && (
        <div
          onClick={() => setPreview(null)}
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.7)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '20px'
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            style={{
              backgroundColor: 'white',
              borderRadius: '12px',
              padding: '24px',
              maxWidth: '800px',
              width: '100%',
              maxHeight: '90vh',
              overflow: 'auto',
              position: 'relative'
            }}
          >
            <button
              onClick={() => setPreview(null)}
              style={{
                position: 'absolute',
                top: '12px',
                right: '12px',
                background: 'none',
                border: 'none',
                fontSize: '24px',
                cursor: 'pointer',
                color: '#6b7280',
                width: '32px',
                height: '32px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                borderRadius: '50%'
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.backgroundColor = '#f3f4f6';
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.backgroundColor = 'transparent';
              }}
            >
              ×
            </button>

            <h3 style={{ margin: '0 0 16px 0', color: '#1f2937', paddingRight: '40px' }}>
              {getName(preview)}
            </h3>

            {isImage(getType(preview)) && getDownloadUrl(preview) ? (
              <img
                src={getDownloadUrl(preview) ?? ''}
                alt={getName(preview)}
                style={{
                  maxWidth: '100%',
                  maxHeight: '60vh',
                  objectFit: 'contain',
                  borderRadius: '8px'
                }}
              />
            ) : (
              <div style={{
                padding: '48px',
                textAlign: 'center',
                backgroundColor: '#f9fafb',
                borderRadius: '8px'
              }}>
                <div style={{ fontSize: '64px', marginBottom: '16px' }}>
                  {getFileIcon(getType(preview))}
                </div>
                <p style={{ color: '#6b7280', marginBottom: '16px' }}>
                  This file type cannot be previewed.
                </p>
                <button
                  onClick={() => void handleDownload(preview)}
                  style={{
                    padding: '8px 24px',
                    backgroundColor: '#3b82f6',
                    color: 'white',
                    border: 'none',
                    borderRadius: '6px',
                    cursor: 'pointer',
                    fontSize: '14px'
                  }}
                >
                  Download to View
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default AttachmentViewer;
