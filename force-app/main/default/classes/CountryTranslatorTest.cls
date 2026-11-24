@IsTest
private class CountryTranslatorTest {
    @IsTest static void knownCode()      { System.assertEquals('איטליה', CountryTranslator.toHebrew('T1129102')); }
    @IsTest static void unknownCode()    { System.assertEquals('T9999999', CountryTranslator.toHebrew('T9999999')); }
    @IsTest static void nullInput()      { System.assertEquals(null, CountryTranslator.toHebrew(null)); }
    @IsTest static void whitespaceTrim() { System.assertEquals('ישראל', CountryTranslator.toHebrew('  T1129004  ')); }
    @IsTest static void invocable() {
        List<String> res = CountryTranslator.translateCodesToHebrew(new List<String>{'T1129102','T9999999'});
        System.assertEquals(2, res.size());
        System.assertEquals('איטליה', res[0]);
        System.assertEquals('T9999999', res[1]);
    }
}