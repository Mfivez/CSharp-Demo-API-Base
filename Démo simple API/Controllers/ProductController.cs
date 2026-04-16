using BLL.Interfaces;
using Démo_simple_API.DTO.Product;
using Démo_simple_API.Mappers;
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
        public async Task<ActionResult<List<ProductResponse>>> GetAll(int page = 1, int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize > 50 ? 50 : page;

            var products = await _productService.GetAllProductsAsync(page, pageSize);

            var response = products.Select(ProductMapper.ToResponse).ToList();

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            return Ok(ProductMapper.ToResponse(product));
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create(ProductCreateRequest request)
        {
            var product = ProductMapper.ToEntity(request);

            int newId = await _productService.CreateProductAsync(product);

            product.Id = newId;

            return CreatedAtAction(nameof(GetById), new { id = newId }, ProductMapper.ToResponse(product));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
        {
            var product = ProductMapper.ToEntity(request, id);

            await _productService.UpdateProductAsync(product);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);

            return NoContent();
        }
    }
}