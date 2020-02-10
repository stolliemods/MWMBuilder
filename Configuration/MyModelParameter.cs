using System.Xml.Serialization;

namespace MwmBuilder.Configuration
{
  public class MyModelParameter
  {
    [XmlAttribute("Name")]
    public string Name;
    [XmlText]
    public string Value;

    public override int GetHashCode()
    {
      return this.Name.GetHashCode() * (!string.IsNullOrEmpty(this.Value) ? this.Value.GetHashCode() : 1);
    }
  }
}
