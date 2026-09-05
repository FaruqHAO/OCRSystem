import type { DocumentRecord } from '../types';

const STATUS_LABELS: Record<string, string> = {
  UPLOADED: 'Uploaded',
  PROCESSING: 'Processing',
  QUALITY_FAILED: 'Quality check failed',
  COMPLETED: 'Completed',
  REVIEW_REQUIRED: 'Review required',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
  FAILED: 'Failed',
};

export function StatusCard({ doc }: { doc: DocumentRecord }) {
  const cls = doc.status.toLowerCase();
  return (
    <section className={`panel status-card ${cls}`}>
      <div className="status-row">
        <span className={`status-badge ${cls}`}>{STATUS_LABELS[doc.status] ?? doc.status}</span>
        {doc.overallConfidence !== null && doc.overallConfidence !== undefined && (
          <span className="overall">
            Overall confidence: <strong>{(doc.overallConfidence * 100).toFixed(1)}%</strong>
          </span>
        )}
      </div>

      <dl className="meta">
        <div>
          <dt>Document</dt>
          <dd>
            {doc.countryCode} / {doc.documentType}
          </dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{new Date(doc.createdAt).toLocaleString()}</dd>
        </div>
        {doc.processedAt && (
          <div>
            <dt>Processed</dt>
            <dd>{new Date(doc.processedAt).toLocaleString()}</dd>
          </div>
        )}
      </dl>

      {doc.quality && (
        <p className="quality-info">
          {doc.quality.passed
            ? `Image OK — ${doc.quality.width}×${doc.quality.height}px, blur score ${doc.quality.blurScore.toFixed(1)}`
            : `Image rejected: ${doc.quality.reason}`}
        </p>
      )}

      {doc.errorMessage && <p className="error">{doc.errorMessage}</p>}
    </section>
  );
}