from pathlib import Path
import shutil
import tempfile
import unittest
import restore_skirmish_ground30_authority as tool

class Ground30AuthorityRestoreTests(unittest.TestCase):
    def setUp(self):
        parent=tool.ROOT/'Logs/SkirmishProduction/Offline/AuthorityRestore';parent.mkdir(parents=True,exist_ok=True)
        self.scope=tempfile.TemporaryDirectory(dir=parent);self.root=Path(self.scope.name)
        self.archive=self.root/tool.ARCHIVE;self.archive.parent.mkdir(parents=True)
        shutil.copyfile(tool.ROOT/tool.ARCHIVE,self.archive)
    def tearDown(self):self.scope.cleanup()
    def test_default_verifies_real_archive_without_writing_authority(self):
        self.assertEqual('VerifiedArchive_RestoreRequired',tool.verify_or_restore(self.root))
        self.assertFalse((self.root/tool.MEMBER).exists())
    def test_explicit_restore_exact_actual_bytes_then_idempotent(self):
        self.assertEqual('RestoredExactAuthority',tool.verify_or_restore(self.root,True))
        target=self.root/tool.MEMBER;self.assertEqual(tool.AUTHORITY_SHA,tool.digest(target.read_bytes()))
        before=target.stat().st_mtime_ns
        self.assertEqual('VerifiedExistingAuthority',tool.verify_or_restore(self.root,True))
        self.assertEqual(before,target.stat().st_mtime_ns)
    def test_existing_foreign_bytes_are_preserved(self):
        target=self.root/tool.MEMBER;target.parent.mkdir(parents=True);target.write_bytes(b'preserve foreign evidence')
        with self.assertRaisesRegex(ValueError,'never overwrite'):tool.verify_or_restore(self.root,True)
        self.assertEqual(b'preserve foreign evidence',target.read_bytes())
    def test_changed_archive_rejects_before_target_creation(self):
        self.archive.write_bytes(self.archive.read_bytes()+b'changed')
        with self.assertRaisesRegex(ValueError,'archive bytes changed'):tool.verify_or_restore(self.root,True)
        self.assertFalse((self.root/tool.MEMBER).exists())

if __name__=='__main__':unittest.main()
