# Identity Document OCR — MVP

A small, real, end-to-end pipeline that extracts fields from a **Qatar QID (front side)** photo:

upload → quality check → OCR → field extraction → normalization → confidence scoring → human review.

Everything runs with a single `docker-compose up --build`.

```
┌──────────┐  upload / poll / review   ┌──────────┐   insert / update   ┌─────────┐
│ frontend │ ────────────────────────► │   api    │ ──────────────────► │  mongo  │
│ (React)  │ ◄──────────────────────── │ (.NET)   │ ◄────────────────── │         │
└──────────┘      GET /documents/{id}  └────┬─────┘                     └─────────┘
                                            │  POST /v1/ocr/{type} (image bytes)
                                            ▼
                                     ┌──────────────┐
                                     │ ocr-service  │   PaddleOCR + OpenCV
                                     │   (Python)   │   quality check + OCR lines
                                     └──────────────┘
```

- **api** — ASP.NET Core (minimal APIs). Owns the pipeline: stores uploads, persists documents in MongoDB, enqueues processing jobs on an in-process `Channel<T>` (`ProcessingQueue`), and a background worker (`DocumentProcessingWorker`) drives quality → OCR → extraction → normalization → confidence. Single hardcoded API key via `X-Api-Key`.
- **ocr-service** — FastAPI microservice. `POST /v1/ocr/{document_type}` returns `{quality: {passed, width, height, blurScore, reason}, lines: [{text, confidence, box}]}`. Quality gate = resolution + Laplacian-variance blur. Real OCR via PaddleOCR (`lang=en`), or deterministic mock output when `OCR_MODE=mock`.
- **mongo** — single `documents` collection. Extracted fields are embedded as an array (`ExtractedFields[]`), per the MVP scope (no joins).
- **frontend** — React + Vite SPA. Upload (or a demo image), live status polling, extracted fields with confidence bars, and an approve / correct / reject review screen.

## Setup & running

### Prerequisites

- **Docker Desktop** (Docker Engine + Compose v2) — the only requirement for the full stack.
- Optional for development without Docker: .NET 10 SDK, Node 22+, Python 3.11+, and a local MongoDB.

### First-time setup

```bash
# 1. Get the code
git clone <this-repo>
cd OCRSystem

# 2. (Optional) configure — everything has a working default
cp .env.example .env   # then edit API_KEY / OCR_MODE if you want
```

`.env` is **git-ignored** — never commit it. Out of the box:

| Variable   | Default        | Meaning |
| ---------- | -------------- | ------- |
| `API_KEY`  | `dev-api-key`  | Shared frontend/backend key (`X-Api-Key` header). Change it for anything non-local. |
| `OCR_MODE` | `mock`         | `mock` = instant deterministic demo; `paddle` = real PaddleOCR (first call downloads ~100 MB of models). |

### Run the full stack (recommended)

```bash
docker compose up --build
```

First build takes a few minutes (the OCR image includes PaddlePaddle). The stack starts:

| Service    | URL                     |
| ---------- | ----------------------- |
| Frontend   | http://localhost:5173   |
| API        | http://localhost:8080   |
| Mongo      | internal only (no host port) |
| ocr-service| internal only          |

Stop with `Ctrl+C`, or run detached with `docker compose up -d` (then `docker compose down` to stop; add `-v` to also wipe the mongo/uploads volumes — **this deletes stored documents and images**).

### Run in development (hot reload, no Docker)

Each service runs separately — useful when iterating on one component:

```bash
# Terminal 1 — MongoDB (any local instance on :27017)
docker run -d --name mongo-dev -p 27017:27017 mongo:8

# Terminal 2 — OCR service (FastAPI, port 8000)
cd ocr-service
python -m venv .venv && source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
OCR_MODE=mock uvicorn app.main:app --port 8000

# Terminal 3 — .NET API (port 8080)
cd api
dotnet run

# Terminal 4 — React frontend (port 5173, proxies /api → localhost:8080)
cd frontend
npm install
npm run dev
```

Then open http://localhost:5173. The frontend proxies `/api` to `http://localhost:8080` (override with `VITE_PROXY_TARGET`) and sends the `X-Api-Key` header (override with `VITE_API_KEY`).

### Run the tests

```bash
dotnet test tests/IdentityDocument.Api.Tests   # normalizers + extractor (29 tests)
```

### First run walkthrough

1. Open http://localhost:5173.
2. Click **Try demo image** (or upload a real QID photo).
3. Watch the status move `UPLOADED → PROCESSING → COMPLETED` (or `REVIEW_REQUIRED`), then review: edit fields and **Save corrections**, or **Approve as-is** / **Reject**.

| Service    | URL                     |
| ---------- | ----------------------- |
| Frontend   | http://localhost:5173   |
| API        | http://localhost:8080   |
| Mongo      | internal only (no host port) |
| ocr-service| internal only          |

1. Open http://localhost:5173.
2. Click **Try demo image** (or upload a real QID photo).
3. Watch the status move `UPLOADED → PROCESSING → COMPLETED` (or `REVIEW_REQUIRED`), then review: edit fields and **Save corrections**, or **Approve as-is** / **Reject**.

### Demo mode vs real OCR

The default `OCR_MODE=mock` makes the demo instant and deterministic: the OCR service returns canned QID-like lines, so the whole pipeline (quality → extraction → confidence → review) runs without a real photo or a model download.

To use real PaddleOCR:

```bash
OCR_MODE=paddle docker compose up --build
```

The first real OCR call downloads the PaddleOCR models (~100 MB) at runtime, and each call takes a few seconds on CPU. Upload a clear, well-lit photo of a QID front; blurry or low-resolution images are rejected with `QUALITY_FAILED`. Note that with a real photo the regex/heuristic extraction may produce `REVIEW_REQUIRED` — that is the intended safety behavior, not a bug.

## API

All endpoints except `/health` require `X-Api-Key: dev-api-key` (override with `API_KEY`).

| Method | Path                                  | Description |
| ------ | ------------------------------------- | ----------- |
| `POST` | `/api/v1/documents`                   | Multipart `file` (+ optional `documentType`, default `QID`). Returns `202 {documentId, status}` and enqueues processing. |
| `GET`  | `/api/v1/documents/{id}`              | Status, overall confidence, quality result, extracted fields, review info. |
| `POST` | `/api/v1/documents/{id}/review`       | `{decision: "approve"\|"reject"\|"correct", correctedFields?: [{fieldName, value}]}`. |
| `GET`  | `/health`                             | Liveness probe (no auth). |

### Status flow

```
UPLOADED ─► PROCESSING ─► COMPLETED        ─► APPROVED   (review: approve / correct)
                      └─► REVIEW_REQUIRED  ─┘
                      └─► QUALITY_FAILED
                      └─► FAILED            ─► REJECTED   (review: reject)
```

On startup the worker re-enqueues any document left in `UPLOADED`/`PROCESSING` (crash recovery).

### Confidence

Per-field confidence comes from the OCR line that produced the value (missing or un-normalizable fields get `0`). The overall score is the mean of the per-field scores. `COMPLETED` requires overall ≥ `0.75` (`Processing__CompletedThreshold`), otherwise `REVIEW_REQUIRED`.

## Document definitions & adding document type #2

Field extraction is driven by declarative **document-definition JSON** files in [`api/definitions/`](api/definitions). Each entry declares:

| Property     | Meaning |
| ------------ | ------- |
| `name`       | Stable key (`qidNumber`, `dateOfBirth`, …) |
| `label`      | Human label shown in the UI |
| `kind`       | `regex` — pull a value from OCR text, or `textLine` — pick a line by heuristic |
| `pattern`    | Regex (for `regex` fields) |
| `match`      | `contains` (anywhere), `exact` (whole line), `endOfLine` (last token) |
| `normalizer` | `qidNumber`, `date`, `monthYear`, `upper`, `name`, or none |

**To add a second document type** (e.g. UAE Emirates ID):

1. Add `api/definitions/ae-eid.json` describing its fields (kind/pattern/match/normalizer), mirroring `qa-qid.json`.
2. If a field needs new normalization rules, add one case in `api/Extraction/FieldNormalizers.cs` (a few lines).
3. If the layout needs a bespoke heuristic (like the QID name), add a `kind` branch in `api/Extraction/DocumentExtractor.cs`.
4. Optionally add a `mock_data.py` payload for the new type and a UI label map.

That is the whole seam: **a new definition JSON + a small amount of code**, not a rewrite. Everything else (queue, persistence, confidence, review) is document-agnostic.

## How your data is protected

The pipeline handles government-ID photos, so data safety is a design constraint, not an afterthought:

**What the code already does**

- **No PII in logs.** Log statements carry only document IDs and statuses — never OCR text, names, or QID numbers. `Obfuscation.Redact()` is the mandatory helper for any future log statement that could touch a value.
- **Full values only where the review flow needs them.** `GET /api/v1/documents/{id}` returns complete extracted values — required so a human can actually verify and correct them — and every response is behind the `X-Api-Key` check. `Obfuscation.Redact()` is ready for redacted display modes (e.g. a read-only dashboard) later.
- **No secrets in the repo.** The committed `.env.example` is a template; real values live in a git-ignored `.env`. `appsettings.json` and `docker-compose.yml` contain only placeholder defaults. A repo-wide secret scan (keys, tokens, passwords) comes up clean.
- **Local data stays local.** Uploaded images live in a Docker volume (`uploads`), database records in the `mongo-data` volume — neither leaves your machine, and Mongo has **no host port exposed** (only the internal compose network can reach it).
- **Review is the choke point.** Corrections/approvals flow through one endpoint, which is where audit logging attaches when this leaves MVP.

**Known MVP limits — fine for a demo, not for production**

- Auth is a single hardcoded `X-Api-Key` shared by frontend and backend. Anyone with the frontend can extract it; there is no user identity, no tenancy, no TLS termination story.
- Uploaded images and extracted data are stored unencrypted at rest (Docker volumes) and retained indefinitely.
- The API and OCR service do not rate-limit or cap request payloads beyond the 15 MB upload guard.
- Arabic name lines, MRZ/barcode, liveness, face-match and issuer verification are explicitly out of scope.

Before any real deployment: put real auth (per-user/tenant keys from a secret store) in front, encrypt volumes/backups, set a PII retention + deletion policy, and add audit logging at the review endpoint. The seams for all of these are listed in the next section.

## Path to the full architecture (documented seams, not implemented)

Everything is behind an interface or a single well-defined component, so moving to the production architecture is incremental:

- **RabbitMQ / external queue** — replace `api/Processing/ProcessingQueue.cs` (a `Channel<Guid>`) with a publisher + consumer. The worker body (`DocumentProcessingWorker.ProcessAsync`) is already a self-contained unit that takes a `documentId`; it moves unchanged. Add a dead-letter queue for `FAILED` documents and a retry policy.
- **MinIO / S3** — add an `S3DocumentStorage` implementing `IDocumentStorage` (`SaveAsync` / `OpenReadAsync` / `DeleteAsync`). No callers change; the worker already reads via the interface. Store `FilePath` as the object key and give the API an IAM-limited service account instead of a shared volume.
- **Multi-tenancy** — add `TenantId` to the `Document` aggregate, a `TenantId`-scoped index, and thread it through the repository (`GetAsync(tenantId, id)`) and API key resolution (map API key → tenant). The extraction layer is already tenant-agnostic.
- **Audit logging** — the review endpoint is a single choke point (`POST /documents/{id}/review`); append an outbox/audit write there. An `IdempotencyKey` column prevents duplicate reviews.
- **More document types** — see “adding document type #2” above; MRZ parsing (passports) becomes a new `kind` in the extractor plus a `mrz` normalizer.
- **Production hardening** — real auth (OAuth2/OIDC + per-tenant keys in a secret store), TLS, request size/rate limits, signed object URLs for images, PII-scoped retention/redaction policy, structured logging with correlation IDs.

## Assumptions made (per the brief)

- Fields for QA/QID front: **name, QID number, date of birth, expiry date, nationality** (the brief allowed inferring these).
- .NET 10 (current LTS), MongoDB.Driver (plain driver), Vite + React + TypeScript.
- Quality check (resolution + blur) runs inside the OCR service (it owns OpenCV); the .NET worker drives it and maps failure → `QUALITY_FAILED`.
- Overall confidence = mean of per-field confidences; threshold 0.75.
- A minimal review endpoint was added so the review screen is functional end-to-end.
- The QID name is extracted by a layout heuristic (long, high-confidence line in the upper half of the card that isn't a number/date) — tuned for clear photos; treat it as a candidate in review.