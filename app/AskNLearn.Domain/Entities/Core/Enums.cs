namespace AskNLearn.Domain.Entities.Core
{
    public enum ReportReason
    {
        Spam,
        Harassment,
        Inappropriate,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        Resolved,
        Dismissed
    }

    public enum ModerationStatus
    {
        Pending,
        Approved,
        Flagged,
        AwaitingManualReview
    }
}
