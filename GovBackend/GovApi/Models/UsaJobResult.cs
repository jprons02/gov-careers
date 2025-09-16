namespace GovApi.Models
{
    // A simple record (like a JS object schema) to hold job data
    public record JobResult(
        string Title,
        string Location,
        string Organization,
        string ApplyUrl,
        string SalaryMin,
        string SalaryMax,
        string SalaryType
    );
}
