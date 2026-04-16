using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Filters.Product
{
    public class ProductFilter
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Name { get; set; }
    }
}
