namespace Backend.Const
{
    /// <summary>
    /// Redis connection options loaded from configuration.
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// Standard StackExchange.Redis connection string (e.g. "localhost:6379,abortConnect=false").
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Optional database index; when null uses the default from the connection string (usually 0).
        /// </summary>
        public int? Database { get; set; }

        /// <summary>
        /// Optional prefix to namespace keys (e.g. "file-storage").
        /// </summary>
        public string? KeyPrefix { get; set; }
    }
}
