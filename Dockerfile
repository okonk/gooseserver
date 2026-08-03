# syntax=docker/dockerfile:1

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for layer caching. Goose references CsvToSql.Core, so both
# csproj files must be present for the restore to succeed.
COPY Goose/Goose.csproj Goose/
COPY CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj CsvToSql/CsvToSql.Core/
RUN dotnet restore Goose/Goose.csproj

COPY . .
RUN dotnet publish Goose/Goose.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
# Debian-based (NOT alpine): System.Data.SQLite ships a glibc native library
# under runtimes/linux-x64, which won't load on musl.
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

# sqlite3 is handy for inspecting / editing the game DB from inside the
# container (e.g. making a character a GM), matching the host-side workflow
# documented in the Readme.
RUN apt-get update \
    && apt-get install -y --no-install-recommends sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Run as non-root. The data volume must be writable by this user (uid 1000).
RUN useradd --create-home --uid 1000 goose \
    && mkdir -p /data \
    && chown -R goose:goose /data

USER goose

COPY --from=build /app/publish/ .

# Mutable state (SQLite database, Logs/, settings, crashlog.txt) goes to the
# data dir; mount a persistent volume here (see docker-compose.yml).
VOLUME ["/data"]
ENV GOOSE_DATADIR=/data

EXPOSE 2006

ENTRYPOINT ["dotnet", "Goose.dll"]
