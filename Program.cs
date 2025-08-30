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
  None,
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

    public organismType oType;

    public traitsStruct()
    {
      speed = 1.0f;
      eyesight = 5.0f;
      canMove = true;
      afraidOf = new organismType[0];
      foodSources = new organismType[0];
      hydrationSources = new fluidType[0];
      oType = organismType.None;
    }
  }

  public struct statsStruct {
    public float food;
    public float hydration;
    public float health;

    public statsStruct()
    {
      food = 10.0f;
      hydration = 10.0f;
      health = 10.0f;
    }
  }

  public Vector2 organismPosition = Vector2.Zero;
  public Vector2 target = Vector2.Zero;
  public bool moving = false;
  public traitsStruct traits = new traitsStruct();
  public statsStruct stats = new statsStruct();

  private void wander()
  {
    if (moving == false)
    {
      Random random = new Random();
      float angle = random.Next(-314, 314) / 100;
      Vector2 localTarget = new Vector2(MathF.Cos(angle) * traits.eyesight, MathF.Sin(angle) * traits.eyesight);
      target = organismPosition + localTarget;
      moving = true;
    }
  }

  public void Update()
  {
    if (traits.canMove == true)
    {
      if (stats.food >= 3.0f && stats.hydration >= 3.0f)
      {
        wander();
      }
      else
      {
        if (stats.food < 3.0f)
        {
          float nearestFoodSourceDist = float.PositiveInfinity;
          organism nearestFoodSource = null;
          foreach (basicRenderable renderable in Program.renderables)
          {
            if (renderable is organism)
            {
              organism renderableOrganism = (organism)renderable;
              if (Array.Exists(traits.foodSources, element => element == renderableOrganism.traits.oType))
              {
                float distance = new Vector2(renderableOrganism.position.X - organismPosition.X, renderableOrganism.position.Z - organismPosition.Y).Length();
                if (distance < traits.eyesight && distance < nearestFoodSourceDist)
                {
                  nearestFoodSourceDist = distance;
                  nearestFoodSource = renderableOrganism;
                }
              }
            }
          }

          if (nearestFoodSourceDist < 0.5 && nearestFoodSource != null)
          {
            // Eat food
            nearestFoodSource.stats.health -= 1.0f;
            stats.food = 10.0f;
          }

          if (nearestFoodSource != null)
          {
            // Go to food source
            target = new Vector2(nearestFoodSource.position.X, nearestFoodSource.position.Z);
            moving = true;
          }
          else
          {
            wander();
          }
        }
        if (stats.hydration < 3.0f)
        {
          // Find water sources
        }
      }

      stats.food -= 0.05f;

      if (stats.food <= 0.0f)
      {
        stats.food = 0.0f;
        stats.health -= 0.1f;
      }

      if (stats.hydration <= 0.0f)
      {
        stats.hydration = 0.0f;
        stats.health -= 0.1f;
      }

      if (stats.health <= 0)
      {
        Program.renderables.Remove(this);
      }
    }

    if (new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y).Length() < 0.5f)
    {
      moving = false;
    }

    if (moving == true)
    {
      Vector2 moveDirection = new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y);
      moveDirection = new Vector2(moveDirection.X / moveDirection.Length(), moveDirection.Y / moveDirection.Length());
      moveDirection *= traits.speed / 20.0f;
      organismPosition += moveDirection;
      position = new Vector3(organismPosition.X, 0, organismPosition.Y);
    }
  }
}

class Program() {

  public static List<basicRenderable> renderables = new List<basicRenderable>();
  static void Main()
  {
    Raylib.InitWindow(640, 480, "Ecosystem Simulation");
    Raylib.SetTargetFPS(60);

    // Initalize camera
    Camera3D camera = new Camera3D()
    {
      Position = Vector3.Zero,
      Target = new Vector3(0.0f, 0.0f, 1.0f),
      Up = new Vector3(0.0f, 1.0f, 0.0f),
      FovY = 90.0f,
      Projection = CameraProjection.Perspective
    };

    // Initialize shaders
    Shader lit = Raylib.LoadShader("./shader/lit.vs", "./shader/lit.fs");

    // Initialize renderables
    Image brownImage = Raylib.GenImageColor(10, 10, Color.Red);
    Texture2D brownTexture = Raylib.LoadTextureFromImage(brownImage);

    Model rabbitModel = primShapes.sphere(1, 12);
    unsafe {
      rabbitModel.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = brownTexture;
      rabbitModel.Materials[0].Shader = lit;
    }

    organism rabbit = new organism()
    {
      model = rabbitModel,
      position = new Vector3(0.0f, 0.0f, 5.0f)
    };

    rabbit.traits.oType = organismType.Rabbit;
    rabbit.traits.foodSources = new organismType[] { organismType.Bush };
    rabbit.traits.hydrationSources = new fluidType[] { fluidType.Pond };

    Image greenImage = Raylib.GenImageColor(10, 10, Color.Green);
    Texture2D greenTexture = Raylib.LoadTextureFromImage(greenImage);

    Model bushModel = primShapes.sphere(1, 12);
    unsafe {
      bushModel.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = greenTexture;
      bushModel.Materials[0].Shader = lit;
    }

    organism bush = new organism()
    {
      model = bushModel,
      position = new Vector3(0.0f, 0.0f, 0.0f),
    };

    bush.traits.oType = organismType.Bush;
    bush.traits.canMove = false;

    renderables = new List<basicRenderable> {
      rabbit,
      bush
    };

    Raylib.DisableCursor();

    while (!Raylib.WindowShouldClose())
    {
      // Clear frame and clear for drawing
      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.RayWhite);

      unsafe { Raylib.UpdateCamera(&camera, CameraMode.Free); } // Update camera

      // 3D
      Raylib.BeginMode3D(camera);

      Raylib.DrawGrid(100, 1);

      // Update cycle
      foreach (basicRenderable renderable in renderables)
      {
        renderable.Draw();
        if (renderable is organism)
        {
          organism renderableOrganism = (organism)renderable;
          renderableOrganism.Update();
        }
      }

      Raylib.EndMode3D();

      Raylib.EndDrawing(); // End frame
    }

    Raylib.CloseWindow();
  }
}