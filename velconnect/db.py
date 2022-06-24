import sqlite3
import os
import traceback


def create_or_connect():
    db_name = 'velconnect.db'
    create = False
    if not os.path.exists(db_name, ):
        create = True

    conn = sqlite3.connect(db_name)
    conn.row_factory = sqlite3.Row
    curr = conn.cursor()
    if create:
        # create the db
        with open('CreateDB.sql', 'r') as f:
            curr.executescript(f.read())

    conn.set_trace_callback(print)
    return conn, curr


def query(query: str, data: dict = None) -> list:
    try:
        conn, curr = create_or_connect()
        if data is not None:
            curr.execute(query, data)
        else:
            curr.execute(query)
        values = curr.fetchall()
        conn.close()
        return values
    except:
        print(traceback.print_exc())
        conn.close()
        raise


def insert(query: str, data: dict = None) -> bool:
    try:
        conn, curr = create_or_connect()
        curr.execute(query, data)
        conn.commit()
        conn.close()
        return True
    except:
        print(traceback.print_exc())
        conn.close()
        raise
