#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
WORKDIR /src
COPY ["HubAndHost/HubAndHost.csproj", "HubAndHost/"]
COPY ["GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["SharedTypes/SharedTypes.csproj", "SharedTypes/"]
COPY ["ConsoleSnake/ConsoleSnake.csproj", "ConsoleSnake/"]
RUN dotnet restore "HubAndHost/HubAndHost.csproj"
RUN dotnet restore "ConsoleSnake/ConsoleSnake.csproj"
COPY . .
WORKDIR "/src/HubAndHost"
RUN dotnet publish --no-restore "HubAndHost.csproj" -c Release -o /app/publish
WORKDIR "/src/ConsoleSnake"
RUN dotnet publish --no-restore "ConsoleSnake.csproj" -c Release -o /app/publish/wwwroot

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HubAndHost.dll"]