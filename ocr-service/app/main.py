"""Identity Document OCR microservice.

POST /v1/ocr/{document_type}  -> quality check + OCR, returns lines + confidence.
GET  /healthz                 -> liveness probe for docker-compose.
"""

import os

from fastapi import FastAPI, HTTPException, Request

from . import mock_data, ocr_engine
from .quality import check_image

app = FastAPI(title="Identity Document OCR Service", version="0.1.0")

OCR_MODE = os.getenv("OCR_MODE", "paddle").lower()
OCR_LANG = os.getenv("OCR_LANG", "en")


@app.get("/healthz")
def healthz():
    return {"status": "ok", "mode": OCR_MODE}


@app.post("/v1/ocr/{document_type}")
async def ocr_document(document_type: str, request: Request):
    body = await request.body()
    if not body:
        raise HTTPException(status_code=400, detail="empty body")

    # Mock mode: skip real quality/OCR so the whole pipeline is deterministic.
    if OCR_MODE == "mock":
        return {
            "quality": {"passed": True, "width": 1, "height": 1, "blurScore": 999.0, "reason": None},
            "lines": mock_data.qa_qid_lines(),
            "language": "mock",
        }

    quality = check_image(body)
    if not quality["passed"]:
        return {"quality": quality, "lines": [], "language": OCR_LANG}

    lines = ocr_engine.run_ocr(body, OCR_LANG)
    return {"quality": quality, "lines": lines, "language": OCR_LANG}