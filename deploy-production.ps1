# Script di deployment per RAG API su VM Windows
# Assicurati di avere .NET 8.0 installato sulla VM

Write-Host "🚀 Avvio deployment RAG API in produzione..." -ForegroundColor Green

# Verifica che .NET 8.0 sia installato
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET versione: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET non è installato. Installa .NET 8.0 SDK prima di continuare." -ForegroundColor Red
    exit 1
}

# Crea directory per l'applicazione se non esiste
$APP_DIR = "C:\opt\rag-api"
Write-Host "📁 Creazione directory: $APP_DIR" -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $APP_DIR | Out-Null

# Copia i file dell'applicazione
Write-Host "📋 Copia file applicazione..." -ForegroundColor Yellow
Copy-Item -Path ".\*" -Destination $APP_DIR -Recurse -Force
Set-Location $APP_DIR

# Pulisci build precedenti
Write-Host "🧹 Pulizia build precedenti..." -ForegroundColor Yellow
dotnet clean
Remove-Item -Path "bin", "obj" -Recurse -Force -ErrorAction SilentlyContinue

# Restore dipendenze
Write-Host "📦 Restore dipendenze..." -ForegroundColor Yellow
dotnet restore

# Build per produzione
Write-Host "🔨 Build per produzione..." -ForegroundColor Yellow
dotnet publish -c Release -o .\publish

# Crea file di configurazione produzione (se non esiste)
if (-not (Test-Path "appsettings.Production.json")) {
    Write-Host "⚙️ Creazione appsettings.Production.json..." -ForegroundColor Yellow
    $productionConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=C:\opt\rag-api\rag_database.db"
  },
  "AWS": {
    "BucketName": "$env:AWS_BUCKET_NAME"
  },
  "Jwt": {
    "Key": "$env:JWT_KEY",
    "Issuer": "$env:JWT_ISSUER",
    "Audience": "$env:JWT_AUDIENCE"
  },
  "Pinecone": {
    "ApiKey": "$env:PINECONE_API_KEY",
    "IndexHost": "$env:PINECONE_INDEX_HOST"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
"@

    $productionConfig | Out-File -FilePath "appsettings.Production.json" -Encoding UTF8
} else {
    Write-Host "✅ appsettings.Production.json già esistente" -ForegroundColor Green
}

Write-Host "📝 Note sulla configurazione:" -ForegroundColor Cyan
Write-Host "   - Se hai impostato variabili d'ambiente, verranno usate" -ForegroundColor White
Write-Host "   - Altrimenti, modifica appsettings.Production.json con i valori reali" -ForegroundColor White
Write-Host "   - Le variabili d'ambiente sovrascrivono appsettings.Production.json" -ForegroundColor White

# Crea Windows Service
Write-Host "🔧 Configurazione Windows Service..." -ForegroundColor Yellow

# Installa NSSM se non presente
$nssmPath = "C:\nssm\nssm.exe"
if (-not (Test-Path $nssmPath)) {
    Write-Host "📥 Download NSSM..." -ForegroundColor Yellow
    $nssmUrl = "https://nssm.cc/release/nssm-2.24.zip"
    $nssmZip = "C:\nssm.zip"
    Invoke-WebRequest -Uri $nssmUrl -OutFile $nssmZip
    Expand-Archive -Path $nssmZip -DestinationPath "C:\nssm" -Force
    Remove-Item $nssmZip
}

# Configura il service
& $nssmPath install "RAG-API" "C:\Program Files\dotnet\dotnet.exe" "C:\opt\rag-api\publish\RAG.dll"
& $nssmPath set "RAG-API" AppDirectory "C:\opt\rag-api\publish"
& $nssmPath set "RAG-API" AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
& $nssmPath set "RAG-API" AppEnvironmentExtra ASPNETCORE_URLS=http://0.0.0.0:5000
& $nssmPath set "RAG-API" Description "RAG API Service"
& $nssmPath set "RAG-API" Start SERVICE_AUTO_START

# Avvia il service
Write-Host "▶️ Avvio servizio..." -ForegroundColor Yellow
Start-Service "RAG-API"

# Verifica stato
Write-Host "📊 Verifica stato servizio..." -ForegroundColor Yellow
Get-Service "RAG-API"

Write-Host "✅ Deployment completato!" -ForegroundColor Green
Write-Host "🌐 L'applicazione è disponibile su: http://0.0.0.0:5000" -ForegroundColor Cyan
Write-Host "📝 Logs: Get-EventLog -LogName Application -Source 'RAG-API'" -ForegroundColor Cyan
Write-Host "🛑 Stop: Stop-Service 'RAG-API'" -ForegroundColor Cyan
Write-Host "▶️ Start: Start-Service 'RAG-API'" -ForegroundColor Cyan
Write-Host "🔄 Restart: Restart-Service 'RAG-API'" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔧 Per configurare i secrets:" -ForegroundColor Yellow
Write-Host "   1. Modifica appsettings.Production.json direttamente, OPPURE" -ForegroundColor White
Write-Host "   2. Imposta variabili d'ambiente e riavvia il servizio" -ForegroundColor White 