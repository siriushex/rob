"""Пример MCP сервера для запуска через setup_and_run_mcp_server.sh."""

from __future__ import annotations

import os
from datetime import datetime

from mcp.server.fastmcp import Context, FastMCP

HOST = os.environ.get("MCP_HOST", "127.0.0.1")
PORT = int(os.environ.get("MCP_PORT", "8000"))

server = FastMCP(
    name="rob-mcp-server",
    instructions=(
        "Демонстрационный сервер Model Context Protocol. "
        "Доступные инструменты: echo для повтора сообщения и current_time для получения времени."
    ),
    host=HOST,
    port=PORT,
)


@server.tool(description="Повторить переданное сообщение.")
def echo(message: str, ctx: Context) -> str:
    """Вернуть переданное сообщение и записать его в журнал."""
    ctx.info("Echo message: %s", message)
    return message


@server.tool(name="current_time", description="Получить текущее время в ISO формате.")
def current_time(ctx: Context) -> str:
    """Вернуть текущее локальное время в формате ISO 8601."""
    now = datetime.now().astimezone()
    ctx.debug("Generated timestamp %s", now.isoformat())
    return now.isoformat()


if __name__ == "__main__":
    server.run()
