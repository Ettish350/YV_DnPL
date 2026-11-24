# Object: Opportunity (Order)

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Opportunity (Order) |
| API Name | Opportunity |
| Status | Approved (Neev, 21.10.2026; pull request #27) |

Business role, per the DD: the Opportunity is the order and the shopping cart. It holds the customer, the contact, the audience type, the budget framework, the stage and the amount. An order opened on the site creates an Opportunity with its lines, a bundle breaks into lines when ordered, and the order closes at the sale date, after which operations run in the linked GoMeddo reservations (Doc 1 04-entities, "הזמנה (Opportunity)"). The order carries the lifecycle, statuses, audience, dates, group characteristics, the group count and the definition of the activity days; it has no named participant list, and the coordinator is linked to the order only (Doc 2 04-entities, comments c30 to c33, c49). The DD says the Opportunity uses no record types and that audiences are told apart by an audience-type field and dynamic forms (Doc 1 05-configurations). Per Lior (A1, 21.10.2026) orders use the existing Order record type only, with its own sales process.

Legacy source: the entity `new_reservation` (10247), "הזמנות -מערכת חדשה", has 107 field rows in the legacy-fields file (counted by script). Doc 2 states that the full field list is in appendix A, which is not in the repository set (`dd/INDEX.md`), so each field below is grounded in the legacy file and in the Doc 1 and Doc 2 chapters, not in appendix A. Order lines (`new_bid`) and activity days (`new_activity_day`) are other entities and are not part of this file.

Org check (read-only `sf sobject describe` on `yv-partial`, 18.10.2026): Opportunity has 49 fields, of which five are custom: `Desk_Name__c`, `Desk__c`, `Visit_Start_Date__c`, `Visit_End_Date__c` and `Audience_Category__c`. Two of them cover legacy concepts and are reused (`Visit_Start_Date__c`, `Audience_Category__c`). The org has no multi-currency field, so it is a single-currency org. No budget framework, department, coupon or supporter object exists yet (lookup targets below).

## Field Mapping

Mandatory columns per `Claude_Salesforce_Migration_Guidelines.md` (1-6), plus four added per `00-CONVENTIONS.md`: **Old API Name**, **Unique**, **Section**, **Questions**. Unique is blank everywhere because neither the DD nor an instruction states a requirement. Where "[EXISTING CUSTOM]" follows an API name, the field already exists in `yv-partial`; standard fields are written plainly; every other custom field is proposed (new). Legacy text fields give no length, so `Text(255)` and `Long Text Area(32000)` are the proposed defaults [Implementer]. Picklist values are the legacy values from the "PicklistValues" column of "שדות"; where the field exists in the org, the list is the union of both. The last five rows have no legacy source: four come from the DD and one is the External ID required by `00-CONVENTIONS.md`.

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|
| Accessibility requirements | AccessibilityRequirements__c | האם בקבוצה מבקרים להם מגבלה עבורה נדרשת התאמה? | Long Text Area(32000) |  | האם בקבוצה מבקרים להם מגבלה עבורה נדרשת התאמה? | new_accessibility_requirements |  | Visit & Group |  |
| Account handling comments | AccountHandlingComments__c | הערות לטיפול בחשבון | Long Text Area(32000) |  | הערות לטיפול בחשבון | new_account_handling_comments |  | Payment & Discount |  |
| Visit start date | Visit_Start_Date__c [EXISTING CUSTOM] | יום פעילות - תאריך ראשון | DateTime |  | יום פעילות - תאריך ראשון | new_activity_day_first_date |  | Visit & Group |  |
| Activity location address | ActivityLocationAddress__c | מיקום פעילות אצל המזמין - כתובת | Text(255) |  | מיקום פעילות אצל המזמין - כתובת | new_activity_location_address |  | Visit & Group | [Implementer] Per Neev (21.10.2026): free text, so users can write entries that are not real locations. Replaces the earlier note about one Address field. |
| Activity location city | ActivityLocationCity__c | מיקום פעילות אצל המזמין - ישוב | Text(255) |  | מיקום פעילות אצל המזמין - ישוב | new_activity_location_city |  | Visit & Group | [Implementer] Per Neev (21.10.2026): free text, same as the address row. |
| Actual number of participants | ActualNumberOfParticipants__c | מספר משתתפים בפועל | Number(18,0) |  | מספר משתתפים בפועל | new_actual_number_of_participants |  | Visit & Group |  |
| Additional characterization | AdditionalCharacterization__c | אפיון נוסף להזמנה | Multi-Select Picklist | הכנה לפולין;מגזר חרדי;בא"ח גבעתי;בא"ח גולני;בא"ח העורף;בא"ח חיל האוויר;בא"ח כפיר;בא"ח נחל;בא"ח צנחנים;בא"ח שריון;בא"ח תותחנים;בה"ד חיל הים;בה"ד מגב;בה"ד מודיעין;בהל"ץ;ביה"ס לאיסוף קרבי;ביסל"א;חוות השומר;מחווה אלון;מרכז הובלה;נתיב;עיר הבה"דים-אזרחים;עיר הבה"דים-בה"ד 10;עיר הבה"דים-בה"ד 11;עיר הבה"דים-בה"ד 13;עיר הבה"דים-בה"ד 20;עיר הבה"דים-בה"ד 6;עיר הבה"דים-בה"ד 7;עיר הבה"דים-בה"ד חינוך;עיר הבה"דים-ביה"ס לפיקוד והדרכה;עיר הבה"דים-כוחות הביטחון;עיר הבה"דים-מפקדה;עיר הבה"דים-צה"ל חוץ;Webinars for Teachers;Webinars for Students;Professional Development (for teachers);Awareness (for teachers);פעילות רכז חו"ל;קולכם שמעתי;מסע תלמידים בארץ;פרוייקט קולנוע;זיכרון מתחדש- פרוייקט באינסטגרם;ליל הבדולח;תגלית;מסע;מחנה קיץ;מנהיגות צעירה;לימודים;Workshop for Students | אפיון נוסף להזמנה | new_additional_characterization |  | Visit & Group |  |
| Automatic invoice delivery date | AutomaticInvoiceDeliveryDate__c | תאריך משלוח חשבון אוטומטי | DateTime |  | תאריך משלוח חשבון אוטומטי | new_automatic_invoice_delivery_date |  | Payment & Discount |  |
| Budget framework | BudgetFramework__c | מסגרת תקציבית | Lookup (to Budget_Framework__c) |  | מסגרת תקציבית | new_budget_framework |  | General Info | [Implementer] Per Lior (21.10.2026): stub Budget_Framework__c (name only, D-009); one budget framework per order. |
| Cancellation comment | CancellationComment__c | הערות לביטול | Long Text Area(32000) |  | הערות לביטול | new_cancellation_comment |  | General Info | [Implementer] For Stage B, per Neev (19.10.2026): visible on the page only when Stage is cancelled (מבוטל), together with Cancellation date and reason. |
| Cancellation date | CancellationDate__c | תאריך ביטול הפעילות | DateTime |  | תאריך ביטול הפעילות | new_cancellation_date |  | General Info | [Implementer] For Stage B, per Neev (19.10.2026): visible only when Stage is cancelled (מבוטל). |
| Cancellation reason | CancellationReason__c | סיבה לביטול הפעילות | Picklist | מס' משתתפים מועט;מזג אוויר;הקפצה;ביטול ביום הביקור;מצב בטחוני;קורונה;שביתה;הזמנה כפולה;הזמנה מהאינטרנט;נדחה למועד שטרם נקבע;הפך להשתלמות;נסיון;אחר;בוטל ע"י המזמין;העברה לאגף אחר;צהל- שינויי לוז;צהל- היסעים;נדחה לתאריך אחר;מלחמה;הפגנה | סיבה לביטול הפעילות | new_cancellation_reason |  | General Info | [Implementer] For Stage B, per Neev (19.10.2026): visible only when Stage is cancelled (מבוטל). |
| Cancelled by | CancelledBy__c | שם מבטל ההזמנה | Lookup (to User) |  | שם מבטל ההזמנה | new_cancelled_by |  | General Info |  |
| Cancelled with assignments | CancelledWithAssignments__c | הזמנה בוטלה עם שיבוצים | Checkbox |  | הזמנה בוטלה עם שיבוצים | new_cancelled_with_event_item |  | General Info |  |
| Where are you from (city, state) | CityState__c | מאיפה אתם? (עיר, מדינה) | Text(255) |  | Where are you from? (City, State) | new_city_state |  | Visit & Group |  |
| Description | Description | הערות | Long Text Area(32000) |  | הערות | new_comments |  | General Info | [Implementer] The legacy comments field goes to the standard Description; the other comment fields are separate custom fields. |
| Contact | ContactId | מזמין | Lookup (to Contact) |  | מזמין | new_contact_id |  | General Info |  |
| Coupon | Coupon__c | קופון | Lookup (to Coupon__c) |  | קופון | new_coupon |  | Payment & Discount | [Implementer] Per Lior (21.10.2026): stub Coupon__c (name only, D-009). |
| Total discount | TotalDiscount__c | סך הנחה | Currency(18,2) |  | סך הנחה | new_discount |  | Payment & Discount | [Implementer] Per Neev (21.10.2026): stays a field; Total to pay is Amount minus this field. |
| Discount approved by | DiscountApprovedBy__c | מאשר ההנחה | Lookup (to User) |  | מאשר ההנחה | new_discount_approved_by |  | Payment & Discount |  |
| Discount budget source | DiscountBudgetSource__c | תמיכה בהנחה | Lookup (to Budget_Framework__c) |  | תמיכה בהנחה | new_discount_support |  | Payment & Discount | [Implementer] Per Lior (21.10.2026): lookup to Budget framework; no Supporter object. |
| Contact email | ContactEmail__c | אימייל - איש קשר | Email |  | אימייל - איש קשר | new_email_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): no email field is created on the order; the Lightning record page pulls the email from the linked Contact (not a real or formula field). Rule per Neev: all contact details are derived from the Contact, and a field is created on the order only if a valuable reason is shown. |
| Second contact email | SecondContactEmail__c | אימייל - איש קשר משני | Email |  | אימייל - איש קשר משני | new_email_second_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): no field on the order; a second Contact lookup (Contact or Person Account) will be added and the Lightning record page pulls this detail from that Contact, as for the first contact. |
| Family connection to the Holocaust | FamilyConnectionToHolocaust__c | האם למי מהמבקרים יש קשר משפחתי לשואה? | Long Text Area(32000) |  | האם למי מהמבקרים יש קשר משפחתי לשואה? | new_family_connection_to_holocaus |  | Visit & Group |  |
| First visit in 5 years | IsFirstVisit__c | האם זהו ביקורכם הראשון ביד ושם ב-5 שנים האחרונות – | Checkbox |  | האם זהו ביקורכם הראשון ביד ושם ב-5 שנים האחרונות – | new_first_visit |  | Visit & Group |  |
| Contact first name | ContactFirstName__c | שם פרטי - איש קשר | Text(255) |  | שם פרטי - איש קשר | new_firstname_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): derived from the Contact; no field on the order unless a valuable reason is shown. |
| Second contact first name | SecondContactFirstName__c | שם פרטי - איש קשר משני | Text(255) |  | שם פרטי - איש קשר משני | new_firstname_second_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): no field on the order; a second Contact lookup (Contact or Person Account) will be added and the Lightning record page pulls this detail from that Contact, as for the first contact. |
| Form language | FormLanguage__c | שפת הטופס | Picklist |  | שפת הטופס | new_form_language |  | General Info | [Implementer] Per Neev (19.10.2026): values come from a global value set named Language, to be created later; for now Hebrew, English, Russian, French and Spanish. The Staff object has a Language field with values today. Nothing is implemented. |
| Gefen type | GefenType__c | סוג הגפן | Picklist | תכנית גפן 14605 - פרוייקט י"א משרד החינוך;תכנית גפן 14640- ניידות חינוכיות עד תשפ"ה;תכנית גפן 14556- ימי עיון לתלמידים;תכנית גפן 2774- בשבילי הזיכרון- תכנית למורים;מבצעים פדגוגים- משולם ע"י משרד החינוך;מבצעים פדגוגים- משולם ע"י המזמין;תכנית גפן 56743 - ניידות חינוכיות;תכנית גפן- השת' חדר מורים | סוג הגפן | new_gefen_type |  | Payment & Discount |  |
| Generate and send automatic invoice | GenerateSendAutomaticInvoice__c | הפק ושלח חשבון אוטומטי | Checkbox |  | הפק ושלח חשבון אוטומטי | new_generate_send_automatic_invoice |  | Payment & Discount |  |
| Grade | Grade__c | כיתה | Picklist | א';ב';ג';ד';ה';ו';ז';ח';ט';י';י"א;י"ב;יחידת הסברה;ה'-ו';ז'-ח' | כיתה | new_grade |  | Visit & Group | [Implementer] Per Neev (19.10.2026): values come from a global value set (name to be confirmed, for example Grade), to be created later from the legacy values. Nothing is implemented. |
| Holocaust survivors in the group | HasHolocaustSurvivors__c | האם יש בקבוצה ניצול/ת שואה? | Checkbox |  | האם יש בקבוצה ניצול/ת שואה? | new_holocaust_survivors |  | Visit & Group |  |
| Internal order comments | InternalComments__c | הערות פנימיות להזמנה | Long Text Area(32000) |  | הערות פנימיות להזמנה | new_internal_comments |  | General Info |  |
| Approved by coordinator | IsApprovedByCoordinator__c | אישור רכז | Checkbox |  | אישור רכז | new_is_approved_by_coordinator |  | General Info |  |
| Assignments need handling | IsCareOfSchedulesRequired__c | האם דרוש טיפול בשיבוצים | Checkbox |  | האם דרוש טיפול בשיבוצים | new_is_care_of_schedules_required |  | General Info |  |
| Concentrated invoice created | IsConcentratedInvoiceCreated__c | הופק חשבון מרוכז | Checkbox |  | הופק חשבון מרוכז | new_is_created_concentrated_invoice |  | Payment & Discount |  |
| Includes ceremony | IsIncludeCeremony__c | כולל טקס | Checkbox |  | כולל טקס | new_is_include_ceremony |  | Visit & Group |  |
| Signed confirmation received | IsSignedConfirmationReceived__c | התקבל אשור חתום | Checkbox |  | התקבל אשור חתום | new_is_order_confirmed |  | General Info |  |
| Purchase order required | IsPurchaseOrderRequired__c | נדרשת הזמנת רכש | Checkbox |  | נדרשת הזמנת רכש | new_is_purchase_order_required |  | Payment & Discount |  |
| Order locked | IsOrderLocked__c | האם ההזמנה נעולה | Checkbox |  | האם ההזמנה נעולה | new_is_reservation_locked |  | General Info |  |
| Second contact required | IsSecondContactRequired__c | האם נדרש איש קשר משני | Checkbox |  | האם נדרש איש קשר משני | new_is_second_contact |  | Contact & Reminders |  |
| Second contact | SecondContact__c | איש קשר משני | Lookup (to Contact) |  |  |  |  | Contact & Reminders | [Implementer] Added at Stage B per Neev (19.10.2026 and 21.10.2026): the second Contact lookup that replaces the second contact detail fields; the page pulls its details from that Contact (relationship SecondContact__r). |
| Confirmation email sent | IsConfirmationEmailSent__c | נשלח מייל אישור | Checkbox |  | נשלח מייל אישור | new_is_sent_email_confirmation |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): should be a Stage value, not a separate checkbox; row stays until the Stage list is updated. |
| VIP visitor | IsVisitorVip__c | אח"מ | Checkbox |  | אח"מ | new_is_visitor_vip |  | Visit & Group |  |
| Without institution | IsWithoutInstitution__c | ללא מוסד | Checkbox |  | ללא מוסד | new_is_without_institution |  | General Info |  |
| Journey end date | JourneyEndDate__c | תאריך סיום מסע | DateTime |  | תאריך סיום מסע | new_journey_end_date |  | Visit & Group |  |
| Journey start date | JourneyStartDate__c | תאריך תחילת מסע | DateTime |  | תאריך תחילת מסע | new_journey_start_date |  | Visit & Group |  |
| Instruction language | InstructionLanguage__c | שפת ההדרכה | Picklist |  | שפת ההדרכה | new_language |  | General Info | [Implementer] Per Neev (19.10.2026): values come from the same global value set named Language as Form language, to be created later. Nothing is implemented. |
| Contact last name | ContactLastName__c | שם משפחה - איש קשר | Text(255) |  | שם משפחה - איש קשר | new_lastname_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): derived from the Contact; no field on the order unless a valuable reason is shown. |
| Second contact last name | SecondContactLastName__c | שם משפחה - איש קשר משני | Text(255) |  | שם משפחה - איש קשר משני | new_lastname_second_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): no field on the order; a second Contact lookup (Contact or Person Account) will be added and the Lightning record page pulls this detail from that Contact, as for the first contact. |
| Contact phone | ContactPhone__c | טלפון - איש קשר | Phone |  | טלפון - איש קשר | new_mobile_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): derived from the Contact; no field on the order unless a valuable reason is shown. |
| Second contact phone | SecondContactPhone__c | טלפון - איש קשר משני | Phone |  | טלפון - איש קשר משני | new_mobile_second_contact |  | Contact & Reminders | [Implementer] Per Neev (19.10.2026): no field on the order; a second Contact lookup (Contact or Person Account) will be added and the Lightning record page pulls this detail from that Contact, as for the first contact. |
| Order name | Name | שם ההזמנה | Text(120) |  | שם הקבוצה | new_name |  | General Info |  |
| Legacy order number | LegacyOrderNumber__c | מספר הזמנה (מערכת ישנה) | Text(255) |  | מספר הזמנה | new_num |  | General Info | [Implementer] Per Lior (21.10.2026): migrated orders keep their legacy number here, searchable. |
| Order sequence | OrderSequence__c | מספר רץ של הזמנה | Auto Number |  |  |  |  | General Info | [Implementer] Per Lior (21.10.2026): the running sequence. Format {0000000} confirmed by Neev (21.10.2026). |
| Order number | OrderNumber__c | מספר הזמנה | Formula (Text) |  |  |  |  | General Info | [Implementer] Built 18.11.2026 (YV20-228, Doc 2 A04-06): Formula Text, label Order number with Hebrew translation, `Department__r.Order_Prefix__c & "-" & OrderSequence__c`; with no department or no prefix, only the sequence. Prefix `Department__c.Order_Prefix__c` is required (Anna, 18.11.2026). Not found by global search (formula field). On `Opportunity_Order_Lightning_Page`, read-only for Admin. |
| Number of external guides | NumberOfExternalGuides__c | מספר מדריכים חיצוניים | Number(18,0) |  | מספר מדריכים חיצוניים | new_number_of_external_guides |  | Visit & Group |  |
| Number of groups | NumberOfGroups__c | מספר קבוצות | Number(18,0) |  | מספר קבוצות | new_number_of_groups |  | Visit & Group |  |
| Number of participants | NumberOfParticipants__c | מספר משתתפים | Number(18,0) |  | מספר משתתפים | new_number_of_participants |  | Visit & Group |  |
| Online order type | OnlineOrderType__c | סוג הזמנה מקוונת | Picklist | קבוצה פרטית - הזמנה ישירה;קבוצה פרטית - הזמנה באמצעות סוכן;הזמנה בי"ס;קיוסק | סוג הזמנה מקוונת | new_online_reservation_type |  | General Info |  |
| Payment comments | PaymentComments__c | הערות לתשלום | Text(255) |  | הערות לתשלום | new_payment_comments |  | Payment & Discount |  |
| Payment status | PaymentStatus__c | סטטוס תשלום | Picklist | ללא;שולם;טרם שולם;שולם ובוצע זיכוי מלא;שולם ובוצע זיכוי חלקי;שולם (התחשבנות פנימית);טרם שולם (התחשבנות פנימית);שמירת מקום - אין צורך בתשלום;ממתין לתשלום;אין צורך בתשלום;התחייבות לתשלום;התקבל וואוצ'ר;תשלום נכשל;הופק חשבון;שולם חלקית;חשבון בוטל;הופק חשבון חלקית;הועבר לטיפול כספים;לחיוב בחבילה | סטטוס תשלום | new_payment_status |  | Payment & Discount |  |
| Purchase order number | PurchaseOrderNumber__c | מספר הזמנת רכש | Text(255) |  | מספר הזמנת רכש | new_purchase_order_number |  | Payment & Discount |  |
| Purpose of visit | PurposeOfVisit__c | מטרת הביקור | Text(255) |  | מטרת הביקור | new_purpose_of_visit |  | Visit & Group |  |
| Reference | Reference__c | אסמכתא | Text(255) |  | אסמכתא | new_reference |  | Payment & Discount |  |
| Religious affiliation | ReligiousAffiliation__c | שיוך דתי | Text(255) |  | Religious affiliation | new_religious_affiliation |  | Visit & Group |  |
| Lead Source | LeadSource | מקור הזמנה | Picklist | Advertisement;Employee Referral;External Referral;Partner;Public Relations;Seminar - Internal;Seminar - Partner;Trade Show;Web;Word of mouth;Other;Donorbox;אינטרנט כולל תשלום;דלפק מבקרים;הזמנה ידנית;אינטרנט תשלום טלפוני;מערכת אחודה | מקור הזמנה | new_reservation_source |  | General Info |  |
| Stage | StageName | סטטוס הזמנה | Picklist | חדש;ממתין לאישור מרכז הזמנות;אושרה;מבוטל | סטטוס הזמנה | new_reservation_status |  | General Info | [Implementer] Per Lior (21.10.2026): Order record type with its own sales process; אושרה is Closed Won, מבוטל is Closed Lost; legacy הצעת מחיר maps to חדש. Other Stage values stay with the other record types. |
| Handling status | HandlingStatus__c | סטטוס טיפול | Picklist | ממתין לשיבוץ;סיום טיפול | סטטוס הזמנה | new_reservation_status |  | General Info | [Implementer] Per Lior (21.10.2026): set by automation from the activity days. Migrated orders in these two legacy statuses load as אושרה plus this value. |
| Department | Department__c | מחלקה משותפת להזמנה | Lookup (to Department__c) |  | מחלקה משותפת להזמנה;גורם אחראי/מחלקה | new_responsible_agent |  | General Info | [Implementer] Per Lior (21.10.2026): stub Department__c (name only, D-009), one lookup; two legacy labels share this logical name. |
| Account | AccountId | מוסד/סוכן נסיעות - חדש | Lookup (to Account) |  | מוסד/סוכן נסיעות - חדש | new_rsv_institution |  | General Info |  |
| Order sub-source | OrderSubSource__c | מקור משני להזמנה | Picklist | מערכת הזמנות מקוונת;מובייל;טריפ אדוויזור;בית הספר;כניסה מהירה;הנצחה הזמנה ידנית;קיוסק | מקור משני להזמנה | new_sub_source_reservation |  | General Info |  |
| Audience type | Audience_Category__c [EXISTING CUSTOM] | סוג קהל | Multi-Select Picklist | מורים ישראלים;כוחות הבטחון;תלמידים ישראלים;בלתי פורמלי;מורים מחו"ל;פרחי הוראה;קבוצה/תוכנית חינוכית מחו"ל;ותיקים;מבוגרים;משפחות;קהל רחב;גמלאים;מורים;משלחות וביקורים רשמיים;אחר;חוקרים | קהל יעד | new_target_population |  | General Info | [Implementer] Per Lior (21.10.2026): values are the Bundle target audience list, shared as a global value set; the 59 legacy values are not merged. Cleanup of combined values goes to Yad Vashem. |
| Age group | AgeGroup__c | שכבת גיל | Picklist | יסודי;מנהיגות צעירה;יסודי וחט"ב;כוחות הבטחון - עובדים;צה"ל כניסה לקבע;חט"ע;מעצבי דעת קהל;חט"ב + חט"ע;יסודי חט"ב וחט"ע;מבוגרים;ישיבת הסדר;לא פורמאלי;חט"ב;יסודי וחט"ע;מכינה;נוער חו"ל;צה"ל בסיסים גדולים;מדרשה;מורים;על תיכון;מכללה;כוחות הביטחון - הכשרות;צה"ל הכנת סגל;סטודנטים;צה"ל השלמת קצונה;פרויקט אסירים;צה"ל נגדים;צה"ל בה"ד 1;צה"ל טירונים;צה"ל הכשרת מפקדים;צה"ל מילואים;צה"ל הכנה לקצונה;צה"ל בכירים;צה"ל חרדים;שרות לאומי / יחידת הסברה;תנועות נוער;גננות;צה"ל סגל מפקדה;צה"ל סדירים;צה"ל קורס המשך - מקצוע;סמינר;צה"ל קצינים;ללא;תלמידים + מורים;קהילה לומדת;מדריכים;קהל רחב;שירות לאומי | שכבת גיל | new_age_group |  | Visit & Group | [Implementer] Per Lior (21.10.2026): back on the order; values are the Bundle age group list, shared as a global value set. Cleanup of combined values goes to Yad Vashem. |
| Total price before discount | TotalPriceBeforeDiscount__c | סה"כ לתשלום | Currency(18,2) |  | סה"כ לתשלום | new_total_price |  | Payment & Discount | [Implementer] Per Neev (21.10.2026): stays a field. |
| Amount | Amount | סכום ההזמנה | Currency(18,2) |  |  |  |  | Payment & Discount | [Implementer] Per Lior (21.10.2026): Amount is the sum of the order lines; no temporary field and no legacy total is loaded. |
| Total to pay | TotalToPay__c | סה"כ לתשלום לאחר הנחה | Formula (Currency) |  |  |  |  | Payment & Discount | [Implementer] Per Lior (21.10.2026): Amount minus Total discount (order-level discount only, V08-01); customer billing derives from it. Total discount stays a field (Neev, 21.10.2026). |
| Visitor name | VisitorName__c | שם המבקר | Text(255) |  | שם המבקר | new_visitor_name |  | Visit & Group |  |
| Gift received by visitor | VisitorPresent__c | מתנה שקיבל המבקר | Text(255) |  | מתנה שקיבל המבקר | new_visitor_present |  | Visit & Group |  |
| Visitor role | VisitorRole__c | תפקיד המבקר | Text(255) |  | תפקיד המבקר | new_visitor_role |  | Visit & Group |  |
| Yad Vashem comments to customer | YadVashemCommentsToCustomer__c | הערות יד ושם ללקוח | Long Text Area(32000) |  | הערות יד ושם ללקוח | new_yv_comments |  | General Info |  |
| Responsible coordinator | ResponsibleCoordinator__c | רכז אחראי ביד ושם | Lookup (to B25__Staff__c) |  | רכז אחראי ביד ושם | new_yv_coordinator |  | General Info | [Implementer] Per Neev (20.10.2026): lookup to the GoMeddo Staff record; the coordinator is a role on the staff record. Replaces the earlier Contact-or-Owner question. |
| Visit photographed | IsVisitPhotographed__c | צילום הביקור | Checkbox |  | צילום הביקור |  |  | Visit & Group | [Implementer] Per Neev (20.10.2026): a new field the DD adds for official visits (Doc 2, official visits chapter), not an existing legacy field; there is no data to migrate. |
| Meeting held | IsMeetingHeld__c | פגישה | Checkbox |  | פגישה |  |  | Visit & Group | [Implementer] Per Neev (20.10.2026): a new field the DD adds for official visits (Doc 2, official visits chapter), not an existing legacy field; there is no data to migrate. |
| Visit status | VisitStatus__c | סטטוס ביקור | Picklist | ביקור ראשון;ביקור חוזר | סטטוס ביקור |  |  | Visit & Group | [Implementer] Per Neev (20.10.2026): a new field the DD adds for official visits, no data to migrate. Two values per Lior (21.10.2026). |
| Visitor description | VisitorDescription__c | תאור המבקר | Picklist | שגריר;שגריר חדש;נשיא;מלך/נסיך/קיסר;ראש ממשלה;קנצלר;משרד החוץ;תורם;תורם פוטנציאלי;אחר | תאור המבקר |  |  | Visit & Group | [Implementer] Per Neev (20.10.2026): a new field the DD adds for official visits (Doc 2, official visits chapter), not an existing legacy field; there is no data to migrate. |
| Close date | CloseDate | תאריך סגירה | Date |  |  |  |  | General Info | [Implementer] Per Lior (21.10.2026): set by automation at אושרה (Closed Won). Migrated orders: createdon (the YV dev team adds it to the export), fallback first activity date. |
| Purchase order received | IsPurchaseOrderReceived__c | הזמנת רכש התקבלה | Checkbox |  |  |  |  | Payment & Discount |  |
| Billed party | BilledParty__c | גורם מחויב | Lookup (to Account) |  |  |  |  | Payment & Discount | [Implementer] Per Lior (21.10.2026): from Doc 2 army and security chapter, a body or a travel agency; no legacy field. |
| Mute reminders | IsMuteReminders__c | השתקת תזכורות | Checkbox |  |  |  |  | Contact & Reminders |  |
| Feedback guide | FeedbackGuide__c | מדריך משוב | Lookup (to B25__Staff__c) |  |  |  |  | General Info | [Implementer] Per Lior (21.10.2026): lookup to GoMeddo Staff; no feedback object; field-level security limited to the guide-unit profiles. |
| Legacy ID | Legacy_ID__c | מזהה מערכת ישנה | Text(255) |  |  |  |  | System Information |  |


### Proposed Lightning Page section grouping

Four new sections, plus the standard "System Information" section for system and integration fields (Created By and Last Modified By stay there).

- **General Info**: Name, OrderSequence__c, OrderNumber__c, LegacyOrderNumber__c, AccountId, ContactId, StageName, HandlingStatus__c, LeadSource, CloseDate, Description, InternalComments__c, YadVashemCommentsToCustomer__c, Audience_Category__c, Department__c, BudgetFramework__c, ResponsibleCoordinator__c, FeedbackGuide__c, FormLanguage__c, InstructionLanguage__c, OnlineOrderType__c, OrderSubSource__c, IsOrderLocked__c, IsSignedConfirmationReceived__c, IsApprovedByCoordinator__c, IsCareOfSchedulesRequired__c, IsWithoutInstitution__c, and the cancellation fields (CancellationDate__c, CancellationReason__c, CancellationComment__c, CancelledBy__c, CancelledWithAssignments__c)
- **Visit & Group**: Visit_Start_Date__c, NumberOfGroups__c, NumberOfParticipants__c, ActualNumberOfParticipants__c, NumberOfExternalGuides__c, AgeGroup__c, Grade__c, AdditionalCharacterization__c, the activity location, accessibility, journey and visitor fields
- **Payment & Discount**: amounts (including Total to pay), discount, billed party, payment, invoice, purchase order, Gefen and Ministry of Education fields
- **Contact & Reminders**: contact details, confirmation email and mute-reminders fields
- **System Information** (existing standard section, not new): Legacy_ID__c, Created By, Last Modified By

This grouping is a proposal for review, not a decision; the DD does not specify a Lightning Page layout.

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|
| AccountId | `Account` (standard, exists) | Legacy `new_rsv_institution`. Match by the institution's legacy ID on Account once the Account file defines it; institutions load first. |
| ContactId | `Contact` (standard, exists) | Legacy `new_contact_id`. Match by the contact's legacy ID on Contact; contacts load first. |
| CancelledBy__c, DiscountApprovedBy__c | `User` (standard, exists) | Legacy `new_cancelled_by`, `new_discount_approved_by` (SystemUser). Match by user name or email; matching key to be decided. |
| Department__c | `Department__c` (stub, name only, D-009; to be created) | Legacy `new_responsible_agent` (`new_rsv_department`). |
| BudgetFramework__c | `Budget_Framework__c` (stub, name only, D-009; to be created) | Legacy `new_budget_framework`. One budget framework per order. |
| Coupon__c | `Coupon__c` (stub, name only, D-009; to be created) | Legacy `new_coupon`. |
| DiscountBudgetSource__c | `Budget_Framework__c` (stub, to be created) | Legacy `new_discount_support` (`new_supporter`). No Supporter object (Lior B4). |
| ResponsibleCoordinator__c | `B25__Staff__c` (GoMeddo Staff, exists) | Legacy `new_yv_coordinator` (`new_service_provider`). Per Neev the coordinator is a role on the staff record. Match by the staff record linked to the legacy service provider; staff records load first. |
| FeedbackGuide__c | `B25__Staff__c` (GoMeddo Staff, exists) | Doc 2 entities chapter; no legacy field. Per Neev the feedback guide is a staff member. |
| BilledParty__c | `Account` (standard, exists) | Doc 2 army and security chapter; no legacy field. A body or a travel agency. |

The parent objects Product2 and Bundle__c are documented (01_Product2.md, 02_Bundle.md); no change to them is needed except the ruling on Bundle-to-order (02_Bundle.md open question 18). The Account, Contact, Department, Budget framework and Coupon objects have no file in this folder yet.

## Dropped legacy fields

- אגף אחראי (legacy `new_responsible_division`): Doc 2, users, permissions and data migration and comments c790, c791: routing has no division level. [Implementer]
- סה"כ שולם (legacy `new_total_payments`): Derived: the paid amount is summed from the linked payments (model-register row 26). [Implementer]
- Currency (legacy `transactioncurrencyid`): The org has one currency and the DD leaves currency open (Doc 2 entities chapter, open data-model issues). [Implementer]
- האם יש תחומי עניין/עיסוק משותפים (legacy `new_common_interest`): Free text read by people; no DD rule, report or automation uses it. Dropped per Neev, 19.10.2026. [Implementer]
- סוג הנחה (legacy `new_discount_type`): Redundant. The DD fixes the calculation method by level (percent on a line, amount on the order) and no longer uses the Ministry of Education and Mishoa values; funding is carried by Discount support. Dropped per Neev, 19.10.2026, in favor of Discount support. [Implementer]
- האם להראות הערת אוכל באישור הזמנה (legacy `new_is_share_kislasi_ok`): Dropped per Lior (21.10.2026, B6): it showed the cafeteria contact paragraph in the order confirmation; that code is commented out in the legacy generator, and the coordinator picks the confirmation sections in the new system. [Implementer]
- סטטוס (legacy `new_status`): Dropped per Lior (21.10.2026, B12): the 5 and 10 minute holds are the portal cart timer and the payment steps belong in Payment status; the values are not moved to Stage. [Implementer]
- סה”כ לתשלום לאחר ההנחה (legacy `new_total_price_after_discount`): Not loaded, per Lior (21.10.2026, B14): Amount is the sum of the order lines and Total to pay is a formula. [Implementer]
- סוג פעילות יומית מהאינטרנט (legacy `new_tour_type_internet`): Redundant; the order already links to its bundle through the existing Connected Bundle object (`Connected_Bundle__c`, lookups to `Bundle__c` and Opportunity). Dropped per Neev, 20.10.2026. [Implementer]
- כמה זמן אתם מקדישים לסיור? (legacy `new_tour_time_available`): Not relevant; the bundle and its products capture how long the tour is. Dropped per Neev, 20.10.2026. [Implementer]
- כתובת שונה מהמוסד (legacy `new_is_address_different_from_institution`): Overlaps the activity location address and city, which are free text (Text 255); the DD has no rule for it. Dropped per Neev (21.10.2026). [Implementer]
- אמצעי תשלום (legacy `new_payment_type`): The legacy picklist has one value (פלאקארד) and the payment itself is a separate record that already has a payment method (`Payment__c.Payment_Method__c`). Dropped per Neev, 19.10.2026. [Implementer]
- גורם משלם (legacy `new_paying_agent`): Redundant; the legacy fields file itself notes it may be redundant, and the DD handles payer identity elsewhere (billed party, account type, zero-price lines). Dropped per Neev, 19.10.2026. [Implementer]
- מספר פעמים שהזמנה עודכנה אונליין (legacy `new_number_of_times_updated_online`): Counter with no use in the DD, which sets no limit on online edits (Doc 2, reminders chapter, comments c733 and c734). Dropped per Neev, 19.10.2026. [Implementer]
- מספר משתתפים בגילאי 10-14 (legacy `new_num_of_young_participants`): Redundant; the order keeps the number of participants and the actual number, and age is a group attribute. Dropped per Neev, 19.10.2026. [Implementer]
- שעות משרד החינוך (legacy `new_ministry_of_education_hours`): Free text with no use in the DD; the Ministry of Education discount is retired and pricing comes from the budget framework and price book. Dropped per Neev, 19.10.2026. [Implementer]
- תעריף משרד החינוך (legacy `new_ministry_of_education_tariff`): Free text with no use in the DD; the Ministry of Education discount is retired and pricing comes from the budget framework and price book. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת אוטומטית אחרונה- לקראת ביקור כולל לוז 2 (legacy `new_last_auto_reminder_date`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת ידנית אחרונה - לקראת ביקור כולל לוז (legacy `new_last_auto_reminder_date_before_visit`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת ידנית אחרונה - תשלום על ההזמנה (legacy `new_last_auto_reminder_date_payment`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת ידנית אחרונה -החזרת אישור הזמנה חתום (legacy `new_last_auto_reminder_date_return_signed`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת ידנית אחרונה -תשלום על חשבון שהופק (legacy `new_last_auto_reminder_date_to_pay_invoice`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת אוטומטית אחרונה- לקראת ביקור כולל לוז (legacy `new_last_auto_reminder_including_luz`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת אוטומטית אחרונה -החזרת אישור הזמנה חתום (legacy `new_last_auto_reminder_return_signed`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תזכורת אוטומטית אחרונה -החזרת אישור הזמנה חתום 2 (legacy `new_last_auto_reminder_return_signed22`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]
- תאריך הפקת חשבון אוטומטי אחרון (legacy `new_last_automatic_invoice_generation_date`): "Last sent" date; the DD logs every send on the order and its reminder engine works from templates, not from a last-sent date. Dropped per Neev, 19.10.2026. [Implementer]

## Open Questions

Lior answered every item of pull request #27 on 21.10.2026 (see "Answers from Lior" below); each answer is applied to the rows above. Still open, and none blocks the rest of the file:

1. **Totals (settled).** Per Neev, 21.10.2026: Total price before discount and Total discount stay fields, and Total to pay is the formula Amount minus Total discount (Lior B14).
2. **[Neev] Desk fields.** Find out who created `Desk_Name__c`, `Desk__c` and `Visit_End_Date__c` (Lior A2). They stay as they are, off the order page.
3. **[Neev, with Anna] Bundle file.** Through a pull request to Anna: update 02_Bundle.md open question 18: no new junction object is needed, because the order is already linked to the Bundle through the existing `Connected_Bundle__c` and the Bundle has its Bundle Items as a child object (Lior C11, C12; Neev, 21.10.2026). `Connected_Bundle_Item__c` is not needed, rename `Legacy_System_Id__c` to `Legacy_ID__c` (A5) and make the Bundle's target audience and age group lists global value sets (B13). Lior's question E1: `Connected_Bundle__c` links to the order only (lookups to Opportunity, Bundle, Staff and Main Resource) and has no activity-day lookup (read-only describe, 21.10.2026).
4. **[Yad Vashem] Combined values.** Cleanup of the combined audience and age group values, for example יסודי וחט"ב (B13).
5. **[YV dev team] Export.** Add `createdon` to the legacy export (B16); if future orders are migrated, supply the last-sent dates for the send log (C3).
6. **Value sets.** The Language and Grade global value sets, and the Bundle's two lists as shared value sets, are created in Stage B.

## Answers from Lior

Answers given before the pull request. Answered by Neev (owner of the order entity, D-029) in the Claude session; Lior can still change them in the pull request.

1. **Visit_Start_Date__c, legacy `new_activity_day_first_date` ("יום פעילות - תאריך ראשון").** Kept on the Opportunity. Answered by Neev, 19.10.2026, in session. The question was "Activity day dates live on the activity day; is the first date kept on the order?"

### Lior's answers to pull request #27 (21.10.2026)

Item numbers as in the pull request description (A = cross-field, B = per row, C = Neev's drops). A4 and B7 were addressed to Anna; Lior answered them too. Full table: `Opportunity PR27 - questions and answers - V2 - 21.10.xlsx` in this folder. Applied to the rows above on 21.10.2026; answered questions are cleared from the Questions column.

- **A1.** Orders use the existing Order record type only, with its own sales process (Stage values below). Grant and Individual Gift stay with fundraising. Implementer note added as C4 in knowledge/dd-corrections.md (Doc 1 05 says no record types).
- **A2.** Leave Desk_Name__c, Desk__c and Visit_End_Date__c as they are, off the order page. Do not delete. Neev to find out who created them.
- **A3.** No. Orders do not use Payment__c. Payments run through customer billing (master-detail to Opportunity), financial account and receipts (model register rows 26 to 29).
- **A5.** Legacy_ID__c on Opportunity, per 00-CONVENTIONS.md. Request to Anna: rename the Bundle's Legacy_System_Id__c to Legacy_ID__c before it is deployed. Product2 stays as already decided.
- **A6.** Yes: label Audience type, Hebrew translation סוג קהל.
- **A7.** OK. Line and activity-day levels go in their own files.
- **B1.** As Neev proposed: Multi-Select Picklist, kept on the order.
- **B2.** Create the lookup now to a stub Budget_Framework__c (name only, D-009). One budget framework per order.
- **B3.** Stub Coupon object (name only), lookup now.
- **B4.** No Supporter object. Make the field a lookup to Budget framework, label Discount budget source (Doc 2 08: every discount has an approver and a budget source).
- **B5.** Keep Order locked as a checkbox, independent of Stage. Locking and releasing is a permission that applies at any stage (Doc 2 15, Lock Reservations Manager).
- **B6.** Drop IsShareFoodNote__c. It showed the cafeteria contact paragraph in the order confirmation; that code is already commented out in the legacy confirmation generator, and in the new system the coordinator picks the confirmation sections (Doc 2 03).
- **B8.** Auto Number for the running sequence, plus a formula Order number = department prefix + sequence (Doc 2 04, prefix by department). Migrated orders keep their legacy number in a searchable text field, Legacy order number, so customers can still find in-flight orders by it.
- **B9.** Reuse standard Lead Source; the Order record type shows only the order values. Keep Order sub-source and Online order type as they are.
- **B10.** Stage values: חדש, ממתין לאישור מרכז הזמנות, אושרה (Closed Won), מבוטל (Closed Lost). Legacy הצעת מחיר maps to חדש. The order closes at the customer's approval (Doc 1 03 and 4.3, Doc 2 08 step 7); scheduling then runs in GoMeddo. ממתין לשיבוץ and סיום טיפול move to a new order picklist, Handling status (סטטוס טיפול), set by automation from the activity days (Doc 2 A09-01). Migrated orders in those two legacy statuses load as אושרה plus the matching Handling status. Confirmation email sent, Order locked and the Status__c values are not Stage values. Implementer note C5 in knowledge/dd-corrections.md.
- **B11.** Stub Department__c (D-009), one lookup field, label מחלקה משותפת להזמנה (Doc 2 06 rename).
- **B12.** Remove Status__c. Do not move its values into Stage: the 5 and 10 minute holds are the portal cart timer, and the payment steps belong in Payment status. In the legacy code this value set is set on event registrations and event items, not on orders.
- **B13.** Two fields on the order, audience and age group, using the same value lists as the Bundle (the Bundle's target audience and age group lists, as shared value sets). Age group comes back on the order (C9 reversed). Do not merge the 59 legacy values into one list. The cleanup of the combined values (for example יסודי וחט"ב) goes to Yad Vashem. Request to Anna (02_Bundle.md): the Bundle's two lists become global value sets.
- **B14.** No temporary field. Load the order lines; Amount is their sum. New formula Total to pay = Amount minus Total discount (the order-level discount; never both levels, V08-01). The customer billing amount due is derived from Total to pay, not from Amount (model register row 26 updated). Neev settles the three totals together (before discount, discount, to pay).
- **B15.** Two values, as the DD (Doc 2 06): ביקור ראשון, ביקור חוזר.
- **B16.** Close date is set by automation when the order reaches אושרה (Closed Won), that is at the customer's approval. Migrated orders: createdon, as Neev proposed (ask the YV dev team to add it to the export), fallback the first activity date.
- **B17.** Agreed: a field on the order, lookup to GoMeddo Staff; no feedback object now (Doc 2 04-entities wins over Doc 1 4.5). Field-level security limits it to the guide-unit profiles (Doc 2 15). Model register row 7 updated; implementer note C6.
- **C3.** 1. The invoice date belongs on the financial account (Lior 08.10.2026). 2. Only if future orders are migrated: load the legacy last-sent dates into the send log so they are not sent again. No order fields.
- **C7.** Yes: add Billed party (גורם מחויב), Lookup to Account (Doc 2 05: a body or a travel agency).
- **C11, C12.** Agreed: the order records its bundles through the existing `Connected_Bundle__c`; the Bundle's items are its child `Bundle_Item__c` records. `Connected_Bundle_Item__c` is not needed (Lior confirmed Neev's correction, 21.10.2026). This replaces Lior's 18.10 answer to 02_Bundle.md E3 (lookup on the order line). Neev updates 02_Bundle.md open question 18 through a pull request to Anna.
- **Neev on C11, C12 (21.10.2026).** The order is already connected to the Bundle through the junction object `Connected_Bundle__c`, and the Bundle has Bundle Items as its child object. No new objects are needed on these parts, and `Connected_Bundle_Item__c` is not needed. Confirmed by Lior, 21.10.2026.
- **Other.** C1, C2, C4, C5, C6, C8, C10 dropped: agreed. C9 (Age group) is reversed by B13.
- **A4.** Only future orders (activity date after go-live), as Doc 2 15. Map all fields anyway. The 2026 data stays Yad Vashem's call.
- **B7.** Yes: translation שם ההזמנה.

## Status

Approved. Neev, owner of the order entity (D-029), confirmed in the Claude session on 21.10.2026 that no field question is open; Lior's answers of 21.10.2026 are applied. Pull request #27. Stage B may start.

## Stage B (21.10.2026)

Metadata is in `tickets/Data Migration/metadata/Opportunity/`. Nothing is deployed. All of it is for approval through the Stage B pull request.

**Generated (73 fields):** 6 LongTextArea, 13 Text, 4 Number, 1 MultiselectPicklist, 4 DateTime, 10 Lookup, 12 Picklist, 19 Checkbox, 2 Currency, 1 AutoNumber, 1 Formula. Also 3 stub objects (`Budget_Framework__c`, `Coupon__c`, `Department__c`, name only, D-009), 2 global value sets (`Language`, `Grade`; `Age_Group` was created in the org by Neev on 21.10.2026 and `AgeGroup__c` uses it), the Hebrew translations, field permissions on `Admin` (Read and Edit; the formula and the auto number are read only), the record page `Order_Opportunity_Record_Page` (label "Order-Opportunity- Record Page"), and the `Orders` app proposal (one `profileActionOverrides` entry for the System Administrator profile, added to the copy retrieved on 21.10.2026). The workbook tab "הזמנה-Opportunity" has 84 field columns (after dropping the address flag and holding the Order number); the eight contact detail rows are left out because they are not fields.

**Proposed changes to existing components (not under main/default):** the Order record type (values for the new picklists, Lead Source limited to the order values), the Order sales process (Stage values חדש, ממתין לאישור מרכז הזמנות, אושרה, מבוטל), and the Stage and Lead Source value sets. The Order record type needs the new picklist values, otherwise they show no choices on orders.

**Not built:**
- `ContactEmail__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `SecondContactEmail__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `ContactFirstName__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `SecondContactFirstName__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `ContactLastName__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `SecondContactLastName__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `ContactPhone__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `SecondContactPhone__c`: no field on the order: shown on the page from the linked Contact (Neev)
- `OrderNumber__c`: Was ON HOLD per Neev (21.10.2026) until the prefix of each department is known; built 18.11.2026 (YV20-228). `OrderSequence__c` (Auto Number {0000000}) is built; the Order number formula (prefix plus sequence, Doc 2 institution routing) is added later.

**Checks (conventions, Stage B step 6):** every relationship is in the Relationship Fields table (the Second contact lookup was added as a row); no parent object needs a change except the stubs; field permissions cover every created field on `Admin`. The `YV Order user` profile does not exist in `yv-partial` yet (checked 21.10.2026), so its file was removed from Stage B. Per Neev (21.10.2026): for now all field permissions are on System Administrator only; the custom permission set `YadVashem_Order_Permission_Set` (`permissionsets/`) holds object access (Read, Create and Edit, no Delete, on Opportunity, OpportunityLineItem, `Connected_Bundle__c`, Account, Contact, Product2, `Bundle__c`, `Bundle_Item__c`, the three stubs and `B25__Staff__c`; Neev will adjust the levels himself) plus `AgeGroup__c`. Fields created from now on are also added to that permission set. [Implementer] Per Neev (21.10.2026) `Connected_Bundle_Item__c` is the same as `Bundle_Item__c`, so no separate access is added.

**Decided by Neev on 21.10.2026:** the activity location address and city are free text (built as Text 255); the auto number format is {0000000}.

**Decisions still needed before deploy:**
1. ~~Address differs from institution~~: dropped per Neev (21.10.2026), see Dropped legacy fields.
2. Order number: ON HOLD per Neev (21.10.2026) until the prefix of each department is known; the DD wants a prefix by department (Doc 2, institution routing). Stage B continues without `OrderNumber__c`.
3. Primary contact details on the page: decided by Neev (21.10.2026). The record page has an empty section "Primary Contact Details" (after Contact & Reminders); Neev adds the Contact fields himself in the page builder, taken through the Contact lookup. No custom field and no Contact Roles. The second contact details are placed through SecondContact__c.
4. ~~YV Order user profile~~: per Neev (21.10.2026) the `YV Order user` profile file is removed from Stage B because the profile does not exist in `yv-partial`. [Implementer] FLS for it must be added once Niv or Anna create the profile (D-033, `00-CONVENTIONS.md` step 4). This is a known gap against the conventions, not a decision to skip it.
