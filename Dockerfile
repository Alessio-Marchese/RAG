# Dockerfile per RAG API - .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia file di progetto e restore dipendenze
COPY ["RAG.csproj", "./"]
RUN dotnet restore "RAG.csproj"

# Copia tutto il codice sorgente
COPY . .
WORKDIR "/src"

# Build dell'applicazione
RUN dotnet build "RAG.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RAG.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Crea utente non-root per sicurezza
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Configurazione per produzione
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "RAG.dll"] 