import { useRef, useState } from 'react';

// Tiny valid 1x1 PNG — enough to exercise the pipeline in mock OCR mode.
const DEMO_PNG_B64 =
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==';

function demoFile(): File {
  const bytes = Uint8Array.from(atob(DEMO_PNG_B64), (c) => c.charCodeAt(0));
  return new File([bytes], 'demo-qid.png', { type: 'image/png' });
}

interface Props {
  busy: boolean;
  error: string | null;
  onUpload: (file: File) => Promise<void>;
}

export function Upload({ busy, error, onUpload }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [fileName, setFileName] = useState<string | null>(null);

  return (
    <section className="panel">
      <h2>Upload a QID image</h2>
      <p className="hint">Front side of a Qatar ID card — jpg / png / webp, max 15 MB.</p>

      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        disabled={busy}
        onChange={(e) => setFileName(e.target.files?.[0]?.name ?? null)}
      />

      <div className="actions">
        <button
          className="primary"
          disabled={busy || !fileName}
          onClick={() => {
            const file = inputRef.current?.files?.[0];
            if (file) void onUpload(file);
          }}
        >
          {busy ? 'Uploading…' : 'Upload'}
        </button>
        <button className="secondary" disabled={busy} onClick={() => void onUpload(demoFile())}>
          Try demo image
        </button>
      </div>

      <p className="hint">
        No QID photo handy? <strong>Try demo image</strong> runs the full pipeline against mock OCR output, so you
        can see statuses, extraction and review end-to-end.
      </p>

      {error && <p className="error">{error}</p>}
    </section>
  );
}