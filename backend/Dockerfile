FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/CocoCarbon.Domain/CocoCarbon.Domain.csproj src/CocoCarbon.Domain/
COPY src/CocoCarbon.Infrastructure/CocoCarbon.Infrastructure.csproj src/CocoCarbon.Infrastructure/
COPY src/CocoCarbon.Api/CocoCarbon.Api.csproj src/CocoCarbon.Api/
RUN dotnet restore src/CocoCarbon.Api/CocoCarbon.Api.csproj
COPY src/ src/
RUN dotnet publish src/CocoCarbon.Api/CocoCarbon.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/storage && chmod 700 /app/storage
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "CocoCarbon.Api.dll"]
