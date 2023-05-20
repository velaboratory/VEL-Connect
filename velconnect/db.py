import sqlite3
import os
import traceback


class DB:
    def __init__(self, db_name):
        self.db_name = db_name

    def create_or_connect(self):
        create = False
        if not os.path.exists(self.db_name):
            create = True

        conn = sqlite3.connect(self.db_name)
        conn.row_factory = sqlite3.Row
        curr = conn.cursor()
        if create:
            # create the db
            with open('CreateDB.sql', 'r') as f:
                curr.executescript(f.read())

        conn.set_trace_callback(print)
        return conn, curr

    def query(self, query_string: str, data: dict = None) -> list:
        conn = None
        try:
            conn, curr = self.create_or_connect()
            if data is not None:
                curr.execute(query_string, data)
            else:
                curr.execute(query_string)
            values = curr.fetchall()
            conn.close()
            return values
        except:
            print(traceback.print_exc())
            if conn is not None:
                conn.close()
            raise

    def insert(self, query_string: str, data: dict = None) -> bool:
        try:
            conn, curr = self.create_or_connect()
            curr.execute(query_string, data)
            conn.commit()
            conn.close()
            return True
        except:
            print(traceback.print_exc())
            conn.close()
            raise
