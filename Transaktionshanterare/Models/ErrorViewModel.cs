namespace Labb2infrastruktur.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    // Lägg till en egenskap för att hålla felmeddelandet
        public string? ErrorMessage { get; set; }
}
