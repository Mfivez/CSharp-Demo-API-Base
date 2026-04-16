using DAL.Filters.Product;
using BLL.Interfaces;
using DAL.Interfaces;
using Domain.Entities;

namespace BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<Product>> GetAllProductsAsync(ProductFilter filter)
        {
            if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
            {
                throw new ArgumentException("Le prix minimum ne peut pas être supérieur au prix maximum.");
            }
            return await _productRepository.GetAllAsync(filter);
        }


        public async Task<Product> GetProductByIdAsync(int id)
        {
            Product? product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }

            return product;
        }

        public async Task<int> CreateProductAsync(Product product)
        {
            return await _productRepository.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            bool updated = await _productRepository.UpdateAsync(product);

            if (!updated)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            bool deleted = await _productRepository.DeleteAsync(id);

            if (!deleted)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }
        }
    }
}