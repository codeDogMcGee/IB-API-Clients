import json
from time import sleep
from threading import Thread
from logging import getLogger
from ibapi.client import EClient
from ibapi.execution import ExecutionFilter

from client._wrapper import Wrapper

logger = getLogger('IBApiLogger.client.client')


class Client(EClient):
    def __init__(self, port: int, connection_id: int, recv_thread: Thread, wrapper: Wrapper):
        EClient.__init__(self, wrapper)
        self.connection_id = connection_id
        self.port = port
        self.fetching_executions_in_progress = wrapper.fetching_executions_in_progress
        self.recv_thread = recv_thread
        self.valid_order_id = wrapper.valid_order_id

    def connect_to_ib(self) -> None:
        host = '127.0.0.1'
        self.connect(host, self.port, self.connection_id)

        self.recv_thread.start()
        logger.info(f'Connecting to IB {host}:{self.port}, Client Id:{self.connection_id}')

        # self.valid_order_id gets set upon succesful connection
        while self.valid_order_id is None:  # self.valid_order_id is defined in Wrapper
            sleep(.2)  # give IB a moment to connect

    def disconnect_ib(self) -> None:
        self.disconnect()
        while self.recv_thread.is_alive():
            pass
        logger.info(f'Disconnected from IB Client: {self.connection_id}')

    def get_executions(self, executions_filter: ExecutionFilter = ExecutionFilter()) -> str:
        self.fetching_executions_in_progress = True
        self.reqExecutions(self.request_ids['get_executions'], executions_filter)
        logger.info('Requesting Executions')

        # wait for the data for finish downloading
        while self.fetching_executions_in_progress:
            pass

        logger.info('Executions Fetched')
        #########################################################################################
        # model it returns similar to the csharp client,
        # where it iterates the properies of each execution in self.executions and
        # turns all three objects (executions, contract, commission_report) into one dict,
        # the below is temporary to get things working
        #########################################################################################

        output = []
        for exec_id, execution in self.executions.items():
            output.append({
                'symbol': execution.contract.symbol,
                'side': execution.execution.side,
                'shares': execution.execution.shares,
                'price': execution.execution.price
            })

        logger.debug('Sending executions json to front end.')
        return json.dumps(output)
