using SMART_NOC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMART_NOC.Services
{
    /// <summary>
    /// ?? ADVANCED SEARCH ENGINE
    /// Provides comprehensive search, filtering, and sorting capabilities
    /// </summary>
    public class SearchService
    {
        private static SearchService? _instance;
        public static SearchService Instance => _instance ??= new SearchService();

        public enum SearchMode
        {
            Contains,      // Basic substring search
            StartsWith,    // Prefix search
            Regex,         // Regular expression search
            Fuzzy          // Fuzzy/approximate matching
        }

        /// <summary>?? Search tickets by multiple criteria</summary>
        public List<TicketLog> SearchTickets(List<TicketLog> tickets, string query, SearchMode mode = SearchMode.Contains)
        {
            if (string.IsNullOrWhiteSpace(query)) return tickets;

            try
            {
                var results = mode switch
                {
                    SearchMode.Contains => SearchByContains(tickets, query),
                    SearchMode.StartsWith => SearchByStartsWith(tickets, query),
                    SearchMode.Regex => SearchByRegex(tickets, query),
                    SearchMode.Fuzzy => SearchByFuzzy(tickets, query),
                    _ => SearchByContains(tickets, query)
                };

                CrashLogger.LogInfo($"Search completed: {results.Count} results found for '{query}'", "SearchService");
                return results;
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "SearchService_SearchTickets", "ERROR");
                return new List<TicketLog>();
            }
        }

        /// <summary>?? Substring search (case-insensitive)</summary>
        private List<TicketLog> SearchByContains(List<TicketLog> tickets, string query)
        {
            string lowerQuery = query.ToLower();
            return tickets.Where(t =>
                (t.TT_IOH?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.Header?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.SegmentPM?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.RootCause?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.Region?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.Status?.ToLower().Contains(lowerQuery) ?? false) ||
                (t.CutPoint?.ToLower().Contains(lowerQuery) ?? false)
            ).ToList();
        }

        /// <summary>?? Prefix search (starts with)</summary>
        private List<TicketLog> SearchByStartsWith(List<TicketLog> tickets, string query)
        {
            string lowerQuery = query.ToLower();
            return tickets.Where(t =>
                (t.TT_IOH?.ToLower().StartsWith(lowerQuery) ?? false) ||
                (t.Header?.ToLower().StartsWith(lowerQuery) ?? false) ||
                (t.SegmentPM?.ToLower().StartsWith(lowerQuery) ?? false) ||
                (t.RootCause?.ToLower().StartsWith(lowerQuery) ?? false)
            ).ToList();
        }

        /// <summary>?? Regular expression search</summary>
        private List<TicketLog> SearchByRegex(List<TicketLog> tickets, string pattern)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                return tickets.Where(t =>
                    regex.IsMatch(t.TT_IOH ?? "") ||
                    regex.IsMatch(t.Header ?? "") ||
                    regex.IsMatch(t.SegmentPM ?? "") ||
                    regex.IsMatch(t.RootCause ?? "")
                ).ToList();
            }
            catch
            {
                return new List<TicketLog>();
            }
        }

        /// <summary>?? Fuzzy search (Levenshtein distance)</summary>
        private List<TicketLog> SearchByFuzzy(List<TicketLog> tickets, string query, int maxDistance = 2)
        {
            var results = new List<TicketLog>();

            foreach (var ticket in tickets)
            {
                int minDistance = int.MaxValue;

                minDistance = Math.Min(minDistance, LevenshteinDistance(ticket.TT_IOH ?? "", query));
                minDistance = Math.Min(minDistance, LevenshteinDistance(ticket.Header ?? "", query));
                minDistance = Math.Min(minDistance, LevenshteinDistance(ticket.SegmentPM ?? "", query));
                minDistance = Math.Min(minDistance, LevenshteinDistance(ticket.RootCause ?? "", query));

                if (minDistance <= maxDistance)
                {
                    results.Add(ticket);
                }
            }

            return results.OrderBy(t => LevenshteinDistance(t.TT_IOH ?? "", query)).ToList();
        }

        /// <summary>?? Calculate Levenshtein distance (fuzzy matching)</summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            var len1 = s1.Length;
            var len2 = s2.Length;
            var d = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++) d[i, 0] = i;
            for (int j = 0; j <= len2; j++) d[0, j] = j;

            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[len1, len2];
        }

        /// <summary>?? Advanced filtering by multiple criteria</summary>
        public List<TicketLog> FilterByCriteria(List<TicketLog> tickets, Dictionary<string, object> criteria)
        {
            try
            {
                var results = tickets.AsEnumerable();

                if (criteria.ContainsKey("status"))
                {
                    var statusFilter = criteria["status"]?.ToString();
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        results = results.Where(t => t.Status?.Contains(statusFilter) ?? false);
                    }
                }

                if (criteria.ContainsKey("region"))
                {
                    var regionFilter = criteria["region"]?.ToString();
                    if (!string.IsNullOrEmpty(regionFilter))
                    {
                        results = results.Where(t => t.Region == regionFilter);
                    }
                }

                if (criteria.ContainsKey("segment"))
                {
                    var segmentFilter = criteria["segment"]?.ToString();
                    if (!string.IsNullOrEmpty(segmentFilter))
                    {
                        results = results.Where(t => t.SegmentPM?.Contains(segmentFilter) ?? false);
                    }
                }

                if (criteria.ContainsKey("dateFrom") && DateTime.TryParse(criteria["dateFrom"].ToString(), out var dateFrom))
                {
                    results = results.Where(t => DateTime.TryParse(t.OccurTime, out var d) && d >= dateFrom);
                }

                if (criteria.ContainsKey("dateTo") && DateTime.TryParse(criteria["dateTo"].ToString(), out var dateTo))
                {
                    results = results.Where(t => DateTime.TryParse(t.OccurTime, out var d) && d <= dateTo);
                }

                return results.ToList();
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "SearchService_FilterByCriteria", "ERROR");
                return new List<TicketLog>();
            }
        }

        /// <summary>?? Sort results by specified field</summary>
        public List<TicketLog> SortBy(List<TicketLog> tickets, string field, bool ascending = true)
        {
            try
            {
                var sorted = field.ToLower() switch
                {
                    "id" or "tt_ioh" => ascending 
                        ? tickets.OrderBy(t => t.TT_IOH).ToList()
                        : tickets.OrderByDescending(t => t.TT_IOH).ToList(),

                    "segment" or "segmentpm" => ascending
                        ? tickets.OrderBy(t => t.SegmentPM).ToList()
                        : tickets.OrderByDescending(t => t.SegmentPM).ToList(),

                    "status" => ascending
                        ? tickets.OrderBy(t => t.Status).ToList()
                        : tickets.OrderByDescending(t => t.Status).ToList(),

                    "date" or "occurtime" => ascending
                        ? tickets.OrderBy(t => DateTime.TryParse(t.OccurTime, out var d) ? d : DateTime.MinValue).ToList()
                        : tickets.OrderByDescending(t => DateTime.TryParse(t.OccurTime, out var d) ? d : DateTime.MinValue).ToList(),

                    _ => tickets
                };

                return sorted;
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "SearchService_SortBy", "ERROR");
                return tickets;
            }
        }

        /// <summary>?? Get search statistics</summary>
        public Dictionary<string, int> GetSearchStatistics(List<TicketLog> tickets)
        {
            try
            {
                return new Dictionary<string, int>
                {
                    ["total"] = tickets.Count,
                    ["open"] = tickets.Count(t => t.Status?.Contains("DOWN") ?? false),
                    ["closed"] = tickets.Count(t => t.Status?.Contains("UP") ?? false),
                    ["regions"] = tickets.Select(t => t.Region).Distinct().Count(),
                    ["segments"] = tickets.Select(t => t.SegmentPM).Distinct().Count(),
                    ["rootCauses"] = tickets.Select(t => t.RootCause).Distinct().Count()
                };
            }
            catch (Exception ex)
            {
                CrashLogger.Log(ex, "SearchService_GetStatistics", "ERROR");
                return new Dictionary<string, int>();
            }
        }
    }
}
