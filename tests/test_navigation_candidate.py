import io
import os
from pathlib import Path
import stat
import tempfile
import unittest
import zipfile

import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
import build_navigation_candidate as builder


def make_zip(entries):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w") as archive:
        for info, data in entries:
            archive.writestr(info, data)
    return stream.getvalue()


class BuilderSafetyTests(unittest.TestCase):
    def test_broken_output_symlink_is_rejected_before_resolving(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            target = temp / "missing-target"
            link = temp / "output-link"
            link.symlink_to(target, target_is_directory=True)
            with self.assertRaises(builder.BuildError):
                builder.ensure_new_destination(link)
            self.assertTrue(link.is_symlink())
            self.assertFalse(target.exists())

    def test_bad_artifact_hash_rejected(self):
        with self.assertRaisesRegex(builder.BuildError, "SHA-256 mismatch"):
            builder.verify_sha256(b"not the pinned artifact", "0" * 64, "artifact")

    def test_traversal_member_rejected(self):
        unsafe = zipfile.ZipInfo("../escape.txt")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "out"
            root.mkdir()
            with self.assertRaisesRegex(builder.BuildError, "Unsafe ZIP member"):
                builder.safe_extract(make_zip([(unsafe, b"bad")]), root)
            self.assertFalse((Path(temp) / "escape.txt").exists())

    def test_absolute_windows_and_backslash_paths_rejected(self):
        for name in ("/absolute.txt", "C:/drive.txt", "folder\\escape.txt", "a//b.txt", "a/./b.txt"):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as temp:
                root = Path(temp) / "out"
                root.mkdir()
                with self.assertRaises(builder.BuildError):
                    builder.safe_extract(make_zip([(zipfile.ZipInfo(name), b"bad")]), root)

    def test_symlink_member_rejected(self):
        info = zipfile.ZipInfo("link")
        info.create_system = 3
        info.external_attr = (stat.S_IFLNK | 0o777) << 16
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "out"
            root.mkdir()
            with self.assertRaisesRegex(builder.BuildError, "symlinks"):
                builder.safe_extract(make_zip([(info, b"target")]), root)
            self.assertFalse((root / "link").exists())

    def test_archive_extracts_at_output_root(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "out"
            root.mkdir()
            archive = make_zip([
                (zipfile.ZipInfo("README.md"), b"readme"),
                (zipfile.ZipInfo("Source/Example.cs"), b"source"),
            ])
            count = builder.safe_extract(archive, root)
            self.assertEqual(count, 2)
            self.assertEqual((root / "README.md").read_bytes(), b"readme")
            self.assertEqual((root / "Source/Example.cs").read_bytes(), b"source")
            self.assertFalse((root / "Hermes-Practice-v0.3.0/README.md").exists())

    def test_extract_refuses_overwrite_and_preserves_existing_file(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "out"
            root.mkdir()
            existing = root / "README.md"
            existing.write_text("keep me")
            with self.assertRaisesRegex(builder.BuildError, "overwrite"):
                builder.safe_extract(make_zip([(zipfile.ZipInfo("README.md"), b"replace")]), root)
            self.assertEqual(existing.read_text(), "keep me")

    def test_symlinked_parent_rejected_before_creating_outside_directory(self):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            root = base / "out"
            outside = base / "outside"
            root.mkdir()
            outside.mkdir()
            (root / "linked").symlink_to(outside, target_is_directory=True)
            archive = make_zip([(zipfile.ZipInfo("linked/new/file.txt"), b"bad")])
            with self.assertRaisesRegex(builder.BuildError, "escaped the output root"):
                builder.safe_extract(archive, root)
            self.assertFalse((outside / "new").exists())

    def test_existing_output_directory_is_not_overwritten(self):
        with tempfile.TemporaryDirectory() as temp:
            existing = Path(temp) / "output"
            existing.mkdir()
            sentinel = existing / "sentinel"
            sentinel.write_text("keep")
            with self.assertRaisesRegex(builder.BuildError, "already exists"):
                builder.ensure_new_destination(existing)
            self.assertEqual(sentinel.read_text(), "keep")

    def test_relative_destination_is_resolved_before_creation(self):
        with tempfile.TemporaryDirectory() as temp:
            previous = os.getcwd()
            try:
                os.chdir(temp)
                output = builder.ensure_new_destination(Path("relative-out"))
                self.assertTrue(output.is_absolute())
                self.assertEqual(output, (Path(temp) / "relative-out").resolve())
            finally:
                os.chdir(previous)

    def test_output_inside_checkout_is_rejected_without_creation(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp) / "checkout"
            repo.mkdir()
            output = repo / "candidate"
            with self.assertRaisesRegex(builder.BuildError, "outside the source checkout"):
                builder.ensure_new_destination(output, repo)
            self.assertFalse(output.exists())

    def test_tree_hash_excludes_only_root_manifest(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / "manifest.json").write_text("root manifest")
            (root / "nested").mkdir()
            nested_manifest = root / "nested" / "manifest.json"
            nested_manifest.write_text("nested manifest")
            files, digest = builder.hash_tree(root)
            self.assertNotIn("manifest.json", files)
            self.assertIn("nested/manifest.json", files)
            self.assertEqual(files["nested/manifest.json"], builder.sha256(b"nested manifest"))
            self.assertTrue(digest)

    def test_duplicate_and_case_colliding_members_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "out"
            root.mkdir()
            archive = make_zip([
                (zipfile.ZipInfo("Source/A.cs"), b"one"),
                (zipfile.ZipInfo("source/a.cs"), b"two"),
            ])
            with self.assertRaisesRegex(builder.BuildError, "Duplicate/case-colliding"):
                builder.safe_extract(archive, root)


if __name__ == "__main__":
    unittest.main(verbosity=2)
