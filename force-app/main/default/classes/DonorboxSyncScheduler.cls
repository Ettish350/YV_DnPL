/**
 * Runs the Donorbox sync nightly.
 * Schedule once (Anonymous Apex):  DonorboxSyncScheduler.scheduleNightly();
 * The job runs as the user who schedules it.
 */
public with sharing class DonorboxSyncScheduler implements Schedulable {
    public static final String JOB_NAME = 'Donorbox nightly sync';

    public void execute(SchedulableContext ctx) {
        System.enqueueJob(new DonorboxSync());
    }

    /** Every day at 02:00 in the scheduling user's time zone. */
    public static Id scheduleNightly() {
        return System.schedule(JOB_NAME, '0 0 2 * * ?', new DonorboxSyncScheduler());
    }
}