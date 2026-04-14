using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;

namespace BLL.Interfaces
{
    public interface IProductService
    {
        List<Product> GetAllProducts();
        Product? GetProductById(int id);
    }
}
