"""Install only into a separate clean KSP copy; preserve pre-existing save data."""
import argparse, hashlib, io, json, pathlib, shutil, tempfile, urllib.request, zipfile
p=argparse.ArgumentParser();p.add_argument('--ksp-root',required=True,type=pathlib.Path);p.add_argument('--download-dependencies',action='store_true');a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1];game=a.ksp_root.resolve();gd=game/'GameData'
if not (gd/'Squad').is_dir():p.error('KSP root must contain GameData/Squad')
save=game/'saves/MegaCrew Practice'
if save.exists():p.error('MegaCrew Practice already exists. Use a fresh copy; this installer never overwrites a save.')
allowed={'Squad','ModuleManager.4.2.3.dll'}
existing=[x.name for x in gd.iterdir() if x.name not in allowed and not x.name.startswith('.')]
if existing:p.error('Use a clean separate copy. Unexpected GameData entries: '+', '.join(existing))
if a.download_dependencies:
 with tempfile.TemporaryDirectory() as d:
  for dep in json.loads((root/'dependencies.json').read_text()):
   print('Downloading',dep['name'],dep['version'])
   data=urllib.request.urlopen(dep['url'],timeout=120).read()
   if hashlib.sha256(data).hexdigest()!=dep['sha256']:raise RuntimeError('Archive hash mismatch: '+dep['name'])
   target=pathlib.Path(d)/dep['name'];target.mkdir()
   with zipfile.ZipFile(io.BytesIO(data)) as z:
    for n in z.namelist():
     if not (target/n).resolve().is_relative_to(target.resolve()):raise RuntimeError('Unsafe archive path')
    z.extractall(target)
   for x in (target/'GameData').iterdir():
    if x.name.startswith('.') or x.name=='ModuleManager.4.2.1.dll':continue
    if x.is_dir():shutil.copytree(x,gd/x.name,dirs_exist_ok=True)
    else:shutil.copy2(x,gd/x.name)
   extras=target/'Extras/StationPartsExpansionReduxIVAs'
   if extras.exists():shutil.copytree(extras,gd/extras.name,dirs_exist_ok=True)
else:p.error('For a fresh copy supply --download-dependencies; manual installation is documented in README.md')
shutil.copytree(root/'GameData/MegaCrew',gd/'MegaCrew')
shutil.copytree(root/'saves/MegaCrew Practice',save)
(save/'Ships/VAB').mkdir(parents=True,exist_ok=True)
shutil.copy2(root/'Ships/VAB/Hermes Walkthrough v2.craft',save/'Ships/VAB')
settings=gd/'FreeIva/settings.cfg'
if settings.exists():shutil.copy2(settings,settings.with_suffix('.cfg.before-megacrew.bak'))
shutil.copy2(root/'docs/FreeIVA-slow-movement.cfg',settings)
print('Installed. Launch this KSP copy and verify plugin startup in KSP.log before loading MegaCrew Practice.')
