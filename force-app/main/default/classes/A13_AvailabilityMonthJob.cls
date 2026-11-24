/**
 * A13-01: runs the Availability Month builder for one month, in the background.
 * Queueable with callouts, because Hebcal is called. Started by A13_AvailabilityMonthSchedule.
 */
public without sharing class A13_AvailabilityMonthJob implements Queueable, Database.AllowsCallouts {

    private Integer year;
    private Integer month;

    public A13_AvailabilityMonthJob(Integer year, Integer month) {
        this.year = year;
        this.month = month;
    }

    public void execute(QueueableContext ctx) {
        A13_AvailabilityMonthBuilder.Result r = A13_AvailabilityMonthBuilder.run(year, month);
        System.debug(LoggingLevel.INFO, 'A13-01 ' + year + '-' + month + ': ' + r.message
            + (r.errors.isEmpty() ? '' : ' | ' + String.join(r.errors, ' | ')));
    }
}