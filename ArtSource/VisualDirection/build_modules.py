"""Visual direction prototype. Run once in Blender via MCP; preserves existing scenes.
Meters; Unity export uses Y up. Station origins are floor-center, frontage is -Y.
Exports only this new scene and its dependencies, never the open character file.
"""
import bpy, math, os, json
from mathutils import Vector

ROOT = 'C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity'
OUT = ROOT + '/Assets/VisualDirection/Models'
SOURCE = ROOT + '/ArtSource/VisualDirection'
if bpy.data.scenes.get('TT_VisualDirection_v02'):
    raise RuntimeError('Prototype already exists; inspect before regenerating.')
previous_scene = bpy.context.window.scene
scene = bpy.data.scenes.new('TT_VisualDirection_v02')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = {
    'VD_Steel': ('#A7BFC7', .36), 'VD_Cabinet': ('#648894', .64),
    'VD_Dark': ('#30434B', .75), 'VD_Wood': ('#D6A468', .73),
    'VD_WoodEdge': ('#A96D3D', .8), 'VD_Ceramic': ('#F1E8D5', .38),
    'VD_Water': ('#58AEBB', .3), 'VD_Oil': ('#655038', .8),
    'VD_Heat': ('#EEA54B', .6), 'VD_Tile': ('#D3C6AC', .9),
    'VD_Grout': ('#8C918A', .95), 'VD_Wall': ('#E5DCC7', .9)
}
materials = {}
def rgba(h):
    return tuple(int(h[i:i+2],16)/255 for i in (1,3,5))+(1,)
for name,(color,rough) in palette.items():
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.diffuse_color=rgba(color); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=rgba(color); bs.inputs['Roughness'].default_value=rough
    materials[name]=m

parts=[]
def box(name,loc,size,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob=bpy.context.object; ob.name=name; ob.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(materials['VD_'+mat])
    if bevel:
        b=ob.modifiers.new('Soft manufactured edges','BEVEL');b.width=bevel;b.segments=2
        bpy.ops.object.modifier_apply(modifier=b.name)
        n=ob.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=n.name)
    parts.append(ob);return ob
def cylinder(name,loc,radius,depth,mat,rotation=(0,0,0),vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rotation)
    ob=bpy.context.object;ob.name=name;ob.data.materials.append(materials['VD_'+mat]);parts.append(ob);return ob
def pipe(name,points,radius,mat):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=radius;curve.bevel_resolution=1;curve.resolution_u=1
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for p,co in zip(spline.points,points):p.co=(*co,1)
    ob=bpy.data.objects.new(name,curve);scene.collection.objects.link(ob);curve.materials.append(materials['VD_'+mat])
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH');parts.append(bpy.context.object)
def cabinet(basin=False):
    box('Inset dark plinth',(0,0,.15),(1.66,1.36,.25),'Dark')
    box('Cool cabinet',(0,0,.575 if basin else .66),(1.8,1.5,.77 if basin else .94),'Cabinet',.045)
    for x in (-.435,.435):
        box('Door reveal',(x,-.757,.64),(.82,.018,.78),'Dark',.012)
        box('Steel door',(x,-.775,.65),(.77,.032,.72),'Steel',.025)
        box('Handle',(x,-.808,.89),(.28,.065,.045),'Dark',.017)
def top(material='Wood'):
    box('Worktop shadow edge',(0,0,1.095),(1.9,1.6,.055),'Dark',.02)
    box('Worktop',(0,0,1.155),(1.9,1.6,.09),material,.035)
def woodtop():
    top('Wood')
    for y in (-.26,.26):box('Quiet board seam',(0,y,1.201),(1.80,.009,.003),'WoodEdge',0)
    box('Wood end grain',(0,-.792,1.151),(1.79,.015,.035),'WoodEdge',.003)
def recess(kind):
    # Open basin: surrounding rim pieces, inset walls and visible low floor.
    box('Basin shadow',(0,0,.965),(1.80,1.5,.055),'Dark')
    for x in (-.79,.79):box('Side rim',(x,0,1.16),(.32,1.6,.08),'Steel')
    for y in (-.67,.67):box('End rim',(0,y,1.16),(1.28,.26,.08),'Steel')
    box('Basin floor',(0,0,1.0),(1.3,1.1,.045),kind,.06)
    for x in (-.64,.64):box('Basin wall',(x,0,1.085),(.05,1.10,.16),'Cabinet',.012)
    for y in (-.53,.53):box('Basin wall',(0,y,1.085),(1.3,.05,.16),'Cabinet',.012)

stats={}
def finish(name,display_x):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:p.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join();ob=bpy.context.object;ob.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    # Triangulate export deterministically; retain editable bevel geometry in source.
    ob.data.calc_loop_triangles()
    stats[name]={'triangles':len(ob.data.loop_triangles),'materials':len(ob.data.materials),'dimensions':list(ob.dimensions)}
    bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_triangles=True)
    ob.location.x=display_x;parts=[]

cabinet();woodtop();finish('CounterWood',0)
cabinet();top('Steel');finish('CounterSteel',2.4)
cabinet();top('Steel')
box('Board underside',(-.03,0,1.23),(1.36,.99,.05),'WoodEdge',.05)
box('Cutting board',(-.03,0,1.265),(1.30,.93,.045),'Wood',.05)
blade=box('Broad knife blade',(-.17,-.05,1.31),(.56,.17,.028),'Steel',.009);blade.rotation_euler.z=.28
handle=box('Knife handle',(.24,.068,1.32),(.30,.105,.07),'Dark',.023);handle.rotation_euler.z=.28
finish('PrepCounter',4.8)
cabinet(True);recess('Oil')
box('Raised control back',(0,.67,1.35),(1.8,.19,.38),'Cabinet',.04)
box('Control inset',(0,.555,1.36),(1.60,.045,.18),'Dark',.02)
for x in (-.53,.53):cylinder('Temperature knob',(x,.515,1.37),.065,.045,'Steel',(math.pi/2,0,0))
box('Heat indicator',(0,.519,1.365),(.19,.015,.035),'Heat',.01)
for x in (-.33,.33):
    for dx in (-.26,.26):box('Basket side',(x+dx,0,1.22),(.028,1.0,.13),'Steel',.006)
    for y in (-.48,.48):box('Basket end',(x,y,1.22),(.55,.028,.13),'Steel',.006)
    for y in (-.36,-.18,0,.18,.36):box('Basket grid',(x,y,1.18),(.52,.016,.018),'Steel',.003)
    for dx in (-.16,0,.16):box('Basket grid',(x+dx,0,1.18),(.016,.94,.018),'Steel',.003)
    pipe('Basket lift',[(x,-.48,1.28),(x,-.69,1.42),(x,-.77,1.42)],.035,'Steel')
    box('Basket grip',(x,-.72,1.44),(.22,.16,.065),'Dark',.02)
finish('Fryer',7.2)
cabinet(True);recess('Water')
pipe('Gooseneck tap',[(.34,.66,1.20),(.34,.66,1.55),(.34,.58,1.66),(.34,.29,1.66),(.34,.22,1.57)],.055,'Steel')
cylinder('Tap base',(.34,.66,1.215),.10,.045,'Steel')
box('Tap lever',(.57,.63,1.30),(.26,.075,.06),'Dark',.02)
cylinder('Plate in wash',(-.19,-.08,1.07),.34,.045,'Ceramic',vertices=24)
cylinder('Plate center',(-.19,-.08,1.095),.27,.015,'Steel',vertices=24)
finish('Sink',9.6)
box('Grout foundation',(0,0,-.083),(1,1,.094),'Grout',0)
box('Ceramic tile',(0,0,-.018),(.972,.972,.036),'Tile',.008)
finish('FloorTile',12)
box('Wall body',(0,0,.8),(1.9,.20,1.6),'Wall',.015)
box('Base trim',(0,-.115,.13),(1.9,.05,.26),'Cabinet',.008)
box('Wall cap',(0,0,1.62),(1.94,.27,.08),'WoodEdge',.016)
box('Warm rail',(0,-.112,1.36),(1.9,.035,.065),'Wood',.01)
finish('WallShort',14.4)
os.makedirs(SOURCE,exist_ok=True)
bpy.data.libraries.write(SOURCE+'/kitchen-modules-v01.blend',{scene},fake_user=True)
with open(SOURCE+'/module-metrics.json','w') as f:json.dump({'palette':palette,'modules':stats},f,indent=2)
bpy.context.window.scene=previous_scene
result={'scene':scene.name,'source':SOURCE+'/kitchen-modules-v01.blend','modules':stats,'original_scene_restored':previous_scene.name}
