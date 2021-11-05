#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SnakesSilo/SnakesSilo.csproj", "SnakesSilo/"]
COPY ["GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["Grains/Grains.csproj", "Grains/"]
RUN dotnet restore "SnakesSilo/SnakesSilo.csproj"
COPY .. .
WORKDIR "/src/SnakesSilo"
RUN dotnet build "SnakesSilo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnakesSilo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnakesSilo.dll"]