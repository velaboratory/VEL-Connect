## VELConnect API Setup

1. `cd velconnect`
2. Create pip env: `python3 -m venv env`
3. Activate the env `. env/bin/activate`
4. Install packages `pip install -r requirements.txt`
5. Run `./run_server.sh`
    - Or set up systemctl service:
        ```ini
        [Unit]
        Description=VelNet Logging
        Requires=network.target
        After=network.target

        [Service]
        User=ubuntu
        Group=ubuntu
        Environment="PATH=/home/ubuntu/VEL-Connect/velconnect/env/bin"
        WorkingDirectory=/home/ubuntu/VEL-Connect/velconnect
        ExecStart=/home/ubuntu/VEL-Connect/velconnect/env/bin/uvicorn --port 8005 main:app
        [Install]
        WantedBy=multi-user.target
        ```
    - Enter the above in `/etc/systemd/system/velconnect.service`
    - `sudo systemctl enable velconnect.service`
    - `sudo systemctl start velconnect.service`