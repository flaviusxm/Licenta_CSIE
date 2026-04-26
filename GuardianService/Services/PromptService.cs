namespace GuardianService.Services
{
    public interface IPromptService
    {
        string GetModerationPrompt(string content, string? title = null);
        string GetVerificationPrompt();
    }

    public class PromptService : IPromptService
    {
        public string GetModerationPrompt(string content, string? title = null)
        {
            return $@"Sistem: Ești un moderator de conținut pentru o platformă universitară.
Sarcină: Analizează textul de mai jos pentru limbaj licențios, hărțuire, spam sau conținut ilegal.

Titlu: {title ?? "N/A"}
Conținut: {content}

Răspunde STRICT în format JSON, fără alt text:
{{
  ""isSafe"": boolean,
  ""reason"": ""scurtă explicație în Română""
}}";
        }

        public string GetVerificationPrompt()
        {
            return @"Analizează această imagine a unei legitimații de student. 
EXTRAGE: 
1. Numele complet al studentului.
2. Facultatea/Universitatea.
3. Data expirării (sau anul vizat).

Dacă imaginea nu este o legitimație, setează isValid la false.
Răspunde STRICT în format JSON:
{
  ""isValid"": boolean,
  ""extractionDetails"": ""Nume: ..., Facultate: ..., Expiră: ..."",
  ""recommendation"": ""Aprobat/Respins - motiv""
}";
        }
    }
}
