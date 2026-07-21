import os
import json
import re
from fastapi import FastAPI, Request
from fastapi.responses import HTMLResponse, JSONResponse
import psycopg2
import redis

app = FastAPI()

# Serve the wizard HTML at root
@app.get("/", response_class=HTMLResponse)
async def get_wizard():
    with open("wizard.html", "r") as f:
        return f.read()

# Health check endpoint
@app.get("/health")
async def health():
    return {"status": "ok"}

# Test PostgreSQL connection
@app.post("/api/test-postgres")
async def test_postgres(request: Request):
    try:
        data = await request.json()
        conn_str = data.get("connectionString", "")
        
        # Validate format
        if not re.match(r'^postgresql://[^:]+:[^@]+@[^:]+:\d+/[^/]+$', conn_str):
            return JSONResponse({"success": False, "message": "Invalid PostgreSQL connection string format"})
        
        # Test connection
        conn = psycopg2.connect(conn_str)
        conn.close()
        return {"success": True, "message": "PostgreSQL connection successful"}
    except Exception as e:
        return JSONResponse({"success": False, "message": f"PostgreSQL connection failed: {str(e)}"})

# Test Redis connection
@app.post("/api/test-redis")
async def test_redis(request: Request):
    try:
        data = await request.json()
        conn_str = data.get("connectionString", "")
        
        # Validate format
        if not re.match(r'^rediss?://([^@]*@)?[^:]+:\d+(/\d+)?$', conn_str):
            return JSONResponse({"success": False, "message": "Invalid Redis connection string format"})
        
        # Test connection
        r = redis.from_url(conn_str)
        r.ping()
        return {"success": True, "message": "Redis connection successful"}
    except Exception as e:
        return JSONResponse({"success": False, "message": f"Redis connection failed: {str(e)}"})

# Save configuration
@app.post("/api/save-config")
async def save_config(request: Request):
    try:
        data = await request.json()
        config_path = "/app/config/aegiscore_instance.json"
        
        with open(config_path, "w") as f:
            json.dump(data, f, indent=2)
        
        return {"success": True, "message": "Configuration saved successfully"}
    except Exception as e:
        return JSONResponse({"success": False, "message": f"Failed to save configuration: {str(e)}"})
