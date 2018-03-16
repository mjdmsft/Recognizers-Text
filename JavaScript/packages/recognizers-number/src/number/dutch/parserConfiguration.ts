import { ParseResult } from "@microsoft/recognizers-text";
import { INumberParserConfiguration } from "../parsers";
import { CultureInfo, Culture } from "../../culture";
import { DutchNumeric } from "../../resources/DutchNumeric";
import { RegExpUtility } from "@microsoft/recognizers-text"

export class DutchNumberParserConfiguration implements INumberParserConfiguration {

    readonly cardinalNumberMap: ReadonlyMap<string, number>;
    readonly ordinalNumberMap: ReadonlyMap<string, number>;
    readonly roundNumberMap: ReadonlyMap<string, number>;
    readonly cultureInfo: CultureInfo;
    readonly digitalNumberRegex: RegExp;
    readonly fractionMarkerToken: string;
    readonly negativeNumberSignRegex: RegExp;
    readonly halfADozenRegex: RegExp;
    readonly halfADozenText: string;
    readonly langMarker: string;
    readonly nonDecimalSeparatorChar: string;
    readonly decimalSeparatorChar: string;
    readonly wordSeparatorToken: string;
    readonly writtenDecimalSeparatorTexts: ReadonlyArray<string>;
    readonly writtenGroupSeparatorTexts: ReadonlyArray<string>;
    readonly writtenIntegerSeparatorTexts: ReadonlyArray<string>;
    readonly writtenFractionSeparatorTexts: ReadonlyArray<string>;

    constructor(ci?: CultureInfo) {
        if (!ci) {
            ci = new CultureInfo(Culture.Dutch);
        }

        this.cultureInfo = ci;

        this.langMarker = DutchNumeric.LangMarker;
        this.decimalSeparatorChar = DutchNumeric.DecimalSeparatorChar;
        this.fractionMarkerToken = DutchNumeric.FractionMarkerToken;
        this.nonDecimalSeparatorChar = DutchNumeric.NonDecimalSeparatorChar;
        this.halfADozenText = DutchNumeric.HalfADozenText;
        this.wordSeparatorToken = DutchNumeric.WordSeparatorToken;

        this.writtenDecimalSeparatorTexts = DutchNumeric.WrittenDecimalSeparatorTexts;
        this.writtenGroupSeparatorTexts = DutchNumeric.WrittenGroupSeparatorTexts;
        this.writtenIntegerSeparatorTexts = DutchNumeric.WrittenIntegerSeparatorTexts;
        this.writtenFractionSeparatorTexts = DutchNumeric.WrittenFractionSeparatorTexts;

        this.cardinalNumberMap = DutchNumeric.CardinalNumberMap;
        this.ordinalNumberMap = DutchNumeric.OrdinalNumberMap;
        this.roundNumberMap = DutchNumeric.RoundNumberMap;
        this.negativeNumberSignRegex = RegExpUtility.getSafeRegExp(DutchNumeric.NegativeNumberSignRegex, "is");
        this.halfADozenRegex = RegExpUtility.getSafeRegExp(DutchNumeric.HalfADozenRegex);
        this.digitalNumberRegex = RegExpUtility.getSafeRegExp(DutchNumeric.DigitalNumberRegex);
    }

    normalizeTokenSet(tokens: ReadonlyArray<string>, context: ParseResult): ReadonlyArray<string> {
        return tokens;
    }

    resolveCompositeNumber(numberStr: string): number {
        if (this.ordinalNumberMap.has(numberStr)) {
            return this.ordinalNumberMap.get(numberStr);
        }

        if (this.cardinalNumberMap.has(numberStr)) {
            return this.cardinalNumberMap.get(numberStr);
        }

        let value = 0;
        let finalValue = 0;
        let strBuilder = "";
        let lastGoodChar = 0;
        for (let i = 0; i < numberStr.length; i++) {
            strBuilder = strBuilder.concat(numberStr[i]);
            if (this.cardinalNumberMap.has(strBuilder) && this.cardinalNumberMap.get(strBuilder) > value) {
                lastGoodChar = i;
                value = this.cardinalNumberMap.get(strBuilder);
            }

            if ((i + 1) === numberStr.length) {
                finalValue += value;
                strBuilder = "";
                i = lastGoodChar++;
                value = 0;
            }
        }
        
        return finalValue;
    }
}