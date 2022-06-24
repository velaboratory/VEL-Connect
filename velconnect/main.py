from imp import reload
import uvicorn
from fastapi.middleware.cors import CORSMiddleware
from fastapi import FastAPI
from api import router as api_router
from website import router as website_router
from fastapi.staticfiles import StaticFiles

app = FastAPI()

origins = [
    "http://velconnect.ugavel.com",
    "https://velconnect.ugavel.com",
    "http://localhost",
    "http://localhost:8080",
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
app.include_router(website_router)

if __name__ == '__main__':
    uvicorn.run("main:app", host='127.0.0.1', port=8005,
                log_level="info", reload=True)
    print("running")
