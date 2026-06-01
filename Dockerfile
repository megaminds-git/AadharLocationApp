FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/AadharLocation.Shared/AadharLocation.Shared.csproj src/AadharLocation.Shared/
COPY src/AadharLocation.Api/AadharLocation.Api.csproj src/AadharLocation.Api/
RUN dotnet restore src/AadharLocation.Api/AadharLocation.Api.csproj

COPY src/AadharLocation.Shared/ src/AadharLocation.Shared/
COPY src/AadharLocation.Api/ src/AadharLocation.Api/
RUN dotnet publish src/AadharLocation.Api/AadharLocation.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AadharLocation.Api.dll"]
