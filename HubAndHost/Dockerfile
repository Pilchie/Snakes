#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
ARG GIT_SHA
WORKDIR /src
COPY ["HubAndHost/HubAndHost.csproj", "HubAndHost/"]
COPY ["GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["SharedTypes/SharedTypes.csproj", "SharedTypes/"]
COPY ["BlazorClient/BlazorClient.csproj", "BlazorClient/"]
COPY ["ConsoleSnake/ConsoleSnake.csproj", "ConsoleSnake/"]
RUN dotnet restore "ConsoleSnake/ConsoleSnake.csproj"
RUN dotnet restore "BlazorClient/BlazorClient.csproj"
RUN dotnet restore "HubAndHost/HubAndHost.csproj"

COPY . .
WORKDIR "/src/HubAndHost"
RUN dotnet publish --no-restore "HubAndHost.csproj" -c Release -o /app/publish /p:GitSha=${GIT_SHA}
WORKDIR "/src/ConsoleSnake"
RUN dotnet publish --no-restore "ConsoleSnake.csproj" -c Release -o /app/publish/wwwroot /p:GitSha=${GIT_SHA}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HubAndHost.dll"]