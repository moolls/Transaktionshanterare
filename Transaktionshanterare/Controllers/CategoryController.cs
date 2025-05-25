using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Xml.Linq;

namespace Labb2infrastruktur.Namespace
{
    public class CategoryController : Controller
    {
     SqliteConnection sqlite;

     public CategoryController()
     {
        sqlite = new SqliteConnection("Data Source=Labb2database.db");
     }
        // GET: CategoryController

     async Task<XElement> SQLResult(string query, string root, string nodeName)
        {
            var xml = new XElement(root);

            try
            {
                await sqlite.OpenAsync();

                using (var command = new SqliteCommand(query, sqlite))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var element = new XElement(nodeName);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = await reader.GetFieldValueAsync<object>(i) ?? "";
                            element.Add(new XElement(reader.GetName(i), value));
                        }
                        xml.Add(element);
                    }
                }
            }
            finally
            {
                await sqlite.CloseAsync();
            }

            return xml;
        }
        public ActionResult Index()
        {
            return View();
        }

    }

    
}