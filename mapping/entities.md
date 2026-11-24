# Proposed entity mapping (pilot scope)

Derived from the entity map in Doc 2, chapter 'Entities'. Architect reviews, does not re-map.

| Source entity | Epic | Proposed target | Kind | Source in Doc 2 | Rationale |
|---|---|---|---|---|---|
| new_yv_product | A1 | Product2 | standard object | Entity map: product = Product | The sellable product with a price book. SKU, duration, min and max participants. |
| new_tour_type | A1 | Bundle | GoMeddo object | Entity map: activity day template = Bundle with its items | 88 fields. Many are online/kiosk display fields that belong to C3, not A1. |
| new_product_subject | A1 | Product_Subject__c | custom object | Not listed separately in the entity map | One field only. Could be a picklist on the product instead. Question for the architect. |
| new_activity_day_location | A1 | YV_Location__c | custom object | Entity map: activity location = YV Location | Hebrew and English name only. |
| new_day_and_time | A1 | Activity_Window__c | custom object | Not in the entity map | Day/time window in which the template is available. Might map to GoMeddo Availability. Question for the architect. |
| new_tag | A1 | Tag__c | custom object | Not in the entity map | Three fields. Standard Topic is an alternative. Question for the architect. |
| new_coupon | A1 | Coupon__c | custom object | Entity map: coupon = custom | Credit or discount with validity dates. |
| new_service_provider | B1 | Contact | standard object, Guide record type | Entity map: service provider / guide = Contact, Guide record type | Most identity and contact fields map to standard Contact fields. Scheduling uses GoMeddo Staff linked to the same Contact. |
| new_certification | B1 | Certification__c | custom object | Entity map: certification matrix = custom | Many-to-many with the service provider, so a junction object is needed. |
| new_certification_category | B1 | Certification_Category__c | custom object | Entity map: certification matrix = custom | Two fields only. |
| new_service_provider_status | B1 | Service_Provider_Status__c | custom object | Doc 2, guide availability process, dated statuses spec | Receives the 11 role checkboxes from the service provider. The main structural change. |
| new_pickup_station | B1 | Pickup_Station__c | custom object | Not in the entity map | 61,496 rows in the source. Check how many are active before loading. |
| new_contract_days | B1 | Contract_Days__c | custom object | Entity map: contract days = custom | The provider's contractual workday quota. |

## Lookup targets outside the pilot

| Source entity | Proposed target | Handling | Note |
|---|---|---|---|
| new_almog_interface | Almog_Interface__c | stub | Touches the Almog billing interface. Stub now. |
| new_event_reservations | deferred relationship | deferred | A Doc 3 entity. Do not build it now and do not create the lookup. |
| new_budget_framework | Budget_Framework__c | stub | Entity map: budget framework = custom. Stub now, fully built in A3. |
| new_rsv_department | Department__c | stub | Entity map: department = custom. Stub now, fully built in C2. |
| new_resource | GoMeddo Resource | exists | Entity map: resource = GoMeddo Resource. Already installed. |
| new_activity_day | GoMeddo Reservation | exists | Entity map: activity day = GoMeddo parent reservation. |
| new_yv_time | picklist | not an object | Hours. A picklist is better than an object. |
| new_contact_title | Salutation | standard field | Title. Standard Contact field. |
| new_country | MailingCountry | standard field | Country. Standard address field. |
| SystemUser | User | standard object | Links the service provider to a system user. |
| many-to-many (marked in YV workbook) | junction object | junction object | Not an object in the source. Marked this way in the YV workbook. |
