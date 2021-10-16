from velconnect.db import connectToDB
from flask import Blueprint, request, jsonify, render_template
from velconnect.logger import logger
import time
import simplejson as json

bp = Blueprint('website', __name__, template_folder='templates')


@bp.route('/', methods=['GET'])
def index():
    return render_template('index.jinja')


@bp.route('/pair', methods=['GET'])
def pair():
    return render_template('pair.jinja')


@bp.route('/success', methods=['GET'])
def success():
    return render_template('success.jinja')


@bp.route('/failure', methods=['GET'])
def failure():
    return render_template('failure.jinja')
