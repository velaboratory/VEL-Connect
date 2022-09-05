docker build --tag velconnect .
docker rm web
docker run -p 8081:80 --name web velconnect

