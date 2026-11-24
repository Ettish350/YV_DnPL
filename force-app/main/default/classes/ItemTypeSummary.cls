public class ItemTypeSummary {

    public class FlowInput {
        @InvocableVariable(required=true label='Item Records')
        public List<Item__c> items;
    }

    public class FlowOutput {
        @InvocableVariable(label='Summary HTML')
        public String summaryHtml;
    }

    @InvocableMethod(label='Get Item Type Summary HTML' description='Returns an HTML summary table of items grouped by type and material type')
    public static List<FlowOutput> getSummary(List<FlowInput> inputs) {
        List<FlowOutput> outputs = new List<FlowOutput>();

        for (FlowInput input : inputs) {
            FlowOutput output = new FlowOutput();
            output.summaryHtml = buildHTML(input.items);
            outputs.add(output);
        }

        return outputs;
    }

    private static String buildHTML(List<Item__c> items) {

        // Tally NumberofItems__c per Item_Type__c + Material_Type__c combination
        Map<String, Map<String, Decimal>> totalByTypeAndMaterial = new Map<String, Map<String, Decimal>>();

        for (Item__c item : items) {
            String itemType = item.Item_Type__c;
            String materialType = String.isBlank(item.Material_Type__c) ? '' : item.Material_Type__c;
            Decimal count = item.NumberofItems__c != null ? item.NumberofItems__c : 0;

            if (String.isBlank(itemType)) continue;

            if (!totalByTypeAndMaterial.containsKey(itemType)) {
                totalByTypeAndMaterial.put(itemType, new Map<String, Decimal>());
            }

            Map<String, Decimal> materialMap = totalByTypeAndMaterial.get(itemType);
            if (materialMap.containsKey(materialType)) {
                materialMap.put(materialType, materialMap.get(materialType) + count);
            } else {
                materialMap.put(materialType, count);
            }
        }

        // Get ordered picklist values for Item_Type__c
        List<String> orderedItemTypes = new List<String>();
        Schema.DescribeFieldResult itemTypeField = Item__c.Item_Type__c.getDescribe();
        for (Schema.PicklistEntry entry : itemTypeField.getPicklistValues()) {
            if (entry.isActive()) {
                orderedItemTypes.add(entry.getValue());
            }
        }

        // Get ordered picklist values for Material_Type__c
        List<String> orderedMaterialTypes = new List<String>();
        Schema.DescribeFieldResult materialTypeField = Item__c.Material_Type__c.getDescribe();
        for (Schema.PicklistEntry entry : materialTypeField.getPicklistValues()) {
            if (entry.isActive()) {
                orderedMaterialTypes.add(entry.getValue());
            }
        }
        orderedMaterialTypes.add(''); // blank goes last

        // Build HTML table
        String html = '<table style="width:100%; border-collapse:collapse;">';
        html += '<thead><tr>';
        html += '<th style="text-align:left; padding:8px; border-bottom:1px solid #ccc; border-right:1px solid #ccc;">Item Type</th>';
        html += '<th style="text-align:left; padding:8px; border-bottom:1px solid #ccc; border-right:1px solid #ccc;">Material Type</th>';
        html += '<th style="text-align:right; padding:8px; border-bottom:1px solid #ccc;">Total Items</th>';
        html += '</tr></thead>';
        html += '<tbody>';

        for (String itemType : orderedItemTypes) {
            if (!totalByTypeAndMaterial.containsKey(itemType)) continue;
        
            Map<String, Decimal> materialMap = totalByTypeAndMaterial.get(itemType);
        
            for (String materialType : orderedMaterialTypes) {
                if (!materialMap.containsKey(materialType)) continue;
        
                Decimal total = materialMap.get(materialType);
                String materialLabel = String.isBlank(materialType) ? '—' : materialType;
        
                html += '<tr>';
                html += '<td style="padding:8px; border-right:1px solid #ccc;">' + itemType + '</td>';
                html += '<td style="padding:8px; border-right:1px solid #ccc;">' + materialLabel + '</td>';
                html += '<td style="text-align:right; padding:8px;">' + total.intValue() + '</td>';
                html += '</tr>';
            }
        }
        html += '</tbody></table>';

        return html;
    }
}