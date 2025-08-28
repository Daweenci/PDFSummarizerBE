using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SumarizerService.Models
{
    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public Content[] Contents { get; set; }

        [JsonPropertyName("system_instruction")]
        public Content SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; }

        public GeminiRequest(string text, string systemInstruction)
        {
            this.SystemInstruction = new Content
            {
                Parts = new Part[]
                {
                    new Part { Text = systemInstruction }
                }
            };

            this.Contents = new Content[]
            {
                new Content
                {
                    Parts = new Part[]
                    {
                        new Part { Text = text }
                    }
                }
            };

            this.GenerationConfig = new GenerationConfig
            {
                ResponseMimeType = "application/json",
                ResponseSchema = BuildSummaryResponseSchema()
            };
        }
        private SchemaNode BuildSummaryResponseSchema()
        {
            return new SchemaNode
            {
                Type = "OBJECT",
                Properties = new Dictionary<string, SchemaNode>
                {
                    {
                        "summary", new SchemaNode
                        {
                            Type = "ARRAY",
                            Items = new SchemaNode
                            {
                                Type = "OBJECT",
                                Properties = new Dictionary<string, SchemaNode>
                                {
                                    { "topic", new SchemaNode { Type = "STRING" } },
                                    {
                                        "points", new SchemaNode
                                        {
                                            Type = "ARRAY",
                                            Items = new SchemaNode { Type = "STRING" }
                                        }
                                    }
                                },
                                Required = new[] { "topic", "points" }
                            }
                        }
                    }
                },
                Required = new[] { "summary" }
            };
        }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class GenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; }

        [JsonPropertyName("responseSchema")]
        public SchemaNode ResponseSchema { get; set; }
    }

    public class SchemaNode
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, SchemaNode> Properties { get; set; }

        [JsonPropertyName("items")]
        public SchemaNode Items { get; set; }

        [JsonPropertyName("required")]
        public string[] Required { get; set; }

        [JsonPropertyName("propertyOrdering")]
        public string[] PropertyOrdering { get; set; }
    }
}
