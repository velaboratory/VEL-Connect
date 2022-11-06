## VELConnect API Setup

### Option 1: Pull from Docker Hub:

```
docker run -p 80:80 velaboratory/velconnect
```
and visit http://localhost in your browser.

or 

```sh
docker run -p 8000:80 --name web velaboratory/velconnect
```
 to access from http://localhost:8000 in your browser and name the container "web".

### Option 2: Build Docker Image:

Make sure you're in the `velconnect/` folder.
```
docker build --tag velconnect .
docker rm web
docker run -p 80:80 --name web velconnect
```

or run `./rebuild.sh`

### Option 3: Run Python locally (WSL or native Linux)
1. `cd velconnect`
2. Create pip env: `python3 -m venv env`
3. Activate the env `. env/bin/activate`
4. Install packages `pip install -r requirements.txt`
5. Add `config_mysql.py`
    - Get from some old server
    - Or copy and fill out the values from `config_mysql_template.py`
6. Run `./run_server.sh`
    - Or set up systemctl service:
        ```ini
        [Unit]
        Description=VELConnect API
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
    - `sudo systemctl enable velconnect`
    - `sudo systemctl start velconnect`