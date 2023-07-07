## VEL-Connect Server Setup

### Option 1: Download the latest binary from releases

Then run with:
 - Windows: `velconnect.exe serve`
 - Linux: `./velconnect serve`

Run `./velconnect help` for help

### Option 1: Build and run using Docker Compose

```sh
cd velconnect
docker compose up -d --build
```

and visit http://localhost:8090/\_/ in your browser.

This will set up autorestart of the docker image. To pull updates, just run `docker compose up -d --build` again.

### Option 2: Pull from Docker Hub:

```sh
docker run -p 80:8090 velaboratory/velconnect
```

and visit http://localhost/\_/ in your browser.

or

```sh
docker run -p 8080:8090 --name velconnect velaboratory/velconnect
```

to access from http://localhost:8080/\_/ in your browser and name the container "velconnect".

### Option 3: Run Go locally

1. Make sure to install [Go](https://go.dev/) on your machine
2. `cd velconnect`
3. To run: `go run main.go serve`
4. To build: `go build`
   - Then run the executable e.g. `velconnect.exe serve`

## Set up systemctl service:

```ini
[Unit]
Description = velconnect

[Service]
Type           = simple
User           = root
Group          = root
LimitNOFILE    = 4096
Restart        = always
RestartSec     = 5s
StandardOutput = append:/home/ubuntu/VEL-Connect/velconnect/errors.log
StandardError  = append:/home/ubuntu/VEL-Connect/velconnect/errors.log
ExecStart      = /home/ubuntu/VEL-Connect/velconnect/velconnect serve

[Install]
WantedBy = multi-user.target
```

- Enter the above in `/etc/systemd/system/velconnect.service`
- `sudo systemctl enable velconnect`
- `sudo systemctl start velconnect`
