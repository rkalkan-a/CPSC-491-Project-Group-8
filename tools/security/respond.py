"""Fail-closed source-security response gate. Does not claim malware detection or certification."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import re
from pathlib import Path
from scan_source import scan

SHA = re.compile(r'^[0-9a-f]{40}$')

def decide(root, report_path, commit):
    result = {'schema_version': 1, 'timestamp_utc': datetime.now(timezone.utc).isoformat(),
              'commit': commit if SHA.fullmatch(commit) else 'invalid',
              'allow_publish': False, 'classification': 'validation_failure',
              'action': 'withhold_build_and_publication', 'reason': 'invalid-evidence',
              'finding_count': 0, 'source_report_sha256': None}
    try:
        raw = Path(report_path).read_bytes()
        result['source_report_sha256'] = hashlib.sha256(raw).hexdigest()
        report = json.loads(raw)
        if not SHA.fullmatch(commit) or not isinstance(report, dict):
            raise ValueError()
        if type(report.get('schema_version')) is not int or report['schema_version'] != 1:
            raise ValueError()
        if report.get('commit') != commit:
            raise ValueError()
        # Recompute the scan against this checkout: rejects stale/incomplete reports,
        # modified source, removed findings, and empty manifests.
        current = scan(root, commit)
        if report != current or current['errors'] or not current['files']:
            raise ValueError()
        result['finding_count'] = len(current['findings'])
        if current['findings']:
            result.update(classification='suspected_security_incident',
                          reason='potential-hardcoded-credential')
        else:
            result.update(allow_publish=True, classification='clear',
                          action='permit_normal_pipeline', reason='configured-checks-clear')
    except (OSError, ValueError, TypeError, UnicodeError):
        pass
    return result

def run(root, report_path, commit, evidence_dir):
    result = decide(root, report_path, commit)
    evidence_dir = Path(evidence_dir)
    evidence_dir.mkdir(parents=True, exist_ok=True)
    # Never copy untrusted report text: it could contain secrets. Preserve its hash
    # and a newly computed, redacted source inventory instead.
    (evidence_dir / 'source-scan.json').write_text(json.dumps(scan(root, commit), indent=2)+'\n')
    (evidence_dir / 'response.json').write_text(json.dumps(result, indent=2)+'\n')
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path('.'))
    parser.add_argument('--report', type=Path, required=True)
    parser.add_argument('--commit', required=True)
    parser.add_argument('--evidence-dir', type=Path, required=True)
    args = parser.parse_args()
    response = run(args.root, args.report, args.commit, args.evidence_dir)
    print(json.dumps(response, indent=2))
    raise SystemExit(0 if response['allow_publish'] else 1)
