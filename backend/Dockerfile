# Build the application with the full .NET SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first so dependency restore can be cached between builds.
COPY ["src/HakwadagAssassinGame.Web/HakwadagAssassinGame.Web.csproj", "src/HakwadagAssassinGame.Web/"]
COPY ["src/HakwadagAssassinGame.Application/HakwadagAssassinGame.Application.csproj", "src/HakwadagAssassinGame.Application/"]
COPY ["src/HakwadagAssassinGame.Core/HakwadagAssassinGame.Core.csproj", "src/HakwadagAssassinGame.Core/"]
COPY ["src/HakwadagAssassinGame.Infrastructure/HakwadagAssassinGame.Infrastructure.csproj", "src/HakwadagAssassinGame.Infrastructure/"]
COPY ["src/HakwadagAssassinGame.Utils/HakwadagAssassinGame.Utils.csproj", "src/HakwadagAssassinGame.Utils/"]
RUN dotnet restore "src/HakwadagAssassinGame.Web/HakwadagAssassinGame.Web.csproj"

# Copy the source after restoring project dependencies, then compile the web project.
COPY . .
WORKDIR "/src/src/HakwadagAssassinGame.Web"
RUN dotnet build "HakwadagAssassinGame.Web.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/build

# Publish the compiled application into a runtime-ready directory.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HakwadagAssassinGame.Web.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# Use the smaller ASP.NET runtime image for the final container.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The application listens on port 8080 inside the container.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Copy only published output into the runtime image.
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HakwadagAssassinGame.Web.dll"]
