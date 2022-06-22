# from config import *
import pymysql
from pymysql import converters
from config_mysql import *


def connectToDB():
    conv = converters.conversions.copy()
    conv[246] = float    # convert decimals to floats
    conn = pymysql.connect(
        host=MYSQL_DATABASE_HOST,
        user=MYSQL_DATABASE_USER,
        password=MYSQL_DATABASE_PASSWORD,
        db=MYSQL_DATABASE_DB,
        cursorclass=pymysql.cursors.DictCursor,
        conv=conv,
        ssl={"fake_flag_to_enable_tls": True},
    )
    curr = conn.cursor()

    return conn, curr


def query(query: str, data: dict = None) -> list:
    try:
        conn, curr = connectToDB()
        curr.execute(query, data)
        values = [dict(row) for row in curr.fetchall()]
        curr.close()
        return values
    except Exception:
        print(curr._last_executed)
        curr.close()
        raise


def insert(query: str, data: dict = None) -> bool:
    try:
        conn, curr = connectToDB()
        curr.execute(query, data)
        conn.commit()
        curr.close()
        return True
    except Exception:
        print(curr._last_executed)
        curr.close()
        raise
