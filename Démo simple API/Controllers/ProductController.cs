using BLL.Interfaces;
using Démo_simple_API.DTO.Product;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Démo_simple_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductResponse>>> GetAll()
        {
            var products = await _productService.GetAllProductsAsync();

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            }).ToList();

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            };

            return Ok(response);
        }


        [HttpPost]
        public async Task<ActionResult> Create(ProductCreateRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price
            };

            await _productService.CreateProductAsync(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);

        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
        {
            var product = new Product
            {
                Id = request.Id,
                Name = request.Name,
                Price = request.Price
            };

            await _productService.UpdateProductAsync(product);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return NoContent(); ;
        }
    }
}
