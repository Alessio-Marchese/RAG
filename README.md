# RAG - Refactored Application Guide

## Descrizione Generale
Questa applicazione ASP.NET Core gestisce la configurazione utente, l'upload di file su AWS S3 e la gestione di embeddings tramite Pinecone. Il codice è stato refattorizzato per garantire chiarezza, manutenibilità, separazione delle responsabilità e aderenza ai principi SOLID.

## Formati di file supportati per le Knowledge Rules

L'applicazione supporta l'estrazione automatica del testo dai seguenti formati di file allegati alle knowledge rules:

- **PDF (.pdf)**: Il testo viene estratto da tutte le pagine tramite la libreria PdfPig.
- **Word (.docx)**: Il testo viene estratto tramite la libreria DocX.
- **Testo semplice (.txt)**: Il contenuto viene letto come testo puro.
- **Altri formati**: Vengono letti come testo puro (fallback), ma il risultato potrebbe non essere ottimale.

> **Nota:** Per supportare PDF e DOCX, assicurati che i pacchetti `UglyToad.PdfPig` e `Xceed.Words.NET` siano installati nel progetto.

## Struttura dei Moduli Principali

- **FilesController**: Espone endpoint API per l'upload della configurazione utente. Si occupa solo di orchestrare i servizi e non contiene logica di parsing o serializzazione.
- **UserConfigService (IUserConfigService)**: Si occupa del parsing della form di configurazione utente e della serializzazione in formato testuale. Favorisce la testabilità e la separazione della logica di business.
- **S3StorageService (IS3StorageService)**: Incapsula tutte le operazioni di storage su AWS S3 (upload, cancellazione file utente). Esposto tramite interfaccia per favorire l'iniezione delle dipendenze e i test.
- **PineconeService (IPineconeService)**: Gestisce le chiamate HTTP verso Pinecone per la cancellazione degli embeddings. Esposto tramite interfaccia.
- **CookieJwtValidationMiddleware**: Middleware custom per la validazione del JWT presente nei cookie.

## Principali Migliorie e Best Practice

- **Separazione delle responsabilità**: Ogni classe ha un compito ben definito.
- **Dependency Injection**: Tutti i servizi sono registrati tramite interfacce per favorire la testabilità.
- **Eliminazione di codice duplicato e annidamenti**: La logica di parsing/serializzazione è centralizzata in UserConfigService.
- **Adozione principi SOLID**: Single Responsibility, Dependency Inversion, Open/Closed.
- **Commenti e documentazione**: I punti complessi sono documentati inline e in questo README.

## Dipendenze
- .NET 9.0
- AWS SDK S3
- Pinecone API (via HttpClient)
- Microsoft.AspNetCore.Authentication.JwtBearer
- UglyToad.PdfPig (per PDF)
- Xceed.Words.NET (per DOCX)

## Esempio di Flusso Upload Configurazione
1. L'utente invia una form con le regole di tono e conoscenza.
2. Il controller chiama UserConfigService per il parsing e la serializzazione.
3. Vengono eliminati gli embeddings Pinecone e i file S3 precedenti.
4. Il file di configurazione viene caricato su S3.

## Estendibilità
- Per aggiungere nuove regole o logiche di serializzazione, estendere UserConfigService.
- Per supportare altri storage provider, implementare IS3StorageService.
- Per aggiungere il supporto ad altri formati file, aggiungere un nuovo ramo nella funzione di parsing dei file in UserConfigService.

## Sicurezza
- Validazione JWT tramite middleware custom.
- Gestione sicura delle chiavi e delle configurazioni tramite appsettings.json.

## Note Finali
Per domande o contributi, modificare questo file o aprire una issue. 