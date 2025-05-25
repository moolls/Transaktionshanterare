using Microsoft.CodeAnalysis.Elfie.Model.Strings;

public class Transaction
{
    public int TransactionID { get; set; }
    public string BookingDate { get; set; }
    public string TransactionDate { get; set; }
    public string Reference { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public int? CategoryID { get; set; }

    public string? Description { get; set; }

    
}

