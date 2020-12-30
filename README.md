# Starsky mail
 Starsky mail is a service for sending verification and invite emails. 
 The service is used by [starsky-backend](https://github.com/peroxy/starsky-backend) application.

It uses RabbitMQ for orderly email processing and a .NET 5.0 REST API is used to send messages to queues. 
The API is located inside project called **StarskyMail.Queue.Api**.

Queue messages get consumed by .NET 5.0 background worker inside project called **StarskyMail.Queue.Consumer**.

Common RabbitMQ queue components and classes are located inside class library **StarskyMail.Queue**. 

## Requirements

### Development
- [docker](https://docs.docker.com/get-docker/),
- [docker-compose](https://docs.docker.com/compose/install/) (at least 3.3 version support),
- (_optional: for debugging purposes_) [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0),
- (_optional: if you want to actually send emails_) SendGrid API key.

### Production
TODO: complete this section after first prod deployment..

- [docker](https://docs.docker.com/get-docker/),
- SendGrid API key.

## Development

### Settings

Please check out appsettings.json to configure application settings 
(like RabbitMQ credentials, hostname, or SendGrid settings...).

### Running locally
Please note that this has only been tested with docker on Ubuntu 20.04.

1. Download source files:

```shell script
git clone https://github.com/peroxy/starsky-mail.git
```

2. Go to root directory:

```shell script
cd starsky-mail
```

3. You must specify: 
   - Erlang cookie secret, 
   - password to access RabbitMQ,
   - SendGrid API key (if you want to send emails with consumer).

Create an `.env` file (in the same directory as `docker-compose.yml`) and specify those environment variables:

```shell script
echo "RABBITMQ_ERLANG_COOKIE=secret" > .env
echo "RABBITMQ_DEFAULT_PASS=password" >> .env
echo "SENDGRID_API_KEY=api key" >> .env
echo "SENDGRID_FROM_ADDRESS=mail@example.com" >> .env
```

Environment variables specified in `.env` file will be automatically used by `docker-compose`.

4. Build and run the database and API:

```shell script
docker-compose up
```

5. You will now be able to access:
- RabbitMQ at http://localhost:5672,
- RabbitMQ management application at http://localhost:15672,
- .NET core StarskyMail.Queue.Api at http://localhost:56789. 

You can login to RabbitMQ management application database with credentials specified in `.env` file.

6. (Optional) If you want to debug dotnet projects locally without docker, you will have to use `dotnet user-secrets`:

```shell script
cd src/StarskyMail/StarskyMail.Queue.Api/
dotnet user-secrets set "RabbitMQSettings:Password" "password"

cd src/StarskyMail/StarskyMail.Queue.Consumer/
dotnet user-secrets set "RabbitMQSettings:Password" "password"
dotnet user-secrets set "SendGridSettings:ApiKey" "api key"
dotnet user-secrets set "SendGridSettings:FromAddress" "mail@example.com"
```

