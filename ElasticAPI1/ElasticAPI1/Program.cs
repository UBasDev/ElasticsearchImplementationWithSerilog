using ElasticAPI1.Models;
using ElasticAPI1.Models.Elasticsearch;
using ElasticAPI1.Registrations;
using ElasticAPI1.Services.Abstracts;
using ElasticAPI1.Services.Concretes;
using Elasticsearch.Net;
using Nest;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var configuration = new ConfigurationBuilder()
    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(path: $"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var appSettings = new AppSettings();
configuration.Bind(nameof(AppSettings), appSettings);
builder.Services.AddSingleton(appSettings);
builder.Services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

// Add services to the container.
var uri = new Uri(appSettings.ElasticConfiguration.Uri);
//var uri2 = new Uri("https://elastic:{username}@10.1.65.133:9200");
//var uri3 = new Uri("http://elastic:{username}@localhost:9200");
var personDocumentIndex1 = appSettings.ElasticConfiguration.PersonDocumentIndex;
var settings = new ConnectionSettings(uri: uri).DefaultIndex(defaultIndex: String.Join(",", personDocumentIndex1)).ServerCertificateValidationCallback(CertificateValidations.AllowAll);

var client = new ElasticClient(connectionSettings: settings);
client.Indices.Create(personDocumentIndex1, index => index.Settings(s => s
                .Setting("index.max_result_window", 100000)
                .Analysis(a => a
                    .Normalizers(n => n
                        .Custom("my_normalizer1", cn => cn
                            .Filters("lowercase", "asciifolding")
                        )
                    )
                )
            ).Map<PersonDocument1>(x => x.AutoMap()
                .Properties(p => p
                    .Keyword(k => k
                        .Name(n => n.Name)
                        .Normalizer("my_normalizer1")
                    )
                )
            ));

builder.Services.AddSingleton(client);
//builder.Services.AddScoped<IGenericElasticsearchRepository<PersonDocument1>, GenericElasticsearchRepository<PersonDocument1>>();
builder.Services.AddScoped<IPersonDocumentElasticsearchRepository, PersonDocumentElasticsearchRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiRegistrations(configuration, appSettings.ElasticConfiguration.Uri, environment);

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();