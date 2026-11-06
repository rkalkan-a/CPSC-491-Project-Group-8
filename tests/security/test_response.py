import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

TOOLS = Path(__file__).resolve().parents[2] / 'tools/security'
sys.path.insert(0, str(TOOLS))
from scan_source import scan
from respond import run

COMMIT = 'a' * 40

class ResponseTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root/'Assets').mkdir()
        self.source = self.root/'Assets/Game.cs'
        self.source.write_text('class Game {}\n')
        self.report = self.root/'scan.json'
        self.evidence = self.root/'evidence'
        self.refresh()

    def refresh(self):
        self.report.write_text(json.dumps(scan(self.root, COMMIT)))

    def respond(self):
        return run(self.root, self.report, COMMIT, self.evidence)

    def test_clean_report_allows_normal_pipeline(self):
        self.assertTrue(self.respond()['allow_publish'])

    def test_each_credential_rule_blocks_without_leaking_value(self):
        for name in ['password','api_key','authToken']:
            with self.subTest(name=name):
                self.source.write_text('string '+name+' = "FAKE_ONLY_123";\n')
                self.refresh()
                result = self.respond()
                self.assertFalse(result['allow_publish'])
                self.assertEqual(result['classification'], 'suspected_security_incident')
                self.assertEqual(result['finding_count'], 1)
                for p in self.evidence.iterdir():
                    self.assertNotIn('FAKE_ONLY_123', p.read_text())

    def test_missing_report_blocks(self):
        self.report.unlink()
        self.assertEqual(self.respond()['classification'], 'validation_failure')

    def test_malformed_and_invalid_schema_block(self):
        for raw in ['{', '[]', '{}', 'null', '{"schema_version":true}']:
            with self.subTest(raw=raw):
                self.report.write_text(raw)
                self.assertFalse(self.respond()['allow_publish'])

    def test_wrong_commit_blocks(self):
        value=json.loads(self.report.read_text()); value['commit']='b'*40
        self.report.write_text(json.dumps(value))
        self.assertFalse(self.respond()['allow_publish'])

    def test_source_change_after_scan_blocks(self):
        self.source.write_text('class Changed {}')
        self.assertFalse(self.respond()['allow_publish'])

    def test_removed_finding_cannot_bypass_gate(self):
        self.source.write_text('string password = "FAKE_ONLY";')
        self.refresh(); value=json.loads(self.report.read_text());value['findings']=[]
        self.report.write_text(json.dumps(value))
        self.assertFalse(self.respond()['allow_publish'])

    def test_no_sources_blocks(self):
        self.source.unlink(); self.refresh()
        self.assertFalse(self.respond()['allow_publish'])

    def test_unreadable_encoding_blocks(self):
        self.source.write_bytes(b'\xff\xfe');self.refresh()
        self.assertFalse(self.respond()['allow_publish'])

    def test_symlink_blocks(self):
        self.source.unlink(); self.source.symlink_to(self.report)
        self.refresh();self.assertFalse(self.respond()['allow_publish'])

    def test_bad_report_content_is_not_copied_to_evidence(self):
        self.report.write_text('SECRET_DO_NOT_COPY')
        result=self.respond()
        self.assertIsNotNone(result['source_report_sha256'])
        for p in self.evidence.iterdir():self.assertNotIn('SECRET_DO_NOT_COPY',p.read_text())

    def test_remediation_releases_gate_only_after_new_scan(self):
        self.source.write_text('string token = "FAKE_ONLY";');self.refresh()
        self.assertFalse(self.respond()['allow_publish'])
        self.source.write_text('class Corrected {}')
        self.assertFalse(self.respond()['allow_publish'])
        self.refresh();self.assertTrue(self.respond()['allow_publish'])

    def test_cli_failure_still_writes_evidence(self):
        self.report.unlink()
        result=subprocess.run([sys.executable,str(TOOLS/'respond.py'),'--root',str(self.root),
            '--report',str(self.report),'--commit',COMMIT,'--evidence-dir',str(self.evidence)],capture_output=True)
        self.assertEqual(result.returncode,1)
        self.assertTrue((self.evidence/'response.json').is_file())
        self.assertTrue((self.evidence/'source-scan.json').is_file())

    def test_workflow_blocks_build_and_preserves_evidence(self):
        workflow=(TOOLS.parents[1]/'.github/workflows/main.yml').read_text()
        self.assertIn('needs: [test, security_response]',workflow)
        self.assertIn("needs.security_response.result == 'success'",workflow)
        self.assertIn('name: Preserve security response evidence\n        if: always()',workflow)
        self.assertIn('path: security-evidence/',workflow)

if __name__=='__main__':unittest.main()
