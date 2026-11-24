# Requirements Document

## Introduction

מסמך דרישות זה מגדיר את המפרט הטכני ליצירת תיעוד מקיף של כל הלוגיקה העסקית במערכת YVReservationsAPI. המסמך יכסה את כל הכללים העסקיים, התנאים, הוולידציות, החישובים, התהליכים הרקעיים והתלויות בין רכיבי המערכת — מעבר לתיאור ממשק המשתמש שכבר קיים.

המטרה: מפרט טכני מלא שיאפשר למפתח חדש להבין את כל התהליכים והכללים במערכת מבלי לקרוא את הקוד.

## Glossary

- **Documentation_Generator**: הכלי/תהליך שמפיק את מסמך תיעוד הלוגיקה העסקית
- **Business_Rule**: כלל עסקי — תנאי, חישוב, וולידציה או כל לוגיקה שמשפיעה על התנהגות המערכת
- **Module**: מודול במערכת — יחידה לוגית שמכילה Controllers, Services, ו-Repositories הקשורים לפונקציונליות ספציפית (למשל: Reservation, EventRegistration, ServiceProvider)
- **Condition_Logic**: לוגיקת תנאים — כל if/else/switch שמשנה את זרימת התהליך
- **Data_Filter**: סינון נתונים — כל Where/Filter שמצמצם נתונים לפני הצגה או עיבוד
- **Validation_Rule**: כלל אימות — בדיקה שמתבצעת לפני שמירה או ביצוע פעולה
- **Auto_Calculation**: חישוב אוטומטי — ערך שמחושב על סמך שדות אחרים
- **Field_Dependency**: תלות בין שדות — שינוי בשדה אחד שמשפיע על שדות אחרים
- **Background_Process**: תהליך רקע — פעולה שמופעלת כתוצאה מפעולת משתמש אך אינה גלויה לו ישירות
- **Edge_Case**: מקרה קצה — תרחיש חריג שבו המערכת מתנהגת באופן שונה מהזרימה הרגילה
- **Execution_Order**: סדר ביצוע — הסדר שבו כללים עסקיים מופעלים כאשר מספר כללים רלוונטיים
- **CRM_Integration**: שכבת אינטגרציה עם מערכת ה-CRM (Microsoft Dynamics)
- **YVReservationsAPI**: מערכת ה-API הראשית לניהול הזמנות — .NET/C# Web API
- **ReservationsApp**: אפליקציית הפרונטאנד ב-Angular

## Requirements

### דרישה 1: תיעוד כללים עסקיים לכל מודול

**User Story:** כמפתח חדש, אני רוצה לראות את כל הכללים העסקיים בכל מודול, כדי שאוכל להבין מה המערכת עושה ולמה.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL produce a Markdown document containing all Business_Rules identified per Module in the YVReservationsAPI system, where a Business_Rule is defined as any conditional logic, validation, calculation, or state transition that enforces a domain constraint or determines system behavior
2. WHEN a Business_Rule is documented, THE Documentation_Generator SHALL include the following fields for each rule: a textual description of no more than 300 characters, the business reason the rule exists, the trigger condition that activates the rule, and the conditions under which the rule is NOT triggered
3. WHEN multiple Business_Rules affect the same public method or endpoint, THE Documentation_Generator SHALL specify the Execution_Order of the rules using a numeric sequence starting from 1 indicating the order in which the rules are evaluated
4. THE Documentation_Generator SHALL organize rules by Module, matching the system's three-layer architecture: Controllers, Services, and Repositories, with each layer presented as a separate section and each class within the layer as a subsection
5. IF a Module contains no identified Business_Rules, THEN THE Documentation_Generator SHALL include that Module in the document with an explicit indication that no business rules were identified

### דרישה 2: תיעוד לוגיקת תנאים

**User Story:** כמפתח חדש, אני רוצה להבין את כל תנאי ה-if/else/switch בכל תהליך, כדי שאדע מתי כל נתיב לוגי מופעל.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Condition_Logic branch found in Service classes, including if/else statements, switch statements, ternary expressions, and null-coalescing operators
2. WHEN a Condition_Logic with two paths (if/else) is documented, THE Documentation_Generator SHALL specify the exact condition expression as it appears in the code, the true-path outcome (method calls invoked, values returned, or state changes performed), and the false-path outcome
3. WHEN a Condition_Logic with more than two paths (switch or else-if chain) is documented, THE Documentation_Generator SHALL specify each case value or condition, the outcome for each case, and the default/fallback behavior if one exists
4. WHEN a Condition_Logic contains nested conditions, THE Documentation_Generator SHALL document each nesting level in hierarchical order from the outermost condition inward, indicating the parent condition under which each nested condition is evaluated
5. WHEN a Condition_Logic depends on data from an external source such as the CRM_Integration, THE Documentation_Generator SHALL identify the source service or class name and the specific field evaluated in the condition
6. IF a Condition_Logic is unclear or ambiguous from the code due to missing context, unreachable branches, or contradictory logic, THEN THE Documentation_Generator SHALL explicitly state that the logic is unclear, describe what makes it ambiguous, and SHALL NOT guess the intent

### דרישה 3: תיעוד כללי סינון נתונים

**User Story:** כמפתח חדש, אני רוצה להבין אילו סינונים מופעלים על הנתונים, מאיפה הם מגיעים ולפי אילו שדות.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Data_Filter applied in Repository and Service classes, including LINQ Where clauses, FetchXML filters, and parameterized queries
2. WHEN a Data_Filter is documented, THE Documentation_Generator SHALL specify the data source table or entity, the fields used in the filter, the filter operator (equals, contains, greater than, etc.), and the filter logic combining multiple conditions (AND/OR grouping)
3. WHEN a Data_Filter uses parameters received from the client application, THE Documentation_Generator SHALL trace the parameter origin from the Controller through the Service to the Repository, specifying the parameter name at each layer
4. THE Documentation_Generator SHALL document default filter values and indicate when filters are optional versus required
5. WHEN a Data_Filter includes implicit filters (such as filtering by active status or by user permissions), THE Documentation_Generator SHALL explicitly document those implicit filters and explain why they are applied

### דרישה 4: תיעוד וולידציות לפני שמירה או פעולה

**User Story:** כמפתח חדש, אני רוצה לדעת אילו וולידציות מתבצעות לפני כל שמירה או פעולה, כדי להבין מתי פעולה יכולה להיכשל.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Validation_Rule that executes before a save or action in Service and Controller classes, including model validation attributes, manual field checks, and business rule validations
2. WHEN a Validation_Rule is documented, THE Documentation_Generator SHALL specify the validated field or condition, the validation logic (including the specific comparison or check performed), and the exact error response returned on failure (HTTP status code and error message)
3. WHEN a Validation_Rule is bypassed under specific conditions (such as admin override, specific status values, or feature flags), THE Documentation_Generator SHALL document those bypass conditions and explain why the bypass exists
4. THE Documentation_Generator SHALL group Validation_Rules by the action they protect (Create, Update, Delete, or custom action) and indicate the execution order within each group
5. WHEN a Validation_Rule depends on data from external sources (CRM, other services), THE Documentation_Generator SHALL document the dependency and how stale or missing external data affects the validation outcome

### דרישה 5: תיעוד ערכים מחושבים אוטומטית

**User Story:** כמפתח חדש, אני רוצה להבין אילו שדות מחושבים אוטומטית ואיך, כדי לדעת אילו ערכים לא ניתנים להזנה ידנית.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Auto_Calculation identified in Service classes, including an explicit indication of whether the output field is read-only (not manually editable) or overridable by user input
2. WHEN an Auto_Calculation is documented, THE Documentation_Generator SHALL include the formula or computation logic, the input fields used, the output field that receives the result, and the trigger timing (specifying whether the calculation executes on entity creation, on field value change, on save, or on data load)
3. WHEN an Auto_Calculation depends on external data from the CRM_Integration or other Service classes within the system, THE Documentation_Generator SHALL document the external dependency, the specific entity or table accessed, and the data retrieval method
4. IF an Auto_Calculation involves more than 2 input fields or includes nested conditional logic, THEN THE Documentation_Generator SHALL provide a step-by-step breakdown of the calculation showing each intermediate result
5. IF one or more input fields of an Auto_Calculation are null, missing, or contain invalid values, THEN THE Documentation_Generator SHALL document the system behavior for that scenario, specifying whether the calculation is skipped, uses a default value, or produces an error

### דרישה 6: תיעוד תלויות בין שדות

**User Story:** כמפתח חדש, אני רוצה לדעת איזה שדה משפיע על שדות אחרים, כדי להבין את ההשפעה של כל שינוי.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Field_Dependency where changing one field triggers a change in other fields, including both server-side dependencies (in Service classes) and client-side dependencies (in Angular components)
2. WHEN a Field_Dependency is documented, THE Documentation_Generator SHALL specify the source field (entity and field name), all affected target fields (entity and field name), the nature of the dependency (recalculation, visibility change, value reset, options filtering, or enabled/disabled state change), and the trigger mechanism (on change event, on save, or on load)
3. WHEN Field_Dependencies form a chain where field A affects field B which affects field C, THE Documentation_Generator SHALL document the full dependency chain in order and indicate whether cascading changes execute synchronously or require separate save operations
4. THE Documentation_Generator SHALL present Field_Dependencies in a tabular format with columns for Source Field, Target Field, Dependency Type, and Trigger

### דרישה 7: תיעוד תהליכי רקע

**User Story:** כמפתח חדש, אני רוצה לדעת אילו תהליכים רצים ברקע כתוצאה מפעולות משתמש, כדי להבין את ההשפעות הנסתרות של כל פעולה.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Background_Process that is triggered by a user action and results in at least one of the following: data modification, external system communication, or asynchronous task execution
2. WHEN a Background_Process is documented, THE Documentation_Generator SHALL specify the triggering user action, the process performed in the background, the services and APIs invoked, the data entities created or modified (including entity name and operation type: create, update, or delete), and the execution timing (synchronous, asynchronous-immediate within 5 seconds, or asynchronous-deferred beyond 5 seconds)
3. WHEN a Background_Process communicates with external systems such as the CRM_Integration or email services, THE Documentation_Generator SHALL document the integration point name, the data fields sent to the external system, the expected success response, and the timeout threshold for the communication
4. IF a Background_Process can fail, THEN THE Documentation_Generator SHALL document each distinct failure scenario including the failure trigger condition, the observable system behavior on failure (error indication to user, data rollback, or silent logging), and the retry behavior (maximum retry count and interval between retries, or indication that no retry occurs)
5. WHEN a user action triggers more than one Background_Process, THE Documentation_Generator SHALL document the execution order (sequential or parallel) and any dependencies between the processes

### דרישה 8: תיעוד שירותים ו-API בכל שלב

**User Story:** כמפתח חדש, אני רוצה לדעת אילו Services, Functions ו-APIs נקראים בכל שלב, כדי לנווט בקוד ביעילות.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document the call chain for every public Controller method: from the Controller action through Service methods to Repository calls, where each call chain constitutes one documented process
2. WHEN a call chain is documented, THE Documentation_Generator SHALL include for each step: the class name, the method name, the method parameters with their types, and the return type
3. WHEN a Service method calls another Service method (service-to-service call), THE Documentation_Generator SHALL include the intermediate Service in the call chain and document the full nested path
4. WHEN a process involves the CRM_Integration, THE Documentation_Generator SHALL document the CRM entity accessed, the operation performed (Create, Read, Update, Delete), and both the input fields sent and the output fields received
5. THE Documentation_Generator SHALL identify which database tables or CRM entities are read from and which are written to in each process
6. WHEN a call chain contains conditional branching where different methods are called depending on a condition, THE Documentation_Generator SHALL document each possible path and the condition that determines which path is taken

### דרישה 9: תיעוד מקרי קצה

**User Story:** כמפתח חדש, אני רוצה להבין את מקרי הקצה ואיך המערכת מתנהגת בהם, כדי לטפל נכון בתרחישים חריגים.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document every Edge_Case identified in the code through exception handling (try/catch blocks), null checks, boundary conditions (zero values, empty collections, maximum values), and special case branches
2. WHEN an Edge_Case is documented, THE Documentation_Generator SHALL specify the triggering condition, the system behavior in this case (error response, fallback value, or alternative flow), and whether the behavior is intentional (has explicit handling) or potentially a defect (generic catch or empty handler)
3. IF logic appears to handle an Edge_Case but the intent is unclear from the code, THEN THE Documentation_Generator SHALL explicitly flag the case as "requires clarification" and describe why the intent is unclear
4. THE Documentation_Generator SHALL categorize Edge_Cases by type: null/missing data, boundary values, concurrent access, external system unavailability, and permission/authorization failures

### דרישה 10: מבנה ופורמט המסמך

**User Story:** כמפתח חדש, אני רוצה שהמסמך יהיה מובנה וברור, כדי שאוכל לנווט בו בקלות ולמצוא מידע רלוונטי במהירות.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL organize the output document using a three-level heading hierarchy: Level 1 for Module name, Level 2 for process name within the Module, and Level 3 for individual documented items (controllers, services, or repository methods) within each process
2. THE Documentation_Generator SHALL include a table of contents with links to each Module section and each process subsection within it
3. THE Documentation_Generator SHALL format each documented item with the following fields in this order: Description, Trigger, Condition, Logic, Related Services, Related Tables, and Notes, where fields that are not applicable to a given item SHALL display "N/A"
4. IF documentation is generated for the full system, THEN THE Documentation_Generator SHALL produce one section per Module covering the following modules at minimum: Reservations, EventRegistration, EventReservation, ServiceProvider, Contact, Commemoration, ActivityDay, FrontDesk, ItineraryItem, ScheduleDate, Reports, and Login
5. THE Documentation_Generator SHALL include a cross-reference section listing all inter-module dependencies, where each entry specifies the source Module, the target Module, and the type of dependency (service call, shared data model, or shared database table)
6. THE Documentation_Generator SHALL order processes within each Module section alphabetically by process name

### דרישה 11: עקרונות תיעוד ואיכות

**User Story:** כצוות פיתוח, אנחנו רוצים שהתיעוד יהיה מדויק ואמין, כדי שנוכל להסתמך עליו בתחזוקה ופיתוח.

#### Acceptance Criteria

1. THE Documentation_Generator SHALL document the business logic behind the code — including conditional branches, validation rules, calculations, and state transitions — not merely the UI behavior visible to the user
2. IF the Documentation_Generator encounters logic that cannot be determined from the code alone (such as unreachable branches, contradictory conditions, or undocumented magic values), THEN THE Documentation_Generator SHALL mark the relevant section with an "unclear from code" indicator rather than inferring intent
3. WHEN a Business_Rule has no apparent reason or seems contradictory to other documented rules, THE Documentation_Generator SHALL document the rule as-is and add a note recommending human review
4. THE Documentation_Generator SHALL reference at least the class name and method name for each documented rule to enable traceability back to the source code
5. THE Documentation_Generator SHALL produce documentation that corresponds to the current state of the analyzed code, without carrying over descriptions from previous versions that no longer match the code
