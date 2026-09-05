"""Deterministic canned OCR output for demo/testing without a real QID photo."""


def qa_qid_lines() -> list[dict]:
    return [
        {"text": "STATE OF QATAR", "confidence": 0.99, "box": [[120, 40], [560, 40], [560, 70], [120, 70]]},
        {"text": "QATAR IDENTITY CARD", "confidence": 0.98, "box": [[150, 90], [530, 90], [530, 115], [150, 115]]},
        {"text": "AHMED MOHAMMED AL-THANI", "confidence": 0.97, "box": [[180, 160], [520, 160], [520, 195], [180, 195]]},
        {"text": "234 2123 4567", "confidence": 0.96, "box": [[220, 240], [460, 240], [460, 270], [220, 270]]},
        {"text": "Date of Birth: 15/08/1990", "confidence": 0.95, "box": [[160, 310], [520, 310], [520, 340], [160, 340]]},
        {"text": "Expiry: 09/2030", "confidence": 0.94, "box": [[160, 370], [420, 370], [420, 400], [160, 400]]},
        {"text": "Nationality: QAT", "confidence": 0.93, "box": [[160, 430], [360, 430], [360, 460], [160, 460]]},
    ]