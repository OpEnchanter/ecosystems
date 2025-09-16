using ZeroElectric.Vinculum;
using System.Numerics;
using System.Text.Json;
using ZeroElectric.Vinculum.Extensions;
enum fluidType
{
    None,
    Water
}

class fluidSource : basicRenderable
{
  public fluidType sourceType = fluidType.None;  
  public fluidSource Clone()
  {
    return (fluidSource)this.MemberwiseClone();
  }
}