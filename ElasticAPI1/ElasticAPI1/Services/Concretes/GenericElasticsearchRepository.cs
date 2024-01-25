using ElasticAPI1.Models.Elasticsearch;
using ElasticAPI1.Services.Abstracts;
using Nest;
using System;
using System.Runtime.ExceptionServices;

namespace ElasticAPI1.Services.Concretes
{
    public class GenericElasticsearchRepository<T>(string indexName, ElasticClient elasticClient) : IGenericElasticsearchRepository<T> where T: BaseDocument1
    {
        private readonly string _indexName = indexName;
        private readonly ElasticClient _elasticClient = elasticClient;
        public async Task<string> CreateSingleDocumentAsync(T document)
        {
            var response = await _elasticClient.IndexDocumentAsync<T>(document);
            /*
             var response2 = await _elasticClient.IndexAsync<T>(document, ss => ss.Index("person-index1"));
                //TYPE-SAFE DEGIL ve zaten bu indexi `program.cs` içerisinde belirtip bu servisi generic olarak kullandığımız için birdaha burada belirtmemize gerek yoktur.
            */
            return response.IsValid ? "Document has been created successfully!" : "Failed to create this document!";
        }

        public async Task<string> BulkCreateDocumentsAsync(IEnumerable<T> documents)
        {
            var response = await _elasticClient.IndexManyAsync<T>(documents);
            return response.IsValid ? "Bulk create operation is successfull!" : "Failed to bulk create these documents!";
        }

        public async Task<string> DeleteSingleDocumentAsync(Guid id)
        {
            var response = await _elasticClient.DeleteAsync<T>(new DocumentPath<T>(id));
            /*
                var response = await _elasticClient.DeleteAsync<T>(new DocumentPath<T>(id), ss => ss.Index(_indexName)); 
            */
            return response.IsValid ? "Document has been deleted successfully!" : "Failed to delete this document!";
        }

        public async Task<string> DeleteAllDocumentsAsync()
        {
            var response = await _elasticClient.DeleteByQueryAsync<T>(d => d.Index(_indexName).MatchAll());
            return response.IsValid ? "All documents have been deleted successfully!" : "Failed to delete all documents!";
        }

        public async Task<IEnumerable<T>> GetAllDocumentsAsync()
        {
            var searchResponse = await _elasticClient.SearchAsync<T>(s => s.MatchAll().Size(10000));
            return searchResponse.Documents;
        }

        public async Task<T> GetSingleDocumentByIdAsync(Guid id)
        {
            var response = await _elasticClient.GetAsync<T>(new DocumentPath<T>(id));
            //var response2 = await _elasticClient.GetAsync<T>(id, idx => idx.Index("person-index1")); //TYPE-SAFE DEGIL ve zaten bu indexi `program.cs` içerisinde belirtip bu servisi generic olarak kullandığımız için birdaha burada belirtmemize gerek yoktur.
            return response.Source;
        }

        public async Task<ISearchResponse<T>> SearchAsync(SearchDescriptor<T> query)
        {
            return await _elasticClient.SearchAsync<T>(query);
        }

        public async Task<string> UpdateSingleDocumentAsync(T document)
        {
            var response = await _elasticClient.UpdateAsync<T>(new DocumentPath<T>(document), u => u.Doc(document).RetryOnConflict(3));
            /*
            var response2 = await _elasticClient.UpdateAsync(DocumentPath<T>.Id(document.Id),
                    //TYPE-SAFE DEGIL ve zaten bu indexi `program.cs` içerisinde belirtip bu servisi generic olarak kullandığımız için birdaha burada belirtmemize gerek yoktur.
                ss => ss.Index("person-index1").Doc(document).RetryOnConflict(3)
            );
            */
            return response.IsValid ? "Document has been updated successfully!" : "Failed to update this document!";
        }
        public async Task<string> UpdateMultipleDocumentsAsync(IEnumerable<T> documents)
        {
            var response = await _elasticClient.BulkAsync(b => b.Index(_indexName).UpdateMany(documents, (b, e) => b.Doc(e)));
            return response.IsValid ? "Documents have been updated successfully!" : "Failed to update these documents!";
        }

        public async Task<string> UpdateOrCreateManyDocumentsAsync(IEnumerable<T> documents)
        {
            var result = string.Empty;
            var bulkAll = _elasticClient.BulkAll<T>(documents, b => b.Index(_indexName).BackOffRetries(2)
            .BackOffTime("30s")
            .RefreshOnCompleted()
            .MaxDegreeOfParallelism(Environment.ProcessorCount)
            .Size(documents.Count())
            .DroppedDocumentCallback((bulkResponseItem, entity) =>
            {
                Console.WriteLine($"index return: {bulkResponseItem} - {entity.Id}");
            })
            .ContinueAfterDroppedDocuments()
            .MaxDegreeOfParallelism(documents.Count())
            .Size(documents.Count())
            );

            var waitHandle = new ManualResetEvent(false);
            ExceptionDispatchInfo exceptionDispatchInfo = null;

            var observer = new BulkAllObserver(
                onNext: response =>
                {
                    // do something e.g. write number of pages to console
                    result = "Documents have been created or updated successfully!";
                },
                onError: exception =>
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                    waitHandle.Set();
                    result = "Failed to create or update these documents!";
                },
                onCompleted: () => waitHandle.Set()
            );

            bulkAll.Subscribe(observer);
            waitHandle.WaitOne();
            exceptionDispatchInfo?.Throw();
            bulkAll.Dispose();
            return result;
        }
    }
}
