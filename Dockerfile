# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln ./
COPY AoE4WorldReplaysAPI/AoE4WorldReplaysAPI.csproj ./AoE4WorldReplaysAPI/
COPY AoE4WorldReplaysParser/AoE4WorldReplaysParser.csproj ./AoE4WorldReplaysParser/
RUN dotnet restore AoE4WorldReplaysAPI/AoE4WorldReplaysAPI.csproj

# Copy everything else and build
COPY ./ ./
RUN dotnet publish -c Release -o out AoE4WorldReplaysAPI/AoE4WorldReplaysAPI.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "AoE4WorldReplaysAPI.dll"]
