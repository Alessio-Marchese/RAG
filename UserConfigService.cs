using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using Xceed.Words.NET;

/// <summary>
/// Servizio per il parsing e la serializzazione della configurazione utente.
/// </summary>
public interface IUserConfigService
{
    /// <summary>
    /// Effettua il parsing della form di configurazione utente.
    /// </summary>
    Task<UserConfig> ParseUserConfigAsync(IFormCollection form);
    /// <summary>
    /// Serializza la configurazione utente in formato testuale.
    /// </summary>
    string SerializeUserConfig(UserConfig config);
}

/// <summary>
/// Implementazione di IUserConfigService.
/// </summary>
public class UserConfigService : IUserConfigService
{
    private string ExtractTextFromPdf(Stream pdfStream)
    {
        using var pdf = PdfDocument.Open(pdfStream);
        var sb = new StringBuilder();
        foreach (Page page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private string ExtractTextFromDocx(Stream docxStream)
    {
        using var ms = new MemoryStream();
        docxStream.CopyTo(ms);
        ms.Position = 0;
        using var doc = DocX.Load(ms);
        return doc.Text;
    }

    public async Task<UserConfig> ParseUserConfigAsync(IFormCollection form)
    {
        // Parsing fallback email
        var fallbackEmail = form["fallbackEmail"].FirstOrDefault();
        // Parsing delle regole di tono
        var toneRulesJson = form["toneRules"].FirstOrDefault();
        var toneRules = !string.IsNullOrEmpty(toneRulesJson)
            ? JsonSerializer.Deserialize<List<ToneRule>>(toneRulesJson)
            : new List<ToneRule>();

        // Parsing delle knowledge rules (testo o file)
        var knowledgeRules = new List<KnowledgeRuleDto>();
        int n = 0;
        while (form.ContainsKey($"knowledgeRules[{n}][type]"))
        {
            var type = form[$"knowledgeRules[{n}][type]"].FirstOrDefault();
            if (type == "text")
            {
                var content = form[$"knowledgeRules[{n}][content]"].FirstOrDefault();
                knowledgeRules.Add(new KnowledgeRuleDto { Type = "text", Content = content });
            }
            else if (type == "file")
            {
                var file = form.Files[$"knowledgeRules[{n}][file]"];
                var fileName = form[$"knowledgeRules[{n}][fileName]"].FirstOrDefault();
                if (file != null)
                {
                    string fileContent;
                    var ext = fileName != null ? Path.GetExtension(fileName).ToLowerInvariant() : string.Empty;
                    if (file.ContentType == "application/pdf" || ext == ".pdf")
                    {
                        fileContent = ExtractTextFromPdf(file.OpenReadStream());
                    }
                    else if (file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || ext == ".docx")
                    {
                        fileContent = ExtractTextFromDocx(file.OpenReadStream());
                    }
                    else if (file.ContentType == "text/plain" || ext == ".txt")
                    {
                        using var reader = new StreamReader(file.OpenReadStream());
                        fileContent = await reader.ReadToEndAsync();
                    }
                    else
                    {
                        // Fallback: tenta di leggere come testo
                        using var reader = new StreamReader(file.OpenReadStream());
                        fileContent = await reader.ReadToEndAsync();
                    }
                    knowledgeRules.Add(new KnowledgeRuleDto { Type = "file", FileName = fileName, Content = fileContent });
                }
            }
            n++;
        }

        return new UserConfig
        {
            FallbackEmail = fallbackEmail,
            ToneRules = toneRules,
            KnowledgeRules = knowledgeRules
        };
    }

    public string SerializeUserConfig(UserConfig config)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"UserId: {config.UserId}");
        if (!string.IsNullOrEmpty(config.FallbackEmail))
            sb.AppendLine($"FallbackEmail: {config.FallbackEmail}");
        if (config.ToneRules != null && config.ToneRules.Count > 0)
        {
            sb.AppendLine("ToneRules:");
            foreach (var rule in config.ToneRules)
            {
                sb.AppendLine($"- {rule.Content}");
            }
        }
        if (config.KnowledgeRules != null && config.KnowledgeRules.Count > 0)
        {
            sb.AppendLine("KnowledgeRules:");
            foreach (var rule in config.KnowledgeRules)
            {
                if (rule.Type == "text")
                {
                    sb.AppendLine($"- [text] {rule.Content}");
                }
                else if (rule.Type == "file")
                {
                    sb.AppendLine($"- [file] {rule.FileName}:");
                    sb.AppendLine(rule.Content);
                }
            }
        }
        return sb.ToString();
    }
} 