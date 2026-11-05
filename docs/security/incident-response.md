# CC7.4: source-security incident response

Owner: Nathan Hoang. Sprint 4 work: SCRUM-155, SCRUM-156, SCRUM-157.

## Implemented control

The Unity CI workflow scans `Assets/**/*.cs` for the credential-assignment patterns
used by the existing Unity editor SecurityScanner. The Python adapter is necessary
because the editor UI currently does not export a machine-readable CI report. It
leaves that teammate-owned editor tool unchanged. Both sets of rules must be kept
in sync when detection changes. This is a narrow source-credential check, **not**
a build-signature/integrity validator, malware scanner, or proof of SOC 2 compliance.

`tools/security/respond.py` consumes the report and independently recomputes it
against the checkout. A suspected credential finding blocks the `build` job and
therefore the normal UnityBuild artifact publication. Missing, malformed, stale,
incomplete, or unreadable evidence fails closed as a validation failure; it is not
misrepresented as a confirmed security incident. Clean evidence permits the normal
pipeline, which still requires Unity tests and successful build steps.

The response writes UTC time, commit SHA, classification, action, finding count,
and a SHA-256 hash of the supplied report. A separate redacted source inventory
contains paths, line numbers, rule IDs, and hashes; it never includes source lines
or matched credential values. Untrusted report content is not copied. The workflow
uploads these diagnostics with `if: always()` under SecurityResponse, retained for
14 days. A runner crash or cancellation can prevent upload and requires follow-up.
Do not put real secrets in fixtures or add raw source excerpts to incident reports.

## Response procedure

1. Nathan triages the response JSON and affected workflow/commit. A finding means
   suspected exposure; confirm context before declaring an actual compromise.
2. The gate withholds the build and its publication in this workflow. This is
   preventive containment of a candidate, not withdrawal of an existing release.
3. Preserve the redacted evidence and workflow URL. If credentials are real,
   contact their authorized owner for revocation/rotation; deleting a literal alone
   does not remediate exposure. Handle sensitive investigation details privately.
4. Prepare a reviewed fix, run a fresh scan, and rerun the pipeline. There is no
   bypass/allowlist in this implementation; false positives need review and an
   explicit code change, not a manual clear flag.
5. A teammate reviews the evidence, remediation, communications, and closure.
   Record the reviewer/date and remaining risks. Nathan owns the response record;
   repository/credential-owner contacts and human drill timing still need confirmation.

## Acceptance criteria and tests

- Clean current evidence permits the normal pipeline.
- Configured credential findings block the build and produce a suspected-incident record.
- Missing/malformed evidence, changed source, wrong commit, and removed findings block it.
- Missing/undecodable source and symlinks fail closed.
- Negative results still create diagnostic evidence without leaking the fixture value.
- Removing a finding does not release the gate until a fresh scan is generated.
- The build depends on both Unity tests and the security response job.

Run from the repository root with Python 3.9+:

```sh
python3 -m unittest discover -s tests/security -v
python3 tools/security/scan_source.py --commit "$(git rev-parse HEAD)" --output /tmp/source-scan.json
python3 tools/security/respond.py --report /tmp/source-scan.json --commit "$(git rev-parse HEAD)" --evidence-dir /tmp/security-response
```

The tests use fake values in temporary directories. The local unit/integration
suite covers both command exit status and saved evidence. Workflow wiring is also
checked statically; a green local run does not claim a GitHub run has passed.
Do not commit generated scan outputs, evidence directories, or Unity Library files.

## Scope and unresolved gaps

This gate cannot stop someone with permission to modify/skip the workflow or
publish outside it. Repository rules, approvals, and access controls remain the
team's responsibility. No branch rules or account permissions are changed here.
The regex adapter can miss credentials expressed differently and can flag benign
examples/comments. Findings are potential exposures, not automatically confirmed
incidents. It does not inspect dependencies, binaries, or all other secret formats.
Human timing targets, independent closure review, and communication drills remain
separate from these automated results. No real release removal or credential
rotation was performed. The earlier November 2 dummy-artifact exercise is separate
supporting evidence, not proof of this new pipeline implementation.

## Links

- [Assessment](https://docs.google.com/document/d/1pri54ILsvv_XHxmsm_zz3EQOjGdJFV1q-AlMew-vIzg/edit)
- [Evidence and runbook](https://docs.google.com/document/d/1AIeOaInolT7hAMPvcBDE4rJYroQRGmNtiMhIQhvpXi8/edit)
- [SCRUM-155](https://csu-team-cz6zf17a.atlassian.net/browse/SCRUM-155)
- [SCRUM-156](https://csu-team-cz6zf17a.atlassian.net/browse/SCRUM-156)
- [SCRUM-157](https://csu-team-cz6zf17a.atlassian.net/browse/SCRUM-157)
