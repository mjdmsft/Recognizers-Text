﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using DateObject = System.DateTime;

using Microsoft.Recognizers.Text.Number;
using System;
using System.Linq;

namespace Microsoft.Recognizers.Text.DateTime
{
    public class BaseDatePeriodExtractor : IDateTimeExtractor
    {
        public static readonly string ExtractorName = Constants.SYS_DATETIME_DATEPERIOD; // "DatePeriod";

        private readonly IDatePeriodExtractorConfiguration config;

        public BaseDatePeriodExtractor(IDatePeriodExtractorConfiguration config)
        {
            this.config = config;
        }

        public List<ExtractResult> Extract(string text)
        {
            return Extract(text, DateObject.Now);
        }

        public List<ExtractResult> Extract(string text, DateObject reference)
        {
            var tokens = new List<Token>();
            tokens.AddRange(MatchSimpleCases(text));
            var simpleCasesResults = Token.MergeAllTokens(tokens, text, ExtractorName);
            tokens.AddRange(MergeTwoTimePoints(text, reference));
            tokens.AddRange(MatchDuration(text, reference));
            tokens.AddRange(SingleTimePointWithPatterns(text, reference));
            tokens.AddRange(MatchComplexCases(text, simpleCasesResults, reference));
            tokens.AddRange(MatchYearPeriod(text, reference));

            return Token.MergeAllTokens(tokens, text, ExtractorName);
        }

        private List<Token> MatchYearPeriod(string text, DateObject referece)
        {
            var ret = new List<Token>();

            var matches = this.config.YearPeriodRegex.Matches(text);
            foreach (Match match in matches)
            {
                var matchYear = this.config.YearRegex.Match(match.Value);
                if (matchYear.Success && matchYear.Length == match.Value.Length)
                {
                    var year = ((BaseDateExtractor)this.config.DatePointExtractor).GetYearFromText(matchYear);
                    if (!(year >= Constants.MinYearNum && year <= Constants.MaxYearNum))
                    {
                        continue;
                    }
                }
                ret.Add(new Token(match.Index, match.Index + match.Length));
            }

            return ret;
        }

        private List<Token> MatchSimpleCases(string text)
        {
            var ret = new List<Token>();
            foreach (var regex in this.config.SimpleCasesRegexes)
            {
                var matches = regex.Matches(text);
                foreach (Match match in matches)
                {
                    var matchYear = this.config.YearRegex.Match(match.Value);
                    if (matchYear.Success && matchYear.Length == match.Value.Length)
                    {
                        var year = ((BaseDateExtractor)this.config.DatePointExtractor).GetYearFromText(matchYear);
                        if (!(year >= Constants.MinYearNum && year <= Constants.MaxYearNum))
                        {
                            continue;
                        }
                    }
                    ret.Add(new Token(match.Index, match.Index + match.Length));
                }
            }
            return ret;
        }

        // Complex cases refer to the combination of daterange and datepoint
        // For Example: from|between {DateRange|DatePoint} to|till|and {DateRange|DatePoint}
        private List<Token> MatchComplexCases(string text, List<ExtractResult> simpleDateRangeResults, DateObject reference)
        {
            var er = this.config.DatePointExtractor.Extract(text, reference);

            // Filter out DateRange results that are part of DatePoint results
            // For example, "Feb 1st 2018" => "Feb" and "2018" should be filtered out here
            er.AddRange(simpleDateRangeResults
                .Where(simpleDateRange => !er.Any(datePoint => (datePoint.Start <= simpleDateRange.Start && datePoint.Start + datePoint.Length >= simpleDateRange.Start + simpleDateRange.Length))));

            er = er.OrderBy(t => t.Start).ToList();

            return MergeMultipleExtractions(text, er);
        }

        private List<Token> MergeTwoTimePoints(string text, DateObject reference)
        {
            var er = this.config.DatePointExtractor.Extract(text, reference);

            return MergeMultipleExtractions(text, er);
        }

        private List<Token> MergeMultipleExtractions(string text, List<ExtractResult> extractionResults)
        {
            var ret = new List<Token>();

            if (extractionResults.Count <= 1)
            {
                return ret;
            }

            var idx = 0;

            while (idx < extractionResults.Count - 1)
            {
                var middleBegin = extractionResults[idx].Start + extractionResults[idx].Length ?? 0;
                var middleEnd = extractionResults[idx + 1].Start ?? 0;
                if (middleBegin >= middleEnd)
                {
                    idx++;
                    continue;
                }

                var middleStr = text.Substring(middleBegin, middleEnd - middleBegin).Trim().ToLowerInvariant();
                var match = this.config.TillRegex.Match(middleStr);
                if (match.Success && match.Index == 0 && match.Length == middleStr.Length)
                {
                    var periodBegin = extractionResults[idx].Start ?? 0;
                    var periodEnd = (extractionResults[idx + 1].Start ?? 0) + (extractionResults[idx + 1].Length ?? 0);

                    // handle "from/between" together with till words (till/until/through...)
                    var beforeStr = text.Substring(0, periodBegin).Trim().ToLowerInvariant();
                    if (this.config.GetFromTokenIndex(beforeStr, out int fromIndex)
                        || this.config.GetBetweenTokenIndex(beforeStr, out fromIndex))
                    {
                        periodBegin = fromIndex;
                    }

                    ret.Add(new Token(periodBegin, periodEnd));

                    // merge two tokens here, increase the index by two
                    idx += 2;
                    continue;
                }

                if (this.config.HasConnectorToken(middleStr))
                {
                    var periodBegin = extractionResults[idx].Start ?? 0;
                    var periodEnd = (extractionResults[idx + 1].Start ?? 0) + (extractionResults[idx + 1].Length ?? 0);

                    // handle "between...and..." case
                    var beforeStr = text.Substring(0, periodBegin).Trim().ToLowerInvariant();
                    if (this.config.GetBetweenTokenIndex(beforeStr, out int beforeIndex))
                    {
                        periodBegin = beforeIndex;
                        ret.Add(new Token(periodBegin, periodEnd));

                        // merge two tokens here, increase the index by two
                        idx += 2;
                        continue;
                    }
                }
                idx++;
            }

            return ret;
        }

        //Extract the month of date, week of date to a date range
        private List<Token> SingleTimePointWithPatterns(string text, DateObject reference)
        {
            var ret = new List<Token>();
            var er = this.config.DatePointExtractor.Extract(text, reference);
            if (er.Count < 1)
            {
                return ret;
            }

            foreach (var extractionResult in er)
            {
                if (extractionResult.Start != null && extractionResult.Length != null)
                {
                    string beforeString = text.Substring(0, (int)extractionResult.Start);
                    ret.AddRange(GetTokenForRegexMatching(beforeString, config.WeekOfRegex, extractionResult));
                    ret.AddRange(GetTokenForRegexMatching(beforeString, config.MonthOfRegex, extractionResult));
                }
            }

            return ret;
        }

        private List<Token> GetTokenForRegexMatching(string text, Regex regex, ExtractResult er)
        {
            var ret = new List<Token>();
            var match = regex.Match(text);
            if (match.Success && text.Trim().EndsWith(match.Value.Trim()))
            {
                var startIndex = text.LastIndexOf(match.Value);
                ret.Add(new Token(startIndex, (int)er.Start + (int)er.Length));
            }
            return ret;
        }

        public List<Token> MatchDuration(string text, DateObject reference)
        {
            var ret = new List<Token>();

            var durations = new List<Token>();
            var durationExtractions = config.DurationExtractor.Extract(text, reference);
            foreach (var durationExtraction in durationExtractions)
            {
                var match = config.DateUnitRegex.Match(durationExtraction.Text);
                if (match.Success)
                {
                    durations.Add(new Token(durationExtraction.Start ?? 0,
                        (durationExtraction.Start + durationExtraction.Length ?? 0)));
                }
            }

            foreach (var duration in durations)
            {
                var beforeStr = text.Substring(0, duration.Start).ToLowerInvariant();
                var afterStr = text.Substring(duration.Start + duration.Length).ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(beforeStr) && string.IsNullOrWhiteSpace(afterStr))
                {
                    continue;
                }

                // Match prefix
                var match = this.config.PastRegex.Match(beforeStr);
                if (MatchPrefixRegexInSegment(beforeStr, match))
                {
                    ret.Add(new Token(match.Index, duration.End));
                    continue;
                }

                // within "Days/Weeks/Months/Years" should be handled as dateRange here
                // if duration contains "Seconds/Minutes/Hours", it should be treated as datetimeRange
                match = Regex.Match(beforeStr, config.WithinNextPrefixRegex.ToString(),
                    RegexOptions.RightToLeft | config.WithinNextPrefixRegex.Options);
                if (MatchPrefixRegexInSegment(beforeStr, match))
                {
                    var startToken = match.Index;
                    var matchDate = config.DateUnitRegex.Match(text.Substring(duration.Start, duration.Length));
                    var matchTime = config.TimeUnitRegex.Match(text.Substring(duration.Start, duration.Length));

                    if (matchDate.Success && !matchTime.Success)
                    {
                        ret.Add(new Token(startToken, duration.End));
                    }
                }

                // For cases like "next five days"
                match = Regex.Match(beforeStr, config.FutureRegex.ToString(),
                    RegexOptions.RightToLeft | config.FutureRegex.Options);
                if (MatchPrefixRegexInSegment(beforeStr, match))
                {
                    ret.Add(new Token(match.Index, duration.End));
                    continue;
                }

                // Match suffix
                match = this.config.PastRegex.Match(afterStr);
                if (MatchSuffixRegexInSegment(afterStr, match))
                {
                    ret.Add(new Token(duration.Start, duration.End + match.Index + match.Length));
                    continue;
                }

                match = this.config.FutureRegex.Match(afterStr);
                if (MatchSuffixRegexInSegment(afterStr, match))
                {
                    ret.Add(new Token(duration.Start, duration.End + match.Index + match.Length));
                    continue;
                }

                match = this.config.FutureSuffixRegex.Match(afterStr);
                if (MatchSuffixRegexInSegment(afterStr, match))
                {
                    ret.Add(new Token(duration.Start, duration.End + match.Index + match.Length));
                    continue;
                }

                // in Range Weeks should be handled as dateRange here
                match = Regex.Match(beforeStr, config.InConnectorRegex.ToString(),
                    RegexOptions.RightToLeft | config.InConnectorRegex.Options);
                if (MatchPrefixRegexInSegment(beforeStr, match))
                {
                    var startToken = match.Index;
                    match = config.RangeUnitRegex.Match(text.Substring(duration.Start, duration.Length));
                    if (match.Success)
                    {
                        ret.Add(new Token(startToken, duration.End));
                    }
                }
            }

            return ret;
        }

        private bool MatchSuffixRegexInSegment(string afterStr, Match match)
        {
            var result = match.Success && string.IsNullOrWhiteSpace(afterStr.Substring(0, match.Index));
            return result;
        }

        private bool MatchPrefixRegexInSegment(string beforeStr, Match match)
        {
            var result = match.Success && string.IsNullOrWhiteSpace(beforeStr.Substring(match.Index + match.Length));
            return result;
        }

    }


}
