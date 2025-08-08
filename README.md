# rob

Utility script that synchronizes a server configuration list with a backend.

- After subscription confirmation, `config_sync.py` requests the server list
  and stores it locally in `servers.json`.
- Configuration is refreshed on application start and then periodically on a
  schedule (daily by default).
- Optionally, push notifications can be received via WebSocket to trigger
  immediate synchronization.

Run `python -m py_compile config_sync.py` to verify syntax.
