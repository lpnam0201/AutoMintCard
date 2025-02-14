namespace AutoMintCard
{
    public class ItemToOrder
    {
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int InStockQuantity { get; set; }
        public string Url { get; set; }
    }
}