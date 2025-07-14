#!/bin/bash

# Script di deployment per RAG API su VM Linux
# Assicurati di avere .NET 8.0 installato sulla VM

echo "🚀 Avvio deployment RAG API in produzione..."

# Verifica che .NET 8.0 sia installato
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET non è installato. Installa .NET 8.0 SDK prima di continuare."
    exit 1
fi

# Verifica versione .NET
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET versione: $DOTNET_VERSION"

# Crea directory per l'applicazione se non esiste
APP_DIR="/opt/rag-api"
echo "📁 Creazione directory: $APP_DIR"
sudo mkdir -p $APP_DIR
sudo chown $USER:$USER $APP_DIR

# Copia i file dell'applicazione
echo "📋 Copia file applicazione..."
cp -r . $APP_DIR/
cd $APP_DIR

# Pulisci build precedenti
echo "🧹 Pulizia build precedenti..."
dotnet clean
rm -rf bin/ obj/

# Restore dipendenze
echo "📦 Restore dipendenze..."
dotnet restore

# Build per produzione
echo "🔨 Build per produzione..."
dotnet publish -c Release -o ./publish

# Crea file di configurazione produzione (se non esiste)
if [ ! -f "appsettings.Production.json" ]; then
    echo "⚙️ Creazione appsettings.Production.json..."
    cat > appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/opt/rag-api/rag_database.db"
  },
  "AWS": {
    "BucketName": "${AWS_BUCKET_NAME:-your-production-bucket-name}"
  },
  "Jwt": {
    "Key": "${JWT_KEY:-your-production-jwt-key}",
    "Issuer": "${JWT_ISSUER:-your-production-issuer}",
    "Audience": "${JWT_AUDIENCE:-your-production-audience}"
  },
  "Pinecone": {
    "ApiKey": "${PINECONE_API_KEY:-your-production-pinecone-api-key}",
    "IndexHost": "${PINECONE_INDEX_HOST:-your-production-pinecone-index-host}"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
EOF
else
    echo "✅ appsettings.Production.json già esistente"
fi

echo "📝 Note sulla configurazione:"
echo "   - Se hai impostato variabili d'ambiente, verranno usate"
echo "   - Altrimenti, modifica appsettings.Production.json con i valori reali"
echo "   - Le variabili d'ambiente sovrascrivono appsettings.Production.json"

# Crea service file per systemd
echo "🔧 Configurazione systemd service..."
sudo tee /etc/systemd/system/rag-api.service > /dev/null << EOF
[Unit]
Description=RAG API
After=network.target

[Service]
Type=exec
User=$USER
WorkingDirectory=$APP_DIR/publish
ExecStart=/usr/bin/dotnet $APP_DIR/publish/RAG.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF

# Ricarica systemd e abilita il service
echo "🔄 Configurazione systemd..."
sudo systemctl daemon-reload
sudo systemctl enable rag-api.service

# Avvia il service
echo "▶️ Avvio servizio..."
sudo systemctl start rag-api.service

# Verifica stato
echo "📊 Verifica stato servizio..."
sudo systemctl status rag-api.service --no-pager

echo "✅ Deployment completato!"
echo "🌐 L'applicazione è disponibile su: http://0.0.0.0:5000"
echo "📝 Logs: sudo journalctl -u rag-api.service -f"
echo "🛑 Stop: sudo systemctl stop rag-api.service"
echo "▶️ Start: sudo systemctl start rag-api.service"
echo "🔄 Restart: sudo systemctl restart rag-api.service"
echo ""
echo "🔧 Per configurare i secrets:"
echo "   1. Modifica appsettings.Production.json direttamente, OPPURE"
echo "   2. Imposta variabili d'ambiente e riavvia il servizio" 