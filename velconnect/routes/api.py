import os
import secrets
import json
import string
import aiofiles
import uuid

import fastapi
from fastapi.responses import HTMLResponse, FileResponse
from fastapi import FastAPI, File, UploadFile, Response, Request, status
from enum import Enum

import db

db = db.DB("velconnect.db")

# APIRouter creates path operations for user module
router = fastapi.APIRouter(
    prefix="/api",
    tags=["API"],
    responses={404: {"description": "Not found"}},
)


@router.get("/", response_class=HTMLResponse, include_in_schema=False)
async def read_root():
    return """
<!doctype html>
  <html>
    <head>
      <meta charset="utf-8">
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
          <img src = "/static/img/velconnect_logo_1.png" style="width:10em; margin: 2em auto;" />
        </div>
      </rapi-doc>
    </body>
  </html>
"""


def parse_data(device: dict):
    if 'data' in device and device['data'] is not None and len(device['data']) > 0:
        device['data'] = json.loads(device['data'])


@router.get('/get_all_users')
def get_all_users():
    """Returns a list of all devices and details associated with them."""
    values = db.query("SELECT * FROM `User`;")
    values = [dict(v) for v in values]
    for v in values:
        parse_data(v)
    return values


@router.get('/get_all_devices')
def get_all_devices():
    """Returns a list of all devices and details associated with them."""
    values = db.query("SELECT * FROM `Device`;")
    values = [dict(v) for v in values]
    for device in values:
        parse_data(device)
    return values


@router.get('/get_user_by_pairing_code/{pairing_code}')
def get_user_by_pairing_code(pairing_code: str):
    device = get_device_by_pairing_code_dict(pairing_code)
    if device is not None:
        return device
    return {'error': 'Not found'}, 400


@router.get('/get_device_by_pairing_code/{pairing_code}')
def get_device_by_pairing_code(pairing_code: str):
    device = get_device_by_pairing_code_dict(pairing_code)
    if device is not None:
        return device
    return {'error': 'Not found'}, 400


def get_device_by_pairing_code_dict(pairing_code: str) -> dict | None:
    values = db.query(
        "SELECT * FROM `Device` WHERE `pairing_code`=:pairing_code;", {'pairing_code': pairing_code})
    if len(values) == 1:
        device = dict(values[0])
        parse_data(device)
        return device
    return None


def get_user_for_device(hw_id: str) -> dict:
    values = db.query(
        """SELECT * FROM `UserDevice` WHERE `hw_id`=:hw_id;""", {'hw_id': hw_id})
    if len(values) == 1:
        user_id = dict(values[0])['user_id']
        user = get_user_dict(user_id=user_id)
    else:
        # create new user instead
        user = create_user(hw_id)
    parse_data(user)
    return user


# creates a user with a device autoattached
def create_user(hw_id: str) -> dict | None:
    user_id = str(uuid.uuid4())
    if not db.insert("""INSERT INTO `User`(id) VALUES (:user_id);""", {'user_id': user_id}):
        return None
    if not db.insert("""INSERT INTO `UserDevice`(user_id, hw_id) VALUES (:user_id, :hw_id); """,
                     {'user_id': user_id, 'hw_id': hw_id}):
        return None
    return get_user_for_device(hw_id)


def create_device(hw_id: str):
    db.insert("""
    INSERT OR IGNORE INTO `Device`(hw_id) VALUES (:hw_id);
    """, {'hw_id': hw_id})


@router.get('/device/get_data/{hw_id}')
def get_device_data(request: Request, response: Response, hw_id: str):
    """Gets the device state"""

    devices = db.query("""
    SELECT * FROM `Device` WHERE `hw_id`=:hw_id;
    """, {'hw_id': hw_id})
    if len(devices) == 0:
        response.status_code = status.HTTP_404_NOT_FOUND
        return {'error': "Can't find device with that id."}
    block = dict(devices[0])
    if 'data' in block and block['data'] is not None:
        block['data'] = json.loads(block['data'])

    user = get_user_for_device(hw_id)

    room_key: str = f"{devices[0]['current_app']}_{devices[0]['current_room']}"
    room_data = get_data(response, key=room_key, user_id=user['id'])

    if "error" in room_data:
        response.status_code = None  # this really isn't an error, so we reset the status code
        set_data(request, data={}, key=room_key,
                 modified_by=None, category="room")
        room_data = get_data(response, key=room_key, user_id=user['id'])

    return {'device': block, 'room': room_data, 'user': user}


@router.post('/device/set_data/{hw_id}')
def set_device_data(request: fastapi.Request, hw_id: str, data: dict, modified_by: str = None):
    """Sets the device state"""

    create_device(hw_id)

    # add the client's IP address if no sender specified
    if 'modified_by' in data:
        modified_by = data['modified_by']
    if modified_by is None:
        modified_by: str = str(request.client) + "_" + str(request.headers)

    allowed_keys: list[str] = [
        'os_info',
        'friendly_name',
        'current_app',
        'current_room',
        'pairing_code',
    ]

    for key in data:
        if key in allowed_keys:
            db.insert(f"""
                UPDATE `Device` 
                SET {key}=:value,
                    last_modified=CURRENT_TIMESTAMP, 
                    modified_by=:modified_by 
                WHERE `hw_id`=:hw_id;
                """,
                      {
                          'value': data[key],
                          'hw_id': hw_id,
                          'modified_by': modified_by
                      })
        if key == "data":
            new_data = data['data']
            # get the old json values and merge the data
            old_data_query = db.query("""
                SELECT data
                FROM `Device`
                WHERE hw_id=:hw_id
                """, {"hw_id": hw_id})

            if len(old_data_query) == 1:
                old_data: dict = {}
                if old_data_query[0]['data'] is not None:
                    old_data = json.loads(old_data_query[0]["data"])
                new_data = {**old_data, **new_data}

            # add the data to the db
            db.insert("""
                UPDATE `Device`
                SET data=:data,
                    last_modified=CURRENT_TIMESTAMP
                WHERE hw_id=:hw_id;
                """, {"hw_id": hw_id, "data": json.dumps(new_data)})
    return {'success': True}


def generate_id(length: int = 4) -> str:
    return ''.join(
        secrets.choice(string.ascii_uppercase + string.ascii_lowercase + string.digits) for i in range(length))


class Visibility(str, Enum):
    public = "public"
    private = "private"
    unlisted = "unlisted"


@router.post('/set_data')
def set_data_with_random_key(request: fastapi.Request, data: dict, owner: str, modified_by: str = None,
                             category: str = None, visibility: Visibility = Visibility.public) -> dict:
    """Creates a little storage bucket for arbitrary data with a random key"""
    return set_data(request, data, None, owner, modified_by, category, visibility)


@router.post('/set_data/{key}')
def set_data(request: fastapi.Request, data: dict, key: str = None, owner: str = None, modified_by: str = None,
             category: str = None, visibility: Visibility = Visibility.public) -> dict:
    """Creates a little storage bucket for arbitrary data"""

    # add the client's IP address if no sender specified
    if 'modified_by' in data:
        modified_by = data['modified_by']
    if modified_by is None:
        modified_by: str = str(request.client) + "_" + str(request.headers)

    # generates a key if none was supplied
    if key is None:
        key = generate_id()

        # regenerate if necessary
        while len(db.query("SELECT id FROM `DataBlock` WHERE id=:id;", {"id": key})) > 0:
            key = generate_id()

    # get the old json values and merge the data
    old_data_query = db.query("""
    SELECT data
    FROM `DataBlock`
    WHERE id=:id
    """, {"id": key})

    if len(old_data_query) == 1:
        old_data: dict = json.loads(old_data_query[0]["data"])
        data = {**old_data, **data}

        # add the data to the db
        db.insert("""
        UPDATE `DataBlock` SET
            category = :category,
            modified_by = :modified_by,
            data = :data,
            last_modified = CURRENT_TIMESTAMP
        WHERE id=:id AND owner_id = :owner_id;
        """, {"id": key, "category": category, "modified_by": modified_by, "owner_id": owner, "data": json.dumps(data)})
    else:
        # add the data to the db
        db.insert("""
        INSERT INTO `DataBlock` (id, owner_id, category, modified_by, data, last_modified)
        VALUES(:id, :owner_id, :category, :modified_by, :data, CURRENT_TIMESTAMP);
        """, {"id": key, "owner_id": owner, "category": category, "modified_by": modified_by, "data": json.dumps(data)})

    return {'key': key}


@router.get('/get_data/{key}')
def get_data(response: Response, key: str, user_id: str = None) -> dict:
    """Gets data from a storage bucket for arbitrary data"""

    data = db.query("""
    SELECT * 
    FROM `DataBlock`
    WHERE id=:id 
    """, {"id": key})

    db.insert("""
    UPDATE `DataBlock`
    SET last_accessed = CURRENT_TIMESTAMP
    WHERE id=:id; 
    """, {"id": key})

    try:
        if len(data) == 1:
            block = dict(data[0])
            if 'data' in block and block['data'] is not None:
                block['data'] = json.loads(block['data'])
            if not has_permission(block, user_id):
                response.status_code = status.HTTP_401_UNAUTHORIZED
                return {'error': 'Not authorized to see that data.'}
            replace_userid_with_name(block)
            return block
        response.status_code = status.HTTP_404_NOT_FOUND
        return {'error': 'Not found'}
    except Exception as e:
        print(e)
        response.status_code = status.HTTP_500_INTERNAL_SERVER_ERROR
        return {'error': 'Unknown. Maybe no data at this key.'}


def has_permission(data_block: dict, user_uuid: str) -> bool:
    # if the data is public by visibility
    if data_block['visibility'] == Visibility.public or data_block['visibility'] == Visibility.unlisted:
        return True
    # public domain data
    elif data_block['owner_id'] is None:
        return True
    # if we are the owner
    elif data_block['owner_id'] == user_uuid:
        return True
    else:
        return False


def replace_userid_with_name(data_block: dict):
    if data_block['owner_id'] is not None:
        user = get_user_dict(data_block['owner_id'])
        if user is not None:
            data_block['owner_name'] = user['username']
    del data_block['owner_id']


@router.get('/user/get_data/{user_id}')
def get_user(response: Response, user_id: str):
    user = get_user_dict(user_id)
    if user is None:
        response.status_code = status.HTTP_404_BAD_REQUEST
        return {"error": "User not found"}
    return user


def get_user_dict(user_id: str) -> dict | None:
    values = db.query(
        "SELECT * FROM `User` WHERE `id`=:user_id;", {'user_id': user_id})
    if len(values) == 1:
        user = dict(values[0])
        parse_data(user)
        return user
    return None


@router.post("/upload_file")
async def upload_file_with_random_key(request: fastapi.Request, file: UploadFile, modified_by: str = None):
    return await upload_file(request, file, None, modified_by)


@router.post("/upload_file/{key}")
async def upload_file(request: fastapi.Request, file: UploadFile, key: str | None, modified_by: str = None):
    if not os.path.exists('data'):
        os.makedirs('data')

    # generates a key if none was supplied
    if key is None:
        key = generate_id()

        # regenerate if necessary
        while len(db.query("SELECT id FROM `DataBlock` WHERE id=:id;", {"id": key})) > 0:
            key = generate_id()

    async with aiofiles.open('data/' + key, 'wb') as out_file:
        content = await file.read()  # async read
        await out_file.write(content)  # async write
    # add a datablock to link to the file
    set_data(request, data={'filename': file.filename},
             key=key, category='file', modified_by=modified_by)
    return {"filename": file.filename, 'key': key}


@router.get("/download_file/{key}")
async def download_file(response: Response, key: str):
    # get the relevant datablock
    data = get_data(response, key)
    print(data)
    if response.status_code == status.HTTP_404_NOT_FOUND:
        return 'Not found'
    if data['category'] != 'file':
        response.status_code = status.HTTP_400_BAD_REQUEST
        return 'Not a file'
    return FileResponse(path='data/' + key, filename=data['data']['filename'])


@router.get("/get_all_files")
async def get_all_files():
    data = db.query("""
        SELECT * 
        FROM `DataBlock`
        WHERE visibility='public' AND category='file';  
        """)
    data = [dict(f) for f in data]
    for f in data:
        parse_data(f)
    return data


@router.get("/get_all_images")
async def get_all_images():
    data = db.query("""
        SELECT * 
        FROM `DataBlock`
        WHERE visibility='public' AND category='file';  
        """)
    data = [dict(f) for f in data]
    for f in data:
        parse_data(f)
    images = []
    for f in data:
        if f['data']['filename'].endswith('.png') or f['data']['filename'].endswith('.jpg'):
            images.append({
                'key': f['id'],
                'filename': f['data']['filename']
            })
    return images
