using System;
using System.Xml.Serialization;
using VRageMath;

namespace MwmBuilder.Configuration
{
  public struct MyModelVector
  {
    [XmlAttribute("X")]
    public int X;
    [XmlAttribute("Y")]
    public int Y;
    [XmlAttribute("Z")]
    public int Z;

    public static implicit operator Vector3(MyModelVector vec)
    {
      return new Vector3((float) vec.X, (float) vec.Y, (float) vec.Z);
    }

    public static implicit operator MyModelVector(Vector3 vec)
    {
      return new MyModelVector()
      {
        X = (int) Math.Round((double) vec.X),
        Y = (int) Math.Round((double) vec.Y),
        Z = (int) Math.Round((double) vec.Z)
      };
    }
  }
}
