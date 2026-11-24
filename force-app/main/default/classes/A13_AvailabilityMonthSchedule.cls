/**
 * A13-01: on the 15th of every month, builds next month's Availability Months.
 * Schedule once (Setup > Apex Classes > Schedule Apex, monthly on day 15), or:
 *   System.schedule('A13-01 Availability Months', '0 0 6 15 * ?', new A13_AvailabilityMonthSchedule());
 * Scheduled code cannot call out, so it hands the work to the Queueable A13_AvailabilityMonthJob.
 */
public without sharing class A13_AvailabilityMonthSchedule implements Schedulable {

    public void execute(SchedulableContext ctx) {
        Date next = Date.today().addMonths(1);
        System.enqueueJob(new A13_AvailabilityMonthJob(next.year(), next.month()));
    }
}