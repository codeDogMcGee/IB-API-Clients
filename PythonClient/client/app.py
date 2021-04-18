from logging import getLogger
from threading import Thread

from client._wrapper import Wrapper
from client._client import Client

logger = getLogger('IBApiLogger.client.app')


class App(Wrapper, Client):
    def __init__(self, ib_connection_port: int, ib_connection_id: int):
        ib_thread = IBThread(self)
        self.recv_thread = Thread(target=ib_thread.run)

        Wrapper.__init__(self, ib_connection_id)
        Client.__init__(self, ib_connection_port, ib_connection_id, self.recv_thread, self)


class IBThread:
    def __init__(self, ib_api_instance: App):
        self.api_instance = ib_api_instance

    def run(self):
        self.api_instance.run()
