import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles

from routes.api import router as api_router
from routes.user_count import router as user_count_router
from routes.oculus_api import router as oculus_api_router
from routes.website import router as website_router

app = FastAPI()

origins = [
    "http://velconnect.ugavel.com",
    "https://velconnect.ugavel.com",
    "http://localhost",
    "http://localhost:8080",
    "http://localhost:8000",
    "http://localhost:8005",
    "http://localhost:5173",
    "https://convrged.ugavel.com",
    "http://convrged.ugavel.com",
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.mount("/static", StaticFiles(directory="static"), name="static")

app.include_router(api_router)
app.include_router(user_count_router)
app.include_router(oculus_api_router)
app.include_router(website_router)

if __name__ == '__main__':
    uvicorn.run("main:app", host='127.0.0.1', port=8005,
                log_level="info", reload=True)
    print("running")
