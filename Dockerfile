FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0.11-alpine3.18
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "smbapp.dll"]