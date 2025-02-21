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
    public class VisitorCountFunction
    {
        private readonly ILogger<VisitorCountFunction> _logger;

        public VisitorCountFunction(ILogger<VisitorCountFunction> logger)
        {
            _logger = logger;
        }

        [Function("VisitorCountFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing visitor count update using connection string...");

                // Retrieve the Cosmos DB connection string from environment variables.
                string connectionString = System.Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING")!;

                // Initialize the TableServiceClient using the connection string.
                TableServiceClient serviceClient = new TableServiceClient(connectionString);

                // Get a reference to the "Visitor" table.
                TableClient tableClient = serviceClient.GetTableClient("Visitor");

                // Create the table if it doesn't already exist.
                await tableClient.CreateIfNotExistsAsync();

                // Define the partition and row keys for our visitor count record.
                string partitionKey = "visitor";
                string rowKey = "visitorcount";

                VisitorEntity visitorEntity;

                // Try to retrieve the visitor count entity.
                try
                {
                    Response<VisitorEntity> getResponse = await tableClient.GetEntityAsync<VisitorEntity>(partitionKey, rowKey);
                    visitorEntity = getResponse.Value;
                    visitorEntity.Count++;  // Increment the count.
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    // If the entity is not found, create a new one with Count = 1.
                    visitorEntity = new VisitorEntity
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey,
                        Count = 1
                    };
                }

                // Upsert the entity (insert or update).
                await tableClient.UpsertEntityAsync(visitorEntity, TableUpdateMode.Replace);

                // Build the result object to return.
                var result = new
                {
                    Message = "Visitor count updated successfully.",
                    CurrentVisitorCount = visitorEntity.Count
                };

                // Serialize the result object to JSON.
                string jsonResponse = JsonSerializer.Serialize(result);

                // Create an HTTP response with JSON content.
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                response.Headers.Add("Access-Control-Allow-Origin", "https://strgazgabemg525.z19.web.core.windows.net");

                await response.WriteStringAsync(jsonResponse);

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred processing the visitor count update.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal error occurred. Please check logs for details.");
                return errorResponse;
            }
        }
    }

    public record VisitorEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int Count { get; set; }
        // ETag is required by ITableEntity. We ignore it during JSON serialization.
        [System.Text.Json.Serialization.JsonIgnore]
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
