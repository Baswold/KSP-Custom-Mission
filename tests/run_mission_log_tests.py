"""Compile and run production mission-log model/scenario tests without KSP runtime."""
import argparse
import json
import pathlib
import subprocess
import tempfile

parser=argparse.ArgumentParser()
parser.add_argument('--runtime',type=pathlib.Path,required=True)
parser.add_argument('--csc',type=pathlib.Path,required=True)
args=parser.parse_args()
root=pathlib.Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='mission-log-tests-') as folder:
    output=pathlib.Path(folder)
    refs=['System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll',
          'System.Collections.dll','System.Globalization.dll','System.Text.Encoding.Extensions.dll']
    sources=[root/'Source/CampaignTiming.cs',root/'Source/Logbook/MissionLogModel.cs',
             root/'Source/Logbook/MissionLogScenario.cs',root/'tests/MissionLogTests.cs']
    command=['-nologo','-nostdlib+','-target:exe',f'-out:"{output}/MissionLogTests.dll"']
    command += [f'-reference:"{args.runtime.resolve()/name}"' for name in refs]
    command += [f'"{source}"' for source in sources]
    response=output/'compile.rsp';response.write_text('\n'.join(command)+'\n')
    subprocess.run(['dotnet',str(args.csc), '@'+str(response)],check=True)
    (output/'MissionLogTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{
        'tfm':'net'+args.runtime.name.split('.')[0]+'.0',
        'framework':{'name':'Microsoft.NETCore.App','version':args.runtime.name}}}))
    subprocess.run(['dotnet',str(output/'MissionLogTests.dll')],check=True)
