FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /source

COPY src/StarskyMail/. .
RUN dotnet restore -r linux-musl-x64
WORKDIR /source/StarskyMail.Queue.Api
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained true --no-restore /p:PublishTrimmed=true /p:PublishReadyToRun=true

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine-amd64
WORKDIR /app
COPY --from=build /app ./

EXPOSE 56789

ENTRYPOINT ["./StarskyMail.Queue.Api"]