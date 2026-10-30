import io
import json
import os
from pathlib import Path
import stat
import subprocess
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

    def test_operations_overlay_preflights_collisions_without_overwrite_or_partial_write(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            existing = root / "Source" / "OnboardEngineering.cs"
            existing.parent.mkdir()
            existing.write_text("preserve published core")
            with self.assertRaisesRegex(builder.BuildError, "would overwrite an existing path"):
                builder.overlay_files(root, [("Operations", {
                    "Source/OnboardEngineering.cs": b"replacement",
                    "Source/OnboardOperations.cs": b"must not be written",
                })])
            self.assertEqual(existing.read_text(), "preserve published core")
            self.assertFalse((root / "Source" / "OnboardOperations.cs").exists())

    def test_missing_operations_blob_fails_before_destination_creation(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            repo = temp / "fixture-repo"
            repo.mkdir()
            subprocess.run(["git", "init", "--quiet", str(repo)], check=True)
            subprocess.run(["git", "-C", str(repo), "config", "user.name", "Fixture"], check=True)
            subprocess.run(["git", "-C", str(repo), "config", "user.email", "fixture@example.invalid"], check=True)

            archive = make_zip([
                (zipfile.ZipInfo("README.md"), b"fixture base\n"),
                (zipfile.ZipInfo("Source/MegaCrew.cs"), b"published core\n"),
            ])
            (repo / builder.ZIP_PATH).write_bytes(archive)
            (repo / "stage.patch").write_bytes(("""
diff --git a/README.md b/README.md
--- a/README.md
+++ b/README.md
@@ -1 +1 @@
-fixture base
+fixture patched
""").lstrip().encode("utf-8"))

            for rel in builder.NAVIGATION_TEST_PATHS:
                target = repo / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text("// nav test fixture: " + rel + "\n")
            nav_source = repo / "Source" / "Navigation" / "Fixture.cs"
            nav_source.parent.mkdir(parents=True)
            nav_source.write_text("// fixture navigation source\n")
            for rel in builder.OPERATIONS_SOURCE_PATHS + builder.OPERATIONS_TEST_PATHS:
                target = repo / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text("// operations fixture: " + rel + "\n")
            (repo / "Source" / "MegaCrew.cs").write_text("untrusted core from operations ref\n")

            subprocess.run(["git", "-C", str(repo), "add", "."], check=True)
            subprocess.run([
                "git", "-C", str(repo), "-c", "commit.gpgsign=false",
                "-c", "core.hooksPath=/dev/null", "commit", "--quiet", "-m", "fixture inputs",
            ], check=True)
            fixture_ref = subprocess.run(
                ["git", "-C", str(repo), "rev-parse", "HEAD"], check=True,
                stdout=subprocess.PIPE, text=True,
            ).stdout.strip()

            old_ops_args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": fixture_ref, "output": temp / "old-operations-ref",
                "prepare_only": True, "managed": None, "csc": None,
            })()
            old_ops = builder.build(old_ops_args,
                                    patch_sequence=({
                                        "ref": fixture_ref, "path": "stage.patch",
                                        "expected_sha256": builder.sha256((repo / "stage.patch").read_bytes()),
                                    },),
                                    expected_zip_sha256=builder.sha256(archive))
            old_manifest = json.loads((old_ops / "manifest.json").read_text())
            self.assertEqual(old_manifest["docking_group"]["status"], "absent")
            self.assertIsNone(old_manifest["docking_group"]["files"])
            self.assertFalse((old_ops / "Source/OnboardDockingInstruments.cs").exists())

            empty_ref_args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": "", "output": temp / "empty-operations-ref",
                "prepare_only": True, "managed": None, "csc": None,
            })()
            with self.assertRaisesRegex(builder.BuildError, "Invalid Git ref"):
                builder.build(empty_ref_args,
                              patch_sequence=({
                                  "ref": fixture_ref, "path": "stage.patch",
                                  "expected_sha256": builder.sha256((repo / "stage.patch").read_bytes()),
                              },),
                              expected_zip_sha256=builder.sha256(archive))
            self.assertFalse(empty_ref_args.output.exists())

            missing = repo / "Source" / "OnboardEngineering.cs"
            missing.unlink()
            subprocess.run(["git", "-C", str(repo), "add", "-u"], check=True)
            subprocess.run([
                "git", "-C", str(repo), "-c", "commit.gpgsign=false",
                "-c", "core.hooksPath=/dev/null", "commit", "--quiet", "-m", "remove allowed blob",
            ], check=True)
            missing_ref = subprocess.run(
                ["git", "-C", str(repo), "rev-parse", "HEAD"], check=True,
                stdout=subprocess.PIPE, text=True,
            ).stdout.strip()
            args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": missing_ref, "output": temp / "must-not-exist",
                "prepare_only": True, "managed": None, "csc": None,
            })()
            with self.assertRaisesRegex(builder.BuildError, "Missing Git object"):
                builder.build(args,
                              patch_sequence=({
                                  "ref": fixture_ref, "path": "stage.patch",
                                  "expected_sha256": builder.sha256((repo / "stage.patch").read_bytes()),
                              },),
                              expected_zip_sha256=builder.sha256(archive))
            self.assertFalse(args.output.exists())

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

    def test_ordered_patch_sequence_is_verified_and_applied_before_overlay(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            repo = temp / "fixture-repo"
            repo.mkdir()
            subprocess.run(["git", "init", "--quiet", str(repo)], check=True)
            subprocess.run(["git", "-C", str(repo), "config", "user.name", "Fixture"], check=True)
            subprocess.run(["git", "-C", str(repo), "config", "user.email", "fixture@example.invalid"], check=True)

            archive = make_zip([
                (zipfile.ZipInfo("README.md"), b"fixture base\n"),
                (zipfile.ZipInfo("Source/MegaCrew.cs"), b"published core\n"),
            ])
            first_patch = (
                "diff --git a/Source/Sequence.txt b/Source/Sequence.txt\n"
                "new file mode 100644\n"
                "--- /dev/null\n"
                "+++ b/Source/Sequence.txt\n"
                "@@ -0,0 +1 @@\n"
                "+stage one\n"
            ).encode("utf-8")
            second_patch = (
                "diff --git a/Source/Sequence.txt b/Source/Sequence.txt\n"
                "--- a/Source/Sequence.txt\n"
                "+++ b/Source/Sequence.txt\n"
                "@@ -1 +1 @@\n"
                "-stage one\n"
                "+stage two\n"
            ).encode("utf-8")
            (repo / builder.ZIP_PATH).write_bytes(archive)
            (repo / "first.patch").write_bytes(first_patch)
            (repo / "second.patch").write_bytes(second_patch)
            nav = repo / "Source" / "Navigation" / "Fixture.cs"
            nav.parent.mkdir(parents=True)
            nav.write_text("// fixture navigation source\n")
            for rel in builder.NAVIGATION_TEST_PATHS:
                target = repo / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text("// nav test fixture: " + rel + "\n")
            for rel in builder.OPERATIONS_SOURCE_PATHS + builder.OPERATIONS_TEST_PATHS:
                target = repo / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text("// operations fixture: " + rel + "\n")
            for rel in builder.DOCKING_GROUP_PATHS:
                target = repo / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text("// docking fixture: " + rel + "\n")
            # The source ref contains a different core file, but the operations
            # allowlist must never copy it over the published package.
            (repo / "Source" / "MegaCrew.cs").write_text("untrusted core from operations ref\n")
            subprocess.run(["git", "-C", str(repo), "add", "."], check=True)
            subprocess.run([
                "git", "-C", str(repo), "-c", "commit.gpgsign=false",
                "-c", "core.hooksPath=/dev/null", "commit", "--quiet", "-m", "fixture inputs",
            ], check=True)
            fixture_ref = subprocess.run(
                ["git", "-C", str(repo), "rev-parse", "HEAD"], check=True,
                stdout=subprocess.PIPE, text=True,
            ).stdout.strip()

            patch_sequence = (
                {"ref": fixture_ref, "path": "first.patch", "expected_sha256": builder.sha256(first_patch)},
                {"ref": fixture_ref, "path": "second.patch", "expected_sha256": builder.sha256(second_patch)},
            )
            output = temp / "candidate"
            args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": fixture_ref,
                "output": output, "prepare_only": True, "managed": None, "csc": None,
            })()

            # A bad later hash is rejected before the builder creates its destination.
            bad_sequence = (patch_sequence[0], dict(patch_sequence[1], expected_sha256="0" * 64))
            bad_args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": fixture_ref,
                "output": temp / "rejected", "prepare_only": True, "managed": None, "csc": None,
            })()
            with self.assertRaisesRegex(builder.BuildError, "SHA-256 mismatch"):
                builder.build(bad_args, patch_sequence=bad_sequence,
                              expected_zip_sha256=builder.sha256(archive))
            self.assertFalse(bad_args.output.exists())

            built = builder.build(args, patch_sequence=patch_sequence,
                                  expected_zip_sha256=builder.sha256(archive))
            self.assertEqual((built / "Source" / "Sequence.txt").read_text(), "stage two\n")
            manifest = json.loads((built / "manifest.json").read_text())
            self.assertEqual([item["path"] for item in manifest["patch_sequence"]],
                             ["first.patch", "second.patch"])
            self.assertEqual([item["index"] for item in manifest["patch_sequence"]], [1, 2])
            self.assertEqual([item["sha256"] for item in manifest["patch_sequence"]],
                             [builder.sha256(first_patch), builder.sha256(second_patch)])
            self.assertEqual([item["expected_sha256"] for item in manifest["patch_sequence"]],
                             [builder.sha256(first_patch), builder.sha256(second_patch)])
            self.assertEqual([item["commit"] for item in manifest["patch_sequence"]],
                             [fixture_ref, fixture_ref])
            self.assertEqual(manifest["inputs"][builder.ZIP_PATH]["sha256"], builder.sha256(archive))
            self.assertEqual(manifest["operations_ref"], fixture_ref)
            self.assertEqual(manifest["operations_commit"], fixture_ref)
            self.assertEqual(set(manifest["operations_sources"]), set(builder.OPERATIONS_SOURCE_PATHS))
            self.assertEqual(set(manifest["operations_tests"]), set(builder.OPERATIONS_TEST_PATHS))
            self.assertEqual(manifest["docking_group"]["status"], "present")
            self.assertEqual(manifest["docking_group"]["ref"], fixture_ref)
            self.assertEqual(manifest["docking_group"]["commit"], fixture_ref)
            self.assertEqual(set(manifest["docking_group"]["files"]), set(builder.DOCKING_GROUP_PATHS))
            self.assertEqual(set(manifest["navigation_tests"]["files"]), set(builder.NAVIGATION_TEST_PATHS))
            self.assertEqual((built / "Source" / "MegaCrew.cs").read_text(), "published core\n")
            for rel in builder.OPERATIONS_SOURCE_PATHS + builder.OPERATIONS_TEST_PATHS:
                self.assertEqual((built / rel).read_text(), "// operations fixture: " + rel + "\n")
            for rel in builder.DOCKING_GROUP_PATHS:
                self.assertEqual((built / rel).read_text(), "// docking fixture: " + rel + "\n")
            for rel in builder.NAVIGATION_TEST_PATHS:
                self.assertEqual((built / rel).read_text(), "// nav test fixture: " + rel + "\n")
            nav_only_args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "output": temp / "nav-only", "prepare_only": True, "managed": None, "csc": None,
            })()
            nav_only = builder.build(nav_only_args, patch_sequence=patch_sequence,
                                     expected_zip_sha256=builder.sha256(archive))
            nav_only_manifest = json.loads((nav_only / "manifest.json").read_text())
            self.assertIsNone(nav_only_manifest["operations_ref"])
            self.assertIsNone(nav_only_manifest["operations_sources"])
            self.assertIsNone(nav_only_manifest["operations_tests"])
            self.assertTrue((nav_only / "tests/navigation/run.py").is_file())
            self.assertFalse((nav_only / "Source/OnboardOperations.cs").exists())
            self.assertTrue((built / "provenance" / "first.patch").is_file())
            self.assertTrue((built / "provenance" / "second.patch").is_file())

            # A ref with only six of the seven optional paths is not treated as
            # an older operations ref and fails before creating its output.
            (repo / builder.DOCKING_GROUP_PATHS[0]).unlink()
            subprocess.run(["git", "-C", str(repo), "add", "-u"], check=True)
            subprocess.run([
                "git", "-C", str(repo), "-c", "commit.gpgsign=false",
                "-c", "core.hooksPath=/dev/null", "commit", "--quiet", "-m", "partial docking group",
            ], check=True)
            partial_ref = subprocess.run(
                ["git", "-C", str(repo), "rev-parse", "HEAD"], check=True,
                stdout=subprocess.PIPE, text=True,
            ).stdout.strip()
            partial_args = type("Args", (), {
                "repo": repo, "upstream_ref": fixture_ref, "navigation_ref": fixture_ref,
                "operations_ref": partial_ref, "output": temp / "partial-docking",
                "prepare_only": True, "managed": None, "csc": None,
            })()
            with self.assertRaisesRegex(builder.BuildError, "Incomplete optional docking group"):
                builder.build(partial_args, patch_sequence=patch_sequence,
                              expected_zip_sha256=builder.sha256(archive))
            self.assertFalse(partial_args.output.exists())
            repo_status = subprocess.run(
                ["git", "-C", str(repo), "status", "--porcelain"], check=True,
                stdout=subprocess.PIPE, text=True,
            ).stdout
            self.assertEqual(repo_status, "")


if __name__ == "__main__":
    unittest.main(verbosity=2)
