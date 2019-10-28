from asyncio.futures import CancelledError

from jsonrpcclient.requests import Request as OutRequest

from jsonrpcserver import methods
from jsonrpcserver.response import InvalidParamsResponse, ExceptionResponse

import json

from .internals import *

"""
Methods has to be outside of class/instance, to be found by JSON-RPC.
All task should be canceled on disconnection of websocket.
"""


@methods.add
async def Ping(context: Context, *params) -> str:
    """
        :param context:
        :param params: use this also for empty list of params
            - https://github.com/microsoft/vs-streamjsonrpc sends empty list for no params.
        :return: result - payload
        """
    return "pong"


class Tick:
    @staticmethod
    @methods.add
    async def SubscribeTick(context: Context) -> str:
        """
        :param context: Context of WS
        :param params - useful for filtering results
        """
        try:
            cid = await context.register_future((asyncio.ensure_future(Tick._tick(context))))
            data = {
                "S": 'true',
                "CId": cid,
            }
            return str(data)
        except:
            raise ExceptionResponse

    @staticmethod
    @methods.add
    async def UnsubscribeTick(context: Context, *params) -> str:
        """
            :param context:
            :param params: {CId:int}
            :return: end of subscription
            """

        args = params[0]  # serialized object from Python->C#->Python
        if args is None:
            raise InvalidParamsResponse("Missing parameters")
        args_dict = json.loads(args)
        if "CId" not in args_dict:
            raise InvalidParamsResponse("Missing CancellationToken")
        try:
            await context.cancel_future(args_dict["CId"])

            res = {
                "S": 'false',
                "CId": args_dict["CId"]
            }
            return str(res)
        except:
            raise ExceptionResponse

    @staticmethod
    async def _tick(context: Context):
        counter = 0
        try:
            while True:
                await context.send_my_notification(Notification("Tick", counter))
                counter += 1
                await asyncio.sleep(1.0)
        except CancelledError as ce:
            context.log.debug("Tick end " + str(ce))
        except Exception as e:
            context.log.debug("Tick error: " + str(e))


async def request_add_periodically(context: Context):
    """Send data to WS"""
    # first message after connection
    await context.ws.send_str(str(hello()))
    res = 0
    while True:
        try:
            res = await context.send_with_response(OutRequest("Add", res, 2))
            context.log.debug("add result: "+ str(res))
            await asyncio.sleep(5.0)
        except Exception as e:
            context.log.debug(e)
            break
    return


def hello() -> Notification:
    return Notification("Hello", "my_name")
