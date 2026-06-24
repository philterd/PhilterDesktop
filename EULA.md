---
title: "Philterd Commercial License Agreement"
description: "Commercial license agreement for paid Philterd offerings: the official Philter and Philter Desktop builds, marketplace deployments, updates, and support."
layout: "page"
draft: true
---

<!--
WORKING DRAFT — not legal advice. Keep draft: true. Do not publish, and do not link this from
any marketplace listing or the Philter Desktop install flow, until counsel has reviewed it.

This is the RECONCILED version of the 2023 click-through "Philter License Agreement"
(content/philterd-eula.md), revised per philterd/philterd-website issue #332 to:
  - cover BOTH Philter and Philter Desktop under one company-level agreement;
  - scope the grant/restrictions to the paid "Commercial Offering," not the Apache-licensed code;
  - add an open-source precedence / Apache carve-out (new Section 2);
  - rewrite Open Source Components (Section 5) to recognize Philter's own code + bundled model;
  - add a monetary liability cap (Section 11);
  - update the technical description to ML/NLP (GLiNER) and probabilistic detection (Section 10);
  - replace the US-only restriction with export-compliance language (Section 16);
  - add Philter Desktop per-user terms (Section 4).
Changes are flagged inline with [#332] comments. Diff against the 2023 original before any use.
-->

**Philterd Commercial License Agreement**

Copyright (c) 2023–2026 Philterd, LLC. All rights reserved.

"Philter" is a trademark of Philterd, LLC.

This License Agreement ("Agreement") is entered into between Philterd, LLC ("Philterd") and you,
the user of Philterd's paid Philter&trade; and Philter Desktop&trade; offerings and the services
related to them (collectively, the "Commercial Offering," as further defined in Section 1). As used
in this Agreement, "you" means (i) any person or entity purchasing a subscription or otherwise
acquiring Philterd's permission to use the Commercial Offering (a "Customer"); (ii) any person who
accesses or uses the Commercial Offering on behalf of a Customer or on behalf of themselves; and/or
(iii) any person or entity authorized by the foregoing to use the Commercial Offering (including
employees, agents, contractors and other authorized users of an entity acquiring a subscription
from Philterd).

The person or entity who has purchased a subscription or otherwise acquired Philterd's permission to
use the Commercial Offering is a "Customer." If you are an employee, agent, contractor or other
authorized user of a Customer, then "Customer," as used herein, refers to your employer, the entity
that retains you as an agent or contractor and/or the entity that authorized you to use the
Commercial Offering. Nothing in this Agreement is intended to modify, amend or supersede any written
agreement regarding the software entered into between Philterd and the Customer.

Please read all of the following terms and conditions before using the Commercial Offering. By
clicking "I Agree," installing, accessing, or using the Commercial Offering, or by subscribing
through a marketplace (including AWS Marketplace), you agree to be legally bound by this Agreement.
If you do not agree to the terms and conditions in this Agreement, then you may not install, access
or use the Commercial Offering for any purpose.

<!-- [#332] NEW Section 1 — scopes the agreement to the paid Commercial Offering and names BOTH
     products, instead of claiming ownership of "all of Philter's source code." -->
## 1. The Commercial Offering

This Agreement governs the **Commercial Offering**, which consists of the paid products and services
Philterd provides under a subscription, and not the open-source software described in Section 2. The
Commercial Offering includes:

(a) **Philter** — Philterd's official, packaged server/API software for detecting, de-identifying,
    and redacting sensitive information, as distributed by Philterd as an Amazon Machine Image (AMI),
    container image, or other official build through a marketplace or by Philterd directly;

(b) **Philter Desktop** — Philterd's official, signed, auto-updating Microsoft Windows desktop
    application for detecting and redacting sensitive information from documents on the user's device;

(c) the official **updates, maintenance releases, and upgrades** to (a) and (b) made generally
    available to subscribers during the term;

(d) the **support** Philterd provides for the foregoing as described in Section 8; and

(e) the **Philter® and Philter Desktop® trademarks, branding, and any proprietary (closed-source)
    elements** of the foregoing.

The Commercial Offering does not contain, and does not require, a runtime license key, activation, or
feature gate. Your right to the official builds, updates, and support is contractual under this
Agreement and the applicable order or marketplace listing.

<!-- [#332] NEW Section 2 — the Apache carve-out / open-source precedence clause. THE key reconciliation. -->
## 2. Open-Source Software and Precedence

Philter's source code, and the source code of Philter Desktop, are also published by Philterd as
**open source under the Apache License, Version 2.0** (the "Apache License"), available at
<https://www.apache.org/licenses/LICENSE-2.0>. The bundled name-detection model and other bundled
components are likewise made available under their own open-source licenses (for example, the Apache
License 2.0, with attributions such as NVIDIA), as identified in the NOTICE file accompanying the
software.

Your rights in those open-source components are granted by, and governed by, their respective
open-source licenses. **Nothing in this Agreement limits, supersedes, or revokes any rights you have
in those components under their open-source licenses**, and to the extent of any conflict between
this Agreement and an applicable open-source license as to a given component, the open-source license
controls for that component. This Agreement instead governs the Commercial Offering defined in
Section 1 — the official builds, marketplace deployments, updates, support, and Philterd's trademarks
and proprietary elements — which the Apache License does not grant.

## 3. Limited License

Subject to your compliance with this Agreement and to payment of the applicable subscription fees,
Philterd grants you a personal, limited, non-exclusive, non-transferable, revocable license to
install and use the Commercial Offering for the subscription term, solely in accordance with this
Agreement and any written agreement between Philterd and the Customer. This license automatically
terminates upon the earlier of (i) the expiration or termination of any written agreement between
Philterd and the Customer; (ii) the expiration or non-renewal of your subscription; or (iii)
termination as permitted under Section 14. You may not transfer, assign, sell, rent, sublicense or
otherwise convey this license except as expressly permitted by the applicable order or marketplace
terms.

<!-- [#332] NEW Section 4 — Philter Desktop per-user terms + subscription/marketplace billing. -->
## 4. Subscriptions, Pricing, and Marketplace Terms

4.1 **Pricing.** Fees are as stated on the applicable marketplace listing or order. For **Philter
Desktop**, the subscription is priced **per user, per year** (currently **USD $100 per user per
year**), and the Customer must maintain an active subscription for each individual who uses Philter
Desktop. For **Philter**, pricing and units (for example, per instance-hour or annual) are as stated
on the applicable listing or order.

4.2 **Term and renewal.** A subscription begins on purchase and continues for the term shown on the
order or marketplace listing, renewing as described there until cancelled.

4.3 **Marketplace billing and precedence.** Where you obtain the Commercial Offering through AWS
Marketplace or another marketplace, ordering, metering, billing, taxes, and refunds are handled by
that marketplace under its own terms, which apply to those transactions. If you obtain the Commercial
Offering through AWS Marketplace, the following order of precedence applies to the extent terms
conflict: (1) the AWS Marketplace terms and the agreement governing your use of AWS (including, where
applicable, the Standard Contract for AWS Marketplace), then (2) this Agreement, then (3) any
documentation. This Agreement supplements, but does not replace, your agreement with the marketplace.

## 5. Open Source Components

<!-- [#332] REWRITTEN — recognizes Philter's OWN code + the bundled model as the Apache-licensed
     open source, not just incidental third-party bits. -->
The Commercial Offering incorporates open-source software, including **Philterd's own Philter and
Philter Desktop source code and the bundled detection model**, as well as third-party open-source
components. Each such component is governed by its own license, as identified in the NOTICE file
accompanying the software (see also Section 2). Philterd makes no claim of ownership over third-party
open-source components and makes no representations or warranties regarding them, and is not
responsible or liable for their performance.

## 6. Third Party Vendors

The Commercial Offering, or certain features or functions of it, may include, use, rely on, be
accessed through, or be distributed by third-party vendors (collectively, "Third Party Vendors"),
including Amazon Web Services, Google Cloud Platform, Microsoft Azure, and the Microsoft Store.
Payment of subscription fees may be collected and processed by a Third Party Vendor. Philterd has no
control over Third Party Vendors and makes no representations or warranties about their products or
services, and is not liable for the actions, errors or omissions of any Third Party Vendor. Your use
of the Commercial Offering through a Third Party Vendor may be subject to that vendor's additional
terms.

## 7. Your Account and Security

Where access to the Commercial Offering depends on an account or login credentials (which may be
provided and maintained by a Third Party Vendor), you are responsible for all activity conducted
using your account and credentials, including activity of your employees, contractors, and agents.
You may not allow others to access the Commercial Offering by sharing your account information or
credentials beyond the seats you have purchased, and you agree to use reasonable means to prevent
unauthorized use. You must promptly notify Philterd, and the applicable Third Party Vendor, of any
suspected breach or unauthorized use. Philterd has no responsibility for the security of accounts
provided or maintained by a Third Party Vendor.

## 8. Services and Support

During the paid subscription term, Philterd will provide support for the Commercial Offering as
described on the applicable marketplace listing or order (for example, email support, maintenance
releases, and bug fixes on a commercially reasonable basis). Support and updates are benefits of the
paid subscription and are not provided for self-built distributions made from the open-source code.
Except as set forth in this Agreement or a written agreement between Philterd and the Customer,
Philterd has no other obligation to provide services, maintenance, or support.

## 9. Protected Health Information and Data Protection

The Commercial Offering allows you to perform certain functions (including the ability to input,
store, retrieve and analyze data) that may involve "Protected Health Information" as that term is
defined under the Health Insurance Portability and Accountability Act of 1996 and the Health
Information Technology for Economic and Clinical Health Act of 2009 and the regulations promulgated
under those statutes (collectively, "HIPAA"). You agree to use Protected Health Information only as
permitted by applicable law. The Commercial Offering is intended to assist you in recognizing,
locating, de-identifying and anonymizing certain Protected Health Information, but it is **not
guaranteed to perform these functions without error**, and you must carefully review its output to
ensure that all Protected Health Information has been appropriately handled. **Protected Health
Information you process with the Commercial Offering is not stored by or transmitted to Philterd, and
Philterd cannot access it. Philterd is not a "Business Associate" of you or the Customer under
HIPAA.**

<!-- [#332] OPTIONAL GDPR data-controller language for non-US / non-healthcare customers. -->
Because the Commercial Offering processes data on the systems where you run it (on the user's device
for Philter Desktop; on your infrastructure for Philter) and Philterd does not receive your content,
**you act as the data controller (and, where applicable, processor) for any personal data you process
with the Commercial Offering**, including under the EU/UK General Data Protection Regulation and
similar laws, and you are responsible for establishing a lawful basis for that processing. Philterd
does not process such personal data on your behalf merely by providing the Commercial Offering.

## 10. Software Limitations and Disclaimers

You assume full risk and responsibility for your use of the Commercial Offering and for all results,
analyses, reports and outcomes it generates. The Commercial Offering does not render clinical,
medical, or legal advice, decisions, opinions, or recommendations, and you are solely responsible for
verifying the accuracy of its output and for complying with all applicable laws and regulations. You
may not rely on any output as advice regarding (i) the status of data as Protected Health Information
or (ii) the legal or regulatory obligations that may apply to any data. You acknowledge that the
Commercial Offering is not necessarily error-free. It is provided on an "as-is," "as-available," and
"with all faults" basis, and your use of and reliance on it is at your sole risk.

<!-- [#332] UPDATED technical description: was "rules-based and heuristic"; now ML/NLP + probabilistic. -->
The Commercial Offering detects sensitive information using a combination of **rules-based,
pattern-based, and machine-learning / natural-language-processing methods, including named-entity
recognition models (such as GLiNER)**. **Detection is probabilistic**: results may differ between
data sets and configurations, and the software can miss sensitive information (false negatives) or
flag information that is not sensitive (false positives). The bundled detection model is open-source
software (for example, Apache License 2.0, with required attributions such as NVIDIA) and its output
is likewise probabilistic. The Commercial Offering is **not guaranteed to satisfy any specific legal,
regulatory, or compliance requirement**, including requirements for the de-identification of data. It
is your responsibility to configure it appropriately and to review every result to determine whether
it is acceptable for your use case and for any legal, regulatory, or compliance requirements you may
have. **Redacted or de-identified output must be carefully reviewed by a qualified person before it
is shared, published, or relied upon, and must not be used as the sole control for any legal,
regulatory, privacy, or compliance obligation.**

## 11. Limitation of Liability

Under no circumstances will Philterd be responsible or liable for any special, incidental, indirect,
punitive, exemplary or consequential losses or damages, lost profits, lost revenues, business
interruption, lost contracts or business relationships, or loss of goodwill, even if reasonably
foreseeable or if Philterd was advised of the possibility. Without limiting the foregoing, Philterd
is not liable for any failure of the Commercial Offering to identify, locate, remove, or redact any
sensitive information (including Protected Health Information), for any violation by you of HIPAA or
other law, or for any loss, corruption, or unauthorized disclosure of data.

<!-- [#332] NEW monetary cap as belt-and-suspenders behind the exclusions above. -->
**In addition, and as a fallback to the exclusions above, Philterd's total aggregate liability
arising out of or related to this Agreement and the Commercial Offering will not exceed the fees the
Customer actually paid to Philterd (or through the applicable marketplace) for the Commercial
Offering in the twelve (12) months preceding the event giving rise to the liability.** Some
jurisdictions do not allow certain limitations, so some of the above may not apply to you.

## 12. DISCLAIMER OF WARRANTIES

THE COMMERCIAL OFFERING IS PROVIDED ON AN "AS IS" AND "AS AVAILABLE" BASIS. TO THE FULLEST EXTENT
PERMITTED BY APPLICABLE LAW, PHILTERD DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, ORAL OR WRITTEN,
INCLUDING WITHOUT LIMITATION ALL IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE, TITLE, AND NON-INFRINGEMENT, AND ALL WARRANTIES ARISING FROM STATUTE, COURSE OF DEALING, OR
TRADE USAGE. THIS DISCLAIMER IS CONSISTENT WITH, AND IN ADDITION TO, THE "AS IS" DISCLAIMER IN THE
APACHE LICENSE. PHILTERD MAKES NO REPRESENTATION OR WARRANTY AS TO THE QUALITY OR ACCURACY OF THE
COMMERCIAL OFFERING OR OF ANY RESULTS IT PRODUCES, OR THAT REDACTION OR DETECTION WILL BE COMPLETE OR
ACCURATE, THAT IT WILL BE ERROR-FREE OR UNINTERRUPTED, OR THAT IT WILL ACCOMPLISH ANY PARTICULAR
RESULT. PHILTERD MAKES NO WARRANTY AS TO THE SECURITY OR PRIVACY OF ANY DATA YOU PROCESS. SOME
JURISDICTIONS DO NOT ALLOW THE EXCLUSION OF IMPLIED WARRANTIES, SO SOME OF THE ABOVE MAY NOT APPLY TO
YOU.

## 13. Indemnification

You agree, on behalf of yourself and the Customer, to indemnify, defend and hold harmless Philterd
and its officers, directors, affiliates and employees from and against any and all damages,
liabilities, claims, losses, costs and expenses (including reasonable attorneys' fees) arising from
or in connection with (i) your use of the Commercial Offering; (ii) your breach of this Agreement;
and/or (iii) your violation of any applicable law, regulation, or legal obligation.

## 14. Term, Termination, and Expiration

This Agreement applies for as long as you have an active subscription. Your license automatically
expires upon the expiration or non-renewal of your subscription or any applicable written agreement
between Philterd and the Customer, or upon non-payment of fees. Philterd may suspend or terminate the
subscription and your access to the Commercial Offering for material breach not cured within thirty
(30) days of notice, or as otherwise permitted by the applicable marketplace terms. You may terminate
at any time by ceasing use and cancelling the subscription. **Termination of this Agreement does not
affect your rights in the open-source components under their open-source licenses (Section 2).**
Sections 2, 9, 10, 11, 12, 13, and 15 through 20 survive termination.

## 15. Modifications and Amendments

Philterd may amend this Agreement from time to time. Philterd will use reasonable efforts to notify
you of material amendments, including by presenting the amended Agreement on login, install, or
access. Your continued use of the Commercial Offering after being presented with an amendment
constitutes acceptance of it. The most recent version you have accepted prevails.

<!-- [#332] REPLACED the Section 15 "US-only" restriction with export-compliance only.
     CONFIRM with counsel that worldwide availability is intended. -->
## 16. Export and Sanctions Compliance

You agree to comply with all applicable export control and economic sanctions laws and regulations of
the United States and any other applicable jurisdiction, and not to export, re-export, or use the
Commercial Offering in violation of them. You agree to indemnify Philterd for any liability arising
from your violation of any such law or regulation.

## 17. Force Majeure

Philterd is not liable for any loss, delay, damage, or failure to perform due to causes beyond its
reasonable control, including industrial disputes, power or telecommunications failure, acts of God,
war, natural disasters, terrorism, hacking, viruses, or other causes beyond its reasonable control.

## 18. Entire Agreement

This Agreement, together with the applicable marketplace terms and order, contains the entire
agreement between you and Philterd with respect to the Commercial Offering and supersedes all prior
agreements regarding it (except for any written agreement between Philterd and the Customer). This
Agreement may be accepted electronically, and you consent to the use of electronic signatures and
records and waive any requirement of a non-electronic signature or record.

## 19. Interpretation

Neither party will be deemed the drafter of this Agreement. Headings are for convenience only. If any
provision is held contrary to law, it will be reformed to best accomplish its objective to the extent
permitted, or, failing that, severed, with the remaining provisions remaining in full force. No
failure to enforce any provision is a waiver of it.

## 20. Governing Law, Venue, and Jurisdiction

This Agreement is governed by the laws of the State of West Virginia, without regard to conflicts-of-
laws principles. Any dispute arising out of or in connection with this Agreement or the Commercial
Offering must be brought in the state or federal courts located in West Virginia, and you irrevocably
consent to the jurisdiction and venue of such courts and waive any objection based on lack of
personal jurisdiction, improper venue, or forum non conveniens.

U.S. Government end users acquire the Commercial Offering as "commercial computer software" under FAR
12.212 and DFARS 227.7202.

YOU EXPRESSLY ACKNOWLEDGE THAT YOU HAVE READ THIS AGREEMENT AND UNDERSTAND THE RIGHTS, OBLIGATIONS,
TERMS AND CONDITIONS SET FORTH HEREIN. BY CLICKING "I AGREE," INSTALLING, ACCESSING, OR USING THE
COMMERCIAL OFFERING, YOU CONSENT TO BE BOUND BY THIS AGREEMENT.
