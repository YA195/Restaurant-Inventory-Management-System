using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Store2.Pages
{
    [Authorize]
    public class AnalyticsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AnalyticsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Item> Items { get; set; } = new List<Item>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
        {
            StartDate = startDate;
            EndDate = endDate;

            if (StartDate.HasValue && EndDate.HasValue)
            {
                await LoadItemsBetweenDatesAsync(StartDate.Value, EndDate.Value);
            }
        }

        private async Task LoadItemsBetweenDatesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                string query = @"
                    SELECT 
                        i.Id, 
                        i.Name, 
                        i.Description, 
                        i.Price, 
                        di.Quantity 
                    FROM 
                        dbo.Items i 
                    JOIN 
                        dbo.DailyInventory di ON i.Id = di.ItemId 
                    WHERE 
                        di.Date >= @StartDate AND di.Date <= @EndDate";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    conn.Open();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Items.Clear(); 
                        while (reader.Read())
                        {
                            Items.Add(new Item
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Quantity = Convert.ToInt32(reader["Quantity"]) 
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading items: " + ex.Message);
            }
        }
    }

   
}
