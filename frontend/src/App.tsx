import { useEffect, useRef, useState } from 'react';
import { getDocument, reviewDocument, uploadDocument } from './api';
import type { DocumentRecord, DocumentStatus } from './types';
import { FieldsTable } from './components/FieldsTable';
import { ReviewControls } from './components/ReviewControls';
import { StatusCard } from './components/StatusCard';
import { Upload } from './components/Upload';

const ACTIVE: DocumentStatus[] = ['UPLOADED', 'PROCESSING'];
const REVIEWABLE: DocumentStatus[] = ['COMPLETED', 'REVIEW_REQUIRED', 'APPROVED', 'REJECTED'];

export default function App() {
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [doc, setDoc] = useState<DocumentRecord | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [edits, setEdits] = useState<Record<string, string>>({});
  const prevStatus = useRef<string | null>(null);

  // Poll while the document is in flight.
  useEffect(() => {
    if (!documentId) return;
    let cancelled = false;
    let timer: number | undefined;

    const poll = async () => {
      try {
        const d = await getDocument(documentId);
        if (cancelled) return;
        setDoc(d);
        if (ACTIVE.includes(d.status)) {
          timer = window.setTimeout(poll, 1500);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Failed to load document');
        }
      }
    };

    void poll();
    return () => {
      cancelled = true;
      if (timer) window.clearTimeout(timer);
    };
  }, [documentId]);

  // Seed the editable fields once the document reaches a terminal status.
  useEffect(() => {
    if (!doc) return;
    const terminal = !ACTIVE.includes(doc.status);
    if (terminal && prevStatus.current !== doc.status) {
      setEdits(
        Object.fromEntries(
          doc.extractedFields.map((f) => [f.fieldName, f.reviewedValue ?? f.normalizedValue ?? f.value ?? '']),
        ),
      );
    }
    prevStatus.current = doc.status;
  }, [doc]);

  const handleUpload = async (file: File) => {
    setError(null);
    setBusy(true);
    try {
      const { documentId: id } = await uploadDocument(file);
      setDoc(null);
      setEdits({});
      prevStatus.current = null;
      setDocumentId(id);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setBusy(false);
    }
  };

  const handleReview = async (decision: 'approve' | 'reject' | 'correct') => {
    if (!documentId || !doc) return;
    setBusy(true);
    setError(null);
    try {
      const correctedFields =
        decision === 'correct'
          ? doc.extractedFields.map((f) => ({ fieldName: f.fieldName, value: edits[f.fieldName] ?? '' }))
          : undefined;
      const updated = await reviewDocument(documentId, decision, correctedFields);
      setDoc(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Review failed');
    } finally {
      setBusy(false);
    }
  };

  const reset = () => {
    setDocumentId(null);
    setDoc(null);
    setEdits({});
    setError(null);
    prevStatus.current = null;
  };

  const active = doc !== null && ACTIVE.includes(doc.status);
  const canReview = doc !== null && REVIEWABLE.includes(doc.status);
  const hasFields = doc !== null && doc.extractedFields.length > 0;

  return (
    <main className="app">
      <header className="app-header">
        <h1>Identity Document OCR</h1>
        <p className="subtitle">Qatar QID · extract, verify, review</p>
      </header>

      {!documentId && <Upload busy={busy} error={error} onUpload={handleUpload} />}

      {documentId && doc === null && !error && (
        <div className="panel status-card loading">Waiting for the document…</div>
      )}

      {documentId && doc !== null && (
        <>
          <StatusCard doc={doc} />

          {active && <div className="panel status-card loading">Processing… this page refreshes automatically.</div>}

          {!active && hasFields && (
            <FieldsTable
              fields={doc.extractedFields}
              edits={edits}
              editable={canReview}
              onEdit={(fieldName, value) => setEdits((prev) => ({ ...prev, [fieldName]: value }))}
            />
          )}

          {canReview && (
            <ReviewControls busy={busy} onApprove={() => void handleReview('approve')} onReject={() => void handleReview('reject')} onCorrect={() => void handleReview('correct')} />
          )}

          {doc.review && (
            <p className="review-note">
              Reviewed: {doc.review.decision} at {new Date(doc.review.reviewedAt).toLocaleString()}
            </p>
          )}

          {error && <p className="error">{error}</p>}

          <button className="secondary reset" onClick={reset}>
            Upload another document
          </button>
        </>
      )}
    </main>
  );
}