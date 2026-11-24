# YV20-230 (O5): a product sold online must have notice time, minimum days and booking limit

DD: Doc 2 process 01, V01-02. Org: `yv-partial`, read-only retrieve and one count query on 18.11.2026.

## 1. Impact check result

| Question | Result |
|---|---|
| Online products in `yv-partial` (`Display_Channels__c` includes מקוון) | 1 ("בדיקה - הדרכה 3 שעות"). It has all three values (4, 1, 1). None is missing a value, so nothing is left broken. |
| Existing validation rules on Product2 | None. |
| Triggers on Product2 | None. |
| `A1_Product_Clone` (Tal) | Creates PricebookEntry records only and does not touch `Display_Channels__c` or the three fields. |
| `Create_And_Update_Order_And_Product` | Works on Case, Order, OrderItem, Payment, PricebookEntry. Does not write Product2. |
| `YvProductPricingController`, `yvEduNewReservationController` | Read Product2 only (`Is_Free__c`, `Name`, `DurationOfHours__c`). No DML on Product2. |

Conflicts: none.

## 2. What is built (`metadata/A1/`, deployed to `yv-partial` on 18.11.2026)

Validation rule `Product2.Online_Notice` (Anna asked for the name without the epic prefix): blocks the save when `INCLUDES(Display_Channels__c, "מקוון")` and any of `Minimum_Notice_Time__c`, `MinimumDays__c`, `Advance_Booking_Limit__c` is blank. Message: "חסרה הגדרת זמן התראה מזערי או מינימום ימי עבודה מראש למוצר מקוון". A value of 0 counts as filled. Business-day counting is not part of this rule (the portal, Tal).

## 3. Test result (Apex anonymous, rolled back)

| Case | Result |
|---|---|
| Online, all three values | Saved |
| Online, missing notice time / minimum days / booking limit / all three | Blocked with the DD message (each one tested) |
| Online and internal, no values | Blocked |
| Internal only, no values | Saved |
| No channel, no values | Saved |
| Clone of the existing online product (`clone()` and insert) | Saved |

`DurationOfHours__c` is required on Product2, so test records set it.

## 4. Open points

- The rule uses the name `Online_Notice` without the epic prefix, as Anna asked; `CONVENTIONS.md` says `A1_V01_02_Online_Notice`. The description carries the ticket key and DD code.
- The rule blocks loading legacy online products without the three values. Plan it in the product migration.
