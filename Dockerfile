FROM microsoft/dotnet:latest as builder

RUN dotnet tool install fake-cli -g

FROM microsoft/dotnet:latest
