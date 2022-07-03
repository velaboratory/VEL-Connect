import secrets
import json
import string

import fastapi
from fastapi.responses import HTMLResponse
from pydantic import BaseModel

import db

db = db.DB("velconnect_v2.db")

# APIRouter creates path operations for user module
router = fastapi.APIRouter(
    prefix="/api/v2",
    tags=["API V2"],
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
          <img src = "http://velconnect.ugavel.com/static/favicons/android-chrome-256x256.png" style="width:10em; margin: auto;" />
        </div>
      </rapi-doc>
    </body>
  </html>
"""


@router.get('/get_all_devices')
def get_all_devices():
    """Returns a list of all devices and details associated with them."""
    values = db.query("SELECT * FROM `Device`;")
    return values


@router.get('/get_device_by_pairing_code/{pairing_code}')
def get_device_by_pairing_code(pairing_code: str):
    values = db.query("SELECT * FROM `Device` WHERE `pairing_code`=:pairing_code;",
                      {'pairing_code': pairing_code})
    if len(values) == 1:
        return values[0]
    return {'error': 'Not found'}, 400


def create_device(hw_id: str):
    db.insert("""
    INSERT IGNORE INTO `Device`(hw_id) VALUES (:hw_id);
    """, {'hw_id': hw_id})


@router.get('/device/get_data/{hw_id}')
def get_state(hw_id: str):
    """Gets the device state"""

    devices = db.query("""
    SELECT * FROM `Device` WHERE `hw_id`=:hw_id;
    """, {'hw_id': hw_id})
    if len(devices) == 0:
        return {'error': "Can't find device with that id."}

    room_data = get_data(f"{devices[0]['current_app']}_{devices[0]['current_room']}")

    return {'device': devices[0], 'room': room_data}


@router.post('/device/set_data/{hw_id}')
def set_state(hw_id: str, data: dict, request: fastapi.Request):
    """Sets the device state"""

    create_device(hw_id)

    # add the client's IP address if no sender specified
    if 'modified_by' not in data:
        data['modified_by'] = str(request.client) + "_" + str(request.headers)

    allowed_keys: list[str] = [
        'os_info',
        'friendly_name',
        'modified_by',
        'current_app',
        'current_room',
        'pairing_code',
    ]

    for key in data:
        if key in allowed_keys:
            db.insert(f"""
                UPDATE `Device` 
                SET {key}=:value,
                    last_modified=CURRENT_TIMESTAMP 
                WHERE `hw_id`=:hw_id;
                """,
                      {
                          'value': data[key],
                          'hw_id': hw_id,
                          'sender_id': data['sender_id']
                      })
        if key == "data":
            # get the old json values and merge the data
            old_data_query = db.query("""
                SELECT data
                FROM `Device`
                WHERE hw_id=:hw_id
                """, {"hw_id": hw_id})

            if len(old_data_query) == 1:
                old_data: dict = json.loads(old_data_query[0]["data"])
                data = {**old_data, **data}

            # add the data to the db
            db.insert("""
                UPDATE `Device`
                SET data=:data,
                    last_modified=CURRENT_TIMESTAMP
                WHERE hw_id=:hw_id;
                """, {"hw_id": hw_id, "data": json.dumps(data)})
    return {'success': True}


def generate_id(length: int = 4) -> str:
    return ''.join(
        secrets.choice(string.ascii_uppercase + string.ascii_lowercase + string.digits) for i in range(length))


@router.post('/set_data')
def store_data_with_random_key(request: fastapi.Request, data: dict, category: str = None) -> dict:
    """Creates a little storage bucket for arbitrary data with a random key"""
    return store_data(request, data, None, category)


@router.post('/set_data/{key}')
def store_data(request: fastapi.Request, data: dict, key: str = None, modified_by: str = None, category: str = None) -> dict:
    """Creates a little storage bucket for arbitrary data"""

    # add the client's IP address if no sender specified
    if modified_by is None:
        modified_by = str(request.client) + "_" + str(request.headers)

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
    SELECT data 
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
            return json.loads(data[0])
        return {'error': 'Not found'}
    except:
        return {'error': 'Unknown. Maybe no data at this key.'}
