from logging import Formatter
from datetime import datetime


class LogFormatter(Formatter):
    converter = datetime.fromtimestamp

    # overridden logging.Formatter method
    def formatTime(self, record, datefmt=None):
        ct = self.converter(record.created)
        if datefmt:
            s = ct.strftime(datefmt)
        else:
            t = ct.strftime('%Y-%m-%d %H:%M:%S')
            s = f'{t},{record.msecs:.3f}'
        return s
