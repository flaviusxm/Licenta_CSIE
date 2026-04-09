namespace GuardianService.DTOs
{
    public class ModerationRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Title { get; set; }
    }

    public class ModerationResponse
    {
        public bool IsSafe { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class VerificationRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
    }

    public class VerificationResponse
    {
        public bool IsValid { get; set; }
        public string ExtractionDetails { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
}
