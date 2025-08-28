using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SumarizerService.Models
{
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")] public Candidate[] Candidates { get; set; }

        [JsonPropertyName("usageMetadata")] public UsageMetadata UsageMetadata { get; set; }

        [JsonPropertyName("modelVersion")] public string ModelVersion { get; set; }

        [JsonPropertyName("responseId")] public string ResponseId { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")] public ResponseContent Content { get; set; }

        [JsonPropertyName("finishReason")] public string FinishReason { get; set; }

        [JsonPropertyName("index")] public int Index { get; set; }
    }

    public class ResponseContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
    
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }

public class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        [JsonPropertyName("promptTokensDetails")]
        public PromptTokenDetail[] PromptTokensDetails { get; set; }

        [JsonPropertyName("thoughtsTokenCount")]
        public int ThoughtsTokenCount { get; set; }
    }

    public class PromptTokenDetail
    {
        [JsonPropertyName("modality")]
        public string Modality { get; set; }

        [JsonPropertyName("tokenCount")]
        public int TokenCount { get; set; }
    }

    // Summary-specific response classes for the JSON content
    public class SummaryResponse
    {
        [JsonPropertyName("summary")]
        public List<SummaryTopic> Summary { get; set; }
    }

    public class SummaryTopic
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("points")]
        public List<string> Points { get; set; }
    }
}