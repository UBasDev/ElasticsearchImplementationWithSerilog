using ElasticAPI1.Models.Elasticsearch;
using Nest;

namespace ElasticAPI1.Services.Abstracts
{
    public interface IGenericElasticsearchRepository<T> where T : BaseDocument1
    {
        Task<string> CreateSingleDocumentAsync(T document);
        Task<string> BulkCreateDocumentsAsync(IEnumerable<T> documents);
        Task<T> GetSingleDocumentByIdAsync(Guid id);
        Task<ISearchResponse<T>> SearchAsync(SearchDescriptor<T> query);
        Task<IEnumerable<T>> GetAllDocumentsAsync();
        Task<string> UpdateSingleDocumentAsync(T document);
        Task<string> UpdateOrCreateManyDocumentsAsync(IEnumerable<T> documents);
        Task<string> DeleteSingleDocumentAsync(Guid id);
        Task<string> DeleteAllDocumentsAsync();
    }
}
