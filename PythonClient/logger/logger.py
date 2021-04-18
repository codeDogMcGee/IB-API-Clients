import logging
from os import path, remove
from logger.log_formatter import LogFormatter


class Logger:
    def __init__(self, log_file_path: str):
        self.log_file_path = log_file_path
        self.delete_log_if_exists()
        self.create_main_logger()

    def create_main_logger(self) -> None:
        # create the logger
        logger = logging.getLogger('IBApiLogger')
        logger.setLevel(logging.DEBUG)

        # create the file handler
        file_handler = logging.FileHandler(self.log_file_path)
        file_handler.setLevel(logging.DEBUG)

        # create the stream handler
        stream_handler = logging.StreamHandler()
        stream_handler.setLevel(logging.INFO)

        # set the formatter
        formatter = LogFormatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s', datefmt='%Y%m%d %H:%M:%S.%f')
        file_handler.setFormatter(formatter)
        stream_handler.setFormatter(formatter)

        # add handlers to logger
        logger.addHandler(file_handler)
        logger.addHandler(stream_handler)

    def delete_log_if_exists(self) -> None:
        if path.exists(self.log_file_path):
            remove(self.log_file_path)



