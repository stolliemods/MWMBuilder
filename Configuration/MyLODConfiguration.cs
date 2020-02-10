using System.Xml.Serialization;

namespace MwmBuilder.Configuration
{
  [XmlRoot("LOD")]
  public class MyLODConfiguration
  {
    [XmlAttribute("Distance")]
    public float Distance;
    [XmlAttribute("RenderQuality")]
    public string RenderQuality;
    [XmlElement("Model")]
    public string Model;

    public override int GetHashCode()
    {
      int hashCode = this.Distance.GetHashCode();
      int num = hashCode | hashCode * 397 ^ (this.RenderQuality == null ? "".GetHashCode() : this.RenderQuality.GetHashCode());
      return num | num * 397 ^ (this.Model == null ? "".GetHashCode() : this.Model.GetHashCode());
    }
  }
}
