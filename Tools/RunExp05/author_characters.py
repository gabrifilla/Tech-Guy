"""Original DCC meshes, trim UVs and material atlas. Run in the connected Blender.

The user's scene is preserved. Mesh library FBX is separate from the assembled rest guide.
Native Unity rigid-joint IK remains the animation authority; no attack clips/events are authored.
"""
from pathlib import Path
import json
import math
import bpy
import bmesh
import numpy as np
from mathutils import Matrix,Vector

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Tools/RunExp05/Source'
ASSETS=ROOT/'Assets/_Project/Art/RunExp05/Enemies'
ASSETS.mkdir(parents=True,exist_ok=True)
C=Matrix(((-1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
PALETTE=((.68,.64,.54),(.045,.065,.075),(.43,.24,.12),(.24,.30,.31),(.43,.49,.46),(.12,.63,.58),(.021,.031,.037),(.25,.13,.07))

def enum_value(data,prop,wanted):
    values=[item.identifier for item in data.bl_rna.properties[prop].enum_items]
    if wanted not in values:raise ValueError((prop,wanted,values))
    return wanted

def op_enum(op,prop,wanted):
    values=[item.identifier for item in op.get_rna_type().properties[prop].enum_items]
    if wanted not in values:raise ValueError((prop,wanted,values))
    return wanted

def workspace():
    scene=bpy.data.scenes.get('TechGuy_RUNEXP05')
    if scene is None:scene=bpy.data.scenes.new('TechGuy_RUNEXP05')
    bpy.context.window.scene=scene
    scene['scope']='RUN-EXP-05 original character art; initial user Scene preserved'
    return scene

def collection(name):
    scene=workspace();value=bpy.data.collections.get(name)
    if value is None:value=bpy.data.collections.new(name);scene.collection.children.link(value)
    return value

def atlas():
    existing=bpy.data.materials.get('TG05_Ceramic_Graphite_Copper_Atlas')
    if existing:return existing
    size=1024;y,x=np.mgrid[0:size,0:size];u=(x%256)/255.;v=(y%512)/511.
    slots=(x//256)+(y//512)*4
    base=np.asarray(PALETTE,dtype=np.float32)[slots]
    broad=np.sin(u*8+v*3)*np.sin(v*9-u*2)*.016
    grain=(np.sin(x*12.9898+y*78.233)*43758.5453)%1-.5
    variation=broad+grain*.007
    rgb=np.clip(base+variation[:,:,None],0,1)
    # Authored fault and data-inlay motifs, deliberately sparse, not baked lighting.
    fault=(np.abs(u-(.26+.21*v+.012*np.sin(v*15)))<.003)&(v>.2)&(v<.75)&((slots==0)|(slots==4))
    rgb[fault]*=.65
    trace=((np.abs(v-.71)<.005)&(u>.58)&(u<.8)|(np.abs(u-.8)<.005)&(v>.59)&(v<.71))&((slots==0)|(slots==3))
    rgb[trace]=(.10,.35,.33)
    rgba=np.ones((size,size,4),dtype=np.float32);rgba[:,:,:3]=rgb
    base_image=bpy.data.images.new('TG05_Character_BaseColor',width=size,height=size,alpha=True)
    base_image.pixels.foreach_set(rgba.ravel());base_image.filepath_raw=str(ASSETS/'Character_BaseColor.png')
    base_image.file_format=enum_value(base_image,'file_format','PNG');base_image.save();base_image.pack()
    packed=np.zeros((size,size,4),dtype=np.float32)
    metallic=np.where((slots==2)|(slots==7),.70,.08);smooth=np.where((slots==2)|(slots==7),.48,.20)
    packed[:,:,:3]=metallic[:,:,None];packed[:,:,3]=smooth
    packed_image=bpy.data.images.new('TG05_Character_MetallicSmoothness',width=size,height=size,alpha=True)
    packed_image.colorspace_settings.name=enum_value(packed_image.colorspace_settings,'name','Non-Color')
    packed_image.pixels.foreach_set(packed.ravel());packed_image.filepath_raw=str(ASSETS/'Character_MetallicSmoothness.png')
    packed_image.file_format=enum_value(packed_image,'file_format','PNG');packed_image.save();packed_image.pack()
    mat=bpy.data.materials.new('TG05_Ceramic_Graphite_Copper_Atlas');mat.use_nodes=True
    shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    texture=mat.node_tree.nodes.new('ShaderNodeTexImage');texture.image=base_image
    mat.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
    shader.inputs['Roughness'].default_value=.7;shader.inputs['Metallic'].default_value=.12
    mat.diffuse_color=(.68,.64,.54,1)
    return mat

def mesh_object(name,vertices,faces,tags,group):
    if bpy.data.objects.get('LIB05_'+name):raise RuntimeError('Already authored: '+name)
    mesh=bpy.data.meshes.new(name+'_DCC05');mesh.from_pydata([(-p[0],-p[2],p[1]) for p in vertices],[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new('LIB05_'+name,mesh);group.objects.link(obj);obj['source_key']=name
    # Temporary material slots encode face families; all are collapsed to one atlas after UVs.
    for i,color in enumerate(PALETTE):
        key='TG05_UV_FAMILY_'+str(i);mat=bpy.data.materials.get(key)
        if mat is None:
            mat=bpy.data.materials.new(key);mat.use_nodes=True
            next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(*color,1)
            mat.diffuse_color=(*color,1)
        mesh.materials.append(mat)
    for i,poly in enumerate(mesh.polygons):poly.material_index=tags[i] if i<len(tags) else 0
    bpy.ops.object.select_all(action=op_enum(bpy.ops.object.select_all,'action','DESELECT'))
    obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bevel=obj.modifiers.new('Small authored ceramic bevel',enum_value(bpy.types.Modifier,'type','BEVEL'));bevel.width=.004;bevel.segments=2
    bevel.limit_method=enum_value(bevel,'limit_method','ANGLE');bevel.angle_limit=.62;bevel.material=2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    bpy.ops.object.mode_set(mode=op_enum(bpy.ops.object.mode_set,'mode','EDIT'))
    bpy.ops.mesh.select_all(action=op_enum(bpy.ops.mesh.select_all,'action','SELECT'))
    bpy.ops.uv.smart_project(angle_limit=math.radians(62),island_margin=.025)
    bpy.ops.object.mode_set(mode=op_enum(bpy.ops.object.mode_set,'mode','OBJECT'))
    uv=obj.data.uv_layers.active.data
    for poly in obj.data.polygons:
        slot=poly.material_index
        for index in poly.loop_indices:
            original=uv[index].uv.copy();uv[index].uv=((slot%4+.025+.95*original.x)/4,(slot//4+.025+.95*original.y)/2)
        poly.material_index=0;poly.use_smooth=False
    obj.data.materials.clear();obj.data.materials.append(atlas())
    bm=bmesh.new();bm.from_mesh(obj.data)
    obj['triangles']=sum(len(f.verts)-2 for f in bm.faces)
    obj['nonmanifold_edges']=sum(not e.is_manifold for e in bm.edges)
    obj['volume']=abs(bm.calc_volume(signed=True));bm.free()
    return obj

def append_loft(vertices,faces,tags,rings,sides=12,kind='armor'):
    start=len(vertices)
    for z,cy,rx,ry in rings:
        for j in range(sides):
            angle=2*math.pi*j/sides
            vertices.append((math.cos(angle)*rx,cy+math.sin(angle)*ry,z))
    for i in range(len(rings)-1):
        for j in range(sides):
            a=start+i*sides+j;b=start+i*sides+(j+1)%sides;c=a+sides;d=b+sides
            faces.append((a,c,d,b))
            upper=math.sin((j+.5)*2*math.pi/sides)>.05
            tag=0 if upper else 1
            if kind=='body':tag=3 if upper else 6
            if kind=='limb' and i>=len(rings)-3:tag=6
            if kind=='foot' and not upper:tag=6
            if kind=='jaw':tag=0 if upper else 1
            tags.append(tag)
    faces.extend([tuple(start+j for j in reversed(range(sides))),tuple(start+(len(rings)-1)*sides+j for j in range(sides))]);tags.extend([1,5 if kind=='head' else 2])

def build_fragmento():
    group=collection('TG05_Fragmento_MeshLibrary');atlas()
    recipes={
        'F_Carapaca':([(-.70,-.005,.10,.13),(-.55,.02,.29,.20),(-.31,.065,.398,.28),(-.03,.06,.37,.25),(.23,.02,.29,.19),(.47,-.005,.14,.14)],'body'),
        'F_Cabeca':([(-.22,-.015,.15,.12),(-.12,.01,.251,.17),(.06,.005,.23,.155),(.22,-.015,.18,.09),(.34,-.03,.09,.035)],'head'),
        'F_Testa':([(-.12,0,.13,.025),(-.04,0,.199,.05),(.12,.005,.17,.04),(.21,.005,.065,.015)],'armor'),
        'F_Coxa_com_encaixe':([(0,0,.12,.11),(.08,0,.18,.16),(.22,0,.16,.13),(.48,0,.10,.08),(.73,0,.06,.055),(.86,0,.08,.075),(1,0,.065,.07)],'limb'),
        'F_Canela_com_joelho':([(0,0,.075,.075),(.12,0,.10,.085),(.32,0,.065,.058),(.72,0,.038,.043),(1,0,.025,.028)],'armor'),
        'F_Apoio_bipartido':([(-.14,-.006,.035,.028),(-.05,0,.09,.045),(.055,-.004,.105,.038),(.16,-.008,.065,.03),(.20,-.013,.03,.022)],'foot'),
        'F_Mandibula_laminada':([(-.12,0,.085,.045),(0,0,.13,.075),(.13,-.012,.085,.045),(.26,.005,.05,.03),(.34,.01,.009,.012)],'jaw')}
    for name,(rings,kind) in recipes.items():
        vertices=[];faces=[];tags=[];append_loft(vertices,faces,tags,rings,12,kind);mesh_object(name,vertices,faces,tags,group)
    vertices=[];faces=[];tags=[]
    for center in (-.49,-.17,.13):
        rings=[(center-.15,.711,.16,.055),(center-.10,.727,.247,.09),(center+.055,.73,.251,.09),(center+.15,.710,.18,.055)]
        append_loft(vertices,faces,tags,rings,12,'armor')
    mesh_object('F_Dorso_segmentado',vertices,faces,tags,group)
    refine_dorsal_coverage()
    print('Fragmento authored: eight custom DCC meshes; same18 rigid renderers after reuse.')
    return group

def refine_dorsal_coverage():
    obj=bpy.data.objects['LIB05_F_Dorso_segmentado']
    if obj.get('coverage_refined'):return
    # Widen the three existing scutes to wrap the torso, within its original lateral envelope.
    # This closes the first study's exposed barrel-shaped back; no new rigid part is added.
    for vertex in obj.data.vertices:vertex.co.x*=1.48
    obj.data.update();obj['coverage_refined']=True

def add_surface_normal():
    mat=atlas();image=bpy.data.images.get('TG05_Character_SurfaceNormal')
    if image:return
    size=1024;y,x=np.mgrid[0:size,0:size];u=(x%256)/255.;v=(y%512)/511.;slots=x//256+(y//512)*4
    crease=np.exp(-((u-(.26+.21*v+.012*np.sin(v*15)))/.005)**2)*(v>.2)*(v<.75)*((slots==0)|(slots==4))
    height=-crease*.22
    dy,dx=np.gradient(height);normal=np.stack((-dx*3,-dy*3,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    pixels=np.ones((size,size,4),dtype=np.float32);pixels[:,:,:3]=normal*.5+.5
    image=bpy.data.images.new('TG05_Character_SurfaceNormal',width=size,height=size,alpha=True)
    image.colorspace_settings.name=enum_value(image.colorspace_settings,'name','Non-Color');image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw=str(ASSETS/'Character_Normal.png');image.file_format=enum_value(image,'file_format','PNG');image.save();image.pack()
    texture=mat.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image
    normal_node=mat.node_tree.nodes.new('ShaderNodeNormalMap');normal_node.inputs['Strength'].default_value=.35
    shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    mat.node_tree.links.new(texture.outputs['Color'],normal_node.inputs['Color']);mat.node_tree.links.new(normal_node.outputs['Normal'],shader.inputs['Normal'])

def build_emissor():
    group=collection('TG05_Emissor_MeshLibrary');source=json.loads((SOURCE/'emissor-contract.json').read_text())
    seen=set()
    for part in source['parts']:
        key=part['mesh']
        if key in seen:continue
        seen.add(key);vertices=part['vertices'];triangles=part['triangles'];faces=[tuple(triangles[i:i+3]) for i in range(0,len(triangles),3)]
        # Secondary pass preserves the actual hollow channel. Clean/weld/bevel/unwrap the
        # native forms rather than obstructing the muzzle with an invented solid shape.
        tags=[]
        for face in faces:
            center=sum((Vector(vertices[i]) for i in face),Vector())/len(face)
            tag=5 if key in ('E_Nucleo','E_Camara_interna') else 1 if key=='E_Canal_oco' else 0 if center.y>-.2 else 3
            tags.append(tag)
        mesh_object(key,vertices,faces,tags,group)
    print('Emissor secondary pass: source silhouette/channel retained, cleaned DCC surfaces and coherent atlas.')
    return group

def fragmento_rest_guide():
    data=json.loads((SOURCE/'fragmento-contract.json').read_text());rig=json.loads((SOURCE/'fragmento-rest-rig.json').read_text())
    group=collection('TG05_Fragmento_ArticulatedRestGuide');matrices=[Matrix([p['world'][i:i+4] for i in range(0,16,4)]) for p in rig['parts']]
    def segment(start,end):
        direction=(end-start).normalized();right=Vector((0,1,0)).cross(direction).normalized();up=direction.cross(right)
        return Matrix(((right.x,up.x,(end-start).x,start.x),(right.y,up.y,(end-start).y,start.y),(right.z,up.z,(end-start).z,start.z),(0,0,0,1)))
    for i,leg in enumerate(rig['legs']):
        hip=Vector(leg['hip']);target=Vector(leg['restFoot']);target.y=-.045
        delta=target-hip;distance=min(max(delta.length,.07),.925);direction=delta.normalized();foot=hip+direction*distance
        along=(.44**2-.5**2+distance**2)/(2*distance);height=math.sqrt(max(0,.44**2-along**2))
        bend=Vector((-1 if i%2==0 else 1,.5,0));bend=(bend-direction*bend.dot(direction)).normalized();knee=hip+direction*along+bend*height
        matrices[leg['upper']]=segment(hip,knee);matrices[leg['lower']]=segment(knee,foot);matrices[leg['foot']]=Matrix.Translation(foot)
    for i,part in enumerate(data['parts']):
        original=bpy.data.objects['LIB05_'+part['mesh']];obj=bpy.data.objects.new('Fragmento05_'+str(i)+'_'+part['name'],original.data);group.objects.link(obj);obj.matrix_world=C@matrices[i]@C.inverted()
    library=bpy.data.collections['TG05_Fragmento_MeshLibrary'];library.hide_viewport=True;library.hide_render=True
    return group

def emissor_rest_guide():
    data=json.loads((SOURCE/'emissor-contract.json').read_text());rig=json.loads((SOURCE/'emissor-rest-rig.json').read_text())
    group=collection('TG05_Emissor_ArticulatedRestGuide')
    for i,part in enumerate(data['parts']):
        original=bpy.data.objects['LIB05_'+part['mesh']];obj=bpy.data.objects.new('Emissor05_'+str(i)+'_'+part['name'],original.data);group.objects.link(obj)
        values=rig['parts'][i]['world'];matrix=Matrix([values[j:j+4] for j in range(0,16,4)])
        obj.matrix_world=Matrix.Translation(Vector((-3,0,0)))@C@matrix@C.inverted()
    library=bpy.data.collections['TG05_Emissor_MeshLibrary'];library.hide_viewport=True;library.hide_render=True
    return group

def export_mesh_library(kind):
    group=bpy.data.collections['TG05_'+kind+'_MeshLibrary'];previous=bpy.context.window.scene
    temporary=bpy.data.scenes.new('TG05_TEMP_EXPORT');bpy.context.window.scene=temporary
    report=[]
    for source in group.objects:
        obj=bpy.data.objects.new(source['source_key'],source.data);temporary.collection.objects.link(obj);obj.select_set(True)
        report.append(dict(sourceKey=source['source_key'],triangles=int(source['triangles']),nonmanifoldEdges=int(source['nonmanifold_edges']),volume=float(source['volume']),vertices=len(source.data.vertices)))
    path=ASSETS/(kind+'_MeshLibrary.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={op_enum(bpy.ops.export_scene.fbx,'object_types','MESH')},axis_forward=op_enum(bpy.ops.export_scene.fbx,'axis_forward','-Z'),axis_up=op_enum(bpy.ops.export_scene.fbx,'axis_up','Y'),global_scale=1,apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,use_mesh_modifiers=True)
    bpy.context.window.scene=previous
    for obj in list(temporary.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.scenes.remove(temporary)
    (SOURCE/(kind.lower()+'-dcc-manifest.json')).write_text(json.dumps(dict(blender=bpy.app.version_string,kind=kind,axis='Unity(x,y,z)->Blender(-x,-z,y), verified calibration',atlas='1024 basecolor and metallic/smoothness trim atlas; smart UVs reused deliberately, one material slot per mesh',rig='Native Unity rigid joint IK; assembled Blender rest guide, no humanoid retargeting or authored hit events',meshes=report),indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(kind+'_ArtSource.blend')),copy=True)
    print('Saved editable .blend copy and actual FBX:',kind,'triangles unique=',sum(m['triangles'] for m in report))
