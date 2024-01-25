using ElasticAPI1.Models;
using ElasticAPI1.Models.Elasticsearch;
using ElasticAPI1.Services.Abstracts;
using ElasticAPI1.Services.Concretes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nest;
using System;
//using CreateIndexResponse = Elastic.Clients.Elasticsearch.IndexManagement.CreateIndexResponse;

namespace ElasticAPI1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Test1Controller(ILogger<Test1Controller> logger, IPersonDocumentElasticsearchRepository personDocumentElasticsearchRepository, AppSettings appSettings) : ControllerBase
    {
        private readonly ILogger<Test1Controller> _logger = logger;
        private readonly IPersonDocumentElasticsearchRepository _personDocumentElasticsearchRepository = personDocumentElasticsearchRepository;
        private readonly AppSettings _appSettings1 = appSettings;

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllDocumentsFromElasticsearch()
        {
            var response = await _personDocumentElasticsearchRepository.GetAllDocumentsAsync();
            return Ok(response);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateSingleDocumentToElasticsearch([FromBody] PersonDocument1 requestBody)
        {
            var response = await _personDocumentElasticsearchRepository.CreateSingleDocumentAsync(requestBody);
            return Ok(response);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSingleDocumentById(Guid id)
        {
            var response = await _personDocumentElasticsearchRepository.GetSingleDocumentByIdAsync(id);
            if (response == null) return NotFound();
            return Ok(response);
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateSingleDocument([FromBody] PersonDocument1 requestBody)
        {
            var response = await _personDocumentElasticsearchRepository.UpdateSingleDocumentAsync(requestBody);
            return Ok(response);
        }

        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteSingleDocument(Guid id)
        {
            var response = await _personDocumentElasticsearchRepository.DeleteSingleDocumentAsync(id);
            return Ok(response);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SearchPersons(string searchItem)
        {
            var searchQuery = new SearchDescriptor<PersonDocument1>().Index(_appSettings1.ElasticConfiguration.PersonDocumentIndex).Query(
                q => q.Bool(
                        b => b.Must(
                                /*
                                 mu => mu.Match( //Bu `Match` ifadesi zorunluluk ekliyor.
                                        m => m.Field(f => f.Job).Query("tekstil")
                                    ),
                                 */
                                m => m.Bool(
                                        bb => bb.Should( //Bu wildcardlar sayesinde bu "searchText", bu propertylerden herhangi birisinin içerisinde mevcutsa[contains] entityi döndürür. Küçük-Büyük harf önemsemez.
                                                sh => sh.Wildcard(
                                                        wc => wc.Field(f => f.Name).Value($"*{searchItem}*")
                                                    ),
                                                sh => sh.Wildcard(
                                                        wc => wc.Field(f => f.Email).Value($"*{searchItem}*")
                                                    ),
                                                sh => sh.Wildcard(
                                                        wc => wc.Field(f => f.Job).Value($"*{searchItem}*")
                                                    )
                                            )
                                    )
                            )
                            .Filter(
                                fi => fi.Range(
                                        r => r.Field(f => f.Age).GreaterThan( 1 )
                                    )
                            )
                    )
                ).Sort(sort => sort.Field(f => f.Name, SortOrder.Ascending))
                    .From(0)
                    .Size(10000);

            var searchQuery2 = new SearchDescriptor<PersonDocument1>().Index(_appSettings1.ElasticConfiguration.PersonDocumentIndex).Query(
                    q => q.Term(t => t.Job, searchItem) //Bu sayede direkt bu valueyle eşleşen itemleri dönüdüyor. Yani `Job={value}` şeklinde. Büyük-Küçük harf önemser ve Equals[=] şeklindedir.
                );
            var searchQuery3 = new SearchDescriptor<PersonDocument1>().Index(_appSettings1.ElasticConfiguration.PersonDocumentIndex).Query(
                    q => q.Bool(
                            b => b.Filter(
                                    filt => filt.Term(
                                            t => t.Field(
                                                fiel => fiel.Job //Bu sayede direkt bu valueyle eşleşen itemleri döndürüyor. Yani `Job={value}` şeklinde. Büyük-Küçük harf önemser ve Equals[=] şeklindedir.
                                        )
                                        .Value(searchItem)
                                    )
                                )
                        )
                );
            var searchQuery4 = new SearchDescriptor<PersonDocument1>().Index(_appSettings1.ElasticConfiguration.PersonDocumentIndex).Query(
                    q => q.QueryString(
                            qs => qs.Query("*" + searchItem + "*") //Bu query sayesinde bu "searchText", bu propertylerden herhangi birisinin içerisinde mevcutsa[contains] entityi döndürür. Küçük-Büyük harf önemsemez.
                            .Fields(fi => fi
                                .Field(f => f.Name)
                                .Field(f => f.Email)
                                .Field(f => f.Job)
                            )
                        )
                );
            var searchQuery5 = new SearchDescriptor<PersonDocument1>().Index(_appSettings1.ElasticConfiguration.PersonDocumentIndex).Query(
                    q => q.Bool(
                            b => b.Should(
                                    q1 => q1.MultiMatch( //Bu query sayesinde bu "searchText", bu propertylerden herhangi birisinin içerisinde mevcutsa[contains] entityi döndürür. Küçük-Büyük harf önemsemez.
                                            mm => mm
                                            .Query(searchItem)
                                            .Operator(Operator.Or)
                                            .Fields(fi => fi
                                                .Field (f => f.Name)
                                                .Field(f => f.Email)
                                                .Field(f => f.Job)
                                            )
                                        ),
                                    q2 => q2.QueryString(
                                            qs => qs
                                                .Query("*" + searchItem + "*")
                                                .Fields(fi => fi
                                                .Field(f => f.Name)
                                                .Field(f => f.Email)
                                                .Field(f => f.Job)
                                            )
                                        )
                                )
                        )
                );
            var searchResponse = await _personDocumentElasticsearchRepository.SearchAsync(searchQuery5);
            var response = searchResponse.Documents.ToList();
            var responseCount = searchResponse.Total;
            _logger.LogInformation("Toplam response sayısı: {count}", responseCount);
            return Ok(response);
        }
    }
}
