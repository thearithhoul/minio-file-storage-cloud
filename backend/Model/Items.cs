
namespace Backend.Model
{

    public class RequsetItems
    {
        public List<string> ImageList { get; set; } = [];
        public string? ItemName { get; set; }
        public decimal BasePrice { get; set; }
        public int StockQty { get; set; }
    }

    public class ReadItemsResponse
    {
        public IEnumerable<ItemDto> Items { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

    public class ItemDto
    {
        public string ItemName { get; set; }
        public IEnumerable<string> ImagesId { get; set; }
        public decimal BasePrice { get; set; }
        public int StockQty { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}