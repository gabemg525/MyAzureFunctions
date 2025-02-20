using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MyCosmosDbFunction
{
    public class CosmosTableFunction
    {
        private readonly ILogger<CosmosTableFunction> _logger;

        public CosmosTableFunction(ILogger<CosmosTableFunction> logger)
        {
            _logger = logger;
        }

        [Function("CosmosTableFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Azure Cosmos DB Table API request...");

            // Authenticate using DefaultAzureCredential (managed identity or other chained credentials)
            TokenCredential credential = new DefaultAzureCredential();

            // Replace with your Azure Cosmos DB Table endpoint
            string tableEndpoint = "https://<YOUR_COSMOSDB_ACCOUNT>.table.cosmos.azure.com:443/";

            // Initialize the TableServiceClient
            TableServiceClient serviceClient = new(
                new Uri(tableEndpoint),
                credential
            );

            // Get a reference to your table (use whatever table name you want here)
            TableClient client = serviceClient.GetTableClient("ProductsTable");

            // 1. Create the table if it doesn't exist
            await client.CreateIfNotExistsAsync();

            // 2. Create or update an entity
            Product entity = new()
            {
                RowKey = "123456",
                PartitionKey = "electronics",
                Name = "Smartphone",
                Quantity = 50,
                Price = 699.99m,
                Clearance = false
            };

            await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);

            // 3. Retrieve the entity
            Response<Product> getResponse = await client.GetEntityAsync<Product>(
                rowKey: "123456",
                partitionKey: "electronics"
            );
            Product retrievedEntity = getResponse.Value;

            // 4. Query all items in the category
            string category = "electronics";
            AsyncPageable<Product> queryResults = client.QueryAsync<Product>(
                p => p.PartitionKey == category
            );
            var entities = new List<Product>();
            await foreach (Product prod in queryResults)
            {
                entities.Add(prod);
            }

            // 5. Create an HTTP response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                Message = "Azure Cosmos DB Table operations completed.",
                RetrievedProduct = retrievedEntity,
                TotalProductsInCategory = entities.Count
            });

            return response;
        }
    }

    public record Product : ITableEntity
    {
        public required string RowKey { get; set; }
        public required string PartitionKey { get; set; }
        public required string Name { get; set; }
        public required int Quantity { get; set; }
        public required decimal Price { get; set; }
        public required bool Clearance { get; set; }
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
