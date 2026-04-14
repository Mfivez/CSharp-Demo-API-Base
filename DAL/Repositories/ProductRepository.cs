using System;
using System.Collections.Generic;
using System.Text;
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

        public List<Product> GetAll()
        {
            var products = new List<Product>();

            using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products";

            using SqlCommand command = new SqlCommand(query, connection);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
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

        public Product? GetById(int id)
        {
            Product? product = null;

            using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
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

    }
}
