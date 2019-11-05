from abc import abstractmethod
from dataclasses import dataclass

from jsonrpcsymmetric.jsonrpcsymmetric.typing import ConnectionContext


@dataclass
class Hello:
    """Model for hello"""
    name: str


@dataclass
class Subscription:
    """Model for subscription"""
    i: str
    s: bool


class Api(type):
    """Describes api on the other side"""

    @staticmethod
    @abstractmethod
    async def Add(params) -> int:
        raise NotImplemented

    @staticmethod
    @abstractmethod
    async def Ping() -> str:
        raise NotImplemented

    @staticmethod
    @abstractmethod
    def Tick(params: int) -> str:
        raise NotImplemented

    @staticmethod
    @abstractmethod
    async def Hello(params: Hello) -> None:
        raise NotImplemented

    @staticmethod
    @abstractmethod
    async def SubscribeTick() -> Subscription:
        raise NotImplemented

    @staticmethod
    @abstractmethod
    async def UnsubscribeTick(params: Subscription) -> Subscription:
        raise NotImplemented
