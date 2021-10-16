from functools import wraps
import inspect
from flask import abort, request, make_response, jsonify
from velconnect.db import connectToDB
from flask_limiter import Limiter
from flask_limiter.util import get_remote_address


limiter = Limiter(key_func=get_remote_address)


def require_api_key(level):
    def decorator(view_function):
        def wrapper(*args, **kwargs):
            return view_function(*args, **kwargs)
            key = request.headers.get('x-api-key')

            conn, curr = connectToDB()
            query = """
            SELECT * FROM `APIKey` WHERE `key`=%(key)s;
            """
            curr.execute(query, {'key': key})
            values = [dict(row) for row in curr.fetchall()]
            if len(values) > 0 and values['auth_level'] < level:
                return view_function(*args, **kwargs)
            else:
                abort(401)
        wrapper.__name__ = view_function.__name__
        return wrapper
    return decorator


def required_args(f):
    @wraps(f)
    def decorated_function(*args, **kwargs):
        """ Decorator that makes sure the view arguments are in the request args, otherwise 400 error """
        sig = inspect.signature(f)
        data = request.args

        for arg in sig.parameters.values():
            # Check if the argument is passed from the url
            if arg.name in kwargs:
                continue
            # check if the argument is in the json data
            if data and data.get(arg.name) is not None:
                kwargs[arg.name] = data.get(arg.name)
            # else check if it has been given a default
            elif arg.default is not arg.empty:
                kwargs[arg.name] = arg.default

        missing_args = [arg for arg in sig.parameters.keys()
                        if arg not in kwargs.keys()]
        if missing_args:
            return 'Did not receive args for: {}'.format(', '.join(missing_args)), 400

        return f(*args, **kwargs)
    return decorated_function
