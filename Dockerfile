FROM python:3.12-slim

# Accept Google Maps API key as build arg (optional)
ARG GOOGLE_MAPS_API_KEY=""
ENV GOOGLE_MAPS_API_KEY=${GOOGLE_MAPS_API_KEY}

# Set working directory
WORKDIR /app

# Install system dependencies and create non-root user in a single layer
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser \
    && mkdir -p /app/config \
    && chown -R appuser /app

# Copy application files
COPY --chown=appuser server.py .
COPY --chown=appuser wizard.html .

# Install Python dependencies
RUN pip install --no-cache-dir fastapi uvicorn psycopg2-binary redis

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false \
    ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

# Print startup message and run server
CMD echo "AegisCore Setup Wizard running at http://localhost:8080" && \
    uvicorn server:app --host 0.0.0.0 --port ${PORT:-8080}
