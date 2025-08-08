import json
import sched
import time
from threading import Thread, Event

import requests

SERVERS_FILE = "servers.json"
BACKEND_URL = "https://example.com/api/servers"

def fetch_servers():
    """Fetch list of servers from backend and store locally."""
    response = requests.get(BACKEND_URL, timeout=10)
    response.raise_for_status()
    servers = response.json()
    with open(SERVERS_FILE, "w", encoding="utf-8") as f:
        json.dump(servers, f)


def confirm_subscription():
    """Call after subscription confirmation to sync server list."""
    fetch_servers()


def update_configuration(interval_hours: int = 24):
    """Update configuration at app start and schedule periodic updates."""
    scheduler = sched.scheduler(time.time, time.sleep)

    def _periodic():
        fetch_servers()
        scheduler.enter(interval_hours * 3600, 1, _periodic)

    _periodic()
    Thread(target=scheduler.run, daemon=True).start()
    return scheduler


def start_push_listener(url: str, stop_event: Event | None = None):
    """Listen for push notifications to trigger immediate sync (optional)."""
    try:
        import websocket  # type: ignore
    except ImportError:  # pragma: no cover - optional dependency
        print("websocket-client not installed; push notifications disabled")
        return None

    def on_message(ws, message):  # pragma: no cover - network dependent
        fetch_servers()

    ws = websocket.WebSocketApp(url, on_message=on_message)
    thread = Thread(target=ws.run_forever, daemon=True)
    thread.start()
    if stop_event:
        stop_event.wait()
        ws.close()
    return ws


if __name__ == "__main__":
    # Example usage: sync immediately and schedule periodic refreshes.
    confirm_subscription()
    update_configuration()
