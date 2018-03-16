import { BaseNumberExtractor, RegExpValue, BasePercentageExtractor } from "../extractors";
import { Constants } from "../constants";
import { NumberMode, LongFormatType } from "../models";
import { DutchNumeric } from "../../resources/DutchNumeric";
import { RegExpUtility } from "@microsoft/recognizers-text"

export class DutchNumberExtractor extends BaseNumberExtractor {
    protected extractType: string = Constants.SYS_NUM;

    constructor(mode: NumberMode = NumberMode.Default) {
        super();
        let regexes = new Array<RegExpValue>();

        // Add Cardinal
        let cardExtract: DutchCardinalExtractor | null = null;
        switch (mode) {
            case NumberMode.PureNumber:
                cardExtract = new DutchCardinalExtractor(DutchNumeric.PlaceHolderPureNumber);
                break;
            case NumberMode.Currency:
                regexes.push({ regExp: RegExpUtility.getSafeRegExp(DutchNumeric.CurrencyRegex, "gs"), value: "IntegerNum" });
                break;
            case NumberMode.Default:
                break;
        }

        if (cardExtract === null) {
            cardExtract = new DutchCardinalExtractor();
        }

        cardExtract.regexes.forEach(r => regexes.push(r));

        // Add Fraction
        let fracExtract = new DutchFractionExtractor();
        fracExtract.regexes.forEach(r => regexes.push(r));

        this.regexes = regexes;
    }
}

export class DutchCardinalExtractor extends BaseNumberExtractor {
    protected extractType: string = Constants.SYS_NUM_CARDINAL;

    constructor(placeholder: string = DutchNumeric.PlaceHolderDefault) {
        super();
        let regexes = new Array<RegExpValue>();

        // Add Integer Regexes
        let intExtract = new DutchIntegerExtractor(placeholder);
        intExtract.regexes.forEach(r => regexes.push(r));

        // Add Double Regexes
        let doubleExtract = new DutchDoubleExtractor(placeholder);
        doubleExtract.regexes.forEach(r => regexes.push(r));

        this.regexes = regexes;
    }
}

export class DutchIntegerExtractor extends BaseNumberExtractor {
    protected extractType: string = Constants.SYS_NUM_INTEGER;

    constructor(placeholder: string = DutchNumeric.PlaceHolderDefault) {
        super();

        let regexes = new Array<RegExpValue>(
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.NumbersWithPlaceHolder(placeholder), "gi"),
                value: "IntegerNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.NumbersWithSuffix, "gs"),
                value: "IntegerNum"
            },
            {
                regExp: this.generateLongFormatNumberRegexes(LongFormatType.integerNumDot, placeholder),
                value: "IntegerNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.RoundNumberIntegerRegexWithLocks),
                value: "IntegerNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.NumbersWithDozenSuffix),
                value: "IntegerNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.AllIntRegexWithLocks),
                value: "IntegerFr"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.AllIntRegexWithDozenSuffixLocks),
                value: "IntegerFr"
            }
        );

        this.regexes = regexes;
    }
}

export class DutchDoubleExtractor extends BaseNumberExtractor {
    protected extractType: string = Constants.SYS_NUM_DOUBLE;

    constructor(placeholder: string = DutchNumeric.PlaceHolderDefault) {
        super();

        let regexes = new Array<RegExpValue>(
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleDecimalPointRegex(placeholder)),
                value: "DoubleNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleWithoutIntegralRegex(placeholder)),
                value: "DoubleNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleWithMultiplierRegex, "gs"),
                value: "DoubleNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleWithRoundNumber),
                value: "DoubleNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleAllFloatRegex),
                value: "DoubleFr"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleExponentialNotationRegex),
                value: "DoublePow"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.DoubleCaretExponentialNotationRegex),
                value: "DoublePow"
            },
            {
                regExp: this.generateLongFormatNumberRegexes(LongFormatType.doubleNumDotComma, placeholder),
                value: "DoubleNum"
            }
        );

        this.regexes = regexes;
    }
}

export class DutchFractionExtractor extends BaseNumberExtractor {

    protected extractType: string = Constants.SYS_NUM_FRACTION;

    constructor() {
        super();

        let regexes = new Array<RegExpValue>(
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.FractionNotationRegex),
                value: "FracNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.FractionNotationWithSpacesRegex),
                value: "FracNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.FractionNounRegex),
                value: "FracFr"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.FractionNounWithArticleRegex),
                value: "FracFr"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.FractionPrepositionRegex),
                value: "FracFr"
            }
        );

        this.regexes = regexes;
    }
}

export class DutchOrdinalExtractor extends BaseNumberExtractor {
    protected extractType: string = Constants.SYS_NUM_ORDINAL;

    constructor() {
        super();
        let regexes = new Array<RegExpValue>(
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.OrdinalSuffixRegex),
                value: "OrdinalNum"
            },
            {
                regExp: RegExpUtility.getSafeRegExp(DutchNumeric.OrdinalDutchRegex),
                value: "OrdFr"
            }
        );

        this.regexes = regexes;
    }
}

export class DutchPercentageExtractor extends BasePercentageExtractor {
    constructor() {
        super(new DutchNumberExtractor())
    }

    protected initRegexes(): Array<RegExp> {
        let regexStrs = [
            DutchNumeric.NumberWithSuffixPercentage,
            DutchNumeric.NumberWithPrefixPercentage
        ];

        return this.buildRegexes(regexStrs);
    }
}