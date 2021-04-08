FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
EXPOSE 5000
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["App.Server/App.Server.csproj", "App.Server/"]
COPY ["App.Shared/App.Shared.csproj", "App.Shared/"]
RUN dotnet restore "App.Server/App.Server.csproj"
COPY . .
WORKDIR "/src/App.Server"
RUN dotnet build "App.Server.csproj" -c Debug -o /app

FROM build AS publish
RUN dotnet publish "App.Server.csproj" -c Debug -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "App.Server.dll"]
