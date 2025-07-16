#!/bin/bash

# Script di deployment idempotente per RAG API su VM Linux (Ubuntu)
# Assicurati di avere .NET 8.0 installato sulla VM

set -e

SERVICE_NAME="rag-api"
APP_DIR="/opt/rag-api"
PUBLISH_DIR="$APP_DIR/publish"
DB_FILE="$APP_DIR/rag_database.db"

# 1. Ferma e disabilita il servizio se esiste
if systemctl list-units --full -all | grep -Fq "$SERVICE_NAME.service"; then
    echo "🛑 Fermata servizio esistente..."
    sudo systemctl stop $SERVICE_NAME.service || true
    sudo systemctl disable $SERVICE_NAME.service || true
fi

# 2. Uccidi eventuali processi .NET residui relativi a RAG
if pgrep -f "$PUBLISH_DIR/RAG.dll" > /dev/null; then
    echo "🔪 Kill processi .NET residui..."
    sudo pkill -f "$PUBLISH_DIR/RAG.dll" || true
fi

# 3. Backup del database se esiste
if [ -f "$DB_FILE" ]; then
    echo "💾 Backup database esistente..."
    cp "$DB_FILE" "/tmp/rag_database.db.bak"
fi

# 4. Elimina la directory di deploy ma preserva il database
if [ -d "$APP_DIR" ]; then
    echo "🧹 Rimozione directory di deploy esistente (preservando il database)..."
    find "$APP_DIR" -mindepth 1 ! -name 'rag_database.db' -exec rm -rf {} +
fi

# 5. Ricrea la directory e copia i file (senza sovrascrivere il database)
echo "📁 Creazione directory: $APP_DIR"
sudo mkdir -p $APP_DIR
sudo chown $USER:$USER $APP_DIR
cp -r . $APP_DIR/
cd $APP_DIR

# 6. Ripristina il database se era stato salvato
if [ -f "/tmp/rag_database.db.bak" ]; then
    echo "♻️  Ripristino database..."
    mv /tmp/rag_database.db.bak "$DB_FILE"
fi

# 7. Build e publish puliti
echo "🧹 Pulizia build precedenti..."
dotnet clean
rm -rf bin/ obj/

echo "📦 Restore dipendenze..."
dotnet restore

echo "🔨 Build per produzione..."
dotnet publish -c Release -o ./publish

# 8. Crea file di configurazione produzione (se non esiste)
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
    "IndexHost": "${PINECONE_INDEX_HOST:-your-production-pinecone-host}"
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

# 9. (Ri)crea il service file per systemd
sudo tee /etc/systemd/system/$SERVICE_NAME.service > /dev/null << EOF
[Unit]
Description=RAG API
After=network.target

[Service]
Type=exec
User=$USER
WorkingDirectory=$PUBLISH_DIR
ExecStart=/usr/bin/dotnet $PUBLISH_DIR/RAG.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# 10. Ricarica systemd, abilita e avvia il servizio
sudo systemctl daemon-reload
sudo systemctl enable $SERVICE_NAME.service
sudo systemctl start $SERVICE_NAME.service

# 11. Verifica stato
sudo systemctl status $SERVICE_NAME.service --no-pager

echo "✅ Deployment completato!"
echo "🌐 L'applicazione è disponibile su: http://0.0.0.0:5000"
echo "📝 Logs: sudo journalctl -u $SERVICE_NAME.service -f"
echo "🛑 Stop: sudo systemctl stop $SERVICE_NAME.service"
echo "▶️ Start: sudo systemctl start $SERVICE_NAME.service"
echo "🔄 Restart: sudo systemctl restart $SERVICE_NAME.service"
echo ""
echo "🔧 Per configurare i secrets:"
echo "   1. Modifica appsettings.Production.json direttamente, OPPURE"
echo "   2. Imposta variabili d'ambiente e riavvia il servizio" 