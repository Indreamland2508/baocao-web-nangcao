using System.ComponentModel.DataAnnotations;

namespace BAOCAOWEBNANGCAO.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        [Required]
        public string CustomerEmail { get; set; }

        // --- ĐÃ XÓA CitizenId ---

        public string ShippingAddress { get; set; } // Thêm trường địa chỉ giao hàng
        public string? Note { get; set; }           // Thêm trường Ghi chú

        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }

        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }
}