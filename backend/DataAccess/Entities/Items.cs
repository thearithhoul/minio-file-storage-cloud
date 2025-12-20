
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DataAccess.Entities
{
    [Table("items")]
    public class Items
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("images_id")]
        public string? ImagesId { get; set; }

        [Column("Item_name")]
        public string? ItemName { get; set; }

        [Column("base-price")]
        public decimal BasePrice { get; set; }

        [Column("stock_qty")]
        public int StockQty { get; set; }

        [Column("create_at")]
        public DateTime CreateAt { get; set; }

        [Column("update_at")]
        public DateTime UpdateAt { get; set; }


    }

    public class ReadItems
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("images_id")]
        public string? ImagesId { get; set; }

        [Column("Item_name")]
        public string? ItemName { get; set; }

        [Column("base-price")]
        public decimal BasePrice { get; set; }

        [Column("stock_qty")]
        public int StockQty { get; set; }

        [Column("create_at")]
        public DateTime CreateAt { get; set; }

        [Column("update_at")]
        public DateTime UpdateAt { get; set; }

        [Column("TotalCount")]
        public int TotalCount { get; set; }
        [Column("TotalPages")]
        public int TotalPages { get; set; }


    }
}