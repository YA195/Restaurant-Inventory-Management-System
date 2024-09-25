using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Store2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // List to hold items from the database
        public List<Item> Items { get; set; } = new List<Item>();

        // Bind property for the selected date (default to today's date)
        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            if (SelectedDate == null)
            {
                SelectedDate = DateTime.Today; // Default to today's date if not selected
            }

            // Retrieve connection string from configuration
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            string query = @"SELECT [Name], [Quantity] FROM [RestaurantInventory1].[dbo].[Items] 
                             WHERE CAST([Date] AS DATE) = @Date";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Date", SelectedDate.Value.Date);

                conn.Open();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        Items.Add(new Item
                        {
                            Name = reader["Name"].ToString(),
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });
                    }
                }
            }
        }
    }

    // Define the Item class to store item details
    public class Item
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
