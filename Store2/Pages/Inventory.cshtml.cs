using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace Store2.Pages
{
    [Authorize]
    public class InventoryModel : PageModel
    {
        private readonly ILogger<InventoryModel> _logger;
        private readonly IConfiguration _configuration;

        public InventoryModel(ILogger<InventoryModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public List<Item> Items { get; set; } = new List<Item>();
        public Dictionary<int, int> Counts { get; set; } = new Dictionary<int, int>();

        public async Task OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            string itemQuery = "SELECT DISTINCT [Id], [Name], [Description], [Price] FROM [dbo].[Items]";
            string countQuery = @"
                SELECT di.ItemId, di.Quantity 
                FROM [dbo].[DailyInventory] di
                WHERE CAST(di.Date AS DATE) = @Date";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand itemCmd = new SqlCommand(itemQuery, conn);
                conn.Open();
                using (SqlDataReader reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var item = new Item
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"])
                        };
                        Items.Add(item);
                        Counts[item.Id] = 0;
                    }
                }

                using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@Date", DateTime.Today);
                    using (SqlDataReader reader = await countCmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            int itemId = Convert.ToInt32(reader["ItemId"]);
                            int quantity = Convert.ToInt32(reader["Quantity"]);

                            if (Counts.ContainsKey(itemId))
                            {
                                Counts[itemId] = quantity;
                            }
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                
                foreach (var item in Request.Form.Keys)
                {
                    if (item.StartsWith("Counts[")) 
                    {
                        var itemIdString = item.Split('[', ']')[1]; 
                        if (int.TryParse(itemIdString, out int itemId) &&
                            int.TryParse(Request.Form[item], out int quantity)) 
                        {
                            Counts[itemId] = quantity; 
                        }
                    }
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    foreach (var kvp in Counts)
                    {
                        int itemId = kvp.Key;
                        int quantity = kvp.Value; 

                        string checkQuery = @"SELECT COUNT(*) 
                                      FROM [dbo].[DailyInventory] 
                                      WHERE ItemId = @ItemId 
                                      AND CAST(Date AS DATE) = @Date";

                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ItemId", itemId);
                            checkCmd.Parameters.AddWithValue("@Date", DateTime.Today);

                            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                            if (exists)
                            {
                                string updateQuery = @"UPDATE [dbo].[DailyInventory] 
                                               SET Quantity = @Quantity 
                                               WHERE ItemId = @ItemId 
                                               AND CAST(Date AS DATE) = @Date";

                                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@Quantity", quantity);
                                    updateCmd.Parameters.AddWithValue("@ItemId", itemId);
                                    updateCmd.Parameters.AddWithValue("@Date", DateTime.Today);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                string insertQuery = @"INSERT INTO [dbo].[DailyInventory] 
                                               (ItemId, Quantity, Date) 
                                               VALUES (@ItemId, @Quantity, @Date)";

                                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                                {
                                    insertCmd.Parameters.AddWithValue("@ItemId", itemId);
                                    insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                                    insertCmd.Parameters.AddWithValue("@Date", DateTime.Today);
                                    await insertCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                return RedirectToPage(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating daily inventory");
                return Page(); 
            }
        }
    }
}
