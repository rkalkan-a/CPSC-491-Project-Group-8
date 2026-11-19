"""CI adapter for SecurityScanner.cs credential-assignment checks. No secret values emitted."""
import argparse
import hashlib
import json
import re
from pathlib import Path

RULES = {
    'password-assignment': re.compile(r'\bpassword\s*=\s*"[^"\r\n]+"', re.I),
    'api-key-assignment': re.compile(r'\bapi[_-]?key\s*=\s*"[^"\r\n]+"', re.I),
    'token-assignment': re.compile(r'\b(?:auth[_-]?)?token\s*=\s*"[^"\r\n]+"', re.I),
}

def scan(root, commit):
    root = Path(root)
    findings, errors, files = [], [], {}
    assets = root / 'Assets'
    if not assets.is_dir():
        errors.append('assets-directory-missing')
    for path in sorted(assets.rglob('*.cs')):
        relative = path.relative_to(root).as_posix()
        try:
            if path.is_symlink():
                raise ValueError('symlink')
            data = path.read_bytes()
            text = data.decode('utf-8-sig')
            files[relative] = hashlib.sha256(data).hexdigest()
            for number, line in enumerate(text.splitlines(), 1):
                for rule, pattern in RULES.items():
                    if pattern.search(line):
                        findings.append({'path': relative, 'line': number, 'rule': rule})
        except (OSError, ValueError):
            errors.append('source-unreadable')
    if not files:
        errors.append('no-source-files')
    return {'schema_version': 1, 'commit': commit, 'files': files,
            'findings': findings, 'errors': errors}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path('.'))
    parser.add_argument('--commit', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    report = scan(args.root, args.commit)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    # Detection is evidence, not the release decision; respond.py makes that decision.
    print('Scan completed: %d files, %d potential findings, %d errors' %
          (len(report['files']), len(report['findings']), len(report['errors'])))
