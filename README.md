# RAG API - Refactored Version

## 🚀 Miglioramenti Implementati

### 🔒 Sicurezza
- **Validazione File Robusta**: Implementato `FileValidationService` con whitelist di estensioni e MIME types
- **Middleware JWT Migliorato**: Gestione sicura degli errori senza esposizione di stack trace
- **Rate Limiting**: Protezione contro abusi con limite di 100 richieste/minuto per utente
- **Validazione Configurazione**: Controlli di sicurezza su JWT SecretKey e parametri critici
- **Validazione Input**: Data Annotations su tutti i DTOs per prevenire input malevoli

### 🏗️ Architettura e Struttura
- **Unit of Work Pattern**: Gestione centralizzata delle transazioni database
- **Separazione Responsabilità**: Servizi specializzati per configurazioni utente e storage file
- **Repository Pattern**: Accesso ai dati attraverso interfacce ben definite
- **Dependency Injection**: Configurazione centralizzata di tutti i servizi
- **Caching**: Sistema di cache in-memory per migliorare le performance

### ⚡ Performance e Efficienza
- **Query Ottimizzate**: Uso di `AsNoTracking()` per operazioni di sola lettura
- **Operazioni Parallele**: Upload/delete file eseguiti in parallelo
- **Caching Intelligente**: Cache delle configurazioni utente con TTL di 5 minuti
- **Gestione Memoria**: Streaming per file grandi e validazione dimensioni
- **Connection Pooling**: Configurazione ottimizzata di HttpClient

### 🧹 Codice Pulito
- **Validazione Modelli**: Controllo automatico dei DTOs con messaggi di errore chiari
- **Gestione Errori Centralizzata**: `ExceptionBoundary` per gestione uniforme degli errori
- **Naming Consistente**: Standardizzazione dei nomi e convenzioni
- **Documentazione**: Commenti e struttura chiara del codice
- **Testabilità**: Interfacce ben definite per facilitare i test

## 📁 Struttura del Progetto

```
RAG/
├── Configuration/          # Configurazioni e validatori
├── Controllers/           # API Controllers
├── Data/                  # Entity Framework Context
├── DTOs/                  # Data Transfer Objects con validazioni
├── Entities/              # Modelli di dominio
├── Facades/               # Orchestrazione servizi
├── Middlewares/           # Middleware personalizzati
├── Repositories/          # Accesso ai dati
├── Services/              # Logica di business
└── Mappers/               # Conversione DTO ↔ Entity
```

## 🔧 Servizi Principali

### `IUserConfigurationService`
Gestisce le configurazioni utente con caching e validazioni.

### `IFileStorageService`
Orchestra le operazioni su S3 e Pinecone con operazioni parallele.

### `IFileValidationService`
Validazione robusta dei file con controlli di sicurezza.

### `IUnitOfWork`
Gestione transazionale centralizzata del database.

### `ICacheService`
Sistema di cache in-memory per migliorare le performance.

### `IRateLimitService`
Protezione contro abusi con rate limiting per utente.

## 🚀 Avvio del Progetto

1. **Configurazione**: Copia `appsettings.example.json` in `appsettings.json` e configura i parametri
2. **Database**: Il database SQLite viene creato automaticamente al primo avvio
3. **Avvio**: `dotnet run` o `dotnet watch` per sviluppo

## 🔒 Configurazione Sicurezza

### JWT
- SecretKey minimo 32 caratteri
- Expiration massimo 24 ore
- Validazione Issuer/Audience

### File Upload
- Estensioni consentite: .txt, .pdf, .doc, .docx, .rtf, .md
- Dimensione massima: 10MB
- Validazione MIME type
- Controllo contenuto malevolo

### Rate Limiting
- 100 richieste/minuto per utente per endpoint
- Headers informativi: X-RateLimit-Remaining, X-RateLimit-Limit

## 📊 Performance

- **Caching**: Configurazioni utente cache per 5 minuti
- **Query Ottimizzate**: Include e AsNoTracking per ridurre carico database
- **Operazioni Parallele**: Upload/delete file eseguiti in parallelo
- **Connection Pooling**: HttpClient configurato per pooling

## 🧪 Testing

Il codice è strutturato per facilitare i test:
- Interfacce ben definite per tutti i servizi
- Dependency injection per mock
- Separazione chiara tra logica di business e accesso ai dati

## 🔄 Migrazione da Versione Precedente

1. Aggiorna le dipendenze nel `Program.cs`
2. Rimuovi riferimenti a `ISqliteService` (sostituito da `IUnitOfWork`)
3. Aggiorna le chiamate ai servizi per utilizzare le nuove interfacce
4. Verifica che le validazioni dei DTOs non blocchino le richieste esistenti

## 📈 Monitoraggio

- Rate limiting headers per monitorare l'utilizzo
- Cache hit/miss tracking disponibile
- Errori gestiti uniformemente con messaggi sicuri
- Performance migliorate con operazioni parallele

## 🔮 Roadmap

- [ ] Migrazione a PostgreSQL per produzione
- [ ] Implementazione Redis per cache distribuita
- [ ] Aggiunta di metriche e monitoring
- [ ] Implementazione di audit logging
- [ ] Aggiunta di test unitari e di integrazione

