"""Image quality gate: resolution + blur (Laplacian variance)."""

import os

import cv2
import numpy as np

MIN_DIMENSION = int(os.getenv("QUALITY_MIN_DIMENSION", "500"))
MIN_BLUR_VARIANCE = float(os.getenv("QUALITY_MIN_BLUR_VARIANCE", "60"))


def check_image(image_bytes: bytes) -> dict:
    """Returns {passed, width, height, blurScore, reason}."""
    arr = np.frombuffer(image_bytes, dtype=np.uint8)
    img = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if img is None:
        return {"passed": False, "width": 0, "height": 0, "blurScore": 0.0, "reason": "unreadable-image"}

    h, w = img.shape[:2]
    if min(h, w) < MIN_DIMENSION:
        return {
            "passed": False,
            "width": w,
            "height": h,
            "blurScore": 0.0,
            "reason": f"low-resolution (shortest side {min(h, w)}px < {MIN_DIMENSION}px)",
        }

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    blur = float(cv2.Laplacian(gray, cv2.CV_64F).var())
    if blur < MIN_BLUR_VARIANCE:
        return {
            "passed": False,
            "width": w,
            "height": h,
            "blurScore": blur,
            "reason": f"blurry (laplacian variance {blur:.1f} < {MIN_BLUR_VARIANCE})",
        }

    return {"passed": True, "width": w, "height": h, "blurScore": blur, "reason": None}