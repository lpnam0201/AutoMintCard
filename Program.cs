using System.Net;
using AutoMintCard;
using AutoMintCard.CLI;
using ClosedXML;
using ClosedXML.Excel;
using CommandLine;
using DocumentFormat.OpenXml.Drawing;
using HtmlAgilityPack;

namespace AutoMintCard;

class Program
{
    static HttpClient HttpClient;
    static CookieContainer CookieContainer = new CookieContainer();

    static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;

        InitializeHttpClient();

        Credential credential;
        credential = await Login(options.Username, options.Password);
        await ClearAllCart();

        var items = ReadSheet(options);
        var itemsToOrder = await BuildItemToOrders(items);

        await AddAllToCart(itemsToOrder);
    }

    private static async Task AddAllToCart(List<ItemToOrder> itemToOrders)
    {
        foreach (var itemToOrder in itemToOrders)
        {
            Console.WriteLine($"Product {itemToOrder.ProductId}: need {itemToOrder.Quantity}, {itemToOrder.InStockQuantity} in stock");
            var quantity = itemToOrder.InStockQuantity < itemToOrder.Quantity
                ? itemToOrder.InStockQuantity
                : itemToOrder.Quantity;

            await AddToCart(itemToOrder.ProductId, quantity);
        }
    }

    private static async Task<List<ItemToOrder>> BuildItemToOrders(List<Item> items)
    {
        var itemsToOrder = new List<ItemToOrder>();
        foreach (var item in items)
        {
            var (productId, remainingQuantity, price) = await GetAndParseProductIdAndRelatedInfo(item);
            itemsToOrder.Add(new ItemToOrder
            {
                ProductId = productId,
                Quantity = item.Quantity,
                InStockQuantity = remainingQuantity,
                Price = price
            });
        }

        return GroupDuplicateItems(itemsToOrder);
    }

    private static List<ItemToOrder> GroupDuplicateItems(List<ItemToOrder> itemToOrders)
    {
        return itemToOrders.GroupBy(x => x.ProductId)
            .Select(g => {
                return g.Aggregate((item1, item2) => new ItemToOrder
                {
                    ProductId = item1.ProductId,
                    InStockQuantity = item1.InStockQuantity,
                    Price = item1.Price,
                    Quantity = item1.Quantity + item2.Quantity,
                });
            })
            .ToList();
    }

    private static void InitializeHttpClient()
    {
        var httpClientHandler = new HttpClientHandler()
        {
            UseCookies = true,
            CookieContainer = CookieContainer,
        };
        HttpClient = new HttpClient(httpClientHandler);
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

    private static async Task<Credential> Login(string username, string password)
    {
        var credential = new Credential();

        var mainPageResponse = await HttpClient.GetAsync(Constants.MtgMintCardUrl);
        var mainPageHtml = await mainPageResponse.Content.ReadAsStringAsync();
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(mainPageHtml);

        var securityToken = htmlDoc.DocumentNode
            .SelectNodes("//input[@name='securityToken']")
            .FirstOrDefault()
            .GetAttributeValue("value", "");

        var form = new FormUrlEncodedContent(new []
        {
            new KeyValuePair<string, string>("securityToken", securityToken),
            new KeyValuePair<string, string>("email_address", username),
            new KeyValuePair<string, string>("password", password),

        });
        var loginResponse = await HttpClient.PostAsync(Constants.LoginUrl, form);
        var zenidCookie = CookieContainer.GetCookies(new Uri(Constants.MtgMintCardUrl)).FirstOrDefault();
        
        credential.Cookie = zenidCookie.Value;
        credential.SecurityToken = securityToken;
        
        Console.WriteLine("Login");
        return credential;
    }
    private static async Task ClearAllCart()
    {
        var clearCartResponse = await HttpClient.GetAsync(Constants.ClearAllCartUrl);
        Console.WriteLine("Clear all cart");
    }

    private static async Task<(string, int, decimal)> GetAndParseProductIdAndRelatedInfo(Item item)
    {
        Console.WriteLine($"Parsing url: {item.Url}");
        var getItemResponse = await HttpClient.GetAsync(item.Url);
        var itemPageHtml = await getItemResponse.Content.ReadAsStringAsync();
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(itemPageHtml);

        var addWatchListLink = htmlDoc.DocumentNode
            .SelectNodes("//a[contains(@onclick, 'AddWatchListGet')]")
            .FirstOrDefault();
        var idAttributeValue = addWatchListLink.GetAttributeValue("id", "");
        var productId = idAttributeValue.Split("-")[0];

        var remainingQuantity = GetRemainingQuantity(htmlDoc, productId);
        var price = GetPrice(htmlDoc);
        return (productId, remainingQuantity, price);
    }

    private static int GetRemainingQuantity(HtmlDocument htmlDoc, string productId)
    {
        var outOfStockSpan = htmlDoc.DocumentNode
            .SelectNodes("//span[contains(@class, 'label label-default') and text() = 'Out of Stock']")
            ?.FirstOrDefault();
        if (outOfStockSpan != null)
        {
            return 0;
        }

        var quantityButtonLis = htmlDoc.DocumentNode
            .SelectNodes($"//li[contains(@class, 'ui-state-default') and contains(@value, '{productId}')]")
            .ToList();
        return quantityButtonLis.Select(x => int.Parse(x.InnerText)).Max();
    }

    private static decimal GetPrice(HtmlDocument htmlDoc)
    {
        // The very first span is the price
        var priceSpan = htmlDoc.DocumentNode
            .SelectNodes("//span[@itemprop='price']")
            .FirstOrDefault();
        return decimal.Parse(priceSpan.InnerText);
    }

    private static async Task AddToCart(string productId, int quantity)
    {
        var addToCartUrl = string.Format(Constants.AddToCartUrlTemplate, productId, quantity);
        await HttpClient.GetAsync(addToCartUrl);
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
