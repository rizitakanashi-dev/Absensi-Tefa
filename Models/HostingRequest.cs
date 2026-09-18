namespace Absensi.Models
{
    /// <summary>
    /// Request untuk create hosting request baru
    /// </summary>
    public class HostingRequestCreateDTO
    {
        public int IdProject { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? ProjectDescription { get; set; }
        public string? TechStack { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DocumentationUrl { get; set; }
    }

    /// <summary>
    /// Request untuk update hosting request (hanya field yang bisa diupdate user)
    /// </summary>
    public class HostingRequestUpdateDTO
    {
        public int IdProject { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? ProjectDescription { get; set; }
        public string? TechStack { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DocumentationUrl { get; set; }
    }

    /// <summary>
    /// Request untuk PM review (approve/reject)
    /// </summary>
    public class HostingRequestReviewDTO
    {
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request untuk DevOps update progress
    /// </summary>
    public class HostingRequestDevOpsUpdateDTO
    {
        public string? DevOpsNotes { get; set; }
        public string? HostingUrl { get; set; }
    }

    /// <summary>
    /// Response DTO lengkap dengan JOIN data
    /// </summary>
    public class HostingRequestDTO
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int IdProject { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? ProjectDescription { get; set; }
        public string? TechStack { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DocumentationUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? IdPmReviewer { get; set; }
        public string? PmReviewerName { get; set; }
        public string? PmNotes { get; set; }
        public DateTime? PmReviewedAt { get; set; }
        public int? IdDevOpsHandler { get; set; }
        public string? DevOpsHandlerName { get; set; }
        public string? DevOpsNotes { get; set; }
        public string? HostingUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response DTO ringkas untuk list
    /// </summary>
    public class HostingRequestListDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PmReviewerName { get; set; }
        public string? DevOpsHandlerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
