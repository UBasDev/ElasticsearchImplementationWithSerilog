namespace ElasticAPI1.Models.Elasticsearch
{
    public class PersonDocument1 : BaseDocument1
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public int Age { get; set; } = 0;
    }
}
