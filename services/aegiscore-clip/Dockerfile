# AegisCore CLIP + OCR sidecar (aegiscore-clip).
# Internal service only — never exposed publicly. Listens on :8100.
FROM python:3.14-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    HF_HOME=/models \
    TRANSFORMERS_OFFLINE=1 \
    CLIP_MODEL=openai/clip-vit-base-patch32 \
    CLIP_THRESHOLD=0.82

# tesseract for scanned-PDF OCR, poppler for pdf2image rasterisation.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr libtesseract-dev poppler-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app

# Warm the CLIP weights into the image so runtime needs no network / registry pull.
RUN python -c "import os; from transformers import CLIPModel, CLIPProcessor; m=os.environ['CLIP_MODEL']; CLIPModel.from_pretrained(m); CLIPProcessor.from_pretrained(m)"

RUN useradd --uid 10001 --no-create-home --shell /usr/sbin/nologin clip \
    && chown -R clip:clip /models
USER clip

EXPOSE 8100

HEALTHCHECK --interval=15s --timeout=5s --retries=10 --start-period=90s \
    CMD python -c "import urllib.request,sys; sys.exit(0 if urllib.request.urlopen('http://127.0.0.1:8100/health').status==200 else 1)"

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8100", "--workers", "1"]
