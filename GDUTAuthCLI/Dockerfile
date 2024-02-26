FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GDUTCLI/GDUTCLI.fsproj", "GDUTCLI/"]
RUN dotnet restore "GDUTCLI/GDUTCLI.fsproj"
COPY . .
WORKDIR "/src/GDUTCLI"
RUN dotnet build "GDUTCLI.fsproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GDUTCLI.fsproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GDUTCLI.dll"]
