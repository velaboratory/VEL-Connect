import fastapi

import db

db = db.DB("velconnect.db")

# APIRouter creates path operations for user module
router = fastapi.APIRouter(
    prefix="/api",
    tags=["User Count"],
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
def update_user_count(data: dict):
    db.insert("""
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


@router.get('/get_user_count')
def get_user_count(hours: float = 24):
    values = db.query("""
    SELECT timestamp, total_users 
    FROM `UserCount`
    WHERE TIMESTAMP > DATE_SUB(NOW(), INTERVAL """ + str(hours) + """ HOUR);
    """)
    return values