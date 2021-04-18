from ibapi.contract import Contract
from ibapi.execution import Execution
from ibapi.commission_report import CommissionReport
from ibapi.wrapper import EWrapper
from logging import getLogger

from models.executions_model import ExecutionModel

logger = getLogger('IBApiLogger.client.wrapper')


class Wrapper(EWrapper):
    def __init__(self, connection_id):
        EWrapper.__init__(self)
        self.valid_order_id = None
        self.executions = {}
        self.fetching_executions_in_progress = None
        self.connection_id = connection_id

        # want to be able to track the different requests
        self.request_ids = {'get_executions': 11111}

    # overridden ibapi EWrapper method
    def nextValidId(self, orderId: int) -> None:
        # invoked automatically upon successful api connection
        super().nextValidId(orderId)
        self.valid_order_id = int(orderId)
        logger.debug(f'Set IB Valid Id to: {self.valid_order_id}')

    # overridden ibapi EWrapper method
    def execDetails(self, reqId: int, contract: Contract, execution: Execution) -> None:
        # clientId's of 0 are manual trades from TWS
        super().execDetails(reqId, contract, execution)

        # create the execution and store it so we can add the commission report when available
        execution_model = ExecutionModel(execution, contract, None)
        self.executions[execution.execId] = execution_model

    # overridden ibapi EWrapper method
    def commissionReport(self, commissionReport: CommissionReport) -> None:
        super().commissionReport(commissionReport)

        # get the current execution model
        execution_model = self.executions[commissionReport.execId]

        # update the model with the commission report, was None
        execution_model.commission_report = commissionReport

        # replace entry in self.executions
        self.executions[commissionReport.execId] = execution_model

    # overridden ibapi EWrapper method
    def execDetailsEnd(self, reqId: int) -> None:
        self.fetching_executions_in_progress = False
        logger.debug(f'Execution Details Ended: {reqId}')
