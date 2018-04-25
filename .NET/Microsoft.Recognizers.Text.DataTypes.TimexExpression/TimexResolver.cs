﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Recognizers.Text.DataTypes.TimexExpression
{
    public static class TimexResolver
    {
        public class Resolution
        {
            public class Entry
            {
                public string Timex { get; set; }

                public string Type { get; set; }

                public string Value { get; set; }

                public string Start { get; set; }

                public string End { get; set; }
            }

            public List<Entry> Values { get; private set; }

            public Resolution()
            {
                Values = new List<Entry>();
            }
        }

        public static Resolution Resolve(string[] timexArray, System.DateTime date = default(System.DateTime)) {

            var resolution = new Resolution();
            foreach (var timex in timexArray)
            {
                var t = new TimexProperty(timex);
                var r = ResolveTimex(t, date);
                resolution.Values.AddRange(r);
            }

            return resolution;
        }

        private static List<Resolution.Entry> ResolveTimex(TimexProperty timex, System.DateTime date)
        {
            var types = timex.Types.Count != 0 ? timex.Types : TimexInference.Infer(timex);

            if (types.Contains(Constants.TimexTypes.DateTimeRange))
            {
                return ResolveDateTimeRange(timex);
            }

            if (types.Contains(Constants.TimexTypes.Definite) && types.Contains(Constants.TimexTypes.Time))
            {
                return ResolveDefiniteTime(timex);
            }

            if (types.Contains(Constants.TimexTypes.Definite))
            {
                return ResolveDefinite(timex);
            }

            if (types.Contains(Constants.TimexTypes.DateRange))
            {
                return ResolveDateRange(timex, date);
            }

            if (types.Contains(Constants.TimexTypes.TimeRange))
            {
                return ResolveTimeRange(timex);
            }

            if (types.Contains(Constants.TimexTypes.DateTime))
            {
                return ResolveDateTime(timex, date);
            }

            if (types.Contains(Constants.TimexTypes.Duration))
            {
                return ResolveDuration(timex);
            }

            if (types.Contains(Constants.TimexTypes.Date))
            {
                return ResolveDate(timex, date);
            }

            if (types.Contains(Constants.TimexTypes.Time))
            {
                return ResolveTime(timex);
            }

            return new List<Resolution.Entry>();
        }

        private static List<Resolution.Entry> ResolveDefiniteTime(TimexProperty timex)
        {
            return new List<Resolution.Entry>
            {
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "datetime",
                    Value = $"{TimexValue.DateValue(timex)} {TimexValue.TimeValue(timex)}"
                }
            };
        }

        private static List<Resolution.Entry> ResolveDefinite(TimexProperty timex)
        {
            return new List<Resolution.Entry>
            {
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "date",
                    Value = TimexValue.DateValue(timex)
                }
            };
        }
        private static List<Resolution.Entry> ResolveDate(TimexProperty timex, System.DateTime date)
        {
            return new List<Resolution.Entry>
            {
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "date",
                    Value = LastDateValue(timex, date)
                },
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "date",
                    Value = NextDateValue(timex, date)
                }
            };
        }

        private static string LastDateValue(TimexProperty timex, System.DateTime date)
        {
            if (timex.Month != null && timex.DayOfMonth != null)
            {
                return TimexValue.DateValue(new TimexProperty
                {
                    Year = date.Year - 1,
                    Month = timex.Month,
                    DayOfMonth = timex.DayOfMonth
                });
            }

            if (timex.DayOfWeek != null)
            {
                var day = timex.DayOfWeek == 7 ? DayOfWeek.Monday : (DayOfWeek)timex.DayOfWeek;
                var result = TimexDateHelpers.DateOfLastDay(day, date);
                return TimexValue.DateValue(new TimexProperty
                {
                    Year = result.Year,
                    Month = result.Month,
                    DayOfMonth = result.Day
                });
            }

            return string.Empty;
        }

        private static string NextDateValue(TimexProperty timex, System.DateTime date)
        {
            if (timex.Month != null && timex.DayOfMonth != null)
            {
                return TimexValue.DateValue(new TimexProperty
                {
                    Year = date.Year,
                    Month = timex.Month,
                    DayOfMonth = timex.DayOfMonth
                });
            }

            if (timex.DayOfWeek != null)
            {
                var day = timex.DayOfWeek == 7 ? DayOfWeek.Monday : (DayOfWeek)timex.DayOfWeek;
                var result = TimexDateHelpers.DateOfNextDay(day, date);
                return TimexValue.DateValue(new TimexProperty
                {
                    Year = result.Year,
                    Month = result.Month,
                    DayOfMonth = result.Day
                });
            }

            return string.Empty;
        }

        private static List<Resolution.Entry> ResolveTime(TimexProperty timex)
        {
            return new List<Resolution.Entry>
            {
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "time",
                    Value = TimexValue.TimeValue(timex)
                }
            };
        }

        private static List<Resolution.Entry> ResolveDuration(TimexProperty timex)
        {
            return new List<Resolution.Entry>
            {
                new Resolution.Entry
                {
                    Timex = timex.TimexValue,
                    Type = "duration",
                    Value = TimexValue.DurationValue(timex)
                }
            };
        }

        private static Tuple<string, string> MonthDateRange(int year, int month) {
            return new Tuple<string, string>(
                TimexValue.DateValue(new TimexProperty { Year = year, Month = month, DayOfMonth = 1 }),
                TimexValue.DateValue(new TimexProperty { Year = year, Month = month + 1, DayOfMonth = 1 })
            );
        }

        private static List<Resolution.Entry> ResolveDateRange(TimexProperty timex, System.DateTime date)
        {
            if (timex.Season != null)
            {
                return new List<Resolution.Entry>
                {
                    new Resolution.Entry
                    {
                        Timex = timex.TimexValue,
                        Type = "daterange",
                        Value = "not resolved"
                    }
                };
            }
            else
            {
                if (timex.Year != null && timex.Month != null)
                {
                    var dateRange = MonthDateRange(timex.Year.Value, timex.Month.Value);
                    return new List<Resolution.Entry>
                    {
                        new Resolution.Entry
                        {
                            Timex = timex.TimexValue,
                            Type = "daterange",
                            Start = dateRange.Item1,
                            End = dateRange.Item2
                        }
                    };
                }

                if (timex.Month != null)
                {
                    var y = date.Year;
                    var lastYearDateRange = MonthDateRange(y - 1, timex.Month.Value);
                    var thisYearDateRange = MonthDateRange(y, timex.Month.Value);
            
                    return new List<Resolution.Entry>
                    {
                        new Resolution.Entry
                        {
                            Timex = timex.TimexValue,
                            Type = "daterange",
                            Start = lastYearDateRange.Item1,
                            End = lastYearDateRange.Item2
                        },
                        new Resolution.Entry
                        {
                            Timex = timex.TimexValue,
                            Type = "daterange",
                            Start = thisYearDateRange.Item1,
                            End = thisYearDateRange.Item2
                        }
                    };
                }

                return new List<Resolution.Entry>();
            }
        }

        private static Tuple<string, string> PartOfDayTimeRange(TimexProperty timex)
        {
            switch (timex.PartOfDay)
            {
                case "MO": return new Tuple<string, string>("08:00:00", "12:00:00");
                case "AF": return new Tuple<string, string>("12:00:00", "16:00:00");
                case "EV": return new Tuple<string, string>("16:00:00", "20:00:00");
                case "NI": return new Tuple<string, string>("20:00:00", "24:00:00");
            }

            return new Tuple<string, string>("not resolved", "not resolved");
        }

        private static List<Resolution.Entry> ResolveTimeRange(TimexProperty timex)
        {
            if (timex.PartOfDay != null)
            {
                var range = PartOfDayTimeRange(timex);
                return new List<Resolution.Entry>
                {
                    new Resolution.Entry
                    {
                        Timex = timex.TimexValue,
                        Type = "timerange",
                        Start = range.Item1,
                        End = range.Item2
                    }
                };
            }
            else
            {
                var range = TimexHelpers.ExpandTimeRange(timex);
                return new List<Resolution.Entry>
                {
                    new Resolution.Entry
                    {
                        Timex = timex.TimexValue,
                        Type = "timerange",
                        Start = TimexValue.TimeValue(range.Start),
                        End = TimexValue.TimeValue(range.End)
                    }
                };
            }
        }

        private static List<Resolution.Entry> ResolveDateTime(TimexProperty timex, System.DateTime date)
        {
            var resolvedDates = ResolveDate(timex, date);
            foreach (var resolved in resolvedDates)
            {
                resolved.Type = "datetime";
                resolved.Value = $"{resolved.Value} {TimexValue.TimeValue(timex)}";
            }

            return resolvedDates;
        }

        private static List<Resolution.Entry> ResolveDateTimeRange(TimexProperty timex)
        {
            if (timex.PartOfDay != null)
            {
                var date = TimexValue.DateValue(timex);
                var timeRange = PartOfDayTimeRange(timex);
                return new List<Resolution.Entry>
                {
                    new Resolution.Entry
                    {
                        Timex = timex.TimexValue,
                        Type = "datetimerange",
                        Start = $"{date} {timeRange.Item1}",
                        End = $"{date} {timeRange.Item2}"
                    }
                };
            }
            else
            {
                var range = TimexHelpers.ExpandDateTimeRange(timex);
                return new List<Resolution.Entry>
                {
                    new Resolution.Entry
                    {
                        Timex = timex.TimexValue,
                        Type = "datetimerange",
                        Start = $"{TimexValue.DateValue(range.Start)} {TimexValue.TimeValue(range.Start)}",
                        End = $"{TimexValue.DateValue(range.End)} {TimexValue.TimeValue(range.End)}"
                    }
                };
            }
        }
    }
}
