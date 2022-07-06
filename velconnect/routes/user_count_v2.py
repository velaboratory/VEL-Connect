import fastapi

import db

db = db.DB("velconnect_v2.db")

# APIRouter creates path operations for user module
router = fastapi.APIRouter(
    prefix="/api/v2",
    tags=["User Count V2"],
    responses={404: {"description": "Not found"}},
)

post_user_count_example = {
    "default": {
        "summary": "Example insert for user count",
        "value": {
            "hw_id": "1234",
            "app_id": "example",
            "room_id": "0",
            "total_users": 1,
            "room_users": 1,
            "version": "0.1",
            "platform": "Windows"
        }
    }
}


@router.post('/update_user_count')
def update_user_count(data: dict = fastapi.Body(..., examples=post_user_count_example)) -> dict:
    if 'app_id' not in data:
        data['app_id'] = ""

    db.insert("""
    REPLACE INTO `UserCount` (
        timestamp,
        hw_id,
        app_id,
        room_id,
        total_users,
        room_users,
        version,
        platform
    )
    VALUES(
        CURRENT_TIMESTAMP,
        :hw_id,
        :app_id,
        :room_id,
        :total_users,
        :room_users,
        :version,
        :platform
    );
    """, data)
    return {'success': True}


@router.get('/get_user_count')
def get_user_count(app_id: str = None, hours: float = 24) -> list:
    values = db.query("""
        SELECT timestamp, total_users 
        FROM `UserCount`
        WHERE app_id = :app_id AND 
        timestamp > datetime('now', '-""" + str(hours) + """ Hour');
    """, {"app_id": app_id})
    return values
