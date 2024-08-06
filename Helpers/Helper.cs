namespace AutoMintCard
{
    public static class Helper
    {
        public static int ParseQuantity(string quantityTxt)
        {
            if (int.TryParse(quantityTxt, out var quantity) == true)
            {
                return quantity;
            }

            return 0;
        }
    }
}