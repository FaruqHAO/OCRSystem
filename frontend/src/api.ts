import type { DocumentRecord, FieldCorrection, UploadResponse } from './types';

// MVP auth: single hardcoded key (override via VITE_API_KEY).
const API_KEY = import.meta.env.VITE_API_KEY || 'dev-api-key';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: {
      'X-Api-Key': API_KEY,
      ...(init?.headers ?? {}),
    },
  });

  if (!res.ok) {
    let message = `Request failed (${res.status})`;
    try {
      const body = await res.json();
      if (body?.error) message = body.error;
    } catch {
      // keep generic message
    }
    throw new Error(message);
  }

  return (await res.json()) as T;
}

export function uploadDocument(file: File, documentType = 'QID'): Promise<UploadResponse> {
  const form = new FormData();
  form.append('file', file);
  form.append('documentType', documentType);
  return request<UploadResponse>('/api/v1/documents', { method: 'POST', body: form });
}

export function getDocument(id: string): Promise<DocumentRecord> {
  return request<DocumentRecord>(`/api/v1/documents/${id}`);
}

export function reviewDocument(
  id: string,
  decision: 'approve' | 'reject' | 'correct',
  correctedFields?: FieldCorrection[],
): Promise<DocumentRecord> {
  return request<DocumentRecord>(`/api/v1/documents/${id}/review`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ decision, correctedFields }),
  });
}