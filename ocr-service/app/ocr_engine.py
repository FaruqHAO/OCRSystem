"""Real OCR via PaddleOCR (lazy engine init so mock mode starts instantly)."""

import cv2
import numpy as np

_engine = None
_engine_lang = None


def _get_engine(lang: str):
    global _engine, _engine_lang
    if _engine is None or _engine_lang != lang:
        from paddleocr import PaddleOCR

        _engine = PaddleOCR(lang=lang, show_log=False)
        _engine_lang = lang
    return _engine


def run_ocr(image_bytes: bytes, lang: str) -> list[dict]:
    """Runs OCR and returns [{text, confidence, box}] where box is a quad of [x, y] points."""
    arr = np.frombuffer(image_bytes, dtype=np.uint8)
    img = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if img is None:
        return []

    engine = _get_engine(lang)
    lines = []
    for page in engine.predict(img):
        texts = page.get("rec_texts") or []
        scores = page.get("rec_scores") or []
        polys = page.get("rec_polys") or []
        for text, score, poly in zip(texts, scores, polys):
            lines.append(
                {
                    "text": str(text).strip(),
                    "confidence": round(float(score), 4),
                    "box": [[float(x), float(y)] for x, y in poly],
                }
            )
    return lines