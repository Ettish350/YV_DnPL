# Doc 3 (Events and Finance), V35: what changed in the answer round, and the points that touch Doc 1 and Doc 2

**Status (08.11.2026):** superseded. Yad Vashem approved Doc 3 as final (file of 04.11.2026), and that file is the binding one (D-037): `sources/dd/מסמך 3 אירועים וכספים - מאושר על ידי יד ושם - 2026-11-04.docx`, Markdown in `dd/doc3-events-finance/`. The changes from V35 to the approved file are in `sources/DD-CHANGES-DOC3-2026-11-04.md`. This file was the README of the draft folder `sources/drafts/doc3/` (26.10 to 08.11.2026); the history below is kept as it was written then.

**History (written while V35 was a draft):** Yad Vashem sent 120 comments on the version they received on 15.10.2026; this V35, sent to Yad Vashem on 28.10.2026, answers all of them (118 threads; two empty comments removed), with text changes as Word tracked changes and one answer per comment. Malam's internal explanatory comments were removed at Lior's request, so a change is explained only where it answers a Yad Vashem comment. It is with Yad Vashem for approval. The DD is due to close on 04.11.2026; the full forms list is due from Yad Vashem by then, other material during the first month of implementation.

**What the draft was for:** so implementers know the events and finance world (events, registrations, conferences, seminars, courses, attendance, kiosk, invoicing against Almog, refunds, guide payments) and set up fields with an understanding of how Doc 3 looks.

**Rules (from 08.11.2026):**
- Doc 3 (the approved file) binds alongside Doc 1 and Doc 2 (see `dd/INDEX.md`, section 2). Where Doc 3 and Doc 1 or Doc 2 disagree, `knowledge/dd-corrections.md` rules; anything not ruled there is logged in your sync file or raised as a question Issue, not decided.
- Build only what a Jira (YV20) task asks for. Do not open Doc 3 fields or automations on your own initiative.
- Every statement about an object is still checked against `knowledge/model-register.md` and `knowledge/dd-corrections.md`.
- Read the comments too: the answers to Yad Vashem are in the Word comments (in the text export as `[COMMENT ...]`). Text that is still a tracked change may change after Yad Vashem's review.

**Files:**
- V35 was the Word file with tracked changes and comments kept (no longer in the repo; replaced by the approved file). Three screenshots of the existing finance screens (appendix 13) were replaced with a placeholder, because they show real customer records (D-031). The full copy is on SharePoint, `01 אפיון`.
- `dd/doc3-events-finance/`: the Markdown reading copy, generated with `scripts/dd_to_md.py`. The chapter text export that used to sit in `sources/drafts/doc3/text/` was removed; to get one with comments inline, run `scripts/dd_to_text.py` on the docx into `sources/local/` (CLAUDE.md, step 7).

**Added in V29 (Lior, 27.10.2026)**, each with a comment in the file:
- A20-12: pay at the lobby desk from the kiosk (order status awaiting desk payment, note with the products; payment taken in Smartticket; no receipt record in Salesforce; cash recorded manually as the payment method, how the cash receipt is recorded is an open item).
- A21-21: scheduled job twice a day: reconciliation status on the financial account (Custom) and the order's payment status, and a debt indicator with update time on the Account.
- A16-17: conference change after payment: difference charged by payment link or credited by credit instruction. A16-18 and a configuration record: defaults by conference type.
- Card refunds are sent automatically to Pelecard after the credit instruction is approved, with a control notice to finance (5.10, 5.11, 9.2). Approval stays.
- Receipt link from Almog kept on the receipt record; no copy of the receipt in Salesforce and no sending from Salesforce.
- Almog API extensions the YV developers are asked for (endpoints appendix): invoice notes field, receipt link, customer code in the debt check, credit call.

**Added in V30 to V35 (Lior, 27.10 and 28.10.2026)**:
- V35 (Lior): a full session or show cannot be registered to (3.2.5).
- Receipts only for card payments taken through Salesforce; payments received in Almog show as reconciliation status. Once an invoice exists, the order's payment status follows Almog reconciliation (A21-21).
- Orders-center coordinator issues invoices and sends payment links; cancel, credit and negative invoices stay with finance (dd-corrections C10).
- Credit approval: the orders center for its own orders, the handling department for others. A fully reconciled paid invoice is credited by a manual negative invoice; the Almog credit call is still to be defined (until then finance records it).
- Invoice as a PDF from a basic template with standard Salesforce tools (C14). Customer creation: the Almog API has no create call today, so new customers are opened in Almog by hand.
- Coupon: percentage, fixed ILS amount or exemption; single-use code; created manually as a record linked to an event.
- Actual participants updated only at the lowest level, one permission set (C8). A19-10: participant-in-session records created per session for whole-series registrations.
- Cancellation and change window per activity type (Bundle), or per product when the order is not a bundle.
- Guide trainings in chapter 5.5: automatic out-of-schedule assignment marked for payment, charged to a fixed guide-training budget frame like feedback reviews.
- Conference hotel balance is paid directly to the hotel (proposal 141); A16-17 covers shuttles and items paid to Yad Vashem.
- Smartticket refunds stay in Smartticket (A15-05); a push from Smartticket to a Salesforce endpoint is listed as an open option for the coordination meeting.
- Almog API extensions requested: invoice notes, receipt link, customer code in the debt check, invoice number and customer code on receipts, masked card and item code on receipts, negative invoice lines, overdue vs open invoices, credit call.
- About 250 Hebrew wording fixes.

**Changes in this round that touch Doc 1 or Doc 2** (for implementers; none changes an object of the model):
- Registration and handling fee applies only to an order that includes museum entry (Doc 2 A02-05 still says "or paid services").
- Distances already calculated are stored and reused; the map API is called only for a new pair (adds a small store to Doc 2 process 10).
- Any free hour can be chosen when editing an order, with the fee difference charged: same as Doc 2 V02-08.
- Date changes are unlimited (Yad Vashem's own appendix in Doc 2 still says once; dd-corrections C11).
- New fields, among others: shown on website (event and session), invoice issued for event (event), name for receipt (payment), invoice notes (Gefen programme, notes to the customer, line note), registration deadline (event), maximum number of instalments (event), fee rates with validity dates.
