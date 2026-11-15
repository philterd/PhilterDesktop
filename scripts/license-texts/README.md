# License texts

Canonical license texts used by `generate-third-party-licenses.ps1` for packages that name a license
(an SPDX expression) but don't ship its text. Downloaded unmodified from the SPDX License List data,
release v3.26.0: `https://raw.githubusercontent.com/spdx/license-list-data/v3.26.0/text/<id>.txt`.

The MIT and BSD-3-Clause texts are templates: the generator replaces their `Copyright (c) <year> ...`
line with each package's copyright from `NOTICE`. The Apache License 2.0 text comes from
`PhilterDesktop/Resources/apache-license.txt`.
