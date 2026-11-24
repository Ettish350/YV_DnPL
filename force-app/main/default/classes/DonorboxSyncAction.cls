/**
 * Flow action "Run Donorbox Sync": starts the Donorbox → Salesforce sync in the background.
 * Used by the scheduled flow so the nightly run is visible and editable in Flow Builder.
 */
public with sharing class DonorboxSyncAction {

    @InvocableMethod(label='Run Donorbox Sync' description='Starts the Donorbox sync in the background (skipped if one is already running).' category='Donorbox')
    public static void run() {
        Integer busy = [
            SELECT COUNT() FROM AsyncApexJob
            WHERE ApexClass.Name = 'DonorboxSync' AND Status IN ('Holding', 'Queued', 'Preparing', 'Processing')
        ];
        if (busy == 0) System.enqueueJob(new DonorboxSync());
    }
}