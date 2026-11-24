trigger CaseTrigger on Case (before insert, after insert, before update, after update) {
    CaseTriggerHandler.execute(
        Trigger.new,
        Trigger.oldMap,
        Trigger.isBefore,
        Trigger.isAfter,
        Trigger.isInsert,
        Trigger.IsUpdate
    );
}