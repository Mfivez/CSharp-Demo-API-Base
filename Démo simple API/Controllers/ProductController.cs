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
        public ActionResult<List<ProductResponse>> GetAll()
        {
            var products = _productService.GetAllProducts();

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            }).ToList();

            return Ok(response);
        }

        [HttpGet("{id}")]
        public ActionResult<ProductResponse> GetById(int id)
        {
            var product = _productService.GetProductById(id);

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            };

            return Ok(response);
        }


        [HttpPost]
        public ActionResult Create(ProductCreateRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price
            };

            _productService.CreateProduct(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);

        }

        [HttpPut("{id}")]
        public ActionResult Update(int id, ProductUpdateRequest request)
        {
            var product = new Product
            {
                Id = request.Id,
                Name = request.Name,
                Price = request.Price
            };

            _productService.UpdateProduct(product);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            _productService.DeleteProduct(id);
            return NoContent(); ;
        }
    }
}
