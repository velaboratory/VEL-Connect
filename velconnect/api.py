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
from pyppeteer import launch
from enum import Enum


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
    values = query("SELECT * FROM `Headset`;")
    return values


@router.get('/pair_headset/{pairing_code}')
def pair_headset(pairing_code: str):
    values = query("SELECT * FROM `Headset` WHERE `pairing_code`=:pairing_code;",
                   {'pairing_code': pairing_code})
    if len(values) == 1:
        print(values[0]['hw_id'])
        return {'hw_id': values[0]['hw_id']}
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

    insert("""
    UPDATE `Headset` 
    SET `pairing_code`=:pairing_code, `last_used`=CURRENT_TIMESTAMP 
    WHERE `hw_id`=:hw_id;
    """, data.dict())

    return {'success': True}


def create_headset(hw_id: str):
    insert("""
    INSERT OR IGNORE INTO Headset(hw_id) VALUES (:hw_id);
    """, {'hw_id': hw_id})


@router.get('/get_state/{hw_id}')
def get_headset_details(hw_id: str):
    data = get_headset_details_db(hw_id)
    if data is None:
        return {'error': "Can't find headset with that id."}
    else:
        return data


def get_headset_details_db(hw_id):
    headsets = query("""
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
            insert(f"UPDATE `Headset` SET {key}=:value, modified_by=:sender_id WHERE `hw_id`=:hw_id;", {
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
            insert("UPDATE `Room` SET " + key +
                   "=:value, modified_by=:sender_id WHERE `room_id`=:room_id;", {'value': data[key], 'room_id': room_id, 'sender_id': data['sender_id']})
    return {'success': True}


@router.get('/get_room_details/{room_id}')
def get_room_details(room_id: str):
    return get_room_details_db(room_id)


def get_room_details_db(room_id):
    values = query("""
    SELECT * FROM `Room` WHERE room_id=:room_id;
    """, {'room_id': room_id})
    if len(values) == 1:
        return values[0]
    else:
        return None


def create_room(room_id):
    insert("""
    INSERT OR IGNORE INTO `Room`(room_id) 
    VALUES(
        :room_id
    );
    """, {'room_id': room_id})
    return {'room_id': room_id}


@router.post('/update_user_count', tags=["User Count"])
def update_user_count(data: dict):
    insert("""
    REPLACE INTO `UserCount`
    VALUES(
        CURRENT_TIMESTAMP,
        :hw_id,
        :room_id,
        :total_users,
        :room_users,
        :version,
        :platform
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


class QuestRift(str, Enum):
    quest = "quest"
    rift = "rift"


@router.get('/get_store_details/{quest_rift}/{app_id}', tags=["Oculus API"])
async def get_version_nums(quest_rift: QuestRift, app_id: int):
    browser = await launch(headless=True, options={'args': ['--no-sandbox']})
    page = await browser.newPage()
    await page.goto(f'https://www.oculus.com/experiences/{quest_rift}/{app_id}')

    ret = {}

    # title
    title = await page.querySelector(".app-description__title")
    ret["title"] = await page.evaluate("e => e.textContent", title)

    # description
    desc = await page.querySelector(".clamped-description__content")
    ret["description"] = await page.evaluate("e => e.textContent", desc)

    # versions
    await page.evaluate("document.querySelector('.app-details-version-info-row__version').nextElementSibling.firstChild.click();")
    elements = await page.querySelectorAll('.sky-dropdown__link.link.link--clickable')

    versions = []
    for e in elements:
        v = await page.evaluate('(element) => element.textContent', e)
        versions.append({
            'channel': v.split(':')[0],
            'version': v.split(':')[1]
        })

    ret["versions"] = versions

    await browser.close()

    return ret
