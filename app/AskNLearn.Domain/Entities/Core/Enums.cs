namespace AskNLearn.Domain.Entities.Core
{
    public enum Role
    {
        Member,
        Admin
    }

    public enum UserStatus
    {
        Online,
        Offline
    }

    public enum VerificationRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

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
        AwaitingManualReview,
        Removed
    }

    public enum UserVerificationStatus
    {
        NotVerified,
        EmailVerified,
        IdentityVerified,
        Rejected
    }
}
