using System.Net;
using AutoMintCard;
using AutoMintCard.CLI;
using ClosedXML;
using ClosedXML.Excel;
using CommandLine;

namespace AutoMintCard;

class Program
{
    static HttpClient HttpClient;
    static CookieContainer CookieContainer = new CookieContainer();

    static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args).Value;
        
        //var items = ReadSheet(options);

        InitializeHttpClient();

        Credential credential;
        credential = await Login(options.Username, options.Password);
        await TryCookie();
        //await ClearAllCart();
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
        return credential;
    }

    private static async Task TryCookie()
    {
        var tryResponse = await HttpClient.GetAsync(Constants.TryCheckOrderUrl);
        var mainPageHtml = await tryResponse.Content.ReadAsStringAsync();
        
    }
    private static async Task ClearAllCart()
    {
        var clearCartResponse = await HttpClient.GetAsync(Constants.ClearAllCartUrl);
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
