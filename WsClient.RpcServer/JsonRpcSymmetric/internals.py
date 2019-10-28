import asyncio

import logging

from aiohttp.abc import Application
from aiohttp.client import ClientSession
from typing import Dict, Union, List, Coroutine

from jsonrpcclient.exceptions import ReceivedErrorResponseError
from jsonrpcclient.requests import Request as MyRequest, Notification
from jsonrpcclient.response import Response, JSONRPCResponse, ErrorResponse
from jsonrpcclient.parse import parse

from jsonrpcserver.response import Response as MyResponse


class Singleton(type):
    _instances = {}

    def __call__(cls, *args, **kwargs):
        if cls not in cls._instances:
            cls._instances[cls] = super(Singleton, cls).__call__(*args, **kwargs)
        return cls._instances[cls]


class Context:
    """Context for websocket connection"""
    session: ClientSession
    app: Application
    log: logging.Logger

    def __init__(self, app: Application, url: str, session: ClientSession):
        self.app = app
        self.log = app['logger']
        self.ws = None
        self.session: ClientSession = session
        self.url = url

        # for storage of additional information (Subscription,CancellationTokens,etc.)
        self.data: Dict[str, any] = {}

        # send messages waiting for response
        self.out_requests: Dict[int, asyncio.Event] = {}
        self.out_tasks: Dict[int, Coroutine]
        self.out_responses: Dict[int, Union[JSONRPCResponse, List[JSONRPCResponse]]] = {}

        # running tasks/futures/coroutine for request/notification
        self.__futures_counter = 0
        self.__futures: Dict[int, asyncio.Future] = {}

    async def register_future(self, future: asyncio.Future) -> int:
        tid = self.__futures_counter
        self.__futures.update({tid: future})
        self.__futures_counter += 1
        return tid

    async def cancel_future(self, future_id) -> bool:
        try:
            f = self.__futures.pop(future_id)
            f.cancel()
            return True
        except:
            return False

    def get_all_futures(self)->List[asyncio.Future]:
        try:
            return list(self.__futures.values())
        except:
            return []

    async def send_with_response(self, request: MyRequest) -> any:
        """To send my request and wait for somebody's response"""
        return await RequestWithResponse(self).send_async(request)

    async def send_my_response(self, response: MyResponse) -> None:
        """To send response
        - usually based on somebody's request.
        """
        return await self.ws.send_str(str(response))

    async def send_my_notification(self, notification: Notification) -> None:
        return await self.ws.send_str(str(notification))

    async def handle_result(self, response: Response) -> None:
        response.data = parse(response.text, batch=True, validate_against_schema=True)
        # If received a single error response, raise
        if isinstance(response.data, ErrorResponse):
            raise ReceivedErrorResponseError(response.data)
        else:
            rid: int = response.data.id
            self.out_responses.update({rid: response.data})
            # fire event
            event = self.out_requests.pop(rid)
            event.set()


class RequestWithResponse:

    def __init__(self, context: Context):
        self.event: asyncio.Event = asyncio.Event()
        self.context = context

    async def send_async(self, request: MyRequest):
        self.context.out_requests.update({request["id"]: self.event})
        await self.context.ws.send_str(str(request))
        await self.event.wait()
        res = self.context.out_responses.pop(request["id"])
        return res.result