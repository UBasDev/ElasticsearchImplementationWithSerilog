namespace ElasticAPI1.Models
{
    public class AppSettings
    {
        public ElasticConfiguration ElasticConfiguration { get; set; } = new();
    }
    public class ElasticConfiguration
    {
        public string Uri { get; set; }
        public string PersonDocumentIndex { get; set; }
    }
}
