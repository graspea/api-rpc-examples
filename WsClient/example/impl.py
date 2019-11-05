from abc import ABC
from asyncio.futures import CancelledError
from dataclasses import asdict

from .api import Api, Subscription, Hello
from jsonrpcsymmetric.jsonrpcsymmetric.typing import *
from jsonrpcsymmetric.jsonrpcsymmetric.methods import add_method
from jsonrpcsymmetric.jsonrpcsymmetric.errors import *


class ApiMethods(Api, ABC):
    """Implementation of """
    @staticmethod
    @add_method
    async def Hello(context: ConnectionContext, *params: [Dict]) -> None:
        try:
            obj: Hello = Hello(**params[0])
            context.log.debug(str(obj))
        except AttributeError as e:
            context.log.warning(str(__name__) + " " + "e")

    @staticmethod
    @add_method
    async def Add(context: ConnectionContext, *params) -> int:
        print(str(params))
        return int(params[0])+int(params[1])

    @staticmethod
    @add_method
    async def Ping(context: ConnectionContext, *params) -> str:
        return "pong"

    @staticmethod
    @add_method
    async def SubscribeTick(context: ConnectionContext) -> Subscription:
        cid = await context.register_future((asyncio.ensure_future(tick(context))))
        data = asdict(Subscription(s=True, i=cid))
        return data  # dataclasses are new in 3.7, therefore there is no support from dumps

    @staticmethod
    @add_method
    async def UnsubscribeTick(context: ConnectionContext, params: Dict) -> Subscription:
        """
            :param params:
            :param context:
            :param params: Subscription
            :return: end of subscription
            """
        try:
            obj: Subscription = Subscription(**params)
            await context.cancel_future(obj.i)
            obj.s = False
            return asdict(obj)
        except:
            raise InvalidParamsResponse("Wrong parametrs for " + str(params))


async def tick(context: ConnectionContext):
    """Example of registered task that periodically sends notification"""
    counter = 0
    try:
        while True:
            if context.ws is not None and not context.ws.closed:
                await context.send_my_notification(MyNotification(Api.Tick.__name__, counter))
                counter += 1
                await asyncio.sleep(1.0)
            else:
                break
    except CancelledError as ce:
        context.log.debug("Ping end " + str(ce))
    except Exception as e:
        context.log.debug("Ping error: " + str(e))
