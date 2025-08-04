# RAG API

A REST API for managing files and knowledge rules in a RAG (Retrieval-Augmented Generation) system.

## What is this project

This API allows users to:
- Upload and manage files (PDF, DOC, TXT, etc.)
- Create and modify custom knowledge rules
- Store files on AWS S3
- Index content on Pinecone for semantic search
- Manage user configurations with caching

## Architecture

The project follows a layered architecture with:

- **Controllers**: Handle HTTP requests
- **Facades**: Orchestrate business logic
- **Services**: Implement specific logic (file storage, cache, validation)
- **Repositories**: Manage data access
- **Entities**: Domain models (File, KnowledgeRule)
- **DTOs**: Data transfer objects with validations

## Main Technologies

- **.NET 8** with ASP.NET Core
- **Entity Framework Core** with SQLite
- **AWS S3** for file storage
- **Pinecone** for vector indexing
- **JWT** for authentication
- **Swagger** for API documentation

## Main Features

### File Management
- Upload with type and size validation
- Storage on AWS S3
- Indexing on Pinecone for semantic search
- Deletion with automatic embedding cleanup

### Knowledge Rules
- Creation and modification of custom rules per user
- Pagination for managing large data volumes

### Security
- JWT authentication with claims validation
- Terms and conditions and privacy policy acceptance control
- Rate limiting (100 requests/minute per user)
- File validation with extension whitelist
- Security checks on input
- User storage limit (10MB total between files and knowledge rules)

### Performance
- User configuration caching (5 minutes TTL)
- Parallel operations for upload/delete
- Optimized queries with Entity Framework

## Project Structure

```
RAG/
├── Controllers/           # API endpoints
├── Facades/              # Business logic orchestration
├── Services/             # Business logic
├── Repositories/         # Data access
├── Entities/             # Domain models
├── DTOs/                 # Data Transfer Objects
├── Configuration/        # Configurations and validators
├── Middlewares/          # Custom middlewares
└── Data/                 # Entity Framework context
```

## Configuration

1. Copy `appsettings.example.json` to `appsettings.json`
2. Configure AWS S3 credentials
3. Configure Pinecone API key
4. Set JWT secret key (minimum 32 characters)

## Startup

```bash
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger documentation at `/swagger`.

## Database

The SQLite database is created automatically on first startup with tables:
- `Files`: Metadata of uploaded files
- `KnowledgeRules`: Knowledge rules per user

## API Endpoints

- `GET /api/Users/configuration/paginated` - Paginated list of user configurations
- `PUT /api/Users/configuration` - Update user configuration
- `GET /api/Users/storage/usage` - Get current storage usage (in bytes)

All endpoints require JWT authentication and acceptance of terms and conditions and privacy policy.

## JWT Claims Validation

The authentication middleware automatically verifies the following JWT token claims:
- `termsAccepted`: Must be `true` to access APIs
- `privacyAccepted`: Must be `true` to access APIs

If either claim is `false` or missing, access is denied with an appropriate error message.

