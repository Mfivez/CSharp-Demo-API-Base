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
    }
}
