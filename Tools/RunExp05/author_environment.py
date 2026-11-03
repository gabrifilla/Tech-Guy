"""Archive construction, authored in the live Blender; native collision remains Unity's."""
import bpy, math, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/_Project/Art/RunExp05/Environment'
OUT.mkdir(parents=True,exist_ok=True)

def build(art):
    group=art.collection('TG05_Archive_Environment')
    def module(name,parts):
        v=[];f=[];tags=[]
        for cx,cy,cz,sx,sy,sz,tag in parts:
            # Octagonal laminated cross section: substantial surfaces, eased corners.
            start=len(v);cut=.10
            ring=[(-.5+cut,-.5),(.5-cut,-.5),(.5,-.5+cut),(.5,.5-cut),(.5-cut,.5),(-.5+cut,.5),(-.5,.5-cut),(-.5,-.5+cut)]
            for h in (-.5,.5):
                for x,z in ring:v.append((cx+x*sx,cy+h*sy,cz+z*sz))
            f.extend([tuple(start+i for i in reversed(range(8))),tuple(start+8+i for i in range(8))]);tags.extend([tag,tag])
            for i in range(8):f.append((start+i,start+(i+1)%8,start+(i+1)%8+8,start+i+8));tags.append(tag)
        obj=art.mesh_object(name,v,f,tags,group)
        obj['source_key']=name
        return obj
    # One solid archive core, raised cartridge laminations and a protected central bus.
    # All extents remain inside the native unit cube; no visually open passage is implied.
    cover=[(0,-.44,0,.99,.12,.99,1),(0,-.03,0,.90,.70,.90,3),(0,.43,0,.92,.13,.92,1)]
    for x in (-.248,.248):
        for y in (-.27,-.02,.23):
            cover.append((x,y,0,.465,.235,.975,0 if y!=-.02 else 4))
    cover.extend([(0,.485,0,.12,.025,.94,2),(0,.475,0,.035,.04,.82,6)])
    # Recessed end vents read as cartridge sockets, not hazard lights.
    for z in (-.489,.489):
        for x in (-.33,-.24,-.15,.15,.24,.33):cover.append((x,-.02,z,.032,.11,.016,1))
    module('ArchiveCover',cover)
    # Repeated perimeter service rack: ceramic cheeks protect a dark recessed data stack.
    wall=[(0,0,0,.92,.94,.72,1),(-.40,0,.035,.15,.99,.90,0),(.40,0,.035,.15,.99,.90,0),(0,-.42,.04,.70,.12,.90,3),(0,.42,.04,.70,.12,.90,3)]
    for y in (-.27,-.09,.09,.27):wall.append((0,y,.04,.59,.13,.78,4))
    wall.extend([(-.29,0,.43,.022,.69,.018,2),(.29,0,.43,.022,.69,.018,2)])
    module('ArchiveServiceRack',wall)
    # Export ONLY task-owned library objects through the already calibrated FBX pipeline.
    old=art.ASSETS;art.ASSETS=OUT
    renamed='TG05_Archive_MeshLibrary';group.name=renamed
    assembly_guide(art)
    try:art.export_mesh_library('Archive')
    finally:art.ASSETS=old
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Tools/RunExp05/Source/Archive_ArtSource.blend'),copy=True)
    print(json.dumps({o['source_key']:{'triangles':o['triangles'],'vertices':len(o.data.vertices),'nonmanifold':o['nonmanifold_edges']} for o in group.objects}))

def assembly_guide(art):
    from mathutils import Vector
    group=art.collection('TG05_Archive_AssemblyGuide')
    for key,position,scale in [('ArchiveCover',(4.8,0,1.1),(4.2,4.2,2.2)),('ArchiveServiceRack',(8,0,1.9),(2.45,.48,3.8))]:
        name='Archive05_Display_'+key;obj=bpy.data.objects.get(name)
        if obj is None:obj=bpy.data.objects.new(name,bpy.data.objects['LIB05_'+key].data);group.objects.link(obj)
        obj.location=Vector(position);obj.scale=Vector(scale)
    library=bpy.data.collections['TG05_Archive_MeshLibrary'];library.hide_viewport=True;library.hide_render=True

