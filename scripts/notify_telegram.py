#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Helper script to send quick notifications to Telegram from Antigravity IDE.
Usage:
    python scripts/notify_telegram.py "Nội dung thông báo ở đây"
"""

import sys
import os
import json
import urllib.request
import urllib.parse
from pathlib import Path

# Ensure utf-8 output on Windows
if sys.platform == "win32":
    try:
        if sys.stdout and hasattr(sys.stdout, "reconfigure"):
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

try:
    from dotenv import load_dotenv
    PROJECT_ROOT = Path(__file__).resolve().parent.parent
    load_dotenv(PROJECT_ROOT / ".env")
except ImportError:
    PROJECT_ROOT = Path(__file__).resolve().parent.parent

BOT_TOKEN = os.getenv("TELEGRAM_BOT_TOKEN", "").strip()
CHAT_ID = os.getenv("ALLOWED_CHAT_ID", "").strip()

def send_notification(message: str) -> bool:
    if not BOT_TOKEN or not CHAT_ID:
        print("[ERROR] Missing TELEGRAM_BOT_TOKEN or ALLOWED_CHAT_ID in .env", file=sys.stderr)
        return False

    url = f"https://api.telegram.org/bot{BOT_TOKEN}/sendMessage"
    payload = {
        "chat_id": CHAT_ID,
        "text": message,
        "parse_mode": "Markdown",
        "disable_web_page_preview": True,
    }
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/json"}
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            return response.status == 200
    except urllib.error.HTTPError as e:
        # Fallback to plain text if markdown formatting fails
        payload.pop("parse_mode", None)
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=10) as response:
                return response.status == 200
        except Exception as ex:
            print(f"[ERROR] Failed to send telegram notification: {ex}", file=sys.stderr)
            return False
    except Exception as e:
        print(f"[ERROR] Failed to send telegram notification: {e}", file=sys.stderr)
        return False

if __name__ == "__main__":
    if len(sys.argv) > 1:
        msg = " ".join(sys.argv[1:])
    else:
        msg = "🔔 [Antigravity IDE] Task đã hoàn thành trên máy tính!"
    
    success = send_notification(msg)
    if success:
        print("[OK] Telegram notification sent successfully.")
    else:
        print("[FAILED] Could not send Telegram notification.")

