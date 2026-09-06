# AGENTS.md — OCRSystem

Notes for AI coding agents working in this repo. Keep it current when architecture or workflow changes.

## What this is

Identity Document OCR MVP ( Qatar QID front side ): upload → quality check → OCR → field extraction → normalization → confidence → human review. Full pipeline runs with `docker compose up --build`. Private repo: https://github.com/FaruqHAO/OCRSystem (push via HTTPS + gh credential helper — see Known environment quirks).

## Layout

- `api/` — ASP.NET Core 10 minimal API: MongoDB persistence (`Persistence/`), local-disk storage behind `IDocumentStorage` (`Storage/`), HTTP OCR client behind `IOcrEngine` (`Ocr/`), definition-driven extraction (`Extraction/` + `definitions/*.json`), in-process `Channel<Guid>` queue + background worker (`Processing/`), API-key middleware and `Obfuscation.Redact()` (`Security/`).
- `ocr-service/` — FastAPI + PaddleOCR; `OCR_MODE=mock` returns deterministic canned lines (`app/mock_data.py`), `OCR_MODE=paddle` runs real OCR. Quality gate (resolution + Laplacian blur) lives here.
- `frontend/` — React 19 + Vite SPA: upload/demo, status polling, fields table with confidence, approve/correct/reject review.
- `tests/IdentityDocument.Api.Tests/` — xUnit tests for normalizers + extractor (29 tests).

## Commands

```bash
docker compose up --build                     # full stack (frontend :5173, api :8080)
dotnet test tests/IdentityDocument.Api.Tests  # unit tests
cd frontend && npm run dev                    # standalone frontend (proxies /api → :8080)
```

## Hard rules

- **Never log PII** (QID numbers, names, OCR text). Logs carry document IDs and statuses only; use `Obfuscation.Redact()` if a value must ever be logged.
- Extraction is **definition-driven**: new fields/document types come from `api/definitions/*.json` + normalizer cases, not ad-hoc code. See README "adding document type #2".
- Secrets only via `.env` (git-ignored); `.env.example` is the template. Committed defaults must stay placeholder-grade (`dev-api-key`).
- Never commit `.env`, `uploads/`, `.freebuff/`, `.docker-config/`, or logs (all git-ignored — keep it that way).

## Known environment quirks (this machine)

- **Docker Desktop data lives on E:** (`E:\DockerData`, set via `DataFolder` in Docker Desktop settings) — C: was 100% full and wedged the VM. Watch C: free space; Docker writes there for some caches.
- **Docker builds need a clean credential config**: run compose/build with `DOCKER_CONFIG=$PWD/.docker-config` (the user-level config's credential helper isn't found by buildx).
- **Image store**: Docker was switched to the classic (non-containerd) store after the copied disk corrupted; old images were rebuilt from scratch.
- **GitHub SSH is broken** (no key): push/pull over HTTPS with `gh auth setup-git` (gh account `FaruqHAO`, token in Windows keyring).
- Node 22, .NET 10 SDK, Python 3.x available locally; `gh` CLI authenticated.

## Status / next seams

- MVP complete: pipeline, tests (29 green), frontend, compose stack, README with run + data-protection docs.
- Extension points (documented in README): RabbitMQ swap for `ProcessingQueue`, `S3DocumentStorage` for MinIO, `TenantId` threading for multi-tenancy, audit log at the review endpoint, MRZ parser as a new extractor `kind` for passports.
- Not yet done: second document type (proves the seam), end-to-end compose smoke test on this machine after the Docker surgery.
