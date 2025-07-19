using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using Xceed.Words.NET;
using RAG.DTOs;

public interface IUserConfigService
{
    string SerializeUserConfigForS3(List<KnowledgeRuleRequest> knowledgeRules, List<FileRequest> files);
}

namespace RAG.Services
{
    public class UserConfigService : IUserConfigService
    {
        public string SerializeUserConfigForS3(List<KnowledgeRuleRequest> knowledgeRules, List<FileRequest> files)
        {
            var sb = new StringBuilder();
            
            if (knowledgeRules != null && knowledgeRules.Count > 0)
            {
                sb.AppendLine("KNOWLEDGE RULES:");
                foreach (var rule in knowledgeRules)
                {
                    sb.AppendLine($"- {rule.Content}");
                }
                sb.AppendLine();
            }
            
            if (files != null && files.Count > 0)
            {
                sb.AppendLine("FILE CONTENTS:");
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.Content))
                    {
                        sb.AppendLine($"--- {file.Name} ---");
                        
                        var extractedText = ExtractTextFromBase64File(file.Content, file.ContentType, file.Name);
                        sb.AppendLine(extractedText);
                        sb.AppendLine();
                    }
                }
            }
            
            return sb.ToString();
        }

#region PRIVATE METHODS
        private static string ExtractTextFromPdf(Stream pdfStream)
        {
            using var pdf = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();
            foreach (Page page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private static string ExtractTextFromDocx(Stream docxStream)
        {
            using var ms = new MemoryStream();
            docxStream.CopyTo(ms);
            ms.Position = 0;
            using var doc = DocX.Load(ms);
            return doc.Text;
        }
        private static string ExtractTextFromBase64File(string base64Content, string contentType, string fileName)
        {
            try
            {
                var fileBytes = Convert.FromBase64String(base64Content);
                using var stream = new MemoryStream(fileBytes);
                
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                
                if (contentType == "application/pdf" || ext == ".pdf")
                        {
                    return ExtractTextFromPdf(stream);
                        }
                else if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || ext == ".docx")
                        {
                    return ExtractTextFromDocx(stream);
                        }
                else if (contentType == "text/plain" || ext == ".txt")
                        {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                        }
                        else
                        {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                        }
            }
            catch (Exception ex)
        {
                return $"Error extracting text from file {fileName}: {ex.Message}";
                    }
                }
#endregion
    }
} 