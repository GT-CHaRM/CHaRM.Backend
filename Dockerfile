FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine as builder

RUN dotnet tool install fake-cli -g

WORKDIR /usr/src/app

COPY . .

RUN fake build -t Publish

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-alpine

WORKDIR /usr/src/app

COPY --from=builder /usr/src/app/build .

ENTRYPOINT ["dotnet", "CHaRM.Backend.dll"]
