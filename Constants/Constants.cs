namespace AutoMintCard
{
    public static class Constants
    {
        public static string UrlColumn = "B";
        public static string BuyerColumn = "A";
        public static string QuantityColumn = "E";

        public static string TryCheckOrderUrl = "https://www.mtgmintcard.com/account-history-info?order_id=586353";
        public static string MtgMintCardUrl = "https://www.mtgmintcard.com/";
        public static string LoginUrl = "https://www.mtgmintcard.com/login?action=process";
        public static string ClearAllCartUrl = "https://www.mtgmintcard.com/ajax_index.php?ajax_main_page=ajax_shopping_cart_detail&action=remove_all_product_in_cart&action=remove_all_product";
        public static string AddToCartUrlTemplate = "https://www.mtgmintcard.com/ajax_index.php?ajax_main_page=ajax_shopping_cart&shopping_cart_product_id={0}&shopping_cart_product_qty={1}&action=update_product";
    }
}