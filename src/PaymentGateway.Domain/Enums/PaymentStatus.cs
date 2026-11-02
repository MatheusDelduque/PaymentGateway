namespace PaymentGateway.Domain.Enums
{
    public enum PaymentStatus
    {
        Created = 0,
        Authorized = 1,
        Captured = 2,
        Completed = 3,
        Expired = 4,
        Refunded = 5,
        Canceled = 7,
        Declined = 8,
        Unknown = 99
    }
}
