import math
from pathlib import Path

import bpy


OUTPUT = Path(__file__).resolve().parents[1] / "assets" / "registry-logo-2x2-top-124-render.png"


def make_material(name, color, roughness=0.34, metallic=0.0, alpha=1.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Alpha"].default_value = alpha
    material.blend_method = "BLEND"
    return material


def add_cube(name, x, y, z, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z))
    cube = bpy.context.object
    cube.name = name
    cube.dimensions = (1.45, 1.45, 1.45)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = cube.modifiers.new("even-rounded-edges", "BEVEL")
    bevel.width = 0.12
    bevel.segments = 10
    bevel.affect = "EDGES"
    cube.modifiers.new("weighted-brand-normals", "WEIGHTED_NORMAL")
    cube.data.materials.append(material)
    return cube


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()

    blue = make_material("registry-blue-glass", (0.02, 0.35, 1.0, 0.82), alpha=0.82)
    cyan = make_material("registry-cyan-glass", (0.08, 0.86, 1.0, 0.78), alpha=0.78)
    edge = make_material("edge-highlight", (0.8, 1.0, 1.0, 1.0), roughness=0.18)

    # Base layout:
    #   1 2
    #   3 4
    # Upper cubes exist on positions 1, 2, and 4. Position 3 is intentionally open.
    positions = [
        (-0.8, 0.8, 0.0, blue),
        (0.8, 0.8, 0.0, cyan),
        (-0.8, -0.8, 0.0, cyan),
        (0.8, -0.8, 0.0, blue),
        (-0.8, 0.8, 1.48, cyan),
        (0.8, 0.8, 1.48, blue),
        (0.8, -0.8, 1.48, cyan),
    ]

    for index, (x, y, z, material) in enumerate(positions, start=1):
        cube = add_cube(f"registry-cube-{index}", x, y, z, material)
        outline = add_cube(f"registry-cube-{index}-edge", x, y, z + 0.006, edge)
        outline.scale = (1.02, 1.02, 1.02)
        outline.display_type = "WIRE"
        outline.hide_render = True

    bpy.ops.object.light_add(type="AREA", location=(0, -4.5, 6.2))
    key = bpy.context.object
    key.name = "large-softbox"
    key.data.energy = 480
    key.data.size = 5.5

    bpy.ops.object.camera_add(location=(4.8, -6.8, 4.6), rotation=(math.radians(60), 0, math.radians(38)))
    bpy.context.scene.camera = bpy.context.object

    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 128
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.resolution_x = 1024
    bpy.context.scene.render.resolution_y = 1024
    bpy.context.scene.render.film_transparent = True
    bpy.context.scene.render.filepath = str(OUTPUT)

    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT.with_suffix(".blend")))
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
