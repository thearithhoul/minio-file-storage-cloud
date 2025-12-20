namespace Backend.Const
{
    public class MinioOptions
    {
        public string? MinioEndpoint { get; set; }
        public string? MinioAccessKey { get; set; }
        public string? MinioSecretKey { get; set; }
        public string? MinioBucket { get; set; }

        public bool UseSSL { get; set; }

    }
}