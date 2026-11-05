# SOC 2 CC7.1 — Security Findings and Evidence Report

**Project:** CPSC 491 — Group 8 Unity Dungeon Crawler  
**Assessment date:** November 4, 2026  
**Control:** SOC 2 CC7.1 — Vulnerability Detection and Monitoring  
**Test:** Test 4 — Security Findings Documentation  
**Status:** Initial assessment completed; unresolved risks and follow-up actions documented



## 1. Objective and scope

This report consolidates the results of the security assessments performed for SOC 2 CC7.1. The objective is to review identified security findings, classify their severity and potential impact, and document appropriate corrective actions or follow-up tasks.

The assessment covers three previously conducted security activities:

- Test 1 — Static Code Analysis
- Test 2 — Dependency Vulnerability Review
- Test 3 — Sensitive Information Review

The findings are based on the Unity project's C# scripts, dependency configuration, Unity Editor version, and security testing evidence.

This assessment supports the group's SOC 2 CC7.1 security documentation requirements. It does not establish full SOC 2 compliance or guarantee that the project is free of vulnerabilities.



## 2. Security assessment overview

| Test | Jira ticket | Assessment result |
| --- | --- | --- |
| Test 1 — Static Code Analysis | SCRUM-158 | Two C# projects analyzed; seven warnings reviewed |
| Test 2 — Dependency Vulnerability Review | SCRUM-159 | Dependency inventory documented; security advisories and potential risks reviewed |
| Test 3 — Sensitive Information Review | SCRUM-160 | Initial scan completed; simulated credential detection successful |

Supporting screenshots, scan results, and assessment notes are recorded in the corresponding Jira tickets.

The dependency vulnerability review is also documented in GitHub pull request #70.



## 3. Security findings register

The following table summarizes the results of the security assessments.

Severity classifications distinguish confirmed security vulnerabilities from informational findings and potential risks requiring further investigation.

| ID | Finding | Severity / classification | Status |
| --- | --- | --- | --- |
| F-01 | Weak random-number generator warnings in gameplay code | Informational | Reviewed |
| F-02 | Unity Editor security advisories | Potentially high impact; application impact unconfirmed | Follow-up required |
| F-03 | No direct Brotli API references in assessed C# scripts | Informational | Completed |
| F-04 | Newtonsoft.Json advisory comparison | No identified exposure to reviewed advisory | Completed |
| F-05 | Mono.Cecil dependency review | No confirmed vulnerability in consulted assessment | Reviewed |
| F-06 | Initial sensitive-information scan | No findings reported by scanner | Completed |
| F-07 | Simulated credential-detection test | Successful security control validation | Completed |

The absence of reported findings in a specific scan does not guarantee that all vulnerabilities or exposed credentials have been identified.



## 4. Detailed security findings

### 4.1 F-01 — Weak random-number generator warnings

**Source:** Test 1 — Static Code Analysis  
**Jira:** SCRUM-158  
**Tool:** Security Code Scan v5.6.7  
**Classification:** Informational  
**Status:** Reviewed; no remediation required for the assessed usage

Security Code Scan was executed against the Unity project's runtime and editor C# projects.

**Runtime analysis:**

- Project: `Game.Runtime.csproj`
- Warnings reported: 7
- Warning identifier: `SCS0005`
- Affected file: `Assets/_Game/Scripts/Combat/BleedOnHit.cs`

**Editor analysis:**

- Project: `Game.Editor.csproj`
- Warnings reported: 0

Manual review determined that the seven warnings involved random-number generation used for procedural blood-splatter visual effects.

These operations control visual characteristics such as splatter shapes, sizes, positions, and variations.

The reviewed random-number operations are not used for authentication, encryption, credential generation, or other security-sensitive functionality.

**Security impact:** No security impact identified in the reviewed gameplay context.

**Corrective action:** No code changes required for these findings.

**Evidence:**

- `SOC2_Test1_Results.sarif`
- `SOC2_Test1_Editor_Results.sarif`
- Runtime and editor security scan screenshots
- Manual review of `BleedOnHit.cs`



### 4.2 F-02 — Unity Editor security advisories

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Affected component:** Unity Editor `6000.6.0f1`  
**Classification:** Potentially significant security risk  
**Status:** Follow-up required

The dependency vulnerability review identified two Unity security advisories relevant to the project's declared Unity Editor version.

**Advisory A — eMudhra certificate authority compromise**

- CVE: `CVE-2026-114406`
- Reported affected Unity 6000.6 range: `6000.6.0a1` through `6000.6.8f1`
- Project version: `6000.6.0f1`

This advisory concerns potential risks involving certificate trust and encrypted network communications.

**Advisory B — Brotli decoder vulnerability**

- CVE: `CVE-2026-111497`
- Reported CVSS: `9.2`
- Reported affected Unity 6000.6 range: `6000.6.0a1` through `6000.6.8f1`
- Project version: `6000.6.0f1`

This advisory concerns a vulnerability in Brotli decompression functionality that may affect applications or development tools processing untrusted compressed data.

**Security impact:** The project's declared Unity Editor version falls within the reported affected ranges. Application-specific exploitability has not been established.

**Severity assessment:** Potentially high impact based on the reported advisories. The actual risk to the group's game and development environment requires further investigation.

**Recommended corrective actions:**

1. Monitor official Unity security advisories and patched releases.
2. Coordinate any Unity Editor upgrade with the development team.
3. Investigate potential exposure through networking, decompression, and Unity Editor components.
4. Document remediation decisions and unresolved risks in Jira.
5. Verify project compatibility after an approved security update.

**Evidence:**

- `ProjectSettings/ProjectVersion.txt`
- Unity security advisory references
- Dependency vulnerability review document
- GitHub PR #70

The advisory identifiers, affected ranges, and patch availability should be rechecked against the original vendor publications before final security sign-off.



### 4.3 F-03 — Brotli API source-code review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** Informational  
**Status:** Completed

A Visual Studio Code source-code search was performed to identify direct references to potentially affected Brotli and HTTP decompression APIs.

**Search scope:** `Assets/**/*.cs`

**Search terms:**

- `BrotliStream`
- `BrotliDecoder`
- `HttpClient`
- `AutomaticDecompression`

**Result:** No matching references were identified in the searched C# scripts.

**Security impact:** No direct use of the searched APIs was identified in the assessed source files.

This result does not rule out potential exposure through compiled assemblies, Unity packages, Editor components, or code outside the search scope.

**Corrective action:** No immediate gameplay script changes identified from this search. Continue investigating package-level and Editor-level exposure as necessary.

**Evidence:** `SOC2_Test2_BrotliSearch.png`



### 4.4 F-04 — Newtonsoft.Json dependency review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** No identified exposure to reviewed advisory  
**Status:** Completed for the specified advisory

Unity Package Manager identified the following dependency:

- Unity package: `com.unity.nuget.newtonsoft-json`
- Unity package version: `3.2.2`
- Underlying Newtonsoft.Json version: `13.0.2`

The review considered security advisory `CVE-2024-21907` / `GHSA-5crp-9r3c-p9vr`.

The advisory identifies versions earlier than `13.0.1` as affected by a denial-of-service vulnerability.

The installed underlying library version, `13.0.2`, is newer than the patched version.

**Security impact:** No identified exposure to this specific advisory.

**Corrective action:** No remediation required for the reviewed advisory. Continue monitoring for other applicable vulnerabilities.

**Evidence:**

- `SOC2_Test2_NewtonsoftPackage.png`
- https://github.com/advisories/GHSA-5crp-9r3c-p9vr

This assessment does not establish that the library is free of all possible vulnerabilities.



### 4.5 F-05 — Mono.Cecil dependency review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** No confirmed vulnerability in consulted assessment  
**Status:** Reviewed; continued monitoring recommended

Unity Package Manager identified:

- Unity package: `com.unity.nuget.mono-cecil`
- Unity package version: `1.11.6`
- Underlying library: Mono.Cecil `0.11.5` with an additional Unity-packaged patch, according to package metadata

A consulted third-party security assessment reported no known vulnerabilities for the underlying library version.

**Security impact:** No confirmed vulnerability identified during the documented review.

**Corrective action:** Continue monitoring Unity package updates and Mono.Cecil security advisories.

**Evidence:**

- `SOC2_Test2_MonoCecilPackage.png`
- https://github.com/needle-mirror/com.unity.nuget.mono-cecil/blob/master/package.json
- https://secure.software/nuget/packages/Mono.Cecil/issues/0.11.5

This finding does not establish that every possible vulnerability has been excluded.



### 4.6 F-06 — Initial sensitive-information scan

**Source:** Test 3 — Sensitive Information Review  
**Jira:** SCRUM-160  
**Tool:** Custom Unity Editor Security Scanner  
**Classification:** No findings reported in assessed scope  
**Status:** Completed

A custom Unity Editor security scanner was used to inspect C# scripts for potentially hardcoded passwords, API keys, and authentication tokens.

**Initial scan results:**

- Files scanned: 76
- Potential findings: 0

No potential hardcoded credentials were detected by the scanner's configured patterns during the initial assessment.

**Security impact:** No confirmed credential exposure identified by the initial scan.

**Corrective action:** Retain scan evidence and repeat sensitive-information reviews when security-relevant code changes occur.

**Limitation:** The custom scanner checks predefined patterns in C# scripts. It does not provide comprehensive repository-wide secret detection.

**Evidence:**

- Initial security scanner screenshot
- `SecurityScanner.cs`
- Jira SCRUM-160



### 4.7 F-07 — Simulated credential-detection validation

**Source:** Test 3 — Sensitive Information Review  
**Jira:** SCRUM-160  
**Classification:** Security control validation  
**Status:** Completed

A controlled test was performed to verify that the custom Unity security scanner could detect a simulated hardcoded credential.

**Controlled test results:**

- Files scanned: 77
- Simulated findings detected: 1

The scanner successfully identified the fake credential in the temporary test script.

**Security impact:** No real credential exposure was established by this controlled test.

**Corrective action:** Confirm removal of the temporary test script and retain the detection screenshot as evidence.

**Evidence:**

- Controlled security scanner screenshot
- Jira SCRUM-160



## 5. Severity and impact assessment

The following classifications were used to evaluate the findings:

| Classification | Meaning |
| --- | --- |
| Critical | Confirmed vulnerability with potentially severe consequences requiring urgent remediation |
| High | Confirmed significant vulnerability requiring prioritized remediation |
| Medium | Confirmed vulnerability with moderate potential impact |
| Low | Confirmed vulnerability with limited potential impact |
| Informational | Observation or warning without an identified exploitable security impact |
| Potential risk | Possible security exposure requiring additional investigation |
| No identified exposure | Reviewed check did not establish exposure within its defined scope |

**Assessment summary:**

- No confirmed exploitable vulnerability was identified in the reviewed gameplay source-code warnings.
- The custom credential scanner reported zero potential findings during its initial scan.
- The Unity Editor advisories represent unresolved potential security exposure requiring follow-up.
- The reviewed Newtonsoft.Json advisory did not affect the identified underlying library version.
- The consulted Mono.Cecil assessment did not establish a confirmed vulnerability.

Published vulnerability severity is not necessarily equivalent to the actual severity of a vulnerability in this specific project.



## 6. Corrective actions and follow-up tracking

| Priority | Follow-up action | Related Jira | Status |
| --- | --- | --- | --- |
| High | Investigate Unity Editor advisories and coordinate a patched-version upgrade when available | SCRUM-159 | Pending |
| Medium | Review package-level exposure and monitor dependency security updates | SCRUM-159 | Ongoing |
| Medium | Expand sensitive-information scanning coverage where practical | SCRUM-160 | Recommended |
| Low | Confirm removal of the simulated credential test script | SCRUM-160 | Verification pending |
| Low | Repeat static security analysis after security-relevant code changes | SCRUM-158 | Recommended |

Any confirmed vulnerabilities requiring remediation should receive dedicated Jira follow-up issues with an assigned owner, severity, and target resolution date.

The priority values above are recommended follow-up priorities rather than confirmed application vulnerability severities.



## 7. Evidence and Jira references

| Evidence | Related test | Jira |
| --- | --- | --- |
| Runtime security scan SARIF and screenshot | Static Code Analysis | SCRUM-158 |
| Editor security scan SARIF and screenshot | Static Code Analysis | SCRUM-158 |
| Manual `BleedOnHit.cs` review | Static Code Analysis | SCRUM-158 |
| Dependency vulnerability review report | Dependency Review | SCRUM-159 |
| Unity advisory references | Dependency Review | SCRUM-159 |
| Brotli API search screenshot | Dependency Review | SCRUM-159 |
| Newtonsoft.Json package screenshot | Dependency Review | SCRUM-159 |
| Mono.Cecil package screenshot | Dependency Review | SCRUM-159 |
| Initial security scanner screenshot | Sensitive Information Review | SCRUM-160 |
| Simulated credential-detection screenshot | Sensitive Information Review | SCRUM-160 |

**Related GitHub pull request:** PR #70 — SOC 2 Dependency Vulnerability Review.

The screenshots and security assessment notes are maintained in their respective Jira tickets. Evidence should not contain actual passwords, API keys, or other sensitive credential values.



## 8. Conclusion

The SOC 2 CC7.1 security assessment consolidated results from static code analysis, dependency vulnerability review, and sensitive-information testing.

Static code analysis identified seven weak-randomness warnings, which were reviewed and determined to involve non-security-sensitive visual effects. The editor scan reported zero warnings.

The dependency review documented 13 direct Unity dependencies and 45 resolved package entries, identified Unity Editor security advisories requiring follow-up, and recorded assessments of Newtonsoft.Json and Mono.Cecil.

The sensitive-information review reported zero potential findings in the initial scan and successfully detected a simulated credential during controlled testing.

No confirmed exploitable vulnerability was identified in the reviewed gameplay code or credential-scanning results. Potential risks affecting the Unity Editor and its dependencies remain subject to further investigation and remediation planning.

The assessment provides documented findings, severity classifications, supporting evidence, and recommended corrective actions for SOC 2 CC7.1.

This report does not establish full SOC 2 compliance or guarantee that the Unity project is free of security vulnerabilities.# SOC 2 CC7.1 — Security Findings and Evidence Report

**Project:** CPSC 491 — Group 8 Unity Dungeon Crawler  
**Assessment date:** November 4, 2026  
**Control:** SOC 2 CC7.1 — Vulnerability Detection and Monitoring  
**Test:** Test 4 — Security Findings Documentation  
**Status:** Initial assessment completed; unresolved risks and follow-up actions documented



## 1. Objective and scope

This report consolidates the results of the security assessments performed for SOC 2 CC7.1. The objective is to review identified security findings, classify their severity and potential impact, and document appropriate corrective actions or follow-up tasks.

The assessment covers three previously conducted security activities:

- Test 1 — Static Code Analysis
- Test 2 — Dependency Vulnerability Review
- Test 3 — Sensitive Information Review

The findings are based on the Unity project's C# scripts, dependency configuration, Unity Editor version, and security testing evidence.

This assessment supports the group's SOC 2 CC7.1 security documentation requirements. It does not establish full SOC 2 compliance or guarantee that the project is free of vulnerabilities.



## 2. Security assessment overview

| Test | Jira ticket | Assessment result |
| --- | --- | --- |
| Test 1 — Static Code Analysis | SCRUM-158 | Two C# projects analyzed; seven warnings reviewed |
| Test 2 — Dependency Vulnerability Review | SCRUM-159 | Dependency inventory documented; security advisories and potential risks reviewed |
| Test 3 — Sensitive Information Review | SCRUM-160 | Initial scan completed; simulated credential detection successful |

Supporting screenshots, scan results, and assessment notes are recorded in the corresponding Jira tickets.

The dependency vulnerability review is also documented in GitHub pull request #70.



## 3. Security findings register

The following table summarizes the results of the security assessments.

Severity classifications distinguish confirmed security vulnerabilities from informational findings and potential risks requiring further investigation.

| ID | Finding | Severity / classification | Status |
| --- | --- | --- | --- |
| F-01 | Weak random-number generator warnings in gameplay code | Informational | Reviewed |
| F-02 | Unity Editor security advisories | Potentially high impact; application impact unconfirmed | Follow-up required |
| F-03 | No direct Brotli API references in assessed C# scripts | Informational | Completed |
| F-04 | Newtonsoft.Json advisory comparison | No identified exposure to reviewed advisory | Completed |
| F-05 | Mono.Cecil dependency review | No confirmed vulnerability in consulted assessment | Reviewed |
| F-06 | Initial sensitive-information scan | No findings reported by scanner | Completed |
| F-07 | Simulated credential-detection test | Successful security control validation | Completed |

The absence of reported findings in a specific scan does not guarantee that all vulnerabilities or exposed credentials have been identified.



## 4. Detailed security findings

### 4.1 F-01 — Weak random-number generator warnings

**Source:** Test 1 — Static Code Analysis  
**Jira:** SCRUM-158  
**Tool:** Security Code Scan v5.6.7  
**Classification:** Informational  
**Status:** Reviewed; no remediation required for the assessed usage

Security Code Scan was executed against the Unity project's runtime and editor C# projects.

**Runtime analysis:**

- Project: `Game.Runtime.csproj`
- Warnings reported: 7
- Warning identifier: `SCS0005`
- Affected file: `Assets/_Game/Scripts/Combat/BleedOnHit.cs`

**Editor analysis:**

- Project: `Game.Editor.csproj`
- Warnings reported: 0

Manual review determined that the seven warnings involved random-number generation used for procedural blood-splatter visual effects.

These operations control visual characteristics such as splatter shapes, sizes, positions, and variations.

The reviewed random-number operations are not used for authentication, encryption, credential generation, or other security-sensitive functionality.

**Security impact:** No security impact identified in the reviewed gameplay context.

**Corrective action:** No code changes required for these findings.

**Evidence:**

- `SOC2_Test1_Results.sarif`
- `SOC2_Test1_Editor_Results.sarif`
- Runtime and editor security scan screenshots
- Manual review of `BleedOnHit.cs`



### 4.2 F-02 — Unity Editor security advisories

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Affected component:** Unity Editor `6000.6.0f1`  
**Classification:** Potentially significant security risk  
**Status:** Follow-up required

The dependency vulnerability review identified two Unity security advisories relevant to the project's declared Unity Editor version.

**Advisory A — eMudhra certificate authority compromise**

- CVE: `CVE-2026-114406`
- Reported affected Unity 6000.6 range: `6000.6.0a1` through `6000.6.8f1`
- Project version: `6000.6.0f1`

This advisory concerns potential risks involving certificate trust and encrypted network communications.

**Advisory B — Brotli decoder vulnerability**

- CVE: `CVE-2026-111497`
- Reported CVSS: `9.2`
- Reported affected Unity 6000.6 range: `6000.6.0a1` through `6000.6.8f1`
- Project version: `6000.6.0f1`

This advisory concerns a vulnerability in Brotli decompression functionality that may affect applications or development tools processing untrusted compressed data.

**Security impact:** The project's declared Unity Editor version falls within the reported affected ranges. Application-specific exploitability has not been established.

**Severity assessment:** Potentially high impact based on the reported advisories. The actual risk to the group's game and development environment requires further investigation.

**Recommended corrective actions:**

1. Monitor official Unity security advisories and patched releases.
2. Coordinate any Unity Editor upgrade with the development team.
3. Investigate potential exposure through networking, decompression, and Unity Editor components.
4. Document remediation decisions and unresolved risks in Jira.
5. Verify project compatibility after an approved security update.

**Evidence:**

- `ProjectSettings/ProjectVersion.txt`
- Unity security advisory references
- Dependency vulnerability review document
- GitHub PR #70

The advisory identifiers, affected ranges, and patch availability should be rechecked against the original vendor publications before final security sign-off.



### 4.3 F-03 — Brotli API source-code review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** Informational  
**Status:** Completed

A Visual Studio Code source-code search was performed to identify direct references to potentially affected Brotli and HTTP decompression APIs.

**Search scope:** `Assets/**/*.cs`

**Search terms:**

- `BrotliStream`
- `BrotliDecoder`
- `HttpClient`
- `AutomaticDecompression`

**Result:** No matching references were identified in the searched C# scripts.

**Security impact:** No direct use of the searched APIs was identified in the assessed source files.

This result does not rule out potential exposure through compiled assemblies, Unity packages, Editor components, or code outside the search scope.

**Corrective action:** No immediate gameplay script changes identified from this search. Continue investigating package-level and Editor-level exposure as necessary.

**Evidence:** `SOC2_Test2_BrotliSearch.png`



### 4.4 F-04 — Newtonsoft.Json dependency review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** No identified exposure to reviewed advisory  
**Status:** Completed for the specified advisory

Unity Package Manager identified the following dependency:

- Unity package: `com.unity.nuget.newtonsoft-json`
- Unity package version: `3.2.2`
- Underlying Newtonsoft.Json version: `13.0.2`

The review considered security advisory `CVE-2024-21907` / `GHSA-5crp-9r3c-p9vr`.

The advisory identifies versions earlier than `13.0.1` as affected by a denial-of-service vulnerability.

The installed underlying library version, `13.0.2`, is newer than the patched version.

**Security impact:** No identified exposure to this specific advisory.

**Corrective action:** No remediation required for the reviewed advisory. Continue monitoring for other applicable vulnerabilities.

**Evidence:**

- `SOC2_Test2_NewtonsoftPackage.png`
- https://github.com/advisories/GHSA-5crp-9r3c-p9vr

This assessment does not establish that the library is free of all possible vulnerabilities.



### 4.5 F-05 — Mono.Cecil dependency review

**Source:** Test 2 — Dependency Vulnerability Review  
**Jira:** SCRUM-159  
**Classification:** No confirmed vulnerability in consulted assessment  
**Status:** Reviewed; continued monitoring recommended

Unity Package Manager identified:

- Unity package: `com.unity.nuget.mono-cecil`
- Unity package version: `1.11.6`
- Underlying library: Mono.Cecil `0.11.5` with an additional Unity-packaged patch, according to package metadata

A consulted third-party security assessment reported no known vulnerabilities for the underlying library version.

**Security impact:** No confirmed vulnerability identified during the documented review.

**Corrective action:** Continue monitoring Unity package updates and Mono.Cecil security advisories.

**Evidence:**

- `SOC2_Test2_MonoCecilPackage.png`
- https://github.com/needle-mirror/com.unity.nuget.mono-cecil/blob/master/package.json
- https://secure.software/nuget/packages/Mono.Cecil/issues/0.11.5

This finding does not establish that every possible vulnerability has been excluded.



### 4.6 F-06 — Initial sensitive-information scan

**Source:** Test 3 — Sensitive Information Review  
**Jira:** SCRUM-160  
**Tool:** Custom Unity Editor Security Scanner  
**Classification:** No findings reported in assessed scope  
**Status:** Completed

A custom Unity Editor security scanner was used to inspect C# scripts for potentially hardcoded passwords, API keys, and authentication tokens.

**Initial scan results:**

- Files scanned: 76
- Potential findings: 0

No potential hardcoded credentials were detected by the scanner's configured patterns during the initial assessment.

**Security impact:** No confirmed credential exposure identified by the initial scan.

**Corrective action:** Retain scan evidence and repeat sensitive-information reviews when security-relevant code changes occur.

**Limitation:** The custom scanner checks predefined patterns in C# scripts. It does not provide comprehensive repository-wide secret detection.

**Evidence:**

- Initial security scanner screenshot
- `SecurityScanner.cs`
- Jira SCRUM-160



### 4.7 F-07 — Simulated credential-detection validation

**Source:** Test 3 — Sensitive Information Review  
**Jira:** SCRUM-160  
**Classification:** Security control validation  
**Status:** Completed

A controlled test was performed to verify that the custom Unity security scanner could detect a simulated hardcoded credential.

**Controlled test results:**

- Files scanned: 77
- Simulated findings detected: 1

The scanner successfully identified the fake credential in the temporary test script.

**Security impact:** No real credential exposure was established by this controlled test.

**Corrective action:** Confirm removal of the temporary test script and retain the detection screenshot as evidence.

**Evidence:**

- Controlled security scanner screenshot
- Jira SCRUM-160



## 5. Severity and impact assessment

The following classifications were used to evaluate the findings:

| Classification | Meaning |
| --- | --- |
| Critical | Confirmed vulnerability with potentially severe consequences requiring urgent remediation |
| High | Confirmed significant vulnerability requiring prioritized remediation |
| Medium | Confirmed vulnerability with moderate potential impact |
| Low | Confirmed vulnerability with limited potential impact |
| Informational | Observation or warning without an identified exploitable security impact |
| Potential risk | Possible security exposure requiring additional investigation |
| No identified exposure | Reviewed check did not establish exposure within its defined scope |

**Assessment summary:**

- No confirmed exploitable vulnerability was identified in the reviewed gameplay source-code warnings.
- The custom credential scanner reported zero potential findings during its initial scan.
- The Unity Editor advisories represent unresolved potential security exposure requiring follow-up.
- The reviewed Newtonsoft.Json advisory did not affect the identified underlying library version.
- The consulted Mono.Cecil assessment did not establish a confirmed vulnerability.

Published vulnerability severity is not necessarily equivalent to the actual severity of a vulnerability in this specific project.



## 6. Corrective actions and follow-up tracking

| Priority | Follow-up action | Related Jira | Status |
| --- | --- | --- | --- |
| High | Investigate Unity Editor advisories and coordinate a patched-version upgrade when available | SCRUM-159 | Pending |
| Medium | Review package-level exposure and monitor dependency security updates | SCRUM-159 | Ongoing |
| Medium | Expand sensitive-information scanning coverage where practical | SCRUM-160 | Recommended |
| Low | Confirm removal of the simulated credential test script | SCRUM-160 | Verification pending |
| Low | Repeat static security analysis after security-relevant code changes | SCRUM-158 | Recommended |

Any confirmed vulnerabilities requiring remediation should receive dedicated Jira follow-up issues with an assigned owner, severity, and target resolution date.

The priority values above are recommended follow-up priorities rather than confirmed application vulnerability severities.



## 7. Evidence and Jira references

| Evidence | Related test | Jira |
| --- | --- | --- |
| Runtime security scan SARIF and screenshot | Static Code Analysis | SCRUM-158 |
| Editor security scan SARIF and screenshot | Static Code Analysis | SCRUM-158 |
| Manual `BleedOnHit.cs` review | Static Code Analysis | SCRUM-158 |
| Dependency vulnerability review report | Dependency Review | SCRUM-159 |
| Unity advisory references | Dependency Review | SCRUM-159 |
| Brotli API search screenshot | Dependency Review | SCRUM-159 |
| Newtonsoft.Json package screenshot | Dependency Review | SCRUM-159 |
| Mono.Cecil package screenshot | Dependency Review | SCRUM-159 |
| Initial security scanner screenshot | Sensitive Information Review | SCRUM-160 |
| Simulated credential-detection screenshot | Sensitive Information Review | SCRUM-160 |

**Related GitHub pull request:** PR #70 — SOC 2 Dependency Vulnerability Review.

The screenshots and security assessment notes are maintained in their respective Jira tickets. Evidence should not contain actual passwords, API keys, or other sensitive credential values.



## 8. Conclusion

The SOC 2 CC7.1 security assessment consolidated results from static code analysis, dependency vulnerability review, and sensitive-information testing.

Static code analysis identified seven weak-randomness warnings, which were reviewed and determined to involve non-security-sensitive visual effects. The editor scan reported zero warnings.

The dependency review documented 13 direct Unity dependencies and 45 resolved package entries, identified Unity Editor security advisories requiring follow-up, and recorded assessments of Newtonsoft.Json and Mono.Cecil.

The sensitive-information review reported zero potential findings in the initial scan and successfully detected a simulated credential during controlled testing.

No confirmed exploitable vulnerability was identified in the reviewed gameplay code or credential-scanning results. Potential risks affecting the Unity Editor and its dependencies remain subject to further investigation and remediation planning.

The assessment provides documented findings, severity classifications, supporting evidence, and recommended corrective actions for SOC 2 CC7.1.

This report does not establish full SOC 2 compliance or guarantee that the Unity project is free of security vulnerabilities.