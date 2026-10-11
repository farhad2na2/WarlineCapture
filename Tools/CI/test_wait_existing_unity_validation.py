import contextlib
import io
import pathlib
import tempfile
import unittest
from unittest.mock import patch

from wait_existing_unity_validation import wait_for_completion


class ExistingEditorReceiptTests(unittest.TestCase):
    def check(self, header=None, receipt=None, dispatch=0, timeout=.005, project="/project"):
        with tempfile.TemporaryDirectory() as directory:
            log = pathlib.Path(directory) / "validation.log"
            if header is not None:
                log.write_text(header + "\n")
            if receipt is not None:
                pathlib.Path(str(log) + ".exit").write_text(receipt)
            with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                return wait_for_completion(log, project, "Tests.Run", timeout, dispatch, .001)

    header = "[ExistingEditorValidation] project=/project/Assets executeMethod=Tests.Run"

    def test_matching_success(self):
        self.assertEqual(self.check(self.header, "0\n"), 0)

    def test_failed_native_receipt(self):
        self.assertEqual(self.check(self.header, "1"), 1)

    def test_invalid_receipt(self):
        self.assertEqual(self.check(self.header, "Passed"), 1)

    def test_receipt_without_owned_log(self):
        self.assertEqual(self.check(receipt="0"), 124)

    def test_wrong_method_cannot_pass(self):
        self.assertEqual(self.check(self.header.replace("Tests.Run", "Other.Run"), "0"), 1)

    def test_dispatch_failure_requires_owned_completion(self):
        self.assertEqual(self.check(self.header, "0", dispatch=8), 0)
        self.assertEqual(self.check(receipt="0", dispatch=8), 124)

    def test_missing_completion_times_out(self):
        self.assertEqual(self.check(self.header), 124)

    def test_empty_receipt_cannot_pass(self):
        self.assertEqual(self.check(self.header, ""), 124)

    def test_expired_timeout_cannot_pass(self):
        self.assertEqual(self.check(self.header, "0", timeout=0), 124)

    def test_late_ownership_cannot_pass(self):
        with patch("wait_existing_unity_validation.time.monotonic", side_effect=[0, 0, 61]):
            self.assertEqual(self.check(self.header, "0", timeout=120), 124)

    def test_receipt_read_after_deadline_cannot_pass(self):
        with patch("wait_existing_unity_validation.time.monotonic", side_effect=[0, 0, 0, 11]):
            self.assertEqual(self.check(self.header, "0", timeout=10), 124)

    def test_project_trailing_slash(self):
        self.assertEqual(self.check(self.header, "0", project="/project/"), 0)

    def test_relative_project_path(self):
        header = "[ExistingEditorValidation] project=" + str(pathlib.Path.cwd() / "Assets") + " executeMethod=Tests.Run"
        self.assertEqual(self.check(header, "0", project="."), 0)


if __name__ == "__main__":
    unittest.main()
