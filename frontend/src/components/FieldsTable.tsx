import type { ExtractedField } from '../types';

interface Props {
  fields: ExtractedField[];
  edits: Record<string, string>;
  editable: boolean;
  onEdit: (fieldName: string, value: string) => void;
}

export function FieldsTable({ fields, edits, editable, onEdit }: Props) {
  return (
    <section className="panel">
      <h2>Extracted fields</h2>
      <table className="fields">
        <thead>
          <tr>
            <th>Field</th>
            <th>Value</th>
            <th>Normalized</th>
            <th>Confidence</th>
          </tr>
        </thead>
        <tbody>
          {fields.map((f) => {
            const pct = Math.round(f.confidence * 100);
            const display = f.reviewedValue ?? f.normalizedValue ?? f.value ?? '—';
            return (
              <tr key={f.fieldName} className={f.confidence === 0 ? 'missing' : ''}>
                <td className="label">{f.label}</td>
                <td>
                  {editable ? (
                    <input
                      value={edits[f.fieldName] ?? ''}
                      onChange={(e) => onEdit(f.fieldName, e.target.value)}
                      aria-label={f.label}
                    />
                  ) : (
                    <span className={f.reviewedValue ? 'reviewed' : ''}>{display}</span>
                  )}
                </td>
                <td className="normalized">{f.normalizedValue ?? '—'}</td>
                <td>
                  <div className="conf">
                    <div className="bar">
                      <div
                        className={`fill ${pct >= 80 ? 'good' : pct >= 50 ? 'mid' : 'bad'}`}
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span>{pct}%</span>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </section>
  );
}