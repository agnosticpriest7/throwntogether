"""Original low-cost perimeter details. Explicit Blender MCP authoring, meters.
Uses existing helpers/materials; preserves the open artwork and existing scenes.
"""
import bpy, math, os, ast, json
ROOT='C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity'
OUT=ROOT+'/Assets/Art/RestaurantDetails/Models'; SOURCE=ROOT+'/ArtSource/RestaurantDetails'
NAME='TT_RestaurantDetails_v01'
if bpy.data.scenes.get(NAME): raise RuntimeError('Details already exist; inspect before regeneration')
previous=bpy.context.window.scene; scene=bpy.data.scenes.new(NAME); bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mats={};parts=[];stats={}
for k,n in {'Wood':'RM_Wood','Cream':'RM_Cream','Dark':'RM_Dark','Fabric':'RM_Fabric','Brass':'RM_Brass','Leaf':'KT_Leaf','LeafLight':'KT_LeafLight'}.items():
    mats[k]=bpy.data.materials[n]
for k,h in {'Stone':'87979B','StoneLight':'A6AFAD','Pot':'956950'}.items():
    m=bpy.data.materials.new('DT_'+k);m.diffuse_color=tuple(int(h[i:i+2],16)/255 for i in (0,2,4))+(1,);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color
    mats[k]=m
src=ast.parse(open(ROOT+'/ArtSource/KitchenV2/build_kitchen.py',encoding='utf-8').read())
exec(compile(ast.Module(body=[n for n in src.body if isinstance(n,ast.FunctionDef) and n.name in ('box','cyl','ball','pipe','finish')],type_ignores=[]),'shared_helpers','exec'),globals())
# Compact broad leaves, visibly ornamental rather than loose ingredients.
cyl('Pot',(0,0,.20),.23,.40,'Pot',verts=12)
cyl('Pot rim',(0,0,.38),.26,.065,'Pot',verts=12)
cyl('Soil',(0,0,.412),.215,.009,'Dark',verts=12)
for i in range(7):
    a=i*math.tau/7; x=.17*math.cos(a);y=.17*math.sin(a)
    leaf=ball('Upright leaf',(x,y,.64+(i%2)*.11),(.09,.065,.29),'Leaf' if i%2 else 'LeafLight',8,6)
    leaf.rotation_euler=(.35*math.sin(a),-.35*math.cos(a),a)
finish('PottedPlant',0)
box('Planter base',(0,0,.23),(1.3,.52,.46),'Pot',.05)
box('Planter rim',(0,0,.46),(1.36,.57,.08),'Pot',.03)
box('Recessed soil',(0,0,.506),(1.22,.42,.01),'Dark',.02)
for i,x in enumerate((-.43,0,.43)):
    ball('Clipped foliage',(x,0,.66),(.29,.25,.28),'Leaf',10,6)
    ball('Lit foliage',(x-.07,-.05,.79),(.21,.18,.15),'LeafLight',8,5)
finish('LowPlanter',2)
# Flat modules, subdued joints, no collision or tiny decorative scatter.
box('Paver bed',(0,0,-.09),(1.9,1.6,.15),'Stone',.015)
for x in (-.4775,.4775):
    for y in (-.4025,.4025):box('Paver',(x,y,-.005),(.94,.785,.05),'StoneLight',.02)
finish('SidewalkTile',4)
box('Mat rubber edge',(0,0,.019),(1.9,1.05,.038),'Dark',.07)
box('Woven mat',(0,0,.042),(1.74,.91,.018),'Fabric',.045)
for x in (-.74,.74):box('Woven border',(x,0,.054),(.025,.78,.004),'Cream',0)
for y in (-.37,.37):box('Woven border',(0,y,.054),(1.49,.025,.004),'Cream',0)
finish('EntranceMat',6)
# Shallow canopy stays completely outside the cutaway front wall.
for i in range(7):
    strip=box('Canopy stripe',(-1.08+i*.36,0,0),(.36,1.0,.065),'Fabric' if i%2==0 else 'Cream',.02)
    strip.rotation_euler.x=.16
    box('Short valance',(-1.08+i*.36,-.50,-.15),(.36,.055,.16),'Fabric' if i%2==0 else 'Cream',.022)
for x in (-1.15,1.15):pipe('Canopy bracket',[(x,.45,-.08),(x,-.43,-.22)],.025,'Dark')
finish('EntranceAwning',9)
def sign(text,name,x):
    box('Sign frame',(0,0,0),(1.55,.07,.53),'Wood',.04)
    box('Sign face',(0,-.044,0),(1.43,.025,.41),'Dark',.02)
    curve=bpy.data.curves.new(name+' lettering','FONT');curve.body=text;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=.175;curve.extrude=.001;curve.resolution_u=2
    o=bpy.data.objects.new(name+' lettering',curve);scene.collection.objects.link(o);o.location=(0,-.061,0);o.rotation_euler=(math.pi/2,0,0);curve.materials.append(mats['Cream'])
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');parts.append(bpy.context.object)
    finish(name,x)
sign('MADE HERE','KitchenSign',12)
sign('TAKE A SEAT','DiningSign',14)
bpy.data.libraries.write(SOURCE+'/restaurant-details-v01.blend',{scene},fake_user=True)
with open(SOURCE+'/metrics.json','w') as f:json.dump(stats,f,indent=2)
bpy.context.window.scene=previous
result={'modules':stats,'restored':previous.name}
