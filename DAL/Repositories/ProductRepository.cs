using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task<List<Product>> GetAllAsync()
        {
            var products = new List<Product>();

            await using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products";

            await using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                });
            }

            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            Product? product = null;

            await using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

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

        public async Task AddAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            string query = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price)";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            string query = "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            string query = "DELETE FROM Products WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}