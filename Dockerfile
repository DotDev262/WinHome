# Stage 1: Build dependencies environment securely
FROM python:3.11-slim AS builder

WORKDIR /app

# Install system compilation packages if needed
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements list and install user dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir --user -r requirements.txt

# Stage 2: Ultra-lightweight production execution layer
FROM python:3.11-slim AS runner

WORKDIR /app

# Create a non-privileged system user profile for container execution safety
RUN groupadd -g 999 appuser && \
    useradd -r -u 999 -g appuser appuser

# Copy over compiled dependencies from the builder environment stage
COPY --from=builder /root/.local /home/appuser/.local
COPY . .

# Adjust execution paths and set user profile authorization settings
ENV PATH=/home/appuser/.local/bin:$PATH
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8000
ENTRYPOINT ["python", "main.py"]
