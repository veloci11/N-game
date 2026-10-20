FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Server/NGame.Server/NGame.Server.csproj Server/NGame.Server/
COPY Assets/NGame/Core/ Assets/NGame/Core/
RUN dotnet restore Server/NGame.Server/NGame.Server.csproj
COPY Server/NGame.Server/*.cs Server/NGame.Server/
RUN dotnet publish Server/NGame.Server/NGame.Server.csproj --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/ngame \
    && chown "$APP_UID:$APP_UID" /var/lib/ngame \
    && chmod 0700 /var/lib/ngame
WORKDIR /app
COPY --from=build /app/publish/ ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    NGAME_DATA_DIRECTORY=/var/lib/ngame \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD curl --fail --silent --output /dev/null http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "NGame.Server.dll"]
