using ZeroElectric.Vinculum;
using System.Numerics;
using System.Text.Json;
using ZeroElectric.Vinculum.Extensions;
class primShapes()
{
    public static Model sphere(float radius, int resolution)
    {
        return Raylib.LoadModelFromMesh(Raylib.GenMeshSphere(radius, resolution, resolution));
    }

    public static Model plane(float X, float Y, int resX, int resY)
    {
        return Raylib.LoadModelFromMesh(Raylib.GenMeshPlane(X, Y, resX, resY));
    }
}

class basicRenderable()
{
  public Model model;
  public Vector3 position = Vector3.Zero;
  public Vector3 eulerRotation = Vector3.Zero;

  public void Draw()
  {
    RlGl.rlPushMatrix();
    RlGl.rlTranslatef(position.X, position.Y, position.Z);
    RlGl.rlRotatef(eulerRotation.X, 1.0f, 0.0f, 0.0f);
    RlGl.rlRotatef(eulerRotation.Y, 0.0f, 1.0f, 0.0f);
    RlGl.rlRotatef(eulerRotation.Z, 0.0f, 0.0f, 1.0f);
    Raylib.DrawModel(model, Vector3.Zero, 1.0f, Raylib.RED);
    RlGl.rlPopMatrix();
  }

  public basicRenderable Clone()
  {
    return (basicRenderable)this.MemberwiseClone();
  }
}