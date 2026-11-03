"""Run with Blender Python. Explicit axis/scale calibration before runtime mesh authoring."""
import bpy
from pathlib import Path

root=Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
# Unity's FBX importer mirrors X. Explicit reflection yields the intended +X/+Y/+Z
# axes after Blender -Z-forward/Y-up export: Unity(x,y,z) -> Blender(-x,-z,y).
vertices=[(0,0,0),(-1,0,0),(0,0,1),(0,-1,0)]
mesh=bpy.data.meshes.new('AxisCalibration');mesh.from_pydata(vertices,[],[(0,2,1),(0,1,3),(0,3,2),(1,2,3)]);mesh.update()
obj=bpy.data.objects.new('AxisCalibration',mesh);bpy.context.collection.objects.link(obj)
target=root/'Assets/_Project/Art/RunExp05/Enemies';target.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(target/'AxisCalibration.fbx'),use_selection=False,object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,use_mesh_modifiers=True)
print('RUNEXP05 exported calibration:',target)
