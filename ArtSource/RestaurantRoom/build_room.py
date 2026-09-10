"""Original restaurant furniture/cutaway kit. Uses the versioned kitchen mesh helpers.
Blender meters, floor origin, frontage -Y. Explicit authoring; preserves open artwork.
"""
import bpy, math, os, json, ast
ROOT='C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity'
OUT=ROOT+'/Assets/Art/RestaurantRoom/Models';SOURCE=ROOT+'/ArtSource/RestaurantRoom'
NAME='TT_RestaurantRoom_v01'
if bpy.data.scenes.get(NAME):raise RuntimeError('Room study already exists; inspect before regeneration')
previous=bpy.context.window.scene;scene=bpy.data.scenes.new(NAME);bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette={'Wood':'B58B5C','WoodLight':'CEA778','WoodDark':'795B40','Cream':'D8D0BC','Trim':'536F73','Dark':'34484C','Fabric':'AE584B','FabricLight':'C87864','Glass':'85B2BF','GlassLight':'B8D4D4','Brass':'B49459','Glow':'FFE1A5'}
mats={};parts=[];stats={}
for name,hex in palette.items():
    mat=bpy.data.materials.new('RM_'+name);mat.diffuse_color=tuple(int(hex[i:i+2],16)/255 for i in (0,2,4))+(1,);mat.use_nodes=True
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=mat.diffuse_color
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;mats[name]=mat
tree=ast.parse(open(ROOT+'/ArtSource/KitchenV2/build_kitchen.py',encoding='utf-8').read())
helpers=ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ('box','cyl','ball','pipe','finish')],type_ignores=[])
exec(compile(helpers,'shared_kitchen_mesh_helpers','exec'),globals())
# Table dimensions/top match the original 1.6 x 1.4 x 1.3 collision envelope.
for x in (-.60,.60):
    for y in (-.50,.50):
        box('Taper-style table leg',(x,y,.60),(.15,.15,1.12),'WoodDark',.025)
        box('Leg foot',(x,y,.09),(.16,.16,.12),'Dark',.014)
box('Table apron',(0,0,1.13),(1.38,1.18,.18),'WoodDark',.03)
box('Table rim',(0,0,1.235),(1.6,1.4,.13),'Wood',.06)
box('Inset tabletop',(0,0,1.296),(1.50,1.30,.008),'WoodLight',.025)
for y in (-.22,.22):box('Table board seam',(0,y,1.301),(1.43,.008,.001),'Wood',0)
for i in range(4):box('Table grain',(-.25+(i%2)*.35,-.49+i*.30,1.302),(.34,.004,.001),'Wood',0)
finish('DiningTable',0)
# Chair follows the existing chair footprint and customer pose, without new seat logic.
for x in (-.32,.32):
    for y in (-.10,.49):box('Chair leg',(x,y,.31),(.10,.10,.58),'WoodDark',.02)
box('Chair seat frame',(0,.18,.59),(.84,.80,.12),'WoodDark',.035)
box('Seat cushion',(0,.16,.68),(.80,.75,.12),'Fabric',.065)
box('Cushion piping',(0,.16,.729),(.74,.69,.012),'FabricLight',.045)
for x in (-.35,.35):box('Chair back post',(x,.52,.99),(.10,.10,.74),'WoodDark',.025)
box('Chair upholstered back',(0,.53,1.07),(.74,.12,.61),'Fabric',.075)
box('Back cushion inset',(0,.457,1.07),(.62,.022,.47),'FabricLight',.05)
finish('DiningChair',2.2)
def wall(height=1.32):
    box('Plaster wall',(0,0,height/2),(1.9,.20,height),'Cream',.018)
    box('Lower panel',(0,-.115,.29),(1.9,.04,.56),'Trim',.012)
    box('Skirting',(0,-.144,.065),(1.9,.04,.13),'WoodDark',.009)
    box('Chair rail',(0,-.148,.57),(1.9,.055,.065),'Wood',.012)
    box('Wall cap',(0,0,height+.025),(1.94,.26,.05),'Wood',.012)
wall();finish('WallPanel',4.4)
wall(.70);finish('WallLow',6.6)
wall()
# Opaque illustrated panes avoid Web transparency sorting and reflection cost.
box('Window recess',(0,-.116,.95),(1.46,.032,.61),'Dark',.012)
box('Cool window pane',(0,-.139,.96),(1.32,.015,.47),'Glass',.008)
box('Sky reflected band',(0,-.150,1.07),(1.29,.004,.19),'GlassLight',.002)
for x in (-.71,.71):box('Window jamb',(x,-.176,.95),(.075,.095,.63),'WoodDark',.012)
for z in (.65,1.25):box('Window frame',(0,-.176,z),(1.49,.095,.075),'WoodDark',.012)
box('Window mullion',(0,-.184,.95),(.045,.08,.53),'Cream',.006)
box('Window sill',(0,-.23,.61),(1.57,.22,.065),'Wood',.018)
finish('WindowWall',8.8)
# A closed, cutaway entrance: decorative, not a new exit or collision opening.
box('Door threshold',(0,0,.025),(2.20,.42,.05),'WoodDark',.01)
for x in (-1.04,1.04):box('Cutaway door post',(x,0,.29),(.12,.28,.58),'WoodDark',.018)
for x in (-.50,.50):
    box('Closed door lower panel',(x,0,.20),(.96,.10,.35),'Trim',.02)
    box('Door inset',(x,-.064,.21),(.81,.018,.22),'Glass',.012)
    box('Door top trim',(x,0,.39),(.96,.14,.035),'Wood',.009)
finish('EntranceCutaway',11)
box('Sconce mount',(0,0,.20),(.18,.08,.33),'Dark',.025)
pipe('Sconce arm',[(0,-.04,.26),(0,-.17,.26),(0,-.17,.38)],.025,'Brass')
cyl('Lamp shade',(0,-.17,.43),.16,.06,'Brass',verts=20)
ball('Warm diffuser',(0,-.17,.34),(.10,.10,.12),'Glow')
finish('WallSconce',13.2)
os.makedirs(SOURCE,exist_ok=True)
bpy.data.libraries.write(SOURCE+'/restaurant-room-v01.blend',{scene},fake_user=True)
with open(SOURCE+'/metrics.json','w') as f:json.dump({'palette':palette,'modules':stats},f,indent=2)
bpy.context.window.scene=previous
result={'modules':stats,'restored':previous.name}
