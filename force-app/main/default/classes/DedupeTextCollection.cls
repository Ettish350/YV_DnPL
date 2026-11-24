public with sharing class DedupeTextCollection {

    public class Request {
        @InvocableVariable(label='Text Collection' required=true)
        public List<String> items;

        @InvocableVariable(label='Delimiter (default ", ")')
        public String delimiter;
    }

    public class Result {
        @InvocableVariable(label='Deduped Collection')
        public List<String> dedupedItems;

        @InvocableVariable(label='Deduped Text')
        public String dedupedText;
    }

    @InvocableMethod(
        label='Dedupe Text Collection'
        description='Removes duplicate values using exact, case-sensitive matching. Returns both a collection and a joined string. Preserves original order.'
        category='Collections'
    )
    public static List<Result> dedupe(List<Request> requests) {
        List<Result> results = new List<Result>();

        for (Request req : requests) {
            Result res = new Result();
            res.dedupedItems = new List<String>();

            if (req.items != null) {
                Set<String> seen = new Set<String>();
                for (String item : req.items) {
                    if (item != null && seen.add(item)) {
                        res.dedupedItems.add(item);
                    }
                }
            }

            String delim = String.isBlank(req.delimiter) ? ', ' : req.delimiter;
            res.dedupedText = String.join(res.dedupedItems, delim);

            results.add(res);
        }
        return results;
    }
}