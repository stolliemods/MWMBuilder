using System.Xml.Serialization;

namespace MwmBuilder.Configuration
{
  [XmlRoot("MaterialsLib")]
  public class MyMaterialsLib
  {
    [XmlAttribute("Name")]
    public string Name;
    public string FilePath;
    [XmlElement("Material")]
    public MyMaterialConfiguration[] Materials;
  }
}
