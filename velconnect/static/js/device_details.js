{
// check cookie
    let hw_id = getCookie('hw_id');

    if (hw_id !== "" && hw_id !== undefined && hw_id !== "undefined") {

        httpGetAsync('/api/v2/device/get_data/' + hw_id, (resp) => {
            console.log(resp);
            let respData = JSON.parse(resp);

            if ("error" in respData) {
                window.location.href = "/pair";
            }

            writeClass('hw_id', respData['device']['hw_id']);
            writeClass('pairing_code', respData['device']['pairing_code']);
            writeValue('current_app', respData['device']['current_app']);
            writeValue('current_room', respData['device']['current_room']);
            writeClass('date_created', respData['device']['date_created'] + "<br>" + timeSinceString(respData['device']['date_created']) + " ago");
            writeClass('last_modified', respData['device']['last_modified'] + "<br>" + timeSinceString(respData['device']['last_modified']) + " ago");
            writeValue('user_name', respData['device']['friendly_name']);
            writeValue('avatar_url', respData['device']['data']?.['avatar_url']);
            writeValue('tv_url', respData['room']?.['data']?.['tv_url']);
            writeValue('carpet_color', respData['room']?.['data']?.['carpet_color']);
            if (carpet_color) carpet_color.parentElement.style.color = "" + respData['room']?.['data']?.['carpet_color'];

            loading.style.display = "none";
            headset_details.style.display = "block";
        }, (status) => {
            loading.style.display = "none";
            failure.style.display = "block";
        });
    }
}