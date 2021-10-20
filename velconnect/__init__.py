from flask import Flask, jsonify, make_response, request
from velconnect.auth import limiter
from velconnect.logger import logger
from time import strftime
import traceback


def create_app():
    app = Flask(
        __name__, 
        instance_relative_config=False, 
    )
    app.config.from_pyfile('config.py')

    limiter.init_app(app)

    from .routes import api
    app.register_blueprint(api.bp, url_prefix='/api')

    # from .routes import website
    # app.register_blueprint(website.bp)

    # Error handlers
    app.register_error_handler(404, resource_not_found)
    app.register_error_handler(401, noapikey_handler)
    app.register_error_handler(429, ratelimit_handler)
    app.register_error_handler(Exception, exceptions)

    app.after_request(after_request)

    return app


# @app.after_request
def after_request(response):
    """ Logging after every request. """
    # This avoids the duplication of registry in the log,
    # since that 500 is already logged via @app.errorhandler.
    if response.status_code != 500:
        ts = strftime('[%Y-%b-%d %H:%M]')
        logger.error('%s %s %s %s %s %s',
                     ts,
                     request.remote_addr,
                     request.method,
                     request.scheme,
                     request.full_path,
                     response.status)
    return response


# @app.errorhandler(Exception)
def exceptions(e):
    """ Logging after every Exception. """
    ts = strftime('[%Y-%b-%d %H:%M]')
    tb = traceback.format_exc()
    logger.error('%s %s %s %s %s 5xx INTERNAL SERVER ERROR\n%s',
                 ts,
                 request.remote_addr,
                 request.method,
                 request.scheme,
                 request.full_path,
                 tb)

    return "SERVER ERROR", 500


# @app.errorhandler(429)
def ratelimit_handler(e):
    return make_response(
        jsonify(error="ratelimit exceeded %s" % e.description), 429
    )


def resource_not_found(e):
    return jsonify(error=str(e)), 404


# @app.errorhandler(401)
def noapikey_handler(e):
    return make_response(
        jsonify(error="not authorized %s" % e.description), 401
    )
