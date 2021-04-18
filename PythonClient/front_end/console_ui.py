import logging
import json

from logger.logger import Logger
from ib_api_config import Config
from client.app import App

# initialize logger
Logger(log_file_path='ibapi.log')
logger = logging.getLogger('IBApiLogger.UI')

ib_config = Config()

ib_api = App(ib_connection_port=ib_config.IB_PORT, ib_connection_id=ib_config.IB_CONNECTION_ID)

ib_api.connect_to_ib()

executions_json = ib_api.get_executions()

executions_object = json.loads(executions_json)

for e in executions_object:
    print(e)

ib_api.disconnect_ib()

