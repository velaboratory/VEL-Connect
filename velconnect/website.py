from fastapi import APIRouter
from fastapi.responses import FileResponse


# APIRouter creates path operations for user module
router = APIRouter(
    prefix="",
    tags=["Website"],
)


@router.get('/')
def index():
    return FileResponse("templates/index.html")


@router.get('/pair')
def pair():
    return FileResponse("templates/pair.html")


@router.get('/success')
def success():
    return FileResponse("templates/success.html")


@router.get('/failure')
def failure():
    return FileResponse("templates/failure.html")
