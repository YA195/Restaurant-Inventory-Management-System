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
    public class AddItemModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AddItemModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public Item Item { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();

        public async Task OnGetAsync()
        {
            await LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            string query = "SELECT * FROM [dbo].[Items]";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
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
                            Date = Convert.ToDateTime(reader["Date"])
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                string query = "DELETE FROM [dbo].[Items] WHERE Id = @Id";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id, string description = null, decimal? price = null)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var updates = new List<string>();
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(description))
                {
                    updates.Add("Description = @Description");
                    parameters.Add(new SqlParameter("@Description", description));
                }

                if (price.HasValue)
                {
                    updates.Add("Price = @Price");
                    parameters.Add(new SqlParameter("@Price", price.Value));
                }

                if (updates.Count == 0)
                {
                    return new JsonResult(new { success = false, message = "No fields to update." });
                }

                string query = $"UPDATE [dbo].[Items] SET {string.Join(", ", updates)}, Date = @Date WHERE Id = @Id";
                parameters.Add(new SqlParameter("@Date", DateTime.Now));
                parameters.Add(new SqlParameter("@Id", id));

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }




        public async Task<IActionResult> OnPostAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            string checkQuery = "SELECT COUNT(*) FROM [dbo].[Items] WHERE Name = @Name";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@Name", Item.Name);
                conn.Open();

                int count = (int)await checkCmd.ExecuteScalarAsync();

                if (count > 0)
                {
                    ModelState.AddModelError(string.Empty, "An item with this name already exists.");
                    await LoadItemsAsync();  
                    return Page();  
                }
            }

            string query;

            if (Item.Id == 0)
            {
                query = @"INSERT INTO [dbo].[Items] (Name, Description, Price, Date) 
                  VALUES (@Name, @Description, @Price, @Date)";
            }
            else
            {
                query = @"UPDATE [dbo].[Items] 
                  SET Name = @Name, Description = @Description, Price = @Price, Date = @Date 
                  WHERE Id = @Id";
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", Item.Name);
                cmd.Parameters.AddWithValue("@Description", Item.Description);
                cmd.Parameters.AddWithValue("@Price", Item.Price);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                if (Item.Id != 0)
                {
                    cmd.Parameters.AddWithValue("@Id", Item.Id);
                }

                conn.Open();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToPage("/AddItem");
        }
    }
}
