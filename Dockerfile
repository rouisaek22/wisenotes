# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Notes.Api/Notes.Api.csproj", "src/Notes.Api/"]
RUN dotnet restore "src/Notes.Api/Notes.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Notes.Api"
RUN dotnet build "Notes.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Notes.Api.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notes.Api.dll"]