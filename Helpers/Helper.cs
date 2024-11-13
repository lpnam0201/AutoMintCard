namespace AutoMintCard
{
    public static class Helper
    {
        public static int Parse(string quantityTxt)
        {
            if (int.TryParse(quantityTxt, out var quantity) == true)
            {
                return quantity;
            }

            return 0;
        }
    }
}