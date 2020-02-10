MwmBuilder is tool for building mwm models from fbx files

Note: You may need administrator privledges to copy or create files in Program Files folder, you can copy MwmBuilder and dlls to different folder.

SIMPLE USAGE:
1) Use ModelBuilder tool which has GUI and you can set up all parameters for mwmbuilder easily.

ADVANCED USAGE:
1) Run MwmBuilder.exe /?
2) Use any arguments you would like
Output path argument expects "Content" somewhere in the path, it's best used to build models directly into game's content directory

BASE XML FILE:
<?xml version="1.0"?>
<Model Name="Default">
  <BoneGridSize d2p1:nil="true" xmlns:d2p1="http://www.w3.org/2001/XMLSchema-instance" />
  <Parameter Name="Centered">false</Parameter>
  <Parameter Name="RescaleFactor">0.01</Parameter>
  <Parameter Name="RescaleToLengthInMeters">false</Parameter>
  <Parameter Name="SpecularPower">10</Parameter>
  <Parameter Name="SpecularShininess">0.8</Parameter>
  <Material Name="01 - Default">
    <Parameter Name="SpecularIntensity">0</Parameter>
    <Parameter Name="SpecularPower">2</Parameter>
    <Parameter Name="DiffuseColorX">255</Parameter>
    <Parameter Name="DiffuseColorY">255</Parameter>
    <Parameter Name="DiffuseColorZ">255</Parameter>
    <Parameter Name="Texture">Textures\Models\Cubes\large_assembler_de.dds</Parameter>
    <Parameter Name="NormalTexture" />
  </Material>
</Model>

XML PARAMETERS:
Material name - must match material name in fbx file, if you're not sure, add /e switch, it will create dummy XML for you with materials from fbx
Texture - path is relative, texture must be placed into "Content\Textures" subdirectory
NormalTexture - when empty, it will try to find texture with similar name, in above case "Textures\Models\Cubes\large_assembler_ns.dds"
SpecularPower and SpecularShininess - influences parameters for specular highlight
BoneGridSize and BoneMapping - parameters used for destruction, available only for models created from plates (armor, interior wall...), see armor xml files for details
RescaleFactor, Centered, RescaleToLenghtInMeteres - usually not changed

TEXTURES:
de texture - RGB channel contains diffuse color, A channel contains emissivity
ns texture - RGB channel contains normal map, A channel contains specular map

Please take a look at existing textures to see how it can look for various surfaces
Please use DDS textures compressed as DXT5 to reduce file size, there's plugins for both Photoshop and Gimp to import/export DDS, WTV is very good dds viewer

COLLISION MODELS:
It's now impossible to change collision models, collision model is now based on model path and created in code. For unknown models, it just bounding box.
There will be tools and guide for collision models in few weeks probably

LINKS:
Photoshop DDS plugin: https://developer.nvidia.com/nvidia-texture-tools-adobe-photoshop
Gimp DDS plugin: http://registry.gimp.org/node/70
WTV Viewer: http://www.nvidia.com/object/windows_texture_viewer.html
Model pack: TO BE DONE