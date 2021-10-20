from flask.helpers import send_from_directory
from velconnect.auth import require_api_key
from velconnect.db import connectToDB
from velconnect.logger import logger
from flask import Blueprint, request, jsonify, render_template, url_for
import time
import simplejson as json
from random import random

bp = Blueprint('api', __name__)


@bp.route('/', methods=['GET'])
@require_api_key(0)
def api_home():
    return render_template('api_spec.html')


@bp.route('/api_spec.json', methods=['GET'])
@require_api_key(0)
def api_spec():
    return send_from_directory('static', 'api_spec.json')


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


@bp.route('/pair_headset/<pairing_code>', methods=['GET'])
@require_api_key(0)
def pair_headset(pairing_code):
    conn, curr = connectToDB()
    query = """
    SELECT * FROM `Headset` WHERE `pairing_code`=%(pairing_code)s;
    """
    curr.execute(query, {'pairing_code': pairing_code})
    values = [dict(row) for row in curr.fetchall()]
    curr.close()
    if len(values) == 1:
        print(values[0]['hw_id'])
        return jsonify({'hw_id': values[0]['hw_id']})
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
    ON DUPLICATE KEY UPDATE 
        pairing_code=%(pairing_code)s,
        last_used=CURRENT_TIMESTAMP;
    """
    curr.execute(query, data)
    conn.commit()
    curr.close()

    return 'Success'


@bp.route('/get_headset_details/<hw_id>', methods=['GET'])
@require_api_key(10)
def get_headset_details(hw_id):
    data = get_headset_details_db(hw_id)
    if data is None:
        return jsonify({'error': "Can't find headset with that id."}), 400
    else:
        return jsonify(data)


def get_headset_details_db(hw_id):
    conn, curr = connectToDB()
    query = """
    SELECT * FROM `Headset` WHERE `hw_id`=%(hw_id)s;
    """
    curr.execute(query, {'hw_id': hw_id})
    headsets = [dict(row) for row in curr.fetchall()]
    curr.close()
    if len(headsets) == 0:
        return None

    room = get_room_details_db(headsets[0]['current_room'])

    return {'user': headsets[0], 'room': room}


@bp.route('/set_headset_details/<hw_id>', methods=['POST'])
@require_api_key(10)
def set_headset_details(hw_id):
    return set_headset_details_db(hw_id, request.json)


def set_headset_details_db(hw_id, data):
    logger.error(data)
    conn, curr = connectToDB()
    query = """
    UPDATE `Headset`
    SET `current_room` = %(current_room)s,
    `user_color` = %(user_color)s,
    `user_name` = %(user_name)s
    WHERE `hw_id` = %(hw_id)s;
    """
    data['hw_id'] = hw_id
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
    if len(values) == 1:
        return values[0]
    else:
        return None


@bp.route('/set_room_details/<room_id>', methods=['POST'])
@require_api_key(10)
def set_room_details(room_id):
    return jsonify(set_room_details_db(room_id, request.json))


def set_room_details_db(room_id, data):
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
    return {'room_id': room_id}


@bp.route('/set_room_details/<room_id>/tv_url', methods=['POST'])
@require_api_key(10)
def set_room_details_tv_url(room_id):
    conn, curr = connectToDB()
    query = """
    UPDATE `Room`
    SET `tv_url` = %(tv_url)s
    WHERE `room_id` = %(room_id)s;
    """
    data = request.json
    data['room_id'] = room_id
    curr.execute(query, data)
    conn.commit()
    curr.close()
    return {'room_id': room_id}


@bp.route('/set_room_details/<room_id>/carpet_color', methods=['POST'])
@require_api_key(10)
def set_room_details_carpet_color(room_id):
    conn, curr = connectToDB()
    query = """
    UPDATE `Room`
    SET `tv_url` = %(tv_url)s
    WHERE `carpet_color` = %(carpet_color)s;
    """
    data = request.json
    data['room_id'] = room_id
    curr.execute(query, data)
    conn.commit()
    curr.close()
    return {'room_id': room_id}
