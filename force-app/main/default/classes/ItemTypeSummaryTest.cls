@isTest
public class ItemTypeSummaryTest {

    @isTest
    static void testBuildHTML() {
        // Get two active picklist values for Item_Type__c
        Schema.DescribeFieldResult itemTypeField = Item__c.Item_Type__c.getDescribe();
        List<Schema.PicklistEntry> itemTypeEntries = itemTypeField.getPicklistValues();
        
        String type1 = null;
        String type2 = null;
        for (Schema.PicklistEntry entry : itemTypeEntries) {
            if (entry.isActive() && type1 == null) {
                type1 = entry.getValue();
            } else if (entry.isActive() && type2 == null) {
                type2 = entry.getValue();
                break;
            }
        }

        // Get two active picklist values for Material_Type__c
        Schema.DescribeFieldResult materialTypeField = Item__c.Material_Type__c.getDescribe();
        List<Schema.PicklistEntry> materialTypeEntries = materialTypeField.getPicklistValues();

        String material1 = null;
        String material2 = null;
        for (Schema.PicklistEntry entry : materialTypeEntries) {
            if (entry.isActive() && material1 == null) {
                material1 = entry.getValue();
            } else if (entry.isActive() && material2 == null) {
                material2 = entry.getValue();
                break;
            }
        }

        // Create test items
        List<Item__c> testItems = new List<Item__c>{
            new Item__c(Item_Type__c = type1, Material_Type__c = material1, NumberofItems__c = 3),
            new Item__c(Item_Type__c = type1, Material_Type__c = material1, NumberofItems__c = 2),
            new Item__c(Item_Type__c = type1, Material_Type__c = material2, NumberofItems__c = 4),
            new Item__c(Item_Type__c = type1, Material_Type__c = null, NumberofItems__c = 1), // blank material
            new Item__c(Item_Type__c = type2, Material_Type__c = material1, NumberofItems__c = 5),
            new Item__c(Item_Type__c = null, Material_Type__c = material1, NumberofItems__c = 1) // should be skipped
        };

        ItemTypeSummary.FlowInput input = new ItemTypeSummary.FlowInput();
        input.items = testItems;

        Test.startTest();
        List<ItemTypeSummary.FlowOutput> outputs =
            ItemTypeSummary.getSummary(new List<ItemTypeSummary.FlowInput>{ input });
        Test.stopTest();

        Assert.isNotNull(outputs);
        Assert.isTrue(outputs.size() > 0);

        String html = outputs[0].summaryHtml;
        Assert.isNotNull(html);

        // type1 + material1 should total 5
        Assert.isTrue(html.contains(type1));
        Assert.isTrue(html.contains(material1));
        Assert.isTrue(html.contains('5'));

        // type1 + material2 should appear with total 4
        Assert.isTrue(html.contains(material2));
        Assert.isTrue(html.contains('4'));

        // blank material type should show as dash
        Assert.isTrue(html.contains('—'));

        // type2 should appear
        Assert.isTrue(html.contains(type2));

        // null item type should not produce a row
        Assert.isFalse(html.contains('<td style="padding:8px; border-right:1px solid #ccc;">null</td>'));

        // valid table structure
        Assert.isTrue(html.contains('<table'));
        Assert.isTrue(html.contains('</table>'));
        Assert.isTrue(html.contains('<thead'));
        Assert.isTrue(html.contains('<tbody'));
    }

    @isTest
    static void testEmptyItems() {
        ItemTypeSummary.FlowInput input = new ItemTypeSummary.FlowInput();
        input.items = new List<Item__c>();

        Test.startTest();
        List<ItemTypeSummary.FlowOutput> outputs =
            ItemTypeSummary.getSummary(new List<ItemTypeSummary.FlowInput>{ input });
        Test.stopTest();

        Assert.isNotNull(outputs);
        String html = outputs[0].summaryHtml;

        Assert.isTrue(html.contains('<table'));
        Assert.isTrue(html.contains('</table>'));
        Assert.isFalse(html.contains('<td'));
    }
}