import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('preflight', Path(__file__).resolve().parents[1] / 'tools/mac_preflight.py')
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


class PreflightTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.stock = self.root / 'stock'
        self.stock.mkdir()
        for name in p.STOCK_ITEMS:
            path = self.stock / name
            path.parent.mkdir(parents=True, exist_ok=True)
            if name in ('KSP.app', 'GameData/Squad'):
                path.mkdir()
                (path / 'asset').write_text('stock')
            else:
                path.write_text('build id = 03190\n' if name == 'buildID.txt' else 'stock')

    def test_readme_only_is_not_a_package(self):
        self.assertEqual(len(p.package_errors(self.root)), len(p.PACKAGE_FILES))

    def test_package_manifest_must_parse(self):
        for name in p.PACKAGE_FILES:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('placeholder')
        self.assertTrue(p.package_errors(self.root))
        (self.root / 'dependencies.json').write_text('{"dependencies": []}')
        self.assertEqual(p.package_errors(self.root), [])

    def test_copy_excludes_user_data_and_mods(self):
        for name in ('saves/private.sfs', 'GameData/Luna/config', 'settings.cfg', 'Universe/data'):
            path = self.stock / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('private')
        dest = self.root / 'test-copy'
        p.prepare_copy(self.stock, dest)
        self.assertTrue((dest / 'GameData/Squad/asset').is_file())
        self.assertEqual(list((dest / 'saves').iterdir()), [])
        self.assertFalse((dest / 'GameData/Luna').exists())
        self.assertFalse((dest / 'settings.cfg').exists())
        self.assertFalse((dest / 'Universe').exists())
        self.assertEqual((self.stock / 'saves/private.sfs').read_text(), 'private')

    def test_never_overwrites_existing_destination(self):
        dest = self.root / 'existing'
        dest.mkdir()
        sentinel = dest / 'save'
        sentinel.write_text('keep')
        with self.assertRaises(FileExistsError):
            p.prepare_copy(self.stock, dest)
        self.assertEqual(sentinel.read_text(), 'keep')

    def test_rejects_nested_directories(self):
        for dest in (self.stock, self.stock / 'nested', self.root):
            with self.assertRaises(ValueError):
                p.prepare_copy(self.stock, dest)

    def test_wrong_build_does_not_create_destination(self):
        (self.stock / 'buildID.txt').write_text('build id = 00000')
        dest = self.root / 'wrong'
        with self.assertRaises(ValueError):
            p.prepare_copy(self.stock, dest)
        self.assertFalse(dest.exists())


if __name__ == '__main__':
    unittest.main()
