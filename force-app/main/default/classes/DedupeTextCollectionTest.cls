@IsTest
private class DedupeTextCollectionTest {

    @IsTest
    static void dedupesExactMatchesAndPreservesOrder() {
        DedupeTextCollection.Request req = new DedupeTextCollection.Request();
        req.items = new List<String>{'Alice', 'Bob', 'Alice', 'alice', 'Bob ', 'Bob'};

        Test.startTest();
        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(new List<DedupeTextCollection.Request>{req});
        Test.stopTest();

        // 'alice' and 'Bob ' are distinct under exact, case-sensitive matching
        System.assertEquals(
            new List<String>{'Alice', 'Bob', 'alice', 'Bob '},
            results[0].dedupedItems,
            'Should keep first occurrence order and treat case/whitespace variants as distinct'
        );
    }

    @IsTest
    static void joinsWithDefaultDelimiter() {
        DedupeTextCollection.Request req = new DedupeTextCollection.Request();
        req.items = new List<String>{'Alice', 'Bob', 'Alice', 'Carol'};

        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(new List<DedupeTextCollection.Request>{req});

        System.assertEquals('Alice, Bob, Carol', results[0].dedupedText,
            'Default delimiter should be comma-space with no trailing separator');
    }

    @IsTest
    static void honorsCustomDelimiter() {
        DedupeTextCollection.Request req = new DedupeTextCollection.Request();
        req.items = new List<String>{'Alice', 'Bob'};
        req.delimiter = ' | ';

        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(new List<DedupeTextCollection.Request>{req});

        System.assertEquals('Alice | Bob', results[0].dedupedText);
    }

    @IsTest
    static void blankDelimiterFallsBackToDefault() {
        DedupeTextCollection.Request req = new DedupeTextCollection.Request();
        req.items = new List<String>{'Alice', 'Bob'};
        req.delimiter = '   ';

        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(new List<DedupeTextCollection.Request>{req});

        System.assertEquals('Alice, Bob', results[0].dedupedText,
            'String.isBlank treats whitespace-only as blank, so default applies');
    }

    @IsTest
    static void skipsNullElements() {
        DedupeTextCollection.Request req = new DedupeTextCollection.Request();
        req.items = new List<String>{'Alice', null, 'Bob', null, 'Alice'};

        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(new List<DedupeTextCollection.Request>{req});

        System.assertEquals(new List<String>{'Alice', 'Bob'}, results[0].dedupedItems);
        System.assertEquals('Alice, Bob', results[0].dedupedText);
    }

    @IsTest
    static void handlesNullAndEmptyCollections() {
        DedupeTextCollection.Request nullReq = new DedupeTextCollection.Request();

        DedupeTextCollection.Request emptyReq = new DedupeTextCollection.Request();
        emptyReq.items = new List<String>();

        List<DedupeTextCollection.Result> results =
            DedupeTextCollection.dedupe(
                new List<DedupeTextCollection.Request>{nullReq, emptyReq});

        System.assertEquals(0, results[0].dedupedItems.size());
        System.assertEquals('', results[0].dedupedText, 'Empty input yields empty string, not null');

        System.assertEquals(0, results[1].dedupedItems.size());
        System.assertEquals('', results[1].dedupedText);
    }

    @IsTest
    static void handlesBulkRequests() {
        List<DedupeTextCollection.Request> requests =
            new List<DedupeTextCollection.Request>();

        for (Integer i = 0; i < 200; i++) {
            DedupeTextCollection.Request req = new DedupeTextCollection.Request();
            req.items = new List<String>{'Owner ' + i, 'Owner ' + i, 'Shared'};
            requests.add(req);
        }

        Test.startTest();
        List<DedupeTextCollection.Result> results = DedupeTextCollection.dedupe(requests);
        Test.stopTest();

        System.assertEquals(200, results.size(), 'One result per request, in order');
        System.assertEquals('Owner 0, Shared', results[0].dedupedText);
        System.assertEquals('Owner 199, Shared', results[199].dedupedText);
    }
}