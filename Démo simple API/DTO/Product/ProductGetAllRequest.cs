using System.ComponentModel.DataAnnotations;

namespace Demo_simple_API.DTO.Product
{
    public class ProductGetAllRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Il doit y avoir au minimum 1 page.")]
        public int Page { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "PageSize doit être compris entre 1 et 50.")]
        public int PageSize { get; set; } = 10;

        [Range(0, double.MaxValue, ErrorMessage = "Le prix minimum doit être positif.")]
        public decimal? MinPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le prix maximum doit être positif.")]
        public decimal? MaxPrice { get; set; }

        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string? Name { get; set; }
    }
}