# RAG - Refactored Application Guide

## Descrizione Generale
Questa applicazione ASP.NET Core gestisce la configurazione utente, l'upload di file su AWS S3 e la gestione di embeddings tramite Pinecone. Il codice è stato refattorizzato per garantire chiarezza, manutenibilità, separazione delle responsabilità e aderenza ai principi SOLID.

## Struttura API

### Endpoint Configurazione Utente
- **GET /api/users/{userId}/configuration**: Recupera la configurazione completa dell'utente
- **PUT /api/users/{userId}/configuration**: Aggiorna la configurazione utente

### Endpoint Knowledge Base
- **POST /api/users/{userId}/knowledge-rules**: Aggiunge nuovo item testuale alla knowledge base
- **POST /api/users/{userId}/knowledge-rules/upload**: Upload file PDF per estrazione automatica
- **GET /api/users/{userId}/knowledge-rules/{ruleId}**: Recupera una knowledge rule specifica
- **PUT /api/users/{userId}/knowledge-rules/{ruleId}**: Modifica item esistente (editing inline)
- **DELETE /api/users/{userId}/knowledge-rules/{ruleId}**: Rimuove item dalla knowledge base

### Endpoint Tone Rules
- **POST /api/users/{userId}/tone-rules**: Aggiunge nuovo item di comportamento
- **GET /api/users/{userId}/tone-rules/{ruleId}**: Recupera una tone rule specifica
- **DELETE /api/users/{userId}/tone-rules/{ruleId}**: Rimuove item di comportamento

### Endpoint Domande Non Risposte
- **GET /api/unanswered-questions**: Recupera lista domande senza risposta
- **POST /api/unanswered-questions**: Crea una nuova domanda non risposta
- **GET /api/unanswered-questions/{questionId}**: Recupera una domanda specifica
- **POST /api/unanswered-questions/{questionId}/answer**: Fornisce risposta a domanda (la sposta nella knowledge base)
- **DELETE /api/unanswered-questions/{questionId}**: Scarta domanda non pertinente

## Formati di file supportati per le Knowledge Rules

L'applicazione supporta l'estrazione automatica del testo dai seguenti formati di file allegati alle knowledge rules:

- **PDF (.pdf)**: Il testo viene estratto da tutte le pagine tramite la libreria PdfPig.
- **Word (.docx)**: Il testo viene estratto tramite la libreria DocX.
- **Testo semplice (.txt)**: Il contenuto viene letto come testo puro.
- **Altri formati**: Vengono letti come testo puro (fallback), ma il risultato potrebbe non essere ottimale.

> **Nota:** Per supportare PDF e DOCX, assicurati che i pacchetti `UglyToad.PdfPig` e `Xceed.Words.NET` siano installati nel progetto.

## Struttura dei Moduli Principali

### Controller
- **UsersController**: Gestisce le configurazioni utente complete (GET/PUT)
- **KnowledgeRulesController**: Gestisce la knowledge base (POST, GET, PUT, DELETE, upload PDF)
- **ToneRulesController**: Gestisce le tone rules (POST, GET, DELETE)
- **UnansweredQuestionsController**: Gestisce le domande non risposte (GET, POST, DELETE)
- **FilesController**: Endpoint legacy per l'upload della configurazione utente

### Servizi
- **SqliteDataService (IDataService)**: Gestisce la persistenza dei dati con database SQLite e sincronizzazione S3
- **UserConfigService (IUserConfigService)**: Parsing e serializzazione della configurazione utente
- **S3StorageService (IS3StorageService)**: Gestione storage su AWS S3
- **PineconeService (IPineconeService)**: Gestione embeddings su Pinecone
- **CookieJwtValidationMiddleware**: Middleware custom per validazione JWT

### Modelli Dati
- **UserConfiguration**: Configurazione completa dell'utente
- **KnowledgeRule**: Regola di conoscenza (testo o file)
- **ToneRule**: Regola di tono/comportamento
- **UnansweredQuestion**: Domanda non risposta (senza priority/email)

## Principali Migliorie e Best Practice

### Architettura REST
- **Endpoint RESTful**: Implementazione di endpoint REST standard per tutte le entità
- **Validazione Input**: Validazione completa dei dati in ingresso con ModelState
- **Gestione Errori**: Gestione standardizzata degli errori con codici HTTP appropriati
- **Autorizzazione**: Controllo accessi basato su JWT per ogni endpoint

### Separazione delle Responsabilità
- **Controller**: Solo orchestrazione e controllo accessi
- **Servizi**: Logica di business e accesso ai dati
- **Modelli**: Strutture dati tipizzate e validazione

### Sicurezza
- **Autenticazione JWT**: Tutti gli endpoint richiedono autenticazione
- **Autorizzazione**: Gli utenti possono accedere solo ai propri dati
- **Validazione Files**: Controllo tipo e dimensione file in upload

## Modifiche Rispetto alla Versione Precedente

### Rimozioni
- **Fallback Email**: Eliminato completamente dalla configurazione utente
- **Multilingua**: Rimosso supporto multilingua, tutto in inglese
- **Priority/Email**: Rimossi campi priority e userEmail dalle domande non risposte

### Aggiunte
- **Nuovi Controller**: Implementati controller dedicati per ogni entità
- **Gestione Domande**: Sistema completo per gestire domande non risposte
- **Editing Inline**: Possibilità di modificare knowledge rules esistenti
- **Upload PDF**: Endpoint dedicato per upload e parsing automatico PDF
- **Paginazione**: Sistema di paginazione per knowledge rules (5 elementi per pagina)

## Dipendenze
- .NET 9.0
- AWS SDK S3
- Pinecone API (via HttpClient)
- Microsoft.AspNetCore.Authentication.JwtBearer
- Entity Framework Core SQLite
- UglyToad.PdfPig (per PDF)
- Xceed.Words.NET (per DOCX)

## Configurazione

### appsettings.json
```json
{
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
  }
}
```

### Variabili d'Ambiente
- **AWS_ACCESS_KEY_ID**: Chiave di accesso AWS
- **AWS_SECRET_ACCESS_KEY**: Chiave segreta AWS
- **AWS_REGION**: Regione AWS (es. us-east-1)

## Esempio di Utilizzo

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
      "content": "Contenuto della regola",
      "type": "text"
    }
  ],
  "toneRules": [
    {
      "id": "tr-1",
      "content": "Regola di comportamento"
    }
  ]
}
```

### Knowledge Base
```bash
# Aggiungi testo
POST /api/users/{userId}/knowledge-rules
{
  "content": "Nuovo contenuto testuale",
  "type": "text"
}

# Upload PDF
POST /api/users/{userId}/knowledge-rules/upload
Content-Type: multipart/form-data
file: [file.pdf]

# Modifica item esistente
PUT /api/users/{userId}/knowledge-rules/{ruleId}
{
  "content": "Contenuto modificato"
}
```

### Domande Non Risposte
```bash
# Lista domande
GET /api/unanswered-questions

# Rispondi a domanda
POST /api/unanswered-questions/{questionId}/answer
{
  "answer": "Risposta alla domanda",
  "userId": "user-123"
}
```

## Estendibilità
- **Nuovi Tipi File**: Aggiungere parsing per nuovi formati in KnowledgeRulesController
- **Database**: Implementata persistenza SQLite con sincronizzazione automatica S3
- **Caching**: Aggiungere layer di caching per performance
- **Notifiche**: Implementare sistema di notifiche per nuove domande
- **Backup**: Implementare backup automatico delle configurazioni

## Sicurezza
- **Validazione JWT**: Tramite middleware custom per autenticazione basata su cookie
- **Controllo Accessi**: Ogni utente può accedere solo ai propri dati
- **Validazione Input**: Controllo completo dei dati in ingresso
- **Gestione File**: Controllo tipo e dimensione file in upload
- **Configurazione Sicura**: Chiavi e configurazioni tramite appsettings.json e variabili d'ambiente

## Testing
Per testare l'applicazione:
1. Configurare le variabili d'ambiente AWS
2. Aggiornare appsettings.json con le configurazioni JWT e connection string
3. Avviare l'applicazione con `dotnet run` (il database SQLite verrà creato automaticamente)
4. Utilizzare i dati di esempio caricati automaticamente (userId: "sample-user")

## Note Finali
L'applicazione è ora completamente allineata con i cambiamenti del frontend e supporta tutte le funzionalità richieste. La struttura modulare e l'uso di interfacce facilitano testing e manutenzione futura. La persistenza è implementata con SQLite per garantire affidabilità e performance, con sincronizzazione automatica su S3 per backup e condivisione.

Per domande o contributi, modificare questo file o aprire una issue. 