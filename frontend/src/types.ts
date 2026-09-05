export type DocumentStatus =
  | 'UPLOADED'
  | 'PROCESSING'
  | 'QUALITY_FAILED'
  | 'COMPLETED'
  | 'REVIEW_REQUIRED'
  | 'APPROVED'
  | 'REJECTED'
  | 'FAILED';

export interface Quality {
  passed: boolean;
  width: number;
  height: number;
  blurScore: number;
  reason: string | null;
}

export interface ExtractedField {
  fieldName: string;
  label: string;
  value: string | null;
  normalizedValue: string | null;
  confidence: number;
  reviewedValue: string | null;
}

export interface FieldCorrection {
  fieldName: string;
  value: string;
}

export interface ReviewInfo {
  decision: string;
  reviewedAt: string;
  correctedFields: FieldCorrection[] | null;
}

export interface DocumentRecord {
  documentId: string;
  status: DocumentStatus;
  countryCode: string;
  documentType: string;
  createdAt: string;
  processedAt: string | null;
  overallConfidence: number | null;
  quality: Quality | null;
  extractedFields: ExtractedField[];
  review: ReviewInfo | null;
  errorMessage: string | null;
}

export interface UploadResponse {
  documentId: string;
  status: string;
}