﻿using System.Text.RegularExpressions;

namespace Microsoft.Recognizers.Text.DateTime.German
{
    public sealed class GermanMergedParserConfiguration : GermanCommonDateTimeParserConfiguration, IMergedParserConfiguration
    {
        public Regex BeforeRegex { get; }

        public Regex AfterRegex { get; }

        public Regex SinceRegex { get; }

        public Regex YearAfterRegex { get; }

        public Regex YearRegex { get; }

        public IDateTimeParser GetParser { get; }

        public IDateTimeParser HolidayParser { get; }

        public IDateTimeParser TimeZoneParser { get; }

        public GermanMergedParserConfiguration(DateTimeOptions options) : base(options)
        {
            BeforeRegex = GermanMergedExtractorConfiguration.BeforeRegex;
            AfterRegex = GermanMergedExtractorConfiguration.AfterRegex;
            SinceRegex = GermanMergedExtractorConfiguration.SinceRegex;
            YearAfterRegex = GermanMergedExtractorConfiguration.YearAfterRegex;
            YearRegex = GermanDatePeriodExtractorConfiguration.YearRegex;
            DatePeriodParser = new BaseDatePeriodParser(new GermanDatePeriodParserConfiguration(this));
            TimePeriodParser = new BaseTimePeriodParser(new GermanTimePeriodParserConfiguration(this));
            DateTimePeriodParser = new BaseDateTimePeriodParser(new GermanDateTimePeriodParserConfiguration(this));
            GetParser = new BaseSetParser(new GermanSetParserConfiguration(this));
            HolidayParser = new BaseHolidayParser(new GermanHolidayParserConfiguration());
            TimeZoneParser = new BaseTimeZoneParser();
        }
    }
}
