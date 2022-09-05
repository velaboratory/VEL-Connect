import fastapi
from fastapi import APIRouter
from fastapi.responses import FileResponse
from fastapi.templating import Jinja2Templates

# APIRouter creates path operations for user module
router = APIRouter(
    prefix="",
    tags=["Website"],
    include_in_schema=False
)

templates = Jinja2Templates(directory="templates")


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
def failure(request: fastapi.Request, code: int = 0):
    return templates.TemplateResponse("failure.html", {"request": request, "code": code})


@router.get('/join/{app_id}/{link}')
def join(request: fastapi.Request, app_id: str, link: str):
    return templates.TemplateResponse("join.html", {"request": request, "app_id": app_id, "link": link})
