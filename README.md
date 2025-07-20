# RAG - Refactored Application Guide

## Recent Updates

### Session-Based Architecture (Latest)
- **SessionService**: Centralized user session management
- **Simplified Controllers**: No more userId parameters in endpoints
- **Clean ExceptionBoundary**: Removed complex authorization methods
- **Automatic User Context**: User ID automatically available in facades and services
- **Improved Security**: User can only access their own data through session
- **Simplified API**: Endpoints are cleaner and more intuitive

### Result Pattern Implementation
- **Result Pattern**: Using custom Result classes for type-safe result pattern
- **Automatic Error Propagation**: Errors propagate automatically from services to controllers
- **Type Safety**: Compile-time checking of operation results
- **Consistent Error Handling**: All errors follow the same format and structure
- **Null Safety**: Eliminated null reference exceptions with proper result handling
- **Enhanced ExceptionBoundary**: Service iniettabile che gestisce automaticamente la conversione dei Result in risposte HTTP

### Architecture Refactoring with Facade Pattern
- **UsersFacade**: Introduced facade pattern to separate business logic from controllers
- **UnansweredQuestionsFacade**: Extended facade pattern to unanswered questions operations
- **Complete Business Logic Migration**: All business logic moved from controllers to facades
- **Improved Separation of Concerns**: Controllers now focus only on HTTP concerns (routing, authorization, validation)
- **Better Testability**: Business logic is now isolated and easier to unit test
- **Cleaner Controller Code**: Controllers are more focused and maintainable with minimal dependencies
- **Dependency Injection**: Proper registration of facades in DI container
- **Reduced Controller Complexity**: Controllers now have only two dependencies (facade + ExceptionBoundary)
- **Consistent Architecture**: All controllers follow the same pattern for maintainability

### Enhanced Error Handling
- **Centralized Exception Management**: All exceptions are handled by ExceptionBoundary at controller level
- **Removed Redundant Try-Catch**: Eliminated all try-catch blocks from services and facades for cleaner code
- **Automatic Error Propagation**: Exceptions bubble up automatically to ExceptionBoundary
- **User-Friendly Responses**: Users receive meaningful error messages that help identify the root cause of issues
- **Consistent Error Format**: All errors follow a standardized format with clear descriptions and context
- **AWS Credentials Fix**: Fixed production issue where AWS S3 operations were failing due to incorrect credential configuration - now uses explicit credentials from appsettings.json

## General Description
This ASP.NET Core application manages user configuration, file uploads to AWS S3, and embedding management via Pinecone. The code has been fully refactored to retain only the features actually used by the frontend, ensuring clarity, maintainability, and optimal performance.

## API Endpoints Used by the Frontend

### Authentication Endpoints (Port 5140)
- **GET /api/auth/me** - Verify user authentication
- **POST /api/auth/logout** - User logout

### Configuration Endpoints (Port 5196)
- **GET /api/users/{userId}/configuration** - Load user configuration
- **PUT /api/users/{userId}/configuration** - Save user configuration (including knowledge rules and files)

### Unanswered Questions Endpoints (Port 5196)
- **GET /api/unanswered-questions** - Retrieve unanswered questions
- **DELETE /api/unanswered-questions/{questionId}** - Delete a question

### External Chat Endpoint
- **POST https://n8n-alessio-marchese.com/webhook/chat** - Send chat message and receive response

## Main Features

### Authentication and Session Management
- JWT-based authentication via cookie
- Automatic token validation via custom middleware
- Access control to ensure each user accesses only their own data

### Custom AI Configuration
- **Knowledge Rules**: Custom knowledge rules for the AI
- **Tone Rules**: AI behavior and tone rules
- **File Upload**: Support for PDF, DOCX, and TXT file uploads
- **Granular Management**: Add, edit, and remove individual rules

### AI Assistant Chat
- Integration with external endpoint for AI conversations
- Use of custom configurations to personalize responses

### Knowledge Base Management
- System for tracking unanswered questions
- Deletion of irrelevant questions

## Project Structure

### Controllers
- **UsersController**: Manages complete user configurations (GET/PUT) including knowledge rules and files
- **UnansweredQuestionsController**: Manages unanswered questions (GET, DELETE)

### Facades
- **UsersFacade**: Business logic layer for user configuration operations, separating concerns from controllers
- **UnansweredQuestionsFacade**: Business logic layer for unanswered questions operations, separating concerns from controllers

### Services
- **SqliteDataService**: Handles data persistence with SQLite database
- **UserConfigService**: Parsing and serialization of user configuration
- **IS3StorageService/S3StorageService**: AWS S3 storage management
- **PineconeService**: Pinecone embeddings management
- **IExceptionBoundary/ExceptionBoundary**: Service iniettabile per gestione centralizzata delle eccezioni e conversione automatica dei Result in risposte HTTP
- **CookieJwtValidationMiddleware**: Custom middleware for JWT validation

### Data Models
- **UserConfiguration**: Complete user configuration
- **KnowledgeRule**: Knowledge rule
- **UnansweredQuestion**: Unanswered question
- **File**: File uploaded by the user

### DTOs (Data Transfer Objects)
- **FileRequest**: File upload request (includes Base64 content)
- **FileResponse**: File response data (includes Base64 content)
- **KnowledgeRuleRequest**: Knowledge rule request
- **KnowledgeRuleResponse**: Knowledge rule response
- **UserConfigurationResponse**: Complete user configuration response
- **UpdateUserConfigurationRequest**: Configuration update request

## Supported File Formats

The application automatically extracts text from the following formats:

- **PDF (.pdf)**: Text extraction via PdfPig
- **Word (.docx)**: Text extraction via DocX
- **Plain text (.txt)**: Direct reading
- **Other formats**: Fallback to text reading

## Main Refactor Improvements

### Removal of Unused Code
- Removed unused controllers (KnowledgeRulesController, FilesController)
- Removed endpoints not used by the frontend
- Simplified SqliteDataService by removing unnecessary methods
- Removed unused request/response models

### File Content Management
- **Full Save**: File content is now saved in the SQLite database (previously was set as an empty string)
- **Content Retrieval**: The configuration GET now returns the full file content
- **Structured DTOs**: New DTOs (FileResponse, UserConfigurationResponse) for more structured responses
- **Compatibility**: Maintained compatibility with the existing frontend

### Performance Optimization
- Removed unnecessary S3 sync logic
- Simplified database transaction management
- Reduced excessive logging
- Removed unused file parsing methods

### Improved Maintainability
- Cleaner, more focused code
- Clear separation of responsibilities
- Updated and accurate documentation
- Simplified modular structure

### Upgrade to .NET 8.0
- Migration from .NET 9.0 to .NET 8.0 (LTS)
- Updated all dependencies for compatibility
- Replaced Microsoft.AspNetCore.OpenApi with Swashbuckle.AspNetCore
- Improved stability and long-term support

## Production Deployment

### 🚀 Deployment Options

> **Note**: The project is configured for direct deployment on a Linux VM without Docker to simplify management and reduce complexity. All Docker configurations have been removed.

#### 1. **Direct Deployment on Linux VM**
```bash
# Clone the repository on the VM
git clone <repository-url>
cd RAG

# Make the deployment script executable
chmod +x deploy-production.sh

# Configure environment variables (OPTIONAL - override appsettings.Production.json)
export AWS_ACCESS_KEY_ID="your-aws-key"
export AWS_SECRET_ACCESS_KEY="your-aws-secret"
export AWS_BUCKET_NAME="your-bucket-name"
export JWT_KEY="your-jwt-key"
export JWT_ISSUER="your-issuer"
export JWT_AUDIENCE="your-audience"
export PINECONE_API_KEY="your-pinecone-key"
export PINECONE_INDEX_HOST="your-pinecone-host"

# Run the deployment
./deploy-production.sh

# The application will be available at http://<IP-VM>:5000
```

### 🔧 Production Configuration

#### 📋 Configuration Hierarchy

The application follows this configuration hierarchy (from lowest to highest):

1. **`appsettings.json`** - Base configuration
2. **`appsettings.Production.json`** - Production configuration (overrides appsettings.json)
3. **Environment variables** - Override configuration files

#### ⚙️ Configuration in appsettings.Production.json

The `appsettings.Production.json` file already contains default values for production:

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

**To use only appsettings.Production.json:**
1. Edit the values directly in the file
2. Do not set environment variables
3. The application will use the file values

**To use environment variables (RECOMMENDED for security):**
1. Leave placeholder values in appsettings.Production.json
2. Set environment variables with the real values
3. Environment variables will override the file values

#### 🔐 Environment Variables (Recommended)

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

**Advantages of environment variables:**
- ✅ Not committed to the repository
- ✅ More secure for managing secrets
- ✅ Easy to change without editing files
- ✅ Standard for containerized deployments

#### 🔧 Firewall Configuration (Linux)
```bash
# Enable port 5000
sudo ufw allow 5000/tcp
```

### 📊 Monitoring and Management (Linux)
```bash
# Check status
sudo systemctl status rag-api.service

# Real-time logs
sudo journalctl -u rag-api.service -f

# Restart service
sudo systemctl restart rag-api.service

# Stop service
sudo systemctl stop rag-api.service
```

### 🌐 Application Access

After deployment, the application will be available at:
- **URL**: http://your-vm-ip:5000
- **Health Check**: http://your-vm-ip:5000/health
- **API Endpoints**: http://your-vm-ip:5000/api/*

### 🔒 Production Security

1. **Firewall**: Configure the firewall to allow only port 5000
2. **HTTPS**: Use a reverse proxy (nginx/Apache) with SSL
3. **Secrets**: Use environment variables for secrets (do not commit to the repository)
4. **Updates**: Keep .NET and dependencies up to date
5. **Monitoring**: Configure logging and monitoring

## Configuration

### appsettings.json (Development)
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

### appsettings.Production.json (Production)
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

### Environment Variables (Override appsettings)
- **AWS_ACCESS_KEY_ID**: AWS access key
- **AWS_SECRET_ACCESS_KEY**: AWS secret key
- **AWS_REGION**: AWS region (e.g. us-east-1)
- **AWS_BUCKET_NAME**: S3 bucket name
- **JWT_KEY**: JWT signing key
- **JWT_ISSUER**: JWT issuer
- **JWT_AUDIENCE**: JWT audience
- **PINECONE_API_KEY**: Pinecone API key
- **PINECONE_INDEX_HOST**: Pinecone index host

## Usage Examples

### User Configuration
```bash
# Retrieve configuration (includes file content)
GET /api/users/{userId}/configuration

# Response:
{
  "userId": "user-guid",
  "knowledgeRules": [
    {
      "id": "kr-1",
      "content": "Rule content"
    }
  ],
  "files": [
    {
      "id": "f-1",
      "name": "document.pdf",
      "contentType": "application/pdf",
      "size": 1024000,
      "content": "base64-encoded-content"
    }
  ]
}

# Update configuration
PUT /api/users/{userId}/configuration
{
  "knowledgeRules": [
    {
      "id": "kr-1",
      "content": "Rule content"
    }
  ],
  "files": [
    {
      "id": "f-1",
      "name": "document.pdf",
      "contentType": "application/pdf",
      "size": 1024000,
      "content": "base64-encoded-content"
    }
  ]
}
```

### Unanswered Questions Management
```bash
# List questions
GET /api/unanswered-questions

# Delete question
DELETE /api/unanswered-questions/{questionId}
```

### File Upload
```bash
# Upload configuration
POST /api/files/upload
Content-Type: multipart/form-data
```

## Architecture Patterns

### Facade Pattern: Business Logic Separation

The application uses the Facade pattern to separate business logic from controllers, improving maintainability and testability.

#### How it works
- **Controllers**: Handle HTTP concerns (routing, authorization, request/response formatting)
- **Facades**: Handle business logic and orchestrate service calls
- **Services**: Handle data access and external integrations

#### Benefits
- **Separation of Concerns**: Clear boundaries between layers
- **Testability**: Business logic can be tested independently
- **Maintainability**: Changes to business logic don't affect HTTP layer
- **Reusability**: Facades can be used by different controllers if needed

#### Current Implementation
- **UsersFacade**: Handles all user configuration business logic including:
  - Retrieving user configuration with automatic initialization
  - Updating user configuration with Pinecone embeddings cleanup
  - S3 storage synchronization
  - Database operations orchestration
- **IUsersFacade**: Interface for dependency injection and testing
- **Registration**: Properly registered in DI container in Program.cs
- **Controller Simplification**: UsersController now has only one dependency and focuses purely on HTTP concerns

#### Result: Ultra-Clean Controller
```csharp
[ApiController]
[Route("api/[Controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUsersFacade _usersFacade;

    public UsersController(IUsersFacade usersFacade)
    {
        _usersFacade = usersFacade;
    }

    [HttpGet("{userId}/configuration")]
    public Task<IActionResult> GetUserConfiguration(Guid userId)
    {
        return ExceptionBoundary.RunWithAuthorizationAsync(this, userId, async () =>
        {
            var response = await _usersFacade.GetUserConfigurationAsync(userId);
            return Ok(response);
        });
    }

    [HttpPut("{userId}/configuration")]
    public Task<IActionResult> UpdateUserConfiguration(Guid userId, [FromBody] UpdateUserConfigurationRequest request)
    {
        return ExceptionBoundary.RunWithValidationAndAuthorizationAsync(this, userId, async () =>
        {
            var success = await _usersFacade.UpdateUserConfigurationAsync(userId, request);
            return success ? Ok(new SuccessResponse { Message = "Configuration updated successfully" })
                         : StatusCode(500, new ErrorResponse { Message = "Update failed" });
        });
    }
}
```

### Session-Based Architecture: Simplified User Context

The application uses a session-based architecture to automatically provide user context throughout the application layers, eliminating the need for manual user ID passing.

#### How it works
- **SessionService**: Centralized service that extracts user ID from JWT claims
- **Automatic Context**: User ID is automatically available in facades and services
- **Simplified Controllers**: No need to pass userId parameters in endpoints
- **Security**: Users can only access their own data through session context
- **Clean API**: Endpoints are more intuitive and RESTful

#### Benefits
- **Simplified Controllers**: No more complex authorization logic
- **Automatic Security**: Users can only access their own data
- **Cleaner API**: Endpoints don't need userId parameters
- **Reduced Complexity**: Less boilerplate code
- **Better UX**: More intuitive API design

#### Usage Examples
```csharp
// Controller - No userId parameter needed
[HttpGet("configuration")]
public Task<IActionResult> GetUserConfiguration()
{
    return ExceptionBoundary.RunWithResultAsync(async () =>
    {
        return await _usersFacade.GetUserConfigurationAsync();
    });
}

// Facade - User ID automatically available
public async Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync()
{
    var userId = _sessionService.GetCurrentUserId();
    if (!userId.HasValue)
    {
        return Result<UserConfigurationResponse>.Error("User not authenticated", 401);
    }
    // Use userId.Value for operations...
}

// Service - Receives userId from facade
public async Task<Result<UserConfiguration>> GetUserConfigurationAsync(Guid userId)
{
    // Database operations with userId...
}
```

### Result Pattern: Type-Safe Operation Results

The application uses the Result pattern to provide type-safe operation results and automatic error propagation throughout the application layers.

#### How it works
- **Result<T>**: Generic result type that can contain either a success value or an error
- **SuccessResult<T>**: Contains a successful operation result
- **ErrorResult<T>**: Contains error information with message and status code
- **Automatic Propagation**: Errors automatically propagate from services to controllers
- **Type Safety**: Compile-time checking ensures proper result handling

#### Benefits
- **Type Safety**: Compile-time checking of operation results
- **Null Safety**: Eliminates null reference exceptions
- **Consistent Error Handling**: All errors follow the same format
- **Automatic Propagation**: Errors bubble up automatically
- **Testability**: Easy to test success and error scenarios
- **Maintainability**: Clear error flow and handling

#### Usage Examples
```csharp
// Service layer
public async Task<Result<UserConfiguration>> GetUserConfigurationAsync(Guid userId)
{
    try
    {
        var config = await _context.UserConfigurations.FindAsync(userId);
        return Result<UserConfiguration>.Success(config);
    }
    catch (Exception ex)
    {
        return Result<UserConfiguration>.Error($"Error: {ex.Message}", 500);
    }
}

// Facade layer
public async Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync()
{
    var userId = _sessionService.GetCurrentUserId();
    if (!userId.HasValue)
    {
        return Result<UserConfigurationResponse>.Error("User not authenticated", 401);
    }
    
    var result = await _dataService.GetUserConfigurationAsync(userId.Value);
    if (!result.IsSuccess)
    {
        return Result<UserConfigurationResponse>.Error(result.ErrorMessage!, result.StatusCode ?? 500);
    }
    // Process success result...
}

// Controller layer
public Task<IActionResult> GetUserConfiguration()
{
    return ExceptionBoundary.RunWithResultAsync(_usersFacade.GetUserConfigurationAsync);
}
```

### ExceptionBoundary: Simplified Exception Handling

The project uses a simplified `ExceptionBoundary` pattern that centralizes exception handling and Result pattern conversion, making controllers clean and focused.

#### Available Methods
- **`RunAsync`**: Basic exception handling
#### Metodi Disponibili
- **`RunAsync<T>(Func<Task<Result<T>>> action)`**: Per funzioni che restituiscono `Result<T>`
- **`RunAsync(Func<Task<Result>> action)`**: Per funzioni che restituiscono `Result`

#### Come Funziona
- I controller non scrivono più blocchi try-catch per la gestione degli errori
- La logica dell'endpoint viene passata al metodo appropriato dell'ExceptionBoundary
- La conversione del pattern Result viene gestita automaticamente
- I controller si concentrano solo sull'orchestrazione delle chiamate ai facade
- **Messaggi di Errore Dettagliati**: Tutti i servizi includono messaggi di errore specifici

#### Esempi di Utilizzo

```csharp
// Controller con iniezione dell'ExceptionBoundary
public class UsersController : ControllerBase
{
    private readonly IUsersFacade _usersFacade;
    private readonly IExceptionBoundary _exceptionBoundary;

    public UsersController(IUsersFacade usersFacade, IExceptionBoundary exceptionBoundary)
    {
        _usersFacade = usersFacade;
        _exceptionBoundary = exceptionBoundary;
    }

    // Per funzioni che restituiscono Result<T>
    [HttpGet("configuration")]
    public Task<IActionResult> GetUserConfiguration()
        => _exceptionBoundary.RunAsync(_usersFacade.GetUserConfigurationAsync);

    // Per funzioni che restituiscono Result
    [HttpPut("configuration")]
    public Task<IActionResult> UpdateUserConfiguration([FromBody] UpdateUserConfigurationRequest request)
        => _exceptionBoundary.RunAsync(() => _usersFacade.UpdateUserConfigurationAsync(request));
}

// Esempio con UnansweredQuestionsFacade
public class UnansweredQuestionsController : ControllerBase
{
    private readonly IUnansweredQuestionsFacade _unansweredQuestionsFacade;
    private readonly IExceptionBoundary _exceptionBoundary;

    public UnansweredQuestionsController(IUnansweredQuestionsFacade unansweredQuestionsFacade, IExceptionBoundary exceptionBoundary)
    {
        _unansweredQuestionsFacade = unansweredQuestionsFacade;
        _exceptionBoundary = exceptionBoundary;
    }

    [HttpGet]
    public Task<IActionResult> GetUnansweredQuestions()
        => _exceptionBoundary.RunAsync(_unansweredQuestionsFacade.GetUnansweredQuestionsAsync);

    [HttpDelete("{questionId}")]
    public Task<IActionResult> DeleteUnansweredQuestion(Guid questionId)
        => _exceptionBoundary.RunAsync(() => _unansweredQuestionsFacade.DeleteUnansweredQuestionAsync(questionId));
}
```

### Enhanced Error Handling

The application now provides detailed error messages for all operations:

- **Database Operations**: Specific messages for database connection issues, constraint violations, etc.
- **S3 Operations**: Detailed error messages for file upload/download failures, bucket access issues, etc.
- **Pinecone Operations**: Specific messages for embedding deletion failures, API communication issues, etc.
- **File Processing**: Detailed error messages for file format issues, extraction failures, etc.

### Vantaggi della Nuova Implementazione
- **Servizio Iniettabile**: Facilmente testabile e sostituibile tramite DI
- **Adattamento Automatico**: Un solo metodo che si adatta al tipo di ritorno
- **Centralizzazione**: Tutta la gestione delle eccezioni è in un unico punto
- **Pulizia**: I controller sono più leggibili e mantenibili
- **Personalizzazione**: Facile cambiare la logica di gestione degli errori in futuro
- **Debugging**: Messaggi di errore dettagliati aiutano a identificare e risolvere problemi rapidamente
- **User Experience**: Gli utenti ricevono messaggi di errore significativi invece di generici
- **Type Safety**: Controllo dei tipi a compile-time per i Result
- **Codice Ultra-Pulito**: Rimossi tutti i try-catch ridondanti da servizi e facade

Il servizio è registrato in `Program.cs` e implementato in `Services/ExceptionBoundary.cs`.

### Rimozione Try-Catch Ridondanti

Tutti i try-catch sono stati rimossi da:
- **Services**: SqliteDataService, S3StorageService, PineconeService, UserConfigService
- **Facades**: UsersFacade, UnansweredQuestionsFacade
- **Controllers**: Già puliti grazie all'ExceptionBoundary

**Eccezioni mantenute:**
- **ExceptionBoundary**: Gestione centralizzata delle eccezioni
- **CookieJwtValidationMiddleware**: Gestione specifica errori JWT

**Vantaggi:**
- **Codice più pulito**: Meno boilerplate e più leggibilità
- **Performance**: Meno overhead di try-catch annidati
- **Manutenibilità**: Gestione errori centralizzata
- **Debugging**: Stack trace più chiari senza livelli multipli di catch

## Dependencies
- .NET 8.0 (LTS)
- AWS SDK S3 (3.7.306)
- Pinecone API (via HttpClient)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Entity Framework Core SQLite (8.0.0)
- Swashbuckle.AspNetCore (6.5.0)
- UglyToad.PdfPig (for PDF)
- Xceed.Words.NET (for DOCX)
- **Result Pattern**: Implementazione personalizzata in `Entities/Result.cs`

## Security
- **JWT Authentication**: Validation via custom middleware
- **Authorization**: User access control
- **Input Validation**: Complete input data validation
- **File Management**: File type and size checks
- **Secure Configuration**: Keys via appsettings.json and environment variables

## Testing
To test the application:
1. Configure AWS environment variables
2. Update appsettings.json with JWT and Pinecone configurations
3. Start the application with `dotnet run`
4. The SQLite database will be created automatically
5. Swagger UI available at `/swagger` in development environment

## Final Notes
The application is now fully optimized and aligned with frontend requirements. The refactor has removed all unnecessary code, retaining only essential features, significantly improving performance and maintainability. Migration to .NET 8.0 ensures stability and long-term support. Production deployment has been simplified by removing Docker and configuring direct deployment on Linux VM to reduce complexity and improve management.

For questions or contributions, edit this file or open an issue. 