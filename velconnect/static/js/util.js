
function httpGetAsync(theUrl, callback, failCallback) {
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4) {
            if (xmlHttp.status == 200) {
                callback(xmlHttp.responseText);
            } else {
                failCallback(xmlHttp.status);
            }
        }
    }
    xmlHttp.open("GET", theUrl, true); // true for asynchronous 
    xmlHttp.send(null);
}


function httpPostAsync(theUrl, data, callback, failCallback) {
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4) {
            if (xmlHttp.status == 200) {
                callback(xmlHttp.responseText);
            } else {
                failCallback(xmlHttp.status);
            }
        }
    }
    xmlHttp.open("POST", theUrl, true); // true for asynchronous 
    http.setRequestHeader('Content-type', 'application/json');
    xmlHttp.send(data);
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function writeClass(className, data) {
    if (data == undefined || data == null || data.toString() == 'undefined') {
        data = "";
    }

    let elements = document.getElementsByClassName(className);
    Array.from(elements).forEach(e => {
        e.innerHTML = data;
    });
}

function writeId(idName, data) {
    if (data == undefined || data == null || data.toString() == 'undefined') {
        data = "";
    }

    document.getElementById(idName).innerHTML = data;
}


function writeSrc(className, data) {
    if (data == undefined || data == null || data.toString() == 'undefined') {
        data = "";
    }

    let elements = document.getElementsByClassName(className);
    Array.from(elements).forEach(e => {
        e.src = data;
    });
}