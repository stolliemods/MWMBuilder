using System.Xml.Serialization;

namespace MwmBuilder.Configuration
{
  [XmlRoot("Material")]
  public class MyMaterialConfiguration
  {
    [XmlAttribute("Name")]
    public string Name;
    [XmlElement("Parameter")]
    public MyModelParameter[] Parameters;

    public override int GetHashCode()
    {
      int hashCode = this.Name.GetHashCode();
      foreach (MyModelParameter parameter in this.Parameters)
        hashCode += parameter.GetHashCode();
      return hashCode;
    }
  }
}
