using System;
using System.Globalization;

namespace MwmBuilder
{
  public class MyParser
  {
    public static T Parse<T>(object value)
    {
      return typeof (T).IsEnum ? (T) Enum.Parse(typeof (T), (string) value) : (T) Convert.ChangeType(value, typeof (T), (IFormatProvider) CultureInfo.InvariantCulture);
    }
  }
}
