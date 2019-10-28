import aiohttp

from aiohttp.client_exceptions import ClientError
from jsonrpcclient.response import Response as OutResponse
from jsonrpcserver import async_dispatch as dispatch

from aiohttp.http_websocket import WSCloseCode
from asyncio.futures import CancelledError

# Import handlers and load them
from .handlers import *


class WebSocketConnection(object):
    """Provides one connection at instance"""
    __metaclass__ = Singleton

    context: Context

    def __init__(self, app: Application, url: str, session: ClientSession):
        self.context = Context(app, url, session)
        self.log: logging.Logger = self.context.log

    async def handle(self):
        """Main handler"""
        pending = []
        try:
            self.context.ws = await self.context.session.ws_connect(url=self.context.url, timeout=5.0, autoping=True)
            incoming_handler = asyncio.ensure_future(self.incoming_handler())
            request_period = asyncio.ensure_future(request_add_periodically(self.context))

            # register
            pending.append(request_period)
            pending.append(incoming_handler)
            # await handler
            await incoming_handler

            # cancel/await other tasks
            request_period.cancel()
            await asyncio.wait([request_period])
        except ClientError as e:
            self.log.info(e)
        except CancelledError as ce:
            self.log.debug(ce)
        except Exception as ee:
            self.log.warning(ee)
        finally:
            # clean everything
            try:
                for t in self.context.get_all_futures():
                    t.cancel()
                await asyncio.wait(self.context.get_all_futures())
            except Exception as e:
                # self.log.debug(e)
                pass
            # clean main
            for task in pending:
                task.cancel()
            # close websocket
            if self.context is not None and self.context.ws is not None:
                await self.context.ws.close(code=WSCloseCode.GOING_AWAY)
            return

    async def incoming_handler(self):
        """Consume data from WS"""
        async for msg in self.context.ws:
            if msg.type == aiohttp.WSMsgType.TEXT:
                req = msg.data
                rrd: Dict = json.loads(req)
                if "result" in rrd:
                    await self.context.handle_result(OutResponse(req))
                else:
                    response = await dispatch(req, context=self.context, debug=False)
                    if response.wanted:
                        await self.context.send_my_response(response)
                    else:
                        pass
            elif msg.type == aiohttp.WSMsgType.CLOSED:
                break
            elif msg.type == aiohttp.WSMsgType.ERROR:
                break
