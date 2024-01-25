using ElasticAPI1.Models;
using ElasticAPI1.Models.Elasticsearch;
using ElasticAPI1.Services.Abstracts;
using Nest;

namespace ElasticAPI1.Services.Concretes
{
    public class PersonDocumentElasticsearchRepository(ElasticClient elasticClient, AppSettings appSettings) : GenericElasticsearchRepository<PersonDocument1>(appSettings.ElasticConfiguration.PersonDocumentIndex, elasticClient), IPersonDocumentElasticsearchRepository
    {
        
    }
}
