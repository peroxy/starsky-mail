# Starsky mail
 Starsky mail is a service for sending verification and invite emails. 
 The service is used by [starsky-backend](https://github.com/peroxy/starsky-backend) application.

It uses RabbitMQ for orderly email processing and a .NET 5.0 REST API is used to send messages to queues.
Queues get consumed by .NET 5.0 background worker. 

## Requirements

### Development
- [docker](https://docs.docker.com/get-docker/)
- [docker-compose](https://docs.docker.com/compose/install/) (at least 3.3 version support)


## Local Development
Please note that this has only been tested with docker on Ubuntu 20.04.

1. Download source files:

```shell script
git clone https://github.com/peroxy/starsky-mail.git
```

2. Go to root directory:

```shell script
cd starsky-mail
```

3. You must specify Erlang cookie secret and credentials to access RabbitMQ.

Create an `.env` file (in the same directory as `docker-compose.yml`) and specify those environment variables:

```shell script
echo "RABBITMQ_ERLANG_COOKIE=myVeryLongAndSecureSecret" > .env
echo "RABBITMQ_DEFAULT_USER=username" >> .env
echo "RABBITMQ_DEFAULT_PASS=password" >> .env
```

Environment variables specified in `.env` file will be automatically used by `docker-compose`.

4. Build and run the database and API:

```shell script
docker-compose up
```

5. You will now be able to access:
- RabbitMQ at http://localhost:5672
- RabbitMQ management application at http://localhost:15672

You can login to RabbitMQ management application database with credentials specified in `.env` file.
