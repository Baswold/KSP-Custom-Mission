"""Compile actual mission-log panel/model/scenario against lightweight recording fakes."""
import argparse
import json
import pathlib
import subprocess
import tempfile

parser=argparse.ArgumentParser()
parser.add_argument('--runtime',type=pathlib.Path,required=True)
parser.add_argument('--csc',type=pathlib.Path,required=True)
args=parser.parse_args()
root=pathlib.Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory(prefix='mission-log-panel-tests-') as temp:
    out=pathlib.Path(temp)
    refs=['System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll','System.Collections.dll',
          'System.Globalization.dll','System.Text.Encoding.Extensions.dll','System.Reflection.dll']
    sources=[root/'tests/logbook_panel/KspUnityFakes.cs',root/'tests/logbook_panel/MissionLogPanelTests.cs',
             root/'Source/CampaignTiming.cs',root/'Source/DutyReadinessBridge.cs',
             root/'Source/ScienceObservationModel.cs',root/'Source/Logbook/MissionLogModel.cs',
             root/'Source/Logbook/MissionLogScenario.cs',root/'Source/Logbook/MissionLogPanel.cs']
    command=['-nologo','-nostdlib+','-target:exe',f'-out:"{out}/MissionLogPanelTests.dll"']
    command += [f'-reference:"{args.runtime.resolve()/name}"' for name in refs]
    command += [f'"{source}"' for source in sources]
    response=out/'compile.rsp';response.write_text('\n'.join(command)+'\n')
    subprocess.run(['dotnet',str(args.csc),'@'+str(response)],check=True)
    (out/'MissionLogPanelTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{
        'tfm':'net'+args.runtime.name.split('.')[0]+'.0',
        'framework':{'name':'Microsoft.NETCore.App','version':args.runtime.name}}}))
    subprocess.run(['dotnet',str(out/'MissionLogPanelTests.dll')],check=True)
