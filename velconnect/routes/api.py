from velconnect.auth import require_api_key
from velconnect.db import connectToDB
from velconnect.logger import logger
from flask import Blueprint, request, jsonify
import time
import simplejson as json
from random import random

bp = Blueprint('api', __name__)


@bp.route('/get_all_headsets', methods=['GET'])
@require_api_key(0)
def get_all_headsets():
    conn, curr = connectToDB()
    query = """
    SELECT * FROM `Headset`;
    """
    curr.execute(query, None)
    values = [dict(row) for row in curr.fetchall()]
    curr.close()
    return jsonify(values)


@bp.route('/pair_headset/<pairing_code>', methods=['POST'])
@require_api_key(0)
def pair_headset(pairing_code):
    conn, curr = connectToDB()
    query = """
    SELECT * FROM `Headset` WHERE `pairing_code`=%(pairing_code)s;
    """
    curr.execute(query, {'pairing_code': pairing_code})
    values = [dict(row) for row in curr.fetchall()]
    curr.close()
    if len(values) > 0:
        return jsonify({'hw_id': values['hw_id']})
    return 'Not found', 400


# This also creates a headset if it doesn't exist
@bp.route('/update_pairing_code', methods=['POST'])
@require_api_key(0)
def update_paring_code():

    data = request.json
    if 'hw_id' not in data:
        return 'Must supply hw_id', 400
    if 'pairing_code' not in data:
        return 'Must supply pairing_code', 400
    if 'pairing_code' not in data:
        return 'Must supply pairing_code', 400

    conn, curr = connectToDB()

    query = """
    INSERT INTO `Headset`(
        `hw_id`,
        `pairing_code`,
        `last_used`
    ) VALUES (
        %(hw_id)s,
        %(pairing_code)s,
        CURRENT_TIMESTAMP
    ) 
    ON DUPLICATE UPDATE 
        pairing_code=%(pairing_code)s,
        last_used=CURRENT_TIMESTAMP;
    """
    curr.execute(query, data)
    conn.commit()
    curr.close()

    return 'Success'


@bp.route('/get_room_details/<room_id>', methods=['GET'])
@require_api_key(10)
def get_room_details(room_id):
    return jsonify(get_room_details_db(room_id))


def get_room_details_db(room_id):
    conn, curr = connectToDB()
    query = """
    SELECT * FROM `Room` WHERE room_id=%(room_id)s;
    """
    curr.execute(query, {'room_id': room_id})
    values = [dict(row) for row in curr.fetchall()]
    curr.close()
    return jsonify(values)


@bp.route('/create_room', methods=['GET'])
@require_api_key(10)
def create_room():
    return jsonify(create_room_db())


def create_room_db():
    room_id = random.randint(0, 9999)
    conn, curr = connectToDB()
    query = """
    INSERT INTO `Room` VALUES(
        %(room_id)s
    );
    """
    curr.execute(query, {'room_id': room_id})
    conn.commit()
    curr.close()
    return jsonify({'room_id': room_id})
