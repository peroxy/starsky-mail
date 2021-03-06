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
- [docker-compose](https://docs.docker.com/compose/install/) (at least 3.8 version support),
- (_optional: for debugging purposes_) [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0),
- (_optional: if you want to actually send emails while debugging_) SendGrid API key.

## Development

### Settings

Please check out appsettings.json to configure application settings (like RabbitMQ settings or SendGrid settings...).

### Running locally
Please note that this has only been tested with docker (docker-compose) on Ubuntu 20.04.

1. Download source files:

```shell script
git clone https://github.com/peroxy/starsky-mail.git
```

2. Go to root directory:

```shell script
cd starsky-mail
```

3. You must specify: 
   - SendGrid API key (if you want to send emails with consumer),
   - SendGrid email address you are sending emails from. 

Create an `.env` file (in the same directory as `docker-compose.yml`) and specify those environment variables:

```shell script
echo "SENDGRID_API_KEY=api key" > .env
echo "SENDGRID_FROM_ADDRESS=mail@example.com" >> .env
```

Environment variables specified in `.env` file will be automatically used by `docker-compose`.

4. Build and run the API, consumer and RabbitMQ:

```shell script
docker-compose up
```

5. You will now be able to access:
- RabbitMQ at http://localhost:5672,
- RabbitMQ management application at http://localhost:15672,
- .NET core StarskyMail.Queue.Api at http://localhost:56789. 

You can login to RabbitMQ management application with default credentials specified inside `docker-compose.override.yml`.

A queue consumer will also be launched as a background worker service - it will automatically consume queue messages and send emails if configured. 



6. (Optional) If you want to debug dotnet projects locally without docker, you will have to use `dotnet user-secrets`:

```shell script
cd src/StarskyMail/StarskyMail.Queue.Api/
dotnet user-secrets set "RabbitMQSettings:Username" "username"
dotnet user-secrets set "RabbitMQSettings:Password" "password"

cd src/StarskyMail/StarskyMail.Queue.Consumer/
dotnet user-secrets set "RabbitMQSettings:Username" "username"
dotnet user-secrets set "RabbitMQSettings:Password" "password"
dotnet user-secrets set "SendGridSettings:ApiKey" "api key"
dotnet user-secrets set "SendGridSettings:FromAddress" "mail@example.com"
```

## Deployment

We host entire infrastructure inside Azure, specifically inside Azure Virtual Machine.

### Server requirements

The server (in our case Azure VM) must have these installed:

- [docker](https://docs.docker.com/get-docker/),
- [docker-compose](https://docs.docker.com/compose/install/) (at least 3.8 version support).

#### Setup on Azure Virtual Machine:
1. Create Azure Virtual Machine with Ubuntu installed and setup your public SSH key. Ubuntu 18.04 was used at the time of writing this.
2. Enable SSH (port 22) and whitelist your IP.
3. Connect to your machine:
   
   ```shell script
   ssh username@ipAddress
   ```
   
4. [Install docker](https://docs.docker.com/get-docker/):

    ```shell script
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo usermod -aG docker <your-user>
    ```
    Log out and log back in to be able to use `docker` without `sudo`.   

   
5. [Install docker-compose](https://docs.docker.com/compose/install/):

   ```shell script
    sudo curl -L "https://github.com/docker/compose/releases/download/1.27.4/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
    ```

6. Generate a SSH key:

    ```shell script
    ssh-keygen -t rsa -b 4096 -c "starsky_deploy"
    ```
   
7. Add the public part of SSH key (`~/.ssh/id_rsa.pub`) to our Github repository's deployment keys.

### Repository secrets

These are the required secrets that should be stored inside Github repository secrets:

- Dockerhub:
   - `DOCKERHUB_USERNAME` 
   - `DOCKERHUB_TOKEN` - see [Create an access token](https://docs.docker.com/docker-hub/access-tokens/#create-an-access-token) for more information
- RabbitMQ:
   - `RABBITMQ_DEFAULT_USER`
   - `RABBITMQ_DEFAULT_PASS` - don't make it too long, there were some issues with authentication with a 128 character password, even though it should be supported in theory...
   - `RABBITMQ_ERLANG_COOKIE` - alphanumeric secret with max length of 255 characters
- Server host (Azure VM):
   - `REMOTE_HOST` - remote host IP address to SSH into
   - `REMOTE_USER` - username to SSH with
   - `SERVER_SSH_KEY` - private SSH key (OpenSSH, for example the contents of your `~/.ssh/id_rsa` key) to connect to your server
- SendGrid:
   - `SENDGRID_API_KEY` - [SendGrid](https://sendgrid.com/) API token that has permission to send emails 
   - `SENDGRID_FROM_ADDRESS` - email address to send transactionals emails from, the sender's email address

### How to deploy

Push a tag `*.*.*` (e.g. `1.0.3`) to main branch and it will automatically deploy everything via Github workflow.
See `.github/main.yml` workflow for more info. 

In short, it does this if it gets triggered by a new tag:

- Takes source code from `main` branch and extracts the newest version from tag.
- Configures environment variables used by docker containers from Github repository's secrets.
- Builds and pushes all apps as Docker images to DockerHub.
- Copies environment variables and docker-compose files to Azure VM.
- Stops `starsky-mail` containers on Azure VM, pulls the newest images and starts the containers again.