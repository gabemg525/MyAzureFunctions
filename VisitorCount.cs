using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            try
            {
                _logger.LogInformation("Processing Azure Cosmos DB Table API request using connection string...");

                // Get the connection string from environment variables
                string connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING")!;

                // Initialize the TableServiceClient using the connection string
                TableServiceClient serviceClient = new TableServiceClient(connectionString);

                // Get a reference to your table (will be created if it doesn't exist)
                TableClient client = serviceClient.GetTableClient("ProductsTable");

                // Create the table if it doesn't exist
                await client.CreateIfNotExistsAsync();

                // Create or update an entity
                Product entity = new()
                {
                    RowKey = "123456",
                    PartitionKey = "electronics",
                    Name = "Smartphone",
                    Quantity = 50,
                    Price = 669.99m,
                    Clearance = false
                };

                await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);

                // Retrieve the entity
                Response<Product> getResponse = await client.GetEntityAsync<Product>(
                    rowKey: "123456",
                    partitionKey: "electronics"
                );
                Product retrievedEntity = getResponse.Value;

                // Query all items in the category
                string category = "electronics";
                AsyncPageable<Product> queryResults = client.QueryAsync<Product>(
                    p => p.PartitionKey == category
                );
                var entities = new List<Product>();
                await foreach (Product prod in queryResults)
                {
                    entities.Add(prod);
                }

                // Build the result object
                var result = new
                {
                    Message = "Azure Cosmos DB Table operations completed.",
                    RetrievedProduct = retrievedEntity,
                    TotalProductsInCategory = entities.Count
                };

                // Serialize the result to JSON
                string jsonResponse = JsonSerializer.Serialize(result);

                // Create an HTTP response with JSON content
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(jsonResponse);

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred processing the request.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal error occurred. Please check logs for details.");
                return errorResponse;
            }
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
        // ETag is required by ITableEntity but can be ignored during JSON serialization.
        [System.Text.Json.Serialization.JsonIgnore]
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
