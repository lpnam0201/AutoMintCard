using AutoMintCard;
using AutoMintCard.CLI;
using ClosedXML;
using ClosedXML.Excel;
using CommandLine;

namespace AutoMintCard;

class Program
{
    static void Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;
        
        var items = ReadSheet(options);
    }

    private static List<Item> ReadSheet(Options options)
    {
        var items = new List<Item>();

        // Bug: Can't read sheet containing Pivot table
        using (var workbook = new XLWorkbook(options.FilePath))
        {
            var sheet = workbook.Worksheets.FirstOrDefault(x => x.Name == options.SheetName);
            foreach (var row in sheet.Rows(1, options.LastRow))
            {
                string buyer;
                if (!row.Cell(Constants.BuyerColumn).Value.TryGetText(out buyer))
                {
                    continue;
                }
                
                string url;
                if (!row.Cell(Constants.UrlColumn).Value.TryGetText(out url))
                {
                    continue;
                }

                if (!HasBuyer(buyer) || !HasUrl(url))
                {
                    continue;
                }

                var quantity = (int)row.Cell(Constants.QuantityColumn).Value.GetNumber();

                items.Add(new Item
                {
                    Buyer = buyer,
                    Url = url,
                    Quantity = quantity
                });
            }
        }

        return items;
    }


    private static bool HasBuyer(string buyer)
    {
        return !string.IsNullOrEmpty(buyer);
    }

    private static bool HasUrl(string url)
    {
        return !string.IsNullOrEmpty(url) && url.StartsWith("http");
    }
}
