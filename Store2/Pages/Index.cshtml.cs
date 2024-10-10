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
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Item> Items { get; set; } = new List<Item>();

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            if (SelectedDate == null)
            {
                SelectedDate = DateTime.Today;
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string query = @"SELECT i.[Name], i.[Description], di.[Quantity] 
                             FROM [dbo].[DailyInventory] di
                             JOIN [dbo].[Items] i ON di.[ItemId] = i.[Id]
                             WHERE CAST(di.[Date] AS DATE) = @Date";

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
                            Description = reader["Description"].ToString(), 
                            Quantity = Convert.ToInt32(reader["Quantity"])
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string query = @"DELETE FROM [dbo].[DailyInventory] WHERE ItemId = @Id AND CAST(Date AS DATE) = @Date";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Date", SelectedDate.Value.Date);

                conn.Open();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToPage("/Index", new { SelectedDate });
        }
    }
}
