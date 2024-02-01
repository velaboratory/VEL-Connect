# syntax=docker/dockerfile:1
FROM golang:1.21 as build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download

COPY *.go ./
COPY pb_migrations/ ./pb_migrations

# Build
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o ./velconnect
# RUN ./velconnect-pb migrate up

FROM alpine:3.19.1
COPY --from=build /src/velconnect /velconnect

EXPOSE 8090

# Run
ENTRYPOINT ["./velconnect", "serve", "--http=0.0.0.0:8090"]