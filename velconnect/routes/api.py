import secrets
import json
import string
import aiofiles

import fastapi
from fastapi.responses import HTMLResponse
from fastapi import FastAPI, File, UploadFile

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


def parse_device(device: dict):
    if 'data' in device and device['data'] is not None and len(device['data']) > 0:
        device['data'] = json.loads(device['data'])


@router.get('/get_all_devices')
def get_all_devices():
    """Returns a list of all devices and details associated with them."""
    values = db.query("SELECT * FROM `Device`;")
    values = [dict(v) for v in values]
    for device in values:
        parse_device(device)
    return values


@router.get('/get_device_by_pairing_code/{pairing_code}')
def get_device_by_pairing_code(pairing_code: str):
    values = db.query("SELECT * FROM `Device` WHERE `pairing_code`=:pairing_code;",
                      {'pairing_code': pairing_code})
    if len(values) == 1:
        device = dict(values[0])
        parse_device(device)
        return device
    return {'error': 'Not found'}, 400


def create_device(hw_id: str):
    db.insert("""
    INSERT OR IGNORE INTO `Device`(hw_id) VALUES (:hw_id);
    """, {'hw_id': hw_id})


@router.get('/device/get_data/{hw_id}')
def get_state(request: fastapi.Request, hw_id: str):
    """Gets the device state"""

    devices = db.query("""
    SELECT * FROM `Device` WHERE `hw_id`=:hw_id;
    """, {'hw_id': hw_id})
    if len(devices) == 0:
        return {'error': "Can't find device with that id."}
    block = dict(devices[0])
    if 'data' in block and block['data'] is not None:
        block['data'] = json.loads(block['data'])

    room_key: str = f"{devices[0]['current_app']}_{devices[0]['current_room']}"
    room_data = get_data(room_key)

    if "error" in room_data:
        set_data(request, data={}, key=room_key, modified_by=None, category="room")
        room_data = get_data(room_key)

    return {'device': block, 'room': room_data}


@router.post('/device/set_data/{hw_id}')
def set_state(request: fastapi.Request, hw_id: str, data: dict, modified_by: str = None):
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


@router.post('/set_data')
def set_data_with_random_key(request: fastapi.Request, data: dict, modified_by: str = None,
                             category: str = None) -> dict:
    """Creates a little storage bucket for arbitrary data with a random key"""
    return set_data(request, data, None, modified_by, category)


@router.post('/set_data/{key}')
def set_data(request: fastapi.Request, data: dict, key: str = None, modified_by: str = None,
             category: str = None) -> dict:
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
    REPLACE INTO `DataBlock` (id, category, modified_by, data, last_modified)
    VALUES(:id, :category, :modified_by, :data, CURRENT_TIMESTAMP);
    """, {"id": key, "category": category, "modified_by": modified_by, "data": json.dumps(data)})

    return {'key': key}


@router.get('/get_data/{key}')
def get_data(key: str) -> dict:
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
            return block
        return {'error': 'Not found'}
    except Exception as e:
        print(e)
        return {'error': 'Unknown. Maybe no data at this key.'}


@router.post("/upload_file/{key}")
async def upload_file(request: fastapi.Request, file: UploadFile, key: str,modified_by: str = None):
    async with aiofiles.open('data/' + key, 'wb') as out_file:
        content = await file.read()  # async read
        await out_file.write(content)  # async write
    # add a datablock to link to the file
    set_data(request, {'filename': file.filename}, key, 'file')
    return {"filename": file.filename}
