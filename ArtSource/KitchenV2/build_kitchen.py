"""Original modular kitchen art. Blender meters; frontage -Y; work surface Z=1.2.
Run explicitly via Blender MCP. Preserve the open character file and all earlier art.
"""
import bpy, math, os, json
from mathutils import Vector
ROOT='C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity'
OUT=ROOT+'/Assets/Art/Kitchen/ModelsV2'
SOURCE=ROOT+'/ArtSource/KitchenV2'
NAME='TT_Kitchen_Refinement_v01'
if bpy.data.scenes.get(NAME): raise RuntimeError('Scene exists; inspect before regenerating')
previous=bpy.context.window.scene
scene=bpy.data.scenes.new(NAME);bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette={'Steel':'AABEC3','Highlight':'D7E1DD','Body':'67848A','Dark':'344A50','Wood':'C9985E','WoodLight':'D8AF75','WoodDark':'99693E','Water':'5CA6B2','Oil':'59442C','Red':'BE513E','Amber':'E8B652','Potato':'B78B4E','Spot':'85603C','Tomato':'DA4634','TomatoLight':'EF6743','Leaf':'598E3D','LeafLight':'86B850','Cream':'DDD0B0','Mushroom':'AC8C6B'}
mats={}
for name,color in palette.items():
    m=bpy.data.materials.new('KT_'+name);m.diffuse_color=tuple(int(color[i:i+2],16)/255 for i in (0,2,4))+(1,);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=m.diffuse_color;p.inputs['Roughness'].default_value=.46 if name in ('Steel','Highlight') else .78
    mats[name]=m
parts=[];stats={}
def box(name,p,s,mat='Steel',bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mats[mat])
    if bevel:
        b=o.modifiers.new('Rounded edges','BEVEL');b.width=bevel;b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name)
        n=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=n.name)
    parts.append(o);return o
def cyl(name,p,r,d,mat,rotation=(0,0,0),verts=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d,location=p,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(mats[mat]);parts.append(o);return o
def ball(name,p,s,mat,segments=12,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=p);o=bpy.context.object;o.name=name;o.scale=s;o.data.materials.append(mats[mat]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for f in o.data.polygons:f.use_smooth=True
    parts.append(o);return o
def pipe(name,points,r,mat='Steel'):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=r;curve.bevel_resolution=1;curve.resolution_u=1
    spl=curve.splines.new('POLY');spl.points.add(len(points)-1)
    for p,co in zip(spl.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o);curve.materials.append(mats[mat]);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');parts.append(bpy.context.object)
def base(appliance=False):
    box('Recessed plinth',(0,0,.12),(1.66,1.36,.22),'Dark',.03)
    box('Cabinet carcass',(0,0,.585),(1.8,1.5,.85),'Body',.055)
    # Two framed doors sit below the countertop rather than imposing a flat box facade.
    for x in (-.44,.44):
        box('Door shadow reveal',(x,-.757,.56),(.84,.022,.74),'Dark',.025)
        box('Inset door',(x,-.778,.56),(.79,.035,.68),'Steel',.035)
        box('Door recessed panel',(x,-.8,.51),(.64,.018,.44),'Body',.025)
        pipe('Pull handle',[(x-.16,-.817,.80),(x-.16,-.855,.80),(x+.16,-.855,.80),(x+.16,-.817,.80)],.021,'Dark')
    if appliance:
        box('Vent rail',(0,-.77,1.01),(1.65,.03,.13),'Dark',.012)
        for i in range(9):box('Vent fin',(-.68+i*.17,-.792,1.01),(.085,.02,.018),'Steel',.004)
    else:box('Cabinet upper rail',(0,-.77,1.00),(1.7,.04,.13),'Steel',.02)
def top(wood=True):
    box('Top shadow lip',(0,0,1.10),(1.9,1.6,.06),'Dark',.025)
    box('Rounded work surface',(0,0,1.165),(1.9,1.6,.07),'Wood' if wood else 'Steel',.028)
    if wood:
        for y in (-.48,0,.48):box('Butcher block board',(0,y,1.199),(1.84,.465,.004),'WoodLight' if y==0 else 'Wood',.001)
        for i in range(5):
            y=-.65+i*.29
            box('Subtle wood grain',(-.2+(i%2)*.3,y,1.203),(.45+(i%3)*.12,.006,.001),'WoodDark',0)
        box('Front end grain',(0,-.797,1.165),(1.80,.007,.032),'WoodDark',.002)
def end_panel(side):
    box('Oak end cap',(side*.895,0,.63),(.055,1.45,.87),'Wood',.025)
    box('End cap inset',(side*.927,0,.63),(.012,1.20,.62),'WoodLight',.018)
def bowl(cx,cy,w,d):
    box('Recess shadow',(cx,cy,.975),(w,d,.04),'Dark',.04)
    box('Basin bottom',(cx,cy,1.01),(w-.08,d-.08,.05),'Body',.07)
    for x in (-w/2,w/2):box('Basin side',(cx+x,cy,1.08),(.06,d,.2),'Steel',.025)
    for y in (-d/2,d/2):box('Basin end',(cx,cy+y,1.08),(w,.06,.2),'Steel',.025)
def finish(name,x):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.data.calc_loop_triangles();stats[name]={'triangles':len(o.data.loop_triangles),'materials':len(o.data.materials),'dimensions':list(o.dimensions)}
    bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_triangles=True)
    o.location.x=x;parts=[]
base();top();finish('CounterStraight',0)
base();top();end_panel(-1);finish('CounterEnd',2.3)
base();top();end_panel(-1);end_panel(1);finish('CounterCorner',4.6)
base();top(False);finish('CounterSteel',6.9)
base();top(False)
box('Board shadow',(0,0,1.218),(1.44,1.02,.03),'WoodDark',.05)
box('Chopping board',(-.02,0,1.25),(1.38,.96,.04),'WoodLight',.05)
for y in (-.40,.40):box('Board groove',(0,y,1.272),(1.16,.009,.001),'WoodDark',0)
knife=box('Knife blade',(-.40,.30,1.287),(.49,.15,.025),'Highlight',.006);knife.rotation_euler.z=.14
grip=box('Knife grip',(-.04,.35,1.30),(.27,.10,.06),'Dark',.022);grip.rotation_euler.z=.14
for x in (-.10,.01):cyl('Handle rivet',(x,.35,1.334),.012,.003,'Steel',verts=8)
finish('PrepCounter',9.2)
base(True)
box('Low fryer chamber',(0,0,.94),(1.78,1.48,.12),'Body',.04)
for x in (-.84,.84):box('Outer rolled rim',(x,0,1.165),(.22,1.60,.07),'Steel',.03)
for y in (-.72,.68):box('Rolled rim',(0,y,1.165),(1.62,.16,.07),'Steel',.03)
box('Basin divider',(0,0,1.15),(.09,1.34,.075),'Steel',.02)
for x in (-.38,.38):
    bowl(x,-.03,.62,1.12)
    box('Oil well',(x,-.03,1.048),(.54,1.02,.014),'Oil',.03)
    for dx in (-.28,.28):box('Basket edge',(x+dx,-.03,1.22),(.026,1.06,.03),'Highlight',.006)
    for y in (-.55,.49):box('Basket edge',(x,y,1.22),(.56,.026,.03),'Highlight',.006)
    for y in (-.40,-.2,0,.2,.40):box('Basket wire',(x,y,1.10),(.52,.014,.014),'Steel',.003)
    for dx in (-.18,0,.18):box('Basket wire',(x+dx,-.03,1.10),(.014,1.02,.014),'Steel',.003)
    for dx in (-.26,.26):
        for y in (-.4,0,.4):box('Basket upright',(x+dx,y,1.16),(.015,.015,.12),'Steel',.003)
    pipe('Basket lift',[(x,-.53,1.23),(x,-.70,1.32),(x,-.76,1.32)],.027,'Steel')
    box('Insulated grip',(x,-.72,1.34),(.25,.14,.055),'Dark',.02)
box('Control housing',(0,.64,1.39),(1.76,.25,.39),'Body',.055)
box('Control panel',(0,.50,1.40),(1.56,.028,.20),'Dark',.025)
for x in (-.50,.50):
    cyl('Dial surround',(x,.474,1.41),.092,.02,'Steel',(math.pi/2,0,0),24)
    cyl('Dial',(x,.454,1.41),.069,.024,'Dark',(math.pi/2,0,0),16)
    box('Dial pointer',(x,.439,1.445),(.015,.01,.041),'Highlight',.003)
box('Status lens',(0,.477,1.41),(.14,.024,.05),'Amber',.012)
finish('Fryer',11.5)
base(True)
# One large bowl plus a ribbed drainboard; no permanently modeled dirty dish.
bowl(-.27,-.02,1.08,1.10)
box('Water surface',(-.27,-.02,1.038),(.94,.96,.014),'Water',.05)
cyl('Drain',(-.27,-.02,1.05),.075,.008,'Dark',verts=16)
for x in (-.87,.88):box('Sink rim',(x,0,1.165),(.15,1.6,.07),'Highlight',.025)
for y in (-.72,.68):box('Sink rim',(0,y,1.165),(1.78,.16,.07),'Steel',.025)
box('Drainboard',(.57,-.03,1.16),(.47,1.19,.07),'Steel',.03)
for x in (.42,.52,.62,.72):box('Drainboard rib',(x,-.03,1.204),(.018,1.02,.014),'Highlight',.006)
pipe('Curved tap',[(-.25,.65,1.20),(-.25,.65,1.50),(-.25,.60,1.63),(-.25,.48,1.69),(-.25,.29,1.67),(-.25,.20,1.58)],.047,'Highlight')
cyl('Tap foot',(-.25,.65,1.22),.09,.035,'Steel')
for x in (-.48,-.02):
    cyl('Tap control',(x,.64,1.23),.05,.07,'Dark',verts=12)
    box('Lever',(x,.61,1.29),(.06,.17,.035),'Steel',.012)
finish('Sink',13.8)
def crate():
    base();top()
    box('Crate floor',(0,0,1.235),(1.62,1.20,.05),'WoodDark',.02)
    for level in range(2):
        for y in (-.59,.59):box('Crate long slat',(0,y,1.30+level*.13),(1.66,.055,.095),'WoodLight',.012)
        for x in (-.80,.80):box('Crate end slat',(x,0,1.30+level*.13),(.055,1.17,.095),'Wood',.012)
    for x in (-.77,.77):
        for y in (-.56,.56):box('Crate corner',(x,y,1.37),(.075,.075,.28),'WoodDark',.014)
def produce(kind):
    for i,(x,y) in enumerate([(-.46,-.29),(0,-.28),(.46,-.28),(-.43,.23),(.04,.23),(.47,.22)]):
        if kind=='Potato':
            o=ball('Potato',(x,y,1.43),(.22,.17,.18),'Potato');o.rotation_euler.z=i*.7
            for dx,dy in ((-.08,.03),(.07,-.04)):
                ball('Potato eye',(x+dx,y+dy,1.598),(.018,.018,.008),'Spot',8,4)
        elif kind=='Tomato':
            ball('Tomato',(x,y,1.43),(.205,.19,.18),'Tomato')
            for a in range(5):
                angle=a*math.tau/5
                o=ball('Calyx',(x+math.cos(angle)*.045,y+math.sin(angle)*.045,1.60),(.083,.024,.014),'Leaf',8,4);o.rotation_euler.z=angle
            pipe('Tomato stem',[(x,y,1.6),(x+.025,y,1.66)],.018,'Leaf')
        elif kind=='Mushroom':
            cyl('Mushroom stem',(x,y,1.35),.08,.19,'Cream',verts=12)
            ball('Mushroom cap',(x,y,1.49),(.22,.205,.115),'Mushroom')
            ball('Cap highlight',(x-.04,y+.02,1.585),(.11,.10,.012),'Cream',8,4)
        else:
            ball('Lettuce heart',(x,y,1.43),(.18,.18,.15),'LeafLight')
            for a in range(5):
                angle=a*math.tau/5
                o=ball('Cupped lettuce leaf',(x+math.cos(angle)*.10,y+math.sin(angle)*.1,1.44),(.13,.09,.16),'Leaf' if a%2 else 'LeafLight',10,6);o.rotation_euler=(.3*math.sin(angle),.3*math.cos(angle),angle)
for i,kind in enumerate(('Potato','Mushroom','Tomato','Lettuce')):
    crate();produce(kind);finish('Pantry'+kind,16.1+i*2.3)
os.makedirs(SOURCE,exist_ok=True)
bpy.data.libraries.write(SOURCE+'/kitchen-refinement-v01.blend',{scene},fake_user=True)
with open(SOURCE+'/metrics.json','w') as f:json.dump({'palette':palette,'modules':stats},f,indent=2)
bpy.context.window.scene=previous
result={'scene':scene.name,'modules':stats,'original_scene_restored':previous.name}
