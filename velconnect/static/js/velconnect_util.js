function setDeviceField(device) {
    let hw_id = getCookie('hw_id');
    fetch('/api/device/set_data/' + hw_id, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },

        body: JSON.stringify(device)
    })
        .then(_ => console.log('success'))
        .catch(_ => console.log('fail'));
}

function setDeviceData(data, successCallback, failureCallback) {
    let hw_id = getCookie('hw_id');
    fetch('/api/device/set_data/' + hw_id, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },

        body: JSON.stringify({"data": data})
    })
        .then(_ => {
            console.log('success');
            successCallback?.();
        })
        .catch(_ => {
            console.log('fail');
            failureCallback?.();
        });
}

function setRoomData(data) {
    fetch('/api/set_data/' + current_app.value + "_" + current_room.value, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    })
        .then(_ => console.log('success'))
        .catch(_ => console.log('fail'));
}