using CommandLine;

namespace AutoMintCard.CLI
{
    public class Options 
    {
        [Option("FilePath", Required = true)]
        public string FilePath { get; set; }

        [Option("SheetName", Required = true)]
        public string SheetName { get; set; }

        [Option("LastRow", Required = true)]
        public int LastRow { get; set;}

        [Option("Username", Required = true)]
        public string Username { get; set;}

        [Option("Password", Required = true)]
        public string Password { get; set;}
    }
}
