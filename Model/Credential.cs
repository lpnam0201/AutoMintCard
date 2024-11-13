namespace AutoMintCard
{
    public class Credential 
    {
        public string Cookie { get; set; }
        // parsed from HTML page. CSRF token ?
        public string SecurityToken { get; set; }
    }
}
