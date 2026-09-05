interface Props {
  busy: boolean;
  onApprove: () => void;
  onReject: () => void;
  onCorrect: () => void;
}

export function ReviewControls({ busy, onApprove, onReject, onCorrect }: Props) {
  return (
    <section className="panel review-controls">
      <h2>Review</h2>
      <p className="hint">Edit any field above, then save corrections — or approve / reject the extraction as-is.</p>
      <div className="actions">
        <button className="primary" disabled={busy} onClick={onApprove}>
          Approve as-is
        </button>
        <button className="secondary" disabled={busy} onClick={onCorrect}>
          Save corrections
        </button>
        <button className="danger" disabled={busy} onClick={onReject}>
          Reject
        </button>
      </div>
    </section>
  );
}