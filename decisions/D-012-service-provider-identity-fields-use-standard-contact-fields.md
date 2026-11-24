# D-012: Service provider identity fields use standard Contact fields

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting architect |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
13 service provider fields (first and last name, email, phones, address, title, notes) map to standard Contact fields. No parallel custom fields are created. The full name is dropped (computed). 31 fields become custom fields on Contact.

## DD source / evidence
Doc 2 entity map: service provider = Contact, Guide record type.

## Alternatives considered
Custom copies of identity fields: duplicates data and breaks standard search and email.

## Affects
B1-01, B1-02.

## History
- 16.09.2026: proposed.
