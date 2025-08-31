using RayGUI_cs;
using Raylib_cs;
using System.Numerics;
using RayGUI_cs;
using System.Reflection.Metadata;

// Quick primatives
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
  None,
  Water
}

class organism : basicRenderable
{
  public struct traitsStruct
  {
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

  public struct statsStruct
  {
    public float food;
    public float hydration;
    public float health;
    public bool locateMate;

    public statsStruct()
    {
      food = 10.0f;
      hydration = 10.0f;
      health = 10.0f;
      locateMate = false;
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
      float angle = (float)(Program.random.NextDouble() * MathF.PI * 2); // 0 to 2π
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
                if (distance <= traits.eyesight && distance < nearestFoodSourceDist)
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
          // Find fluid
          float nearestFluidSourceDist = float.PositiveInfinity;
          fluidSource nearestFluidSource = null;
          foreach (basicRenderable renderable in Program.renderables)
          {
            if (renderable is fluidSource)
            {
              fluidSource fluid = (fluidSource)renderable;
              if (traits.hydrationSources.Contains(fluid.sourceType))
              {
                float distance = new Vector2(fluid.position.X - position.X, fluid.position.Y - position.Y).Length();
                if (distance <= traits.eyesight && distance < nearestFluidSourceDist)
                {
                  nearestFluidSourceDist = distance;
                  nearestFluidSource = fluid;
                }
              }
            }
          }

          if (nearestFluidSourceDist < 0.5 && nearestFluidSource != null)
          {
            stats.hydration = 10.0f;
          }
        }
      }

      // Decrement stats
      stats.food -= 0.005f;
      stats.hydration -= 0.0025f;

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
        Program.renderablesToRemove.Add(this);
      }
    }

    if (new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y).Length() < 0.5f)
    {
      moving = false;
    }

    if (moving == true)
    {
      Vector2 moveDirection = new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y);
      //moveDirection = new Vector2(moveDirection.X / moveDirection.Length(), moveDirection.Y / moveDirection.Length());
      moveDirection *= traits.speed / 20.0f;
      organismPosition += moveDirection;
    }

    position = new Vector3(organismPosition.X, 0, organismPosition.Y);
  }

  public organism Clone()
  {
    return (organism)this.MemberwiseClone();
  }
}

class fluidSource : basicRenderable
{
  public fluidType sourceType = fluidType.None;  
  public fluidSource Clone()
  {
    return (fluidSource)this.MemberwiseClone();
  }
}

class Program()
{

  public static List<basicRenderable> renderables = new List<basicRenderable>();
  public static List<basicRenderable> renderablesToRemove = new List<basicRenderable>();

  public static Random random = new Random();

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

    // Rabbit organism
    Image brownImage = Raylib.GenImageColor(10, 10, Color.Brown);
    Texture2D brownTexture = Raylib.LoadTextureFromImage(brownImage);

    Model rabbitModel = primShapes.sphere(0.5f, 12);
    unsafe
    {
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
    rabbit.traits.hydrationSources = new fluidType[] { fluidType.Water };
    rabbit.traits.speed = 0.5f;


    // Fox organism
    Image orangeImage = Raylib.GenImageColor(10, 10, Color.Orange);
    Texture2D orangeTexture = Raylib.LoadTextureFromImage(orangeImage);

    Model foxModel = primShapes.sphere(0.5f, 12);
    unsafe
    {
      foxModel.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = orangeTexture;
      foxModel.Materials[0].Shader = lit;
    }

    organism fox = new organism()
    {
      model = foxModel,
      position = new Vector3(0.0f, 0.0f, 5.0f)
    };

    fox.traits.oType = organismType.Fox;
    fox.traits.foodSources = new organismType[] { organismType.Rabbit };
    fox.traits.hydrationSources = new fluidType[] { fluidType.Water };
    fox.traits.speed = 1.0f;


    // Bush organism
    Image greenImage = Raylib.GenImageColor(10, 10, Color.Green);
    Texture2D greenTexture = Raylib.LoadTextureFromImage(greenImage);

    Model bushModel = primShapes.sphere(1, 12);
    unsafe
    {
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

    // Pond
    Image blueImage = Raylib.GenImageColor(10, 10, Color.Blue);
    Texture2D blueTexture = Raylib.LoadTextureFromImage(blueImage);

    Model pondModel = primShapes.plane(2, 2, 1, 1);
    unsafe
    {
      pondModel.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = blueTexture;
      pondModel.Materials[0].Shader = lit;
    }

    fluidSource pond = new fluidSource()
    {
      model = pondModel,
      sourceType = fluidType.Water
    };

    // Add rabbits
    for (int i = 0; i < 60; i++)
    {
      organism rabbitClone = rabbit.Clone();
      rabbitClone.organismPosition = new Vector2(random.Next(-50, 50), random.Next(-50, 50));
      renderables.Add(rabbitClone);
    }

    // Add foxes
    for (int i = 0; i < 40; i++)
    {
      organism foxClone = fox.Clone();
      foxClone.organismPosition = new Vector2(random.Next(-50, 50), random.Next(-50, 50));
      renderables.Add(foxClone);
    }

    // Add bushes
    for (int i = 0; i < 80; i++)
    {
      organism bushClone = bush.Clone();
      bushClone.organismPosition = new Vector2(random.Next(-50, 50), random.Next(-50, 50));
      renderables.Add(bushClone);
    }

    // Add ponds
    for (int i = 0; i < 80; i++)
    {
      fluidSource pondClone = pond.Clone();
      pondClone.position = new Vector3(random.Next(-50, 50), 0, random.Next(-50, 50));
      renderables.Add(pondClone);
    }

    Raylib.DisableCursor();

    bool menuOpen = false;

    Font defaultFont = Raylib.GetFontDefault();
    
    Dictionary<int, Font> fonts = new Dictionary<int, Font>
    {
      { 16, defaultFont }
    };

    RayGUI.LoadGUI(fonts);
    
    Button btn = new Button(10, 10, 48, 18, "Test");
    btn.Type = ButtonType.Custom;
    btn.BaseColor = Color.LightGray;
    btn.HoverColor = Color.DarkGray;
    btn.Event = () => { Console.WriteLine("Clicked!"); };

    Image backgroundImage = Raylib.GenImageColor(128, 480, Color.Black);
    Texture2D background = Raylib.LoadTextureFromImage(backgroundImage);
    Panel panel = new Panel(0, 0, background);

    Textbox textbox = new Textbox(10, 30, 96, 18, "Type...");
    textbox.BaseColor = Color.LightGray;
    textbox.HoverColor = Color.Black;
    textbox.TextColor = Color.Black;

    GuiContainer container = new GuiContainer();
    container.Add("bg", panel);
    container.Add("button", btn);
    container.Add("text", textbox);
        

    while (!Raylib.WindowShouldClose())
    {
      // Clear frame and clear for drawing
      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.RayWhite);

      if (!menuOpen)
      {
        unsafe { Raylib.UpdateCamera(&camera, CameraMode.Free); } // Update camera 
      }

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

      if (renderablesToRemove.Count > 0)
      {
        foreach (basicRenderable renderable in renderablesToRemove)
        {
          renderables.Remove(renderable);
        }
        renderablesToRemove = new List<basicRenderable>();
      }
      Raylib.EndMode3D();

      // Client
      if (Raylib.IsKeyPressed(KeyboardKey.Tab))
      {
        menuOpen = !menuOpen;

        if (menuOpen)
        {
          Raylib.EnableCursor();
        }
        else
        {
          Raylib.DisableCursor();
        }
      }

      if (menuOpen)
      {
        container.Draw();
      }

      Raylib.EndDrawing(); // End frame
    }

    Raylib.CloseWindow();
  }
}