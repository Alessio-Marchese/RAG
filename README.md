# RAG - Refactored Application Guide

## Recent Updates

### Enhanced Error Handling (Latest)
- **Improved Exception Messages**: All services now provide detailed, specific error messages instead of generic responses
- **Better Debugging**: Database, S3, and Pinecone operations now include context-specific error information
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

### Services
- **SqliteDataService**: Handles data persistence with SQLite database
- **UserConfigService**: Parsing and serialization of user configuration
- **S3StorageService**: AWS S3 storage management
- **PineconeService**: Pinecone embeddings management
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

## ExceptionBoundary: Centralized Exception Handling in Controllers

To avoid duplicating try-catch blocks in controllers, the project uses the `ExceptionBoundary` pattern. This pattern centralizes exception handling and simplifies endpoint implementation.

### How it works
- Controllers no longer write try-catch blocks for error handling.
- The endpoint logic is passed to `ExceptionBoundary.RunAsync`, which handles exceptions and returns a consistent HTTP response.
- Common exceptions (e.g. `ArgumentException`, `UnauthorizedAccessException`) are mapped to appropriate HTTP responses (400, 401, etc.), while others result in a 500 response.
- **Enhanced Error Messages**: All services now include specific error messages in their exceptions, providing detailed information about what went wrong during operations.

### Usage Example

```csharp
[HttpGet("{userId}/configuration")]
public Task<IActionResult> GetUserConfiguration(Guid userId)
{
    return ExceptionBoundary.RunAsync(async () =>
    {
        // ... endpoint logic ...
    });
}
```

### Enhanced Error Handling

The application now provides detailed error messages for all operations:

- **Database Operations**: Specific messages for database connection issues, constraint violations, etc.
- **S3 Operations**: Detailed error messages for file upload/download failures, bucket access issues, etc.
- **Pinecone Operations**: Specific messages for embedding deletion failures, API communication issues, etc.
- **File Processing**: Detailed error messages for file format issues, extraction failures, etc.

### Advantages
- **Centralization**: All exception handling is in one place.
- **Cleanliness**: Controllers are more readable and maintainable.
- **Customization**: It's easy to change error handling logic in the future.
- **Debugging**: Detailed error messages help identify and resolve issues quickly.
- **User Experience**: Users receive meaningful error messages instead of generic ones.

The class is located in `Services/ExceptionBoundary.cs`.

## Dependencies
- .NET 8.0 (LTS)
- AWS SDK S3 (3.7.306)
- Pinecone API (via HttpClient)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Entity Framework Core SQLite (8.0.0)
- Swashbuckle.AspNetCore (6.5.0)
- UglyToad.PdfPig (for PDF)
- Xceed.Words.NET (for DOCX)

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