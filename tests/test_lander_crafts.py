import importlib.util
import math
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('landers',ROOT/'tools/build_landers.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

def craft(name):return m.parse((ROOT/'Ships/VAB'/name).read_text())
def parts(root):return {p.get('part'):p for p in root.nodes('PART')}
def rotate(q,v):
    x,y,z,w=q;vx,vy,vz=v
    tx,ty,tz=2*(y*vz-z*vy),2*(z*vx-x*vz),2*(x*vy-y*vx)
    return (vx+w*tx+y*tz-z*ty,vy+w*ty+z*tx-x*tz,vz+w*tz+x*ty-y*tx)

class LanderCraftTests(unittest.TestCase):
    def test_connected_trees_with_unique_persistent_ids(self):
        for name in ('Hermes Finch Vacuum Lander.craft','Hermes Finch Docking Cradle.craft'):
            root=craft(name);p=parts(root)
            self.assertEqual(len(p),len(root.nodes('PART')))
            for key in p:
                name_part, ident = key.split('_')
                self.assertTrue(name_part and ident.isdigit(), key)
            self.assertEqual(len(set(n.get('persistentId') for n in p.values())),len(p))
            children={k:[v for key,v in n.values if key=='link'] for k,n in p.items()}
            parents={k:[] for k in p}
            for key,links in children.items():
                for link in links:self.assertIn(link,p);parents[link].append(key)
            roots=[k for k,v in parents.items() if not v];self.assertEqual(len(roots),1)
            visited=set()
            def walk(key):
                self.assertNotIn(key,visited);visited.add(key)
                for child in children[key]:walk(child)
            walk(roots[0]);self.assertEqual(visited,set(p))
            for k,n in p.items():
                self.assertLessEqual(len(parents[k]),1)
                if parents[k]:self.assertLess(list(p).index(parents[k][0]),list(p).index(k))
                for key,v in n.values:
                    if key in ('attN','srfN'):self.assertIn(v.split(',')[1].split('_0|')[0],p)
                self.assertTrue(all(math.isfinite(x) for x in m.vector(n.get('pos'))))
                self.assertAlmostEqual(sum(x*x for x in m.vector(n.get('rot'))),1,places=7)

    def test_docking_faces_coincide_and_face_each_other(self):
        p=parts(craft('Hermes Finch Docking Cradle.craft'))
        dock=p['dockingPort2_979000000']
        other=p[next(v for k,v in dock.values if k=='link')]
        faces=[];axes=[]
        for n in (dock,other):
            pos=m.vector(n.get('pos'));q=m.vector(n.get('rot'))
            offset=rotate(q,(0,.2828832,0));axis=rotate(q,(0,1,0))
            faces.append(tuple(a+b for a,b in zip(pos,offset)));axes.append(axis)
        self.assertLess(math.dist(*faces),1e-6)
        self.assertLess(sum(a*b for a,b in zip(*axes)),-.999999)

    def test_lander_has_return_fuel_and_no_separation_explosives(self):
        p=parts(craft('Hermes Finch Vacuum Lander.craft'))
        self.assertEqual(len([k for k in p if k.startswith('landingLeg1-2_')]),4)
        self.assertEqual(len([k for k in p if k.startswith('RCSBlock.v2_')]),4)
        self.assertFalse(any('decoupler' in k.lower() for k in p))
        fuel={}
        for n in p.values():
            for r in n.nodes('RESOURCE'):fuel[r.get('name')]=fuel.get(r.get('name'),0)+float(r.get('amount'))
        self.assertEqual(fuel['LiquidFuel'],360)
        self.assertEqual(fuel['Oxidizer'],440)
        self.assertEqual(fuel['MonoPropellant'],40)
        self.assertGreater(fuel['ElectricCharge'],100)

if __name__=='__main__':unittest.main()
