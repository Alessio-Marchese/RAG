namespace RAG.Configuration
{
    public class AppConfiguration
    {
        public ConnectionStringsConfiguration ConnectionStrings { get; set; } = new();
        public AwsConfiguration AWS { get; set; } = new();
        public JwtConfiguration Jwt { get; set; } = new();
        public PineconeConfiguration Pinecone { get; set; } = new();
    }
} 