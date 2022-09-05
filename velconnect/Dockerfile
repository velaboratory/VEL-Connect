# syntax=docker/dockerfile:1
FROM python:3.10
WORKDIR /usr/src/velconnect
COPY ./requirements.txt /usr/src/requirements.txt
RUN pip install --no-cache-dir --upgrade -r /usr/src/requirements.txt
COPY . /usr/src/velconnect
EXPOSE 80
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]

