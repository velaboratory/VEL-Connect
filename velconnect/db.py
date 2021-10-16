# from config import *
import pymysql
from pymysql import converters
from velconnect.config_mysql import *


def connectToDB():
    conv = converters.conversions.copy()
    conv[246] = float    # convert decimals to floats
    conn = pymysql.connect(
        host=MYSQL_DATABASE_HOST,
        user=MYSQL_DATABASE_USER,
        password=MYSQL_DATABASE_PASSWORD,
        db=MYSQL_DATABASE_DB,
        cursorclass=pymysql.cursors.DictCursor,
        conv=conv
    )
    curr = conn.cursor()

    return conn, curr
