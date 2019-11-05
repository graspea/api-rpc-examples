import asyncio
import logging
import aiohttp
from aiohttp import web

from functools import partial

from aiohttp.abc import Application
from aiohttp.client import ClientSession
from asyncio.futures import CancelledError

from jsonrpcsymmetric.jsonrpcsymmetric.wsrpc import WebSocketConnection
from jsonrpcsymmetric.jsonrpcsymmetric.methods import get_methods
from jsonrpcsymmetric.jsonrpcsymmetric.typing import Methods, ConnectionConfig, ConnectionContext, MyNotification


def get_api() -> Methods:
    """Import methods and return registration"""
    from example.impl import ApiMethods  # actual import of methods in to the context
    return get_methods()  # just returns dict with imported methods from context


async def connect_to_ws_server(app: Application,
                               session: ClientSession,
                               connection_name: str,
                               url: str):
    """Keeps restoring WebSocketConnection"""
    l: logging.Logger = app['logger']
    try:
        functions = [partial(example_task, a="I work!")]

        while True:
            # run one websocket connection, until it completes, then re-run
            await WebSocketConnection(app,
                                      ConnectionConfig(url=url,
                                                       session=session,
                                                       connection_name=connection_name,
                                                       methods=get_api()),
                                      functions
                                      ).handle()
    except CancelledError:
        l.info("Cancelled")
    except Exception as e:
        l.warning(e)


async def example_task(context:ConnectionContext, a: str):
    """
    Example of invoked function from outside of module, that is using connection context
    Has to be named parameters

    :argument context is required to be part of args -> named arg is used in module
    """
    try:
        while True:
            if context.ws is not None and not context.ws.closed:  # always check for ws closed
                await context.send_my_notification(MyNotification("Ping"))
                context.log.debug(a)
                await asyncio.sleep(10.0)
            else:
                break;
    except CancelledError as e:
        context.log.debug(e)


async def on_startup(app: Application):
    # logging
    c_handler = logging.StreamHandler()
    c_handler.setFormatter(logging.Formatter('%(levelname)s: %(message)s'))

    l: logging.Logger = logging.Logger("rpc")
    l.setLevel(logging.DEBUG)
    l.addHandler(c_handler)
    app['logger'] = l

    # connection
    session: ClientSession = aiohttp.ClientSession()
    connection_name = "ws-client"
    app[connection_name] = asyncio.ensure_future(connect_to_ws_server(app,
                                                                      session,
                                                                      connection_name,
                                                                      'http://localhost:5000/ws'))


async def on_shutdown(app: Application):
    app['websocket_client'].cancel()
    aiohttp.ClientSession().close()


if __name__ == "__main__":
    app: Application = web.Application()
    app.on_startup.append(on_startup)
    app.on_shutdown.append(on_shutdown)
    web.run_app(app)
