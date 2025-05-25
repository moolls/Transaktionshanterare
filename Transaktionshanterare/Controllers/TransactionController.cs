using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Runtime.CompilerServices;
using Labb2infrastruktur.Models;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Labb2infrastruktur.Namespace
{
    public class TransactionController : Controller
    {
        private static List<Transaction> transactions = new List<Transaction>(); 

        SqliteConnection sqlite;

        public TransactionController()
        {
            sqlite = new SqliteConnection("Data Source=labb2db.db");

        }
        public ActionResult Index()
        {
            ConnectAPI();
            return View();
        }

        public void ConnectAPI()
{
    if (transactions.Any()) 
    {
        return;
    }

    try
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer 70a8da5a42280be32ff31f57a7c4f0286734d17e");
            string jsonResult = string.Empty;

            using (HttpResponseMessage response = client.GetAsync("https://bank.stuxberg.se/api/iban/SE4550000000058398257466/").Result)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("API:et är för tillfället otillgängligt. Vänligen försök igen senare.");
                }

                jsonResult = response.Content.ReadAsStringAsync().Result;
                transactions = JsonSerializer.Deserialize<List<Transaction>>(jsonResult) ?? new List<Transaction>();
            }
        }
    }
    catch (Exception ex)
    {

        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return; 
    }

    SaveTransactionsToDatabase(transactions);
}


private List<Rule> GetRules()
{
    List<Rule> rules = new List<Rule>();
    try{

    
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        connection.Open();

        string query = "SELECT Reference, CategoryID FROM Rules";
        using (var command = new SqliteCommand(query, connection))
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rules.Add(new Rule
                    {
                        Reference = reader["Reference"].ToString()!,
                        CategoryID = Convert.ToInt32(reader["CategoryID"])
                    });
                }
            }
        }
    }
    }
    catch (Exception ex)
    {

        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }

    return rules;
}


private void SaveTransactionsToDatabase(List<Transaction> transactions)
{
    
    List<Rule> rules = GetRules();

    try{
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        connection.Open();

        foreach (var transaction in transactions)
        {
            // Standardvärde: "Övrigt"
            int categoryId = 1;

            
            foreach (var rule in rules)
            {
                if (transaction.Reference.Equals(rule.Reference, StringComparison.OrdinalIgnoreCase))
                {
                    categoryId = rule.CategoryID;
                    break; 
                }
            }

            
            string insertQuery = @"
                INSERT INTO [Transaction] 
                    (TransactionID, BookingDate, TransactionDate, Reference, Amount, Balance, CategoryID)
                VALUES 
                    (@TransactionID, @BookingDate, @TransactionDate, @Reference, @Amount, @Balance, @CategoryID)
                ON CONFLICT(TransactionID) DO NOTHING";

            using (var command = new SqliteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@TransactionID", transaction.TransactionID);
                command.Parameters.AddWithValue("@BookingDate", transaction.BookingDate);
                command.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate);
                command.Parameters.AddWithValue("@Reference", transaction.Reference);
                command.Parameters.AddWithValue("@Amount", transaction.Amount);
                command.Parameters.AddWithValue("@Balance", transaction.Balance);
                command.Parameters.AddWithValue("@CategoryID", categoryId);

                command.ExecuteNonQuery();
            }
        }
    }
    }
    catch (Exception ex)
    {
        
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return; 
    }
}



public async Task<string> GetDataFromDatabaseAsync(string query)
{
    List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

    try
    {
        await sqlite.OpenAsync();
        using (var command = new SqliteCommand(query, sqlite))
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }
    finally
    {
        await sqlite.CloseAsync();
    }

    
    string jsonString = JsonSerializer.Serialize(rows);
    return jsonString;
}

public async Task<IActionResult> DisplayTransactions()
{
    try{
    string query = "SELECT *, c.Description  FROM [Transaction] Left Join Category c ON [Transaction].CategoryID = c.Id";
    string jsonString = await GetDataFromDatabaseAsync(query);
    List<Transaction> transactions = JsonSerializer.Deserialize<List<Transaction>>(jsonString) ?? new List<Transaction>();
   return View(transactions);  
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
    }
   
}

 public async Task<IActionResult> Edit(int? id)
        {
            try{
            
            if (id == null)
            {
                return BadRequest("ID is required");
            }

            Transaction? transaction = transactions.FirstOrDefault(t => t.TransactionID == id);

            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }

               List<Category> categories = await GetCategories();
               ViewBag.Categories = new SelectList(categories, "Id", "Description");


            return View(transaction);

            }
        catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
        
    }
        }

        
 [HttpPost]
public async Task<IActionResult> AddCategory(string description)
{
    try{
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        await connection.OpenAsync();
        var query = "INSERT INTO Category (Description) VALUES (@Description)";
        using (var command = new SqliteCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Description", description);
            await command.ExecuteNonQueryAsync();
        }
        await connection.CloseAsync();
    }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }
    return RedirectToAction("Categories");
}

public async Task<IActionResult> AddCategoryToTransaction(int TransactionID, int CategoryID)
{
    try{
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        await connection.OpenAsync();

      
        var query = "UPDATE [Transaction] SET CategoryID = @CategoryID WHERE TransactionID = @TransactionID";
        
        using (var command = new SqliteCommand(query, connection))
        {
            
            command.Parameters.AddWithValue("@TransactionID", TransactionID);
            command.Parameters.AddWithValue("@CategoryID", CategoryID);
            
            
            await command.ExecuteNonQueryAsync();
        }

        await connection.CloseAsync();
    }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }

   
    return RedirectToAction("DisplayTransactions");
}

[HttpGet]

public async Task<List<Category>> GetCategories()
{
    try{
    
   string query = "SELECT * FROM Category";
    string jsonString = await GetDataFromDatabaseAsync(query);
    List<Category> categories = JsonSerializer.Deserialize<List<Category>>(jsonString) ?? new List<Category>();
    return categories;

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
    }
}

public async Task<IActionResult> Categories()
{
    try{
    List<Category> categories = await GetCategories(); 
    return View(categories); 
    }
   catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
    }
}


public async Task AddOrUpdateRule(string reference, int CategoryID)
{
    try{
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        await connection.OpenAsync();

        var checkQuery = "SELECT COUNT(*) FROM Rules WHERE Reference = @Reference";
        using (var command = new SqliteCommand(checkQuery, connection))
        {
            command.Parameters.AddWithValue("@Reference", reference);
            var count = (long)await command.ExecuteScalarAsync();

            if (count > 0)
            {
                var updateQuery = "UPDATE Rules SET CategoryID = @CategoryID WHERE Reference = @Reference";
                using (var updateCommand = new SqliteCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@Reference", reference);
                    updateCommand.Parameters.AddWithValue("@CategoryID", CategoryID);
                    await updateCommand.ExecuteNonQueryAsync();
                }
            }
            else
            {
                var insertQuery = "INSERT INTO Rules (Reference, CategoryID) VALUES (@Reference, @CategoryID)";
                using (var insertCommand = new SqliteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Reference", reference);
                    insertCommand.Parameters.AddWithValue("@CategoryID", CategoryID);
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }

        await connection.CloseAsync();
    }
    }
     catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }

}


[HttpPost]
public async Task<IActionResult> AddCategoryToTransactionsByReference(string reference, int CategoryID)
{
    try{
    using (var connection = new SqliteConnection("Data Source=labb2db.db"))
    {
        await connection.OpenAsync();
        
        string updateQuery = "UPDATE [Transaction] SET CategoryID = @CategoryID WHERE Reference = @Reference";
        using (var command = new SqliteCommand(updateQuery, connection))
        {
            command.Parameters.AddWithValue("@CategoryID", CategoryID);
            command.Parameters.AddWithValue("@Reference", reference);
            await command.ExecuteNonQueryAsync();
        }

        string selectQuery = "SELECT Description FROM Category WHERE Id = @CategoryID";
        using (var command = new SqliteCommand(selectQuery, connection))
        {
            command.Parameters.AddWithValue("@CategoryID", CategoryID);
            var result = await command.ExecuteScalarAsync();
            
            if (result is string description)
            {
                await AddOrUpdateRule(reference, CategoryID);
            }
            else
            {
                return BadRequest("Category description not found.");
            }
        }

        await connection.CloseAsync();
    }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }


    return RedirectToAction("DisplayTransactions");
}




    public async Task<IActionResult> CombinedReportViewModel()
{
    try{
    
    string categoryQuery = @"
        SELECT 
            c.Description,
            COUNT(*) AS Count,
            SUM(CASE WHEN t.Amount > 0 THEN t.Amount ELSE 0 END) AS TotalIncome,
            SUM(CASE WHEN t.Amount < 0 THEN t.Amount ELSE 0 END) AS TotalExpenses
        FROM [Transaction] t
        LEFT JOIN Category c ON t.CategoryID = c.Id
        GROUP BY c.Description";

    string categoryJson = await GetDataFromDatabaseAsync(categoryQuery);
    List<Report> categoryReports = JsonSerializer.Deserialize<List<Report>>(categoryJson) ?? new List<Report>();

    string totalQuery = @"
        SELECT 
            'Total' AS Description,
            COUNT(*) AS Count,
            SUM(CASE WHEN t.Amount > 0 THEN t.Amount ELSE 0 END) AS TotalIncome,
            SUM(CASE WHEN t.Amount < 0 THEN t.Amount ELSE 0 END) AS TotalExpenses
        FROM [Transaction] t";

    string totalJson = await GetDataFromDatabaseAsync(totalQuery);
    List<Report> totalReports = JsonSerializer.Deserialize<List<Report>>(totalJson) ?? new List<Report>();

    CombinedReportViewModel viewModel = new CombinedReportViewModel
    {
        ReportByCategory = categoryReports,
        TotalReport = totalReports
    
    };

    SaveViewModelToFile(viewModel);
    return View(viewModel);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
    }
}
[HttpPost]
private void SaveViewModelToFile(CombinedReportViewModel viewModel)
{
    try
    {
        string json = JsonSerializer.Serialize(viewModel, new JsonSerializerOptions { WriteIndented = true });
        
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/viewModel.json");
        System.IO.File.WriteAllText(filePath, json);
    }
     catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        
    }

}


 public IActionResult DownloadViewModel()
 {
     string filePath = "wwwroot/viewModel.json";
    try{
     if (!System.IO.File.Exists(filePath))
     {
         return NotFound("ViewModel file not found.");
     }

     byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
     return File(fileBytes, "application/json", "viewModel.json");
    }
     catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        Response.Redirect(Url.Action("Error", "Transaction", new { ErrorMessage = ex.Message }));
        return null;
    }
 }


public IActionResult Error(string errorMessage)
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = errorMessage
            };
            return View("Error", errorModel);
        }


    }
}