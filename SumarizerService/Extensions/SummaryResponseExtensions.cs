using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SumarizerService.Models;

namespace SumarizerService.Extensions
{
    public static class SummaryResponseExtensions
    {
        public static SummaryResponse MergeSummaries(this IEnumerable<SummaryResponse> summaries)
        {
            var mergedSummary = new SummaryResponse
            {
                Summary = new List<SummaryTopic>()
            };
            var topicDictionary = new Dictionary<string, HashSet<string>>();
            foreach (var summary in summaries)
            {
                foreach (var topic in summary.Summary)
                {
                    if (!topicDictionary.ContainsKey(topic.Topic))
                    {
                        topicDictionary[topic.Topic] = new HashSet<string>();
                    }
                    foreach (var point in topic.Points)
                    {
                        topicDictionary[topic.Topic].Add(point);
                    }
                }
            }
            foreach (var kvp in topicDictionary)
            {
                mergedSummary.Summary.Add(new SummaryTopic
                {
                    Topic = kvp.Key,
                    Points = kvp.Value.ToList()
                });
            }
            return mergedSummary;
        }
    }
}
