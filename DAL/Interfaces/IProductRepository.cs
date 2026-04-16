using Domain.Entities;

namespace DAL.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync(int page, int pageSize);
        Task<Product?> GetByIdAsync(int id);
        Task<int> AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}