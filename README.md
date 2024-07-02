# chatlink
Сhatlink server

This is a test project, an example implementation of a chat server.
Here is an example of the client - https://github.com/bitcodepro/chatlink-client

To run the server, PostgreSQL is required.
You can use Docker:
docker run -d --name postgres_db -p 5432:5432 -e POSTGRES_PASSWORD=123 -e POSTGRES_USER=admin postgres:latest

