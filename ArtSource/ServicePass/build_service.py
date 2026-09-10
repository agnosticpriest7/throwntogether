"""Original service-counter overlays; meters, frontage -Y, counter top Z=1.2.
Preserve the open Blender file. Shared kitchen palette, no gameplay geometry.
"""
import bpy, os, ast, json, math
ROOT='C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity'
OUT=ROOT+'/Assets/Art/ServicePass/Models'; SOURCE=ROOT+'/ArtSource/ServicePass'
NAME='TT_ServicePass_v01'
if bpy.data.scenes.get(NAME):raise RuntimeError('Service study exists; inspect before regeneration')
previous=bpy.context.window.scene;scene=bpy.data.scenes.new(NAME);bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
os.makedirs(OUT,exist_ok=True);os.makedirs(SOURCE,exist_ok=True)
mats={};parts=[];stats={}
for name in ('Steel','Highlight','Body','Dark','Wood','Amber'):
    mats[name]=bpy.data.materials.get('KT_'+name)
    if mats[name] is None:raise RuntimeError('Missing shared kitchen material '+name)
tree=ast.parse(open(ROOT+'/ArtSource/KitchenV2/build_kitchen.py',encoding='utf-8').read())
helpers=ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ('box','cyl','ball','pipe','finish')],type_ignores=[])
exec(compile(helpers,'shared_kitchen_mesh_helpers','exec'),globals())
# A low rail replaces the former overhead shelf; the middle stays open for real food.
box('Serving inset',(0,0,1.212),(1.18,.96,.016),'Dark',.045)
box('Serving mat',(0,0,1.222),(1.10,.88,.012),'Body',.04)
for x in (-.47,.47):
    for y in (-.35,.35):
        box('Landing corner',(x,y,1.231),(.16,.025,.006),'Highlight',.004)
        box('Landing corner',(x,y-math.copysign(.055,y),1.231),(.025,.13,.006),'Highlight',.004)
for x in (-.80,.80):box('Rail support',(x,.65,1.34),(.065,.07,.26),'Steel',.014)
box('Low pass rail',(0,.65,1.48),(1.80,.13,.09),'Steel',.025)
box('Rail inset',(0,.578,1.48),(1.62,.015,.035),'Dark',.004)
cyl('Bell foot',(.73,-.49,1.217),.17,.025,'Dark',verts=24)
cyl('Bell rim',(.73,-.49,1.246),.15,.025,'Steel',verts=24)
ball('Bell dome',(.73,-.49,1.258),(.136,.136,.095),'Amber',segments=20,rings=10)
cyl('Bell button stem',(.73,-.49,1.365),.024,.04,'Dark')
cyl('Bell button',(.73,-.49,1.389),.06,.025,'Steel')
finish('ServicePass',0)
# Open-front return rack: low rails, recessed slate-colored liner, visible real plates.
box('Return tray base',(0,0,1.214),(1.55,1.22,.028),'Steel',.035)
box('Return tray liner',(0,0,1.233),(1.40,1.06,.016),'Dark',.025)
for x in (-.72,.72):
    box('Rack side lower',(x,0,1.28),(.06,1.12,.08),'Body',.018)
    pipe('Rack side handle',[(x,-.49,1.28),(x,-.49,1.43),(x,.49,1.43),(x,.49,1.28)],.026,'Steel')
box('Rack back',(0,.55,1.32),(1.48,.065,.16),'Body',.018)
box('Rack front lip',(0,-.56,1.255),(1.48,.045,.035),'Steel',.012)
for x in (-.45,-.15,.15,.45):box('Tray drainage groove',(x,0,1.243),(.018,.83,.003),'Body',.004)
finish('DishReturnRack',2.3)
bpy.data.libraries.write(SOURCE+'/service-pass-v01.blend',{scene},fake_user=True)
with open(SOURCE+'/metrics.json','w') as f:json.dump(stats,f,indent=2)
bpy.context.window.scene=previous
print(json.dumps({'modules':stats,'restored':previous.name}))
