from fastapi import APIRouter
from fastapi import Query
from typing import Optional
from fastapi.responses import HTMLResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from fastapi import FastAPI, Body, Depends, HTTPException, status
from fastapi.security import OAuth2PasswordBearer
from db import query, insert
from pydantic import BaseModel
from typing import Union


# APIRouter creates path operations for user module
router = APIRouter(
    prefix="/api",
    tags=["API"],
    responses={404: {"description": "Not found"}},
)

oauth2_scheme = OAuth2PasswordBearer(
    tokenUrl="token")  # use token authentication


def api_key_auth(api_key: str = Depends(oauth2_scheme)):
    return True
    values = query(
        "SELECT * FROM `APIKey` WHERE `key`=%(key)s;", {'key': api_key})
    if not (len(values) > 0 and values['auth_level'] < 0):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Forbidden"
        )


@router.get("/", response_class=HTMLResponse, include_in_schema=False)
async def read_root():
    return """
<!doctype html>
  <html>
    <head>
      <meta charset="utf-8"> <!-- Important: rapi-doc uses utf8 charecters -->
      <script type="module" src="https://unpkg.com/rapidoc/dist/rapidoc-min.js"></script>
      <title>API Reference</title>
    </head>
    <body>
      <rapi-doc
        render-style = "read" 
        primary-color = "#bc1f2d" 
        show-header = "false" 
        show-info = "true"
        spec-url = "/openapi.json" 
        default-schema-tab = 'example'
        > 
        <div slot="nav-logo" style="display: flex; align-items: center; justify-content: center;"> 
          <img src = "http://velconnect.ugavel.com/static/favicons/android-chrome-256x256.png" style="width:10em; margin: auto;" />
        </div>
      </rapi-doc>
    </body>
  </html>
"""


@router.get('/get_all_headsets')
def get_all_headsets():
    """Returns a list of all headsets and details associated with them."""
    values = query("SELECT * FROM `Headset`;")
    return values


@router.get('/pair_headset/{pairing_code}')
def pair_headset(pairing_code: str):
    values = query("SELECT * FROM `Headset` WHERE `pairing_code`=%(pairing_code)s;",
                   {'pairing_code': pairing_code})
    if len(values) == 1:
        print(values[0]['hw_id'])
        return {'hw_id': values[0]['hw_id']}
    return {'error': 'Not found'}, 400


class UpdatePairingCode(BaseModel):
    hw_id: str
    pairing_code: str


@router.post('/update_pairing_code')
def update_paring_code(data: UpdatePairingCode):
    """This also creates a headset if it doesn't exist"""

    if 'hw_id' not in data:
        return 'Must supply hw_id', 400
    if 'pairing_code' not in data:
        return 'Must supply pairing_code', 400

    insert("""
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
    """, data)

    return {'success': True}


@router.get('/get_state/{hw_id}')
def get_headset_details(hw_id: str):
    data = get_headset_details_db(hw_id)
    if data is None:
        return {'error': "Can't find headset with that id."}
    else:
        return data


def get_headset_details_db(hw_id):
    headsets = query("""
    SELECT * FROM `Headset` WHERE `hw_id`=%(hw_id)s;
    """, {'hw_id': hw_id})
    if len(headsets) == 0:
        return None

    room = get_room_details_db(headsets[0]['current_room'])

    return {'user': headsets[0], 'room': room}


@router.post('/set_headset_details/{hw_id}')
def set_headset_details_generic(hw_id: str, data: dict):
    print(data)

    allowed_keys = [
        'current_room',
        'pairing_code',
        'user_color',
        'user_name',
        'avatar_url',
        'user_details',
    ]
    for key in data:
        if key in allowed_keys:
            if key == 'current_room':
                create_room(data['current_room'])
            insert("UPDATE `Headset` SET " + key +
                   "=%(value)s, modified_by=%(sender_id)s WHERE `hw_id`=%(hw_id)s;", {'value': data[key], 'hw_id': hw_id, 'sender_id': data['sender_id']})
    return {'success': True}


@router.post('/set_room_details/{room_id}')
def set_room_details_generic(room_id: str, data: dict):
    print(data)
    allowed_keys = [
        'modified_by',
        'whitelist',
        'tv_url',
        'carpet_color',
        'room_details',
    ]

    for key in data:
        if key in allowed_keys:
            insert("UPDATE `Room` SET " + key +
                   "=%(value)s, modified_by=%(sender_id)s WHERE `room_id`=%(room_id)s;", {'value': data[key], 'room_id': room_id, 'sender_id': data['sender_id']})
    return {'success': True}


@router.get('/get_room_details/{room_id}')
def get_room_details(room_id: str):
    return get_room_details_db(room_id)


def get_room_details_db(room_id):
    values = query("""
    SELECT * FROM `Room` WHERE room_id=%(room_id)s;
    """, {'room_id': room_id})
    if len(values) == 1:
        return values[0]
    else:
        return None


def create_room(room_id):
    insert("""
    INSERT IGNORE INTO `Room`(room_id) 
    VALUES(
        %(room_id)s
    );
    """, {'room_id': room_id})
    return {'room_id': room_id}


@router.post('/update_user_count', tags=["User Count"])
def update_user_count(data: dict):
    insert("""
    REPLACE INTO `UserCount`
    VALUES(
        CURRENT_TIMESTAMP,
        %(hw_id)s,
        %(room_id)s,
        %(total_users)s,
        %(room_users)s,
        %(version)s,
        %(platform)s
    );
    """, data)
    return {'success': True}


@router.get('/get_user_count', tags=["User Count"])
def get_user_count(hours: float = 24):
    values = query("""
    SELECT timestamp, total_users 
    FROM `UserCount`
    WHERE TIMESTAMP > DATE_SUB(NOW(), INTERVAL """ + str(hours) + """ HOUR);
    """)
    return values
