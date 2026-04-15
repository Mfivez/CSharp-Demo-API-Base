using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;

namespace DAL.Interfaces
{
    public interface IProductRepository
    {
        List<Product> GetAll();
        Product? GetById(int id);
        void Add(Product product);
        void Update(Product product);
        void Delete(int id);
    }
}
