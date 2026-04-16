using DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<Product>> GetAllAsync(int page, int pageSize)
        {
            var products = new List<Product>();

            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = @"
        SELECT Id, Name, Price
        FROM Products
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

            await using SqlCommand command = new SqlCommand(query, connection);

            int offset = (page - 1) * pageSize;

            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var product = new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                };

                products.Add(product);
            }

            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            Product? product = null;

            await using SqlConnection connection = new SqlConnection(_connectionString);
            const string query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                product = new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                };
            }

            return product;
        }

        public async Task<int> AddAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = """
                INSERT INTO Products (Name, Price)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Price)
                """;

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = "DELETE FROM Products WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }
    }
}