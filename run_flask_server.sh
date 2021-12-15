#!/bin/bash

export FLASK_APP="velconnect"
export FLASK_ENV=development
source env/bin/activate
flask run
