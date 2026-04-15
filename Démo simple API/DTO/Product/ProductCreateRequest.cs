using System.ComponentModel.DataAnnotations;

namespace Démo_simple_API.DTO.Product
{
    public class ProductCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Range(0.01, 10000)]
        public decimal Price { get; set; }
    }
}
