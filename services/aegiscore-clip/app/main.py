"""AegisCore CLIP + OCR sidecar.

Two internal endpoints, both consumed only by the AegisCore API over the compose network:

* ``POST /validate-photo`` — CLIP zero-shot check that an uploaded passport photo is a plain,
  unfiltered image of a real human face (rejects Snapchat / beauty / cartoon / AI-generated).
* ``POST /ocr`` — text extraction from an admission syllabus PDF. Digital text is read directly;
  scanned pages fall back to Tesseract.
"""
from __future__ import annotations

import io
import os
import time

import torch
from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image
from transformers import CLIPModel, CLIPProcessor

MODEL_NAME = os.environ.get("CLIP_MODEL", "openai/clip-vit-base-patch32")
THRESHOLD = float(os.environ.get("CLIP_THRESHOLD", "0.82"))

POSITIVE_PROMPT = "a plain unfiltered passport photo of a real human face"
NEGATIVE_PROMPT = (
    "a photo with Snapchat filter, beauty filter, cartoon effect, "
    "face distortion, or AI generated face"
)

app = FastAPI(title="aegiscore-clip", docs_url=None, redoc_url=None, openapi_url=None)

_model: CLIPModel | None = None
_processor: CLIPProcessor | None = None


def _load() -> tuple[CLIPModel, CLIPProcessor]:
    global _model, _processor
    if _model is None or _processor is None:
        _model = CLIPModel.from_pretrained(MODEL_NAME)
        _model.eval()
        _processor = CLIPProcessor.from_pretrained(MODEL_NAME)
    return _model, _processor


@app.on_event("startup")
def _startup() -> None:  # pragma: no cover - warmup
    _load()


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "model": MODEL_NAME, "threshold": THRESHOLD}


@app.post("/validate-photo")
async def validate_photo(image: UploadFile = File(...)) -> dict:
    raw = await image.read()
    if not raw:
        raise HTTPException(status_code=400, detail="Empty image upload.")

    try:
        pil = Image.open(io.BytesIO(raw)).convert("RGB")
    except Exception as exc:  # noqa: BLE001
        raise HTTPException(status_code=400, detail="Unreadable image.") from exc

    model, processor = _load()
    inputs = processor(
        text=[POSITIVE_PROMPT, NEGATIVE_PROMPT],
        images=pil,
        return_tensors="pt",
        padding=True,
    )
    with torch.no_grad():
        logits = model(**inputs).logits_per_image
        probs = logits.softmax(dim=1).squeeze(0)

    positive = float(probs[0].item())
    passed = positive >= THRESHOLD
    reason = "" if passed else "Snapchat/beauty/cartoon filter or AI-generated face detected"
    return {"passed": passed, "confidence": round(positive, 4), "reason": reason}


@app.post("/ocr")
async def ocr(file: UploadFile = File(...)) -> dict:
    raw = await file.read()
    if not raw:
        raise HTTPException(status_code=400, detail="Empty PDF upload.")

    started = time.time()
    text, method = _extract_pdf_text(raw)
    return {
        "text": text,
        "pages": text.count("\f") + 1 if text else 0,
        "method": method,
        "elapsed_ms": round((time.time() - started) * 1000, 1),
    }


def _extract_pdf_text(raw: bytes) -> tuple[str, str]:
    digital = _digital_pdf_text(raw)
    if len(digital.strip()) >= 200:
        return digital, "digital"

    scanned = _tesseract_pdf_text(raw)
    if len(scanned.strip()) > len(digital.strip()):
        return scanned, "tesseract"
    return digital, "digital"


def _digital_pdf_text(raw: bytes) -> str:
    try:
        from pypdf import PdfReader

        reader = PdfReader(io.BytesIO(raw))
        return "\f".join((page.extract_text() or "") for page in reader.pages)
    except Exception:  # noqa: BLE001
        return ""


def _tesseract_pdf_text(raw: bytes) -> str:
    try:
        import pytesseract
        from pdf2image import convert_from_bytes

        pages = convert_from_bytes(raw, dpi=200)
        return "\f".join(pytesseract.image_to_string(page) for page in pages)
    except Exception:  # noqa: BLE001
        return ""
