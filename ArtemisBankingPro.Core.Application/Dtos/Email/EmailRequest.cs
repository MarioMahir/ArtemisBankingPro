namespace ArtemisBankingPro.Core.Application.Dtos.Email;

public class EmailRequest
{
    public string To { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
}
