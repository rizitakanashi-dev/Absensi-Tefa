namespace Absensi.Models
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public static class ErrorCodes
    {
        // Authentication & Authorization (1xxx)
        public const string AUTH_INVALID_CREDENTIALS = "AUTH_1001";
        public const string AUTH_TOKEN_EXPIRED = "AUTH_1002";
        public const string AUTH_TOKEN_INVALID = "AUTH_1003";
        public const string AUTH_UNAUTHORIZED = "AUTH_1004";
        public const string AUTH_ADMIN_EXISTS = "AUTH_1005";

        // Validation (2xxx)
        public const string VALIDATION_REQUIRED_FIELD = "VAL_2001";
        public const string VALIDATION_INVALID_FORMAT = "VAL_2002";
        public const string VALIDATION_INVALID_DATE = "VAL_2003";

        // Database (3xxx)
        public const string DB_CONNECTION_ERROR = "DB_3001";
        public const string DB_QUERY_ERROR = "DB_3002";
        public const string DB_TRANSACTION_ERROR = "DB_3003";

        // Resource (4xxx)
        public const string RESOURCE_NOT_FOUND = "RES_4001";
        public const string RESOURCE_ALREADY_EXISTS = "RES_4002";

        // Server (5xxx)
        public const string SERVER_INTERNAL_ERROR = "SRV_5001";
        public const string SERVER_UNAVAILABLE = "SRV_5002";
    }
}
