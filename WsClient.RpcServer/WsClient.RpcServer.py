import asyncio
import logging

import aiohttp

from aiohttp.abc import Application

from aiohttp import web
from aiohttp.client import ClientSession
from asyncio.futures import CancelledError

from JsonRpcSymmetric.rpcServer import WebSocketConnection


async def connect_to_ws_server(app: Application, session: ClientSession):
    """Keeps restoring WebSocketConnection"""
    l:logging.Logger = app['logger']
    try:
        while True:
            await WebSocketConnection(app, 'http://localhost:5000/ws', session).handle()
            print("wut")
    except CancelledError:
        l.info("Cancelled")
    except Exception as e:
        l.warning(e)


async def on_startup(app: Application):
    session: ClientSession = aiohttp.ClientSession()
    app['websocket_client'] = asyncio.ensure_future(connect_to_ws_server(app, session))

    # logging
    c_handler = logging.StreamHandler()
    c_handler.setFormatter(logging.Formatter('%(levelname)s: %(message)s'))

    l:logging.Logger = logging.Logger("rpc")
    l.setLevel(logging.DEBUG)
    l.addHandler(c_handler)
    app['logger'] = l


async def on_shutdown(app: Application):
    try:
        app['websocket_client'].cancel()
    except Exception:
        pass


if __name__ == "__main__":
    app: Application = web.Application()
    app.on_startup.append(on_startup)
    app.on_shutdown.append(on_shutdown)
    web.run_app(app)
