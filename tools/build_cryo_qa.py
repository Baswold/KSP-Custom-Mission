"""Compile the one-shot probe only into a marked disposable QA game."""
import argparse,pathlib,subprocess,tempfile
p=argparse.ArgumentParser();p.add_argument('--ksp-root',required=True,type=pathlib.Path);p.add_argument('--csc',required=True,type=pathlib.Path);a=p.parse_args();k=a.ksp_root.resolve();root=pathlib.Path(__file__).resolve().parents[1]
if k.name!='KSP-Hermes-QA' or not (k/'HermesCryoQA.scope').is_file() or (k/'HermesCryoQA.scope').read_text().strip()!='MEGACREW_DISPOSABLE_CRYO_QA_V1':p.error('Refusing to install a test addon into an unmarked/non-QA game')
managed=k/'KSP.app/Contents/Resources/Data/Managed';refs=list(managed.glob('*.dll'))+[k/'GameData/MegaCrew/Plugins/MegaCrew.dll',k/'GameData/REPOSoftTech/DeepFreeze/Plugins/DeepFreeze.dll',k/'GameData/REPOSoftTech/BackgroundResources/BackgroundResources.dll']
if not refs or any(not r.is_file() for r in refs):p.error('QA dependency assembly missing')
out=k/'GameData/HermesQA/HermesCryoQA.dll';out.parent.mkdir(parents=True,exist_ok=True)
args=['-nologo','-target:library','-nostdlib+','-langversion:7.3',f'-out:"{out}"']+[f'-reference:"{r}"' for r in refs]+[f'"{root}/tests/ksp/HermesCryoProbe.cs"']
with tempfile.TemporaryDirectory() as d:
 rsp=pathlib.Path(d)/'qa.rsp';rsp.write_text('\n'.join(args)+'\n');subprocess.run(['dotnet',str(a.csc),'@'+str(rsp)],check=True)
print('Built QA-only probe into marked disposable copy. It is not part of the normal MegaCrew DLL.')
