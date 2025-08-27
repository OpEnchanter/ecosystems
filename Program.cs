using Raylib_cs;
using System.Numerics;

// Quick primatives
class primShapes() {
  public static Model sphere(float radius, int resolution) {
    return Raylib.LoadModelFromMesh(Raylib.GenMeshSphere(radius, resolution, resolution));
  } 
}

class basicRenderable() {
  public Model model;
  public Vector3 position = Vector3.Zero;
  public Vector3 eulerRotation = Vector3.Zero;

  public void Draw() {
    Rlgl.PushMatrix();
    Rlgl.Translatef(position.X, position.Y, position.Z);
    Raylib.DrawModel(model, Vector3.Zero, 1.0f, Color.White);
    Rlgl.PopMatrix();
  }
}

enum organismType {
  Bush,
  Rabbit,
  Fox
}

enum fluidType {
  Pond
}

class organism : basicRenderable {
  public struct traitsStruct {
    public float speed;
    public float eyesight;
    public bool canMove; // Use for plants
    public organismType[] afraidOf;
    public organismType[] foodSources;
    public fluidType[] hydrationSources;

    public traitsStruct() {
      speed = 1.0f;
      eyesight = 5.0f;
      canMove = true;
      afraidOf = new organismType[0];
      foodSources = new organismType[0];
      hydrationSources = new fluidType[0];
    }
  }

  public struct statsStruct {
    public float food;
    public float hydration;

    public statsStruct() {
      food = 10;
      hydration = 10;
    }
  }

  public Vector2 organismPosition = Vector2.Zero;
  public Vector2 target = Vector2.Zero;
  public bool moving = false;
  public traitsStruct traits = new traitsStruct();
  public statsStruct stats = new statsStruct();

  public void Update() {
    if (stats.food <= 3.0f && stats.hydration <= 3.0f) {
      if (moving == false) {
        Random random = new Random();
        float angle = random.Next(-314, 314) / 100;
        Vector2 localTarget = new Vector2(MathF.Cos(angle) * traits.eyesight, MathF.Sin(angle) * traits.eyesight);
        target = organismPosition + localTarget;
        moving = true;
      }
    }

    if (new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y).Length() < 0.5f) {
      moving = false;
    }

    if (moving == true) {
      Vector2 moveDirection = new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y);
      moveDirection = new Vector2(moveDirection.X / moveDirection.Length(), moveDirection.Y / moveDirection.Length());
      moveDirection *= traits.speed / 20.0f;
      organismPosition += moveDirection;
      position = new Vector3(organismPosition.X, 0, organismPosition.Y);
    }

    stats.food -= 0.05f;
  }
}

class Program() {
  static void Main() {
    Raylib.InitWindow(640, 480, "Ecosystem Simulation");
    Raylib.SetTargetFPS(60);
    
    // Initalize camera
    Camera3D camera = new Camera3D() {
      Position = Vector3.Zero,
      Target = new Vector3(0.0f, 0.0f, 1.0f),
      Up = new Vector3(0.0f, 1.0f, 0.0f),
      FovY = 90.0f,
      Projection = CameraProjection.Perspective
    };

    // Initialize shaders
    Shader lit = Raylib.LoadShader("./shader/lit.vs", "./shader/lit.fs");
    
    // Initialize renderables
    Model shadedSphere = primShapes.sphere(1, 100);
    unsafe { shadedSphere.Materials[0].Shader = lit; }

    basicRenderable[] renderables = new basicRenderable[] {
      new organism() {
        model = shadedSphere,
        position = new Vector3(0.0f, 0.0f, 5.0f)
      },
    };

    Raylib.DisableCursor();

    while (!Raylib.WindowShouldClose()) {
      // Clear frame and clear for drawing
      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.RayWhite);
      
      unsafe { Raylib.UpdateCamera(&camera, CameraMode.Free); } // Update camera

      // 3D
      Raylib.BeginMode3D(camera);
      
      // Update cycle
      foreach (basicRenderable renderable in renderables) {
        renderable.Draw();
        if (renderable is organism) {
          organism renderableOrganism = (organism) renderable;
          renderableOrganism.Update();
        }
      }

      Raylib.EndMode3D();

      Raylib.EndDrawing(); // End frame
    }

    Raylib.CloseWindow();
  }
}
