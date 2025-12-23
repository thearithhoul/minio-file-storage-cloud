FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file(s) and restore as distinct layers
COPY backend/*.csproj backend/
RUN dotnet restore backend/backend.csproj

# Copy everything else and publish
COPY backend/ backend/
WORKDIR /src/backend
RUN dotnet publish backend.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "backend.dll"]
