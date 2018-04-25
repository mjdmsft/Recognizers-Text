﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Definitions.Japanese;

namespace Microsoft.Recognizers.Text.Number.Japanese
{
    public class IntegerExtractor : BaseNumberExtractor
    {
        internal sealed override ImmutableDictionary<Regex, string> Regexes { get; }

        protected sealed override string ExtractType { get; } = Constants.SYS_NUM_INTEGER;

        public IntegerExtractor(JapaneseNumberExtractorMode mode = JapaneseNumberExtractorMode.Default)
        {
            var regexes = new Dictionary<Regex, string>()
            {
                {
                    // 123456,  －１２３４５６
                    new Regex(NumbersDefinitions.NumbersSpecialsChars, RegexOptions.IgnoreCase | RegexOptions.Singleline),
                              "IntegerNum"
                },
                {
                    //15k,  16 G
                    new Regex(NumbersDefinitions.NumbersSpecialsCharsWithSuffix, RegexOptions.Singleline),
                              "IntegerNum"
                },
                {
                    //1,234,  ２，３３２，１１１
                    new Regex(NumbersDefinitions.DottedNumbersSpecialsChar, RegexOptions.IgnoreCase | RegexOptions.Singleline),
                              "IntegerNum"
                },
                {
                    //半百  半ダース
                    new Regex(NumbersDefinitions.NumbersWithHalfDozen, RegexOptions.Singleline),
                              "IntegerJpn"
                },
                {
                    //一ダース  五十ダース
                    new Regex(NumbersDefinitions.NumbersWithDozen, RegexOptions.Singleline),
                              "IntegerJpn"
                }
            };

            switch (mode)
            {
                case JapaneseNumberExtractorMode.Default:
                    // 一百五十五, 负一亿三百二十二. 
                    // Uses an allow list to avoid extracting "西九条" from "九"
                    regexes.Add(new Regex(NumbersDefinitions.NumbersWithAllowListRegex, RegexOptions.Singleline),
                                "IntegerJpn");
                    break;

                case JapaneseNumberExtractorMode.ExtractAll:
                    // 一百五十五, 负一亿三百二十二, "西九条" from "九"
                    // Uses no allow lists and extracts all potential integers (useful in Units, for example).
                    regexes.Add(new Regex(NumbersDefinitions.NumbersAggressiveRegex, RegexOptions.Singleline),
                                "IntegerJpn");
                    break;
            }

            Regexes = regexes.ToImmutableDictionary();
        }
    }
}
