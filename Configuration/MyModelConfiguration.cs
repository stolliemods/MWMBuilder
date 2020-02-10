using System.Xml.Serialization;

namespace MwmBuilder.Configuration
{
  [XmlRoot("Model")]
  public class MyModelConfiguration
  {
    [XmlAttribute("Name")]
    public string Name;
    [XmlElement("BoneGridSize")]
    public float? BoneGridSize;
    [XmlArrayItem("Bone")]
    [XmlArray("BoneMapping")]
    public MyModelVector[] BoneMapping;
    [XmlElement("Parameter")]
    public MyModelParameter[] Parameters;
    [XmlElement("Material")]
    public MyMaterialConfiguration[] Materials;
    [XmlElement("MaterialRef")]
    public MyModelParameter[] MaterialRefs;
    [XmlElement("LOD")]
    public MyLODConfiguration[] LODs;

    public bool ShouldSerializeBoneGridSize()
    {
      return this.BoneGridSize.HasValue;
    }

    public bool ShouldSerializeBoneMapping()
    {
      return this.BoneMapping != null;
    }
  }
}
