from fastapi import APIRouter
from fastapi import Depends, HTTPException, status
from fastapi.responses import HTMLResponse
from fastapi.security import OAuth2PasswordBearer
from pydantic import BaseModel

import db

db = db.DB("velconnect.db")

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
    values = db.query(
        "SELECT * FROM `APIKey` WHERE `key`=:key;", {'key': api_key})
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
    values = db.query("SELECT * FROM `Headset`;")
    return values


@router.get('/pair_headset/{pairing_code}')
def pair_headset(pairing_code: str):
    values = db.query("SELECT * FROM `Headset` WHERE `pairing_code`=:pairing_code;",
                   {'pairing_code': pairing_code})
    if len(values) == 1:
        return values[0]
    return {'error': 'Not found'}, 400


class UpdatePairingCode(BaseModel):
    hw_id: str
    pairing_code: int


@router.post('/update_pairing_code')
def update_pairing_code(data: UpdatePairingCode):
    """This also creates a headset if it doesn't exist"""

    print("Update pairing code")
    print(data)

    create_headset(data.hw_id)

    db.insert("""
    UPDATE `Headset` 
    SET `pairing_code`=:pairing_code, `last_used`=CURRENT_TIMESTAMP 
    WHERE `hw_id`=:hw_id;
    """, data.dict())

    return {'success': True}


def create_headset(hw_id: str):
    db.insert("""
    db.insert IGNORE INTO Headset(hw_id) VALUES (:hw_id);
    """, {'hw_id': hw_id})


@router.get('/get_state/{hw_id}')
def get_headset_details(hw_id: str):
    data = get_headset_details_db(hw_id)
    if data is None:
        return {'error': "Can't find headset with that id."}
    else:
        return data


def get_headset_details_db(hw_id):
    headsets = db.query("""
    SELECT * FROM `Headset` WHERE `hw_id`=:hw_id;
    """, {'hw_id': hw_id})
    if len(headsets) == 0:
        return None

    room = get_room_details_db(headsets[0]['current_room'])

    return {'user': headsets[0], 'room': room}


@router.post('/set_headset_details/{hw_id}')
def set_headset_details_generic(hw_id: str, data: dict):
    print("Data:")
    print(data)

    # create_headset(hw_id)

    allowed_keys = [
        'current_room',
        'pairing_code',
        'user_color',
        'user_name',
        'avatar_url',
        'user_details',
        'streamer_stream_id',
        'streamer_control_id',
    ]
    for key in data:
        if key in allowed_keys:
            if key == 'current_room':
                create_room(data['current_room'])
            db.insert(f"UPDATE `Headset` SET {key}=:value, modified_by=:sender_id WHERE `hw_id`=:hw_id;", {
                'value': data[key], 'hw_id': hw_id, 'sender_id': data['sender_id']})
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
            db.insert("UPDATE `Room` SET " + key +
                   "=:value, modified_by=:sender_id WHERE `room_id`=:room_id;",
                   {'value': data[key], 'room_id': room_id, 'sender_id': data['sender_id']})
    return {'success': True}


@router.get('/get_room_details/{room_id}')
def get_room_details(room_id: str):
    return get_room_details_db(room_id)


def get_room_details_db(room_id):
    values = db.query("""
    SELECT * FROM `Room` WHERE room_id=:room_id;
    """, {'room_id': room_id})
    if len(values) == 1:
        return values[0]
    else:
        return None


def create_room(room_id):
    db.insert("""
    db.insert IGNORE INTO `Room`(room_id) 
    VALUES(
        :room_id
    );
    """, {'room_id': room_id})
    return {'room_id': room_id}

