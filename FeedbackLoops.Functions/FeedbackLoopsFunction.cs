using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class PodcastFunctions
{
    private readonly ILogger _logger;
    private readonly EmbeddingService _embeddingService;
    private readonly ChatCompletionService _chatCompletionService;
    private readonly SqlExecutorService _sqlExecutorService;

    public PodcastFunctions(
        ILoggerFactory loggerFactory,
        EmbeddingService embeddingService,
        ChatCompletionService chatCompletionService,
        SqlExecutorService sqlExecutorService)
    {
        _logger = loggerFactory.CreateLogger<PodcastFunctions>();
        _embeddingService = embeddingService;
        _chatCompletionService = chatCompletionService;
        _sqlExecutorService = sqlExecutorService;
    }

    [Function("AddPodcast")]
    public async Task<HttpResponseData> AddPodcastAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "add-podcast")] HttpRequestData req)
    {
        _logger.LogInformation("Received a request to add a new podcast.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<PodcastRequest>(requestBody);

        if (string.IsNullOrEmpty(data?.Title) || string.IsNullOrEmpty(data?.Transcript))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Missing 'title' or 'transcript' in the request body.");
            return badResponse;
        }

        string summaryPrompt = $"Summarize the following podcast transcript:\n{data.Transcript}\nSummary:";
        var summary = await _chatCompletionService.GetChatCompletionAsync(summaryPrompt);
        var embedding = await _embeddingService.GetEmbeddingAsync(summary);

        string insertQuery = "INSERT INTO podcast_episodes (title, summary, transcript, embedding) VALUES (@title, @summary, @transcript, @embedding);";
        await _sqlExecutorService.ExecuteQueryAsync(insertQuery, new { data.Title, data.Transcript, embedding, summary });

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteStringAsync($"Podcast '{data.Title}' added successfully.");
        return response;
    }

    [Function("UpdateUserHistory")]
    public async Task<HttpResponseData> UpdateUserHistoryAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "update-user-history")] HttpRequestData req)
    {
        _logger.LogInformation("Received a request to update user listening history.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<UserHistoryRequest>(requestBody);

        if (string.IsNullOrEmpty(data?.UserId) || string.IsNullOrEmpty(data?.ListeningHistory))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Missing 'user_id' or 'listening_history' in the request body.");
            return badResponse;
        }

        var embedding = await _embeddingService.GetEmbeddingAsync(data.ListeningHistory);

        string updateQuery = "UPDATE users SET listening_history = @history, embedding = @embedding WHERE id = @id;";
        int userId;
        if (!int.TryParse(data.UserId, out userId))
        {
            throw new ArgumentException("Invalid user_id format. Must be an integer.");
        }
        await _sqlExecutorService.ExecuteQueryAsync(updateQuery, new { history = data.ListeningHistory, embedding, id = userId });

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Listening history for user {data.UserId} updated successfully.");
        return response;
    }

    [Function("RecommendPodcasts")]
    public async Task<HttpResponseData> RecommendPodcastsAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "recommend-podcasts")] HttpRequestData req)
    {
        _logger.LogInformation("Received a request to recommend podcasts.");

        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string userIdString = queryParams.Get("userId");

        if (string.IsNullOrEmpty(userIdString))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Missing 'user_id' in query parameters.");
            return badResponse;
        }

        int userId = int.Parse(userIdString);

        var userEmbeddingQuery = "SELECT embedding FROM users WHERE id = @id;";

        var user = await _sqlExecutorService.ExecuteQueryAsync(userEmbeddingQuery, new { id = userId });

        if (user == null || user.Count == 0 || !user[0].ContainsKey("embedding") || user[0]["embedding"] == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"No embedding found for user ID {userId}.");
            return notFoundResponse;
        }

        var userEmbedding = user[0]["embedding"];

        string recommendationQuery = @"
            SELECT id, title, summary, embedding <-> @embedding AS similarity
            FROM podcast_episodes
            WHERE embedding IS NOT NULL
            ORDER BY similarity ASC
            LIMIT 3;";

        var recommendations = await _sqlExecutorService.ExecuteQueryAsync(recommendationQuery, new { embedding = userEmbedding });

        var responseList = new List<PodcastRecommendation>();
        foreach (var rec in recommendations)
        {
            string prompt = $"Summarize the following podcast in 5 words or less:\n\nPodcast: {rec["title"]}\nDescription: {rec["summary"]}\n\nSummary:";
            var shortDescription = await _chatCompletionService.GetChatCompletionAsync(prompt);

            string insertSuggestionQuery = "INSERT INTO suggested_podcasts (user_id, podcast_id, similarity_score) VALUES (@user_id, @podcast_id, @similarity);";
            await _sqlExecutorService.ExecuteQueryAsync(insertSuggestionQuery, new { user_id = userId, podcast_id = rec["id"], similarity = rec["similarity"] });

            responseList.Add(new PodcastRecommendation
            {
                Id = rec["id"].ToString(),
                Title = rec["title"].ToString(),
                Summary = shortDescription,
                Similarity = float.Parse(rec["similarity"].ToString())
            });
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(JsonConvert.SerializeObject(responseList));
        return response;
    }

    [Function("GetSuggestedPodcasts")]
    public async Task<HttpResponseData> GetSuggestedPodcastsAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "get-suggested-podcasts")] HttpRequestData req)
    {
        _logger.LogInformation("Received a request to fetch suggested podcasts for a user.");

        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string userIdString = queryParams.Get("userId");

        if (string.IsNullOrEmpty(userIdString))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Missing 'userId' in query parameters.");
            return badResponse;
        }

        if (!int.TryParse(userIdString, out int userId))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid 'userId'. It must be an integer.");
            return badResponse;
        }

        string query = @"
            SELECT sp.user_id, pe.id AS podcast_id, pe.title
            FROM suggested_podcasts sp
            JOIN podcast_episodes pe ON sp.podcast_id = pe.id
            WHERE sp.user_id = @userId
            ORDER BY sp.similarity_score ASC;";

        var suggested = await _sqlExecutorService.ExecuteQueryAsync(query, new { userId });

        var result = suggested.Select(row => new
        {
            UserId = row["user_id"],
            PodcastId = row["podcast_id"],
            Title = row["title"]
        });

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(JsonConvert.SerializeObject(result));
        return response;
    }
}

public class PodcastRequest
{
    public string Title { get; set; }
    public string Transcript { get; set; }
}

public class UserHistoryRequest
{
    public string UserId { get; set; }
    public string ListeningHistory { get; set; }
}

public class PodcastRecommendation
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public float Similarity { get; set; }
}
