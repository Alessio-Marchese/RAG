# RAG - Refactored Application Guide

## Descrizione Generale
Questa applicazione ASP.NET Core gestisce la configurazione utente, l'upload di file su AWS S3 e la gestione di embeddings tramite Pinecone. Il codice è stato completamente refattorizzato per mantenere solo le funzionalità effettivamente utilizzate dal frontend, garantendo chiarezza, manutenibilità e performance ottimali.

## Endpoint API Utilizzati dal Frontend

### Endpoint di Autenticazione (Porta 5140)
- **GET /api/auth/me** - Verifica autenticazione utente
- **POST /api/auth/logout** - Logout utente

### Endpoint di Configurazione (Porta 5196)
- **GET /api/users/{userId}/configuration** - Carica configurazione utente
- **PUT /api/users/{userId}/configuration** - Salva configurazione utente
- **POST /api/files/upload** - Upload file per configurazione

### Endpoint Domande Non Risposte (Porta 5196)
- **GET /api/unanswered-questions** - Recupera domande senza risposta
- **POST /api/unanswered-questions/{questionId}/answer** - Risponde a una domanda
- **DELETE /api/unanswered-questions/{questionId}** - Elimina una domanda

### Endpoint Chat Esterno
- **POST https://n8n-alessio-marchese.com/webhook/chat** - Invia messaggio chat e riceve risposta

## Funzionalità Principali

### Autenticazione e Gestione Sessione
- Autenticazione basata su JWT tramite cookie
- Validazione automatica dei token tramite middleware custom
- Controllo accessi per garantire che ogni utente acceda solo ai propri dati

### Configurazione Personalizzata dell'AI
- **Knowledge Rules**: Regole di conoscenza personalizzate per l'AI
- **Tone Rules**: Regole di comportamento e tono per l'AI
- **File Upload**: Supporto per upload di file PDF, DOCX e TXT
- **Gestione Granulare**: Aggiunta, modifica e rimozione individuale di regole

### Chat con Assistente AI
- Integrazione con endpoint esterno per conversazioni AI
- Utilizzo delle configurazioni personalizzate per personalizzare le risposte

### Gestione Knowledge Base
- Sistema di domande senza risposta per migliorare la knowledge base
- Possibilità di rispondere alle domande e convertirle in knowledge rules
- Eliminazione di domande non pertinenti

## Struttura del Progetto

### Controller
- **UsersController**: Gestisce le configurazioni utente complete (GET/PUT)
- **UnansweredQuestionsController**: Gestisce le domande non risposte (GET, POST answer, DELETE)
- **FilesController**: Gestisce l'upload di file per configurazione

### Servizi
- **SqliteDataService**: Gestisce la persistenza dei dati con database SQLite
- **UserConfigService**: Parsing e serializzazione della configurazione utente
- **S3StorageService**: Gestione storage su AWS S3
- **PineconeService**: Gestione embeddings su Pinecone
- **CookieJwtValidationMiddleware**: Middleware custom per validazione JWT

### Modelli Dati
- **UserConfiguration**: Configurazione completa dell'utente
- **KnowledgeRule**: Regola di conoscenza
- **ToneRule**: Regola di tono/comportamento
- **UnansweredQuestion**: Domanda non risposta
- **File**: File caricato dall'utente

## Formati di File Supportati

L'applicazione supporta l'estrazione automatica del testo dai seguenti formati:

- **PDF (.pdf)**: Estrazione testo tramite PdfPig
- **Word (.docx)**: Estrazione testo tramite DocX
- **Testo semplice (.txt)**: Lettura diretta
- **Altri formati**: Fallback a lettura come testo

## Principali Migliorie del Refactor

### Rimozione Codice Inutile
- Eliminati controller non utilizzati (KnowledgeRulesController, ToneRulesController)
- Rimossi endpoint non utilizzati dal frontend
- Semplificato SqliteDataService rimuovendo metodi non necessari
- Eliminati modelli di request/response non utilizzati

### Ottimizzazione Performance
- Rimossa logica di sincronizzazione S3 non necessaria
- Semplificata gestione delle transazioni database
- Ridotto logging eccessivo
- Eliminati metodi di parsing file non utilizzati

### Miglioramento Manutenibilità
- Codice più pulito e focalizzato
- Separazione chiara delle responsabilità
- Documentazione aggiornata e accurata
- Struttura modulare semplificata

### Aggiornamento a .NET 8.0
- Migrazione da .NET 9.0 a .NET 8.0 (LTS)
- Aggiornamento di tutte le dipendenze per compatibilità
- Sostituzione di Microsoft.AspNetCore.OpenApi con Swashbuckle.AspNetCore
- Miglioramento della stabilità e supporto a lungo termine

## Deployment in Produzione

### 🚀 Opzioni di Deployment

> **Nota**: Il progetto è stato configurato per deployment diretto su VM Linux senza Docker per semplificare la gestione e ridurre la complessità. Tutte le configurazioni Docker sono state rimosse.

#### 1. **Deployment Diretto su VM Linux**
```bash
# Clona il repository sulla VM
git clone <repository-url>
cd RAG

# Rendi eseguibile lo script di deployment
chmod +x deploy-production.sh

# Configura le variabili d'ambiente (OPZIONALE - sovrascrivono appsettings.Production.json)
export AWS_ACCESS_KEY_ID="your-aws-key"
export AWS_SECRET_ACCESS_KEY="your-aws-secret"
export AWS_BUCKET_NAME="your-bucket-name"
export JWT_KEY="your-jwt-key"
export JWT_ISSUER="your-issuer"
export JWT_AUDIENCE="your-audience"
export PINECONE_API_KEY="your-pinecone-key"
export PINECONE_INDEX_HOST="your-pinecone-host"

# Esegui il deployment
./deploy-production.sh

# L'applicazione sarà disponibile su http://<IP-VM>:5000
```





### 🔧 Configurazione Produzione

#### 📋 Gerarchia di Configurazione

L'applicazione segue questa gerarchia di configurazione (dal più basso al più alto):

1. **`appsettings.json`** - Configurazione di base
2. **`appsettings.Production.json`** - Configurazione produzione (sovrascrive appsettings.json)
3. **Variabili d'ambiente** - Sovrascrivono i file di configurazione

#### ⚙️ Configurazione in appsettings.Production.json

Il file `appsettings.Production.json` contiene già valori di default per la produzione:

```json
{
  "AWS": {
    "BucketName": "your-production-bucket-name"
  },
  "Jwt": {
    "Key": "your-production-jwt-key",
    "Issuer": "your-production-issuer", 
    "Audience": "your-production-audience"
  },
  "Pinecone": {
    "ApiKey": "your-production-pinecone-api-key",
    "IndexHost": "your-production-pinecone-index-host"
  }
}
```

**Per usare solo appsettings.Production.json:**
1. Modifica direttamente i valori nel file
2. Non impostare variabili d'ambiente
3. L'applicazione userà i valori del file

**Per usare variabili d'ambiente (RACCOMANDATO per sicurezza):**
1. Lascia i valori placeholder in appsettings.Production.json
2. Imposta le variabili d'ambiente con i valori reali
3. Le variabili d'ambiente sovrascriveranno i valori del file

#### 🔐 Variabili d'Ambiente (Raccomandato)

```bash
# AWS Configuration
AWS_ACCESS_KEY_ID=your-aws-access-key
AWS_SECRET_ACCESS_KEY=your-aws-secret-key
AWS_REGION=us-east-1
AWS_BUCKET_NAME=your-s3-bucket-name

# JWT Configuration
JWT_KEY=your-secure-jwt-key
JWT_ISSUER=your-jwt-issuer
JWT_AUDIENCE=your-jwt-audience

# Pinecone Configuration
PINECONE_API_KEY=your-pinecone-api-key
PINECONE_INDEX_HOST=your-pinecone-index-host
```

**Vantaggi delle variabili d'ambiente:**
- ✅ Non vengono committate nel repository
- ✅ Più sicure per gestire secrets
- ✅ Facili da cambiare senza modificare file
- ✅ Standard per deployment containerizzati

#### 🔧 Configurazione Firewall (Linux)
```bash
# Abilita porta 5000
sudo ufw allow 5000/tcp
```

### 📊 Monitoraggio e Gestione (Linux)
```bash
# Verifica stato
sudo systemctl status rag-api.service

# Logs in tempo reale
sudo journalctl -u rag-api.service -f

# Riavvio servizio
sudo systemctl restart rag-api.service

# Stop servizio
sudo systemctl stop rag-api.service
```





### 🌐 Accesso all'Applicazione

Dopo il deployment, l'applicazione sarà disponibile su:
- **URL**: http://your-vm-ip:5000
- **Health Check**: http://your-vm-ip:5000/health
- **API Endpoints**: http://your-vm-ip:5000/api/*

### 🔒 Sicurezza in Produzione

1. **Firewall**: Configura il firewall per permettere solo la porta 5000
2. **HTTPS**: Usa un reverse proxy (nginx/Apache) con SSL
3. **Secrets**: Usa variabili d'ambiente per i secrets (non committare nel repository)
4. **Updates**: Mantieni aggiornati .NET e le dipendenze
5. **Monitoring**: Configura logging e monitoring

## Configurazione

### appsettings.json (Sviluppo)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rag_database.db"
  },
  "AWS": {
    "BucketName": "your-bucket-name"
  },
  "Jwt": {
    "Key": "your-jwt-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience"
  },
  "Pinecone": {
    "ApiKey": "your-pinecone-api-key",
    "IndexHost": "your-pinecone-index-host"
  }
}
```

### appsettings.Production.json (Produzione)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rag_database.db"
  },
  "AWS": {
    "BucketName": "your-production-bucket-name"
  },
  "Jwt": {
    "Key": "your-production-jwt-key",
    "Issuer": "your-production-issuer",
    "Audience": "your-production-audience"
  },
  "Pinecone": {
    "ApiKey": "your-production-pinecone-api-key",
    "IndexHost": "your-production-pinecone-index-host"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

### Variabili d'Ambiente (Sovrascrivono appsettings)
- **AWS_ACCESS_KEY_ID**: Chiave di accesso AWS
- **AWS_SECRET_ACCESS_KEY**: Chiave segreta AWS
- **AWS_REGION**: Regione AWS (es. us-east-1)
- **AWS_BUCKET_NAME**: Nome bucket S3
- **JWT_KEY**: Chiave JWT per firma token
- **JWT_ISSUER**: Issuer JWT
- **JWT_AUDIENCE**: Audience JWT
- **PINECONE_API_KEY**: Chiave API Pinecone
- **PINECONE_INDEX_HOST**: Host indice Pinecone

## Esempi di Utilizzo

### Configurazione Utente
```bash
# Recupera configurazione
GET /api/users/{userId}/configuration

# Aggiorna configurazione
PUT /api/users/{userId}/configuration
{
  "knowledgeRules": [
    {
      "id": "kr-1",
      "content": "Contenuto della regola"
    }
  ],
  "toneRules": [
    {
      "id": "tr-1", 
      "content": "Regola di comportamento"
    }
  ],
  "files": [
    {
      "id": "f-1",
      "name": "documento.pdf",
      "contentType": "application/pdf",
      "size": 1024000,
      "content": "base64-encoded-content"
    }
  ]
}
```

### Gestione Domande Non Risposte
```bash
# Lista domande
GET /api/unanswered-questions

# Rispondi a domanda
POST /api/unanswered-questions/{questionId}/answer
{
  "answer": "Risposta alla domanda",
  "userId": "user-123"
}

# Elimina domanda
DELETE /api/unanswered-questions/{questionId}
```

### Upload File
```bash
# Upload configurazione
POST /api/files/upload
Content-Type: multipart/form-data
```

## Dipendenze
- .NET 8.0 (LTS)
- AWS SDK S3 (3.7.306)
- Pinecone API (via HttpClient)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Entity Framework Core SQLite (8.0.0)
- Swashbuckle.AspNetCore (6.5.0)
- UglyToad.PdfPig (per PDF)
- Xceed.Words.NET (per DOCX)

## Sicurezza
- **Autenticazione JWT**: Validazione tramite middleware custom
- **Autorizzazione**: Controllo accessi per utente
- **Validazione Input**: Controllo completo dei dati in ingresso
- **Gestione File**: Controllo tipo e dimensione file
- **Configurazione Sicura**: Chiavi tramite appsettings.json e variabili d'ambiente

## Testing
Per testare l'applicazione:
1. Configurare le variabili d'ambiente AWS
2. Aggiornare appsettings.json con le configurazioni JWT e Pinecone
3. Avviare l'applicazione con `dotnet run`
4. Il database SQLite verrà creato automaticamente
5. Swagger UI disponibile su `/swagger` in ambiente di sviluppo

## Note Finali
L'applicazione è ora completamente ottimizzata e allineata con i requisiti del frontend. Il refactor ha eliminato tutto il codice inutile mantenendo solo le funzionalità essenziali, migliorando significativamente performance e manutenibilità. La migrazione a .NET 8.0 garantisce stabilità e supporto a lungo termine. Il deployment in produzione è stato semplificato rimuovendo Docker e configurando deployment diretto su VM Linux per ridurre la complessità e migliorare la gestione.

Per domande o contributi, modificare questo file o aprire una issue. 