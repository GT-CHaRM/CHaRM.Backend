FROM mcr.microsoft.com/mssql/server:2019-CTP2.2-ubuntu

WORKDIR /usr/src/database

ENV ACCEPT_EULA="Y"

COPY . .

RUN ["bash", "entrypoint.sh"]
