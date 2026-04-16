using Domain.Entities;

namespace BLL.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync(int page, int pageSize);
        Task<Product> GetProductByIdAsync(int id);
        Task<int> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }
}