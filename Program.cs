using ZeroElectric.Vinculum;
using System.Numerics;
using System.Text.Json;
using ZeroElectric.Vinculum.Extensions;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;


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

class basicRenderable()
{
  public Model model;
  public Vector3 position = Vector3.Zero;
  public Vector3 eulerRotation = Vector3.Zero;

  public void Draw()
  {
    RlGl.rlPushMatrix();
    RlGl.rlTranslatef(position.X, position.Y, position.Z);
    Raylib.DrawModel(model, Vector3.Zero, 1.0f, Raylib.RED);
    RlGl.rlPopMatrix();
  }
}

enum organismType {
  None,
  Bush,
  Rabbit,
  Fox
}

enum fluidType
{
  None,
  Water
}

enum sex
{
  Male,
  Female
}

unsafe class organism : basicRenderable
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
    public sex sex;

    public traitsStruct()
    {
      speed = 1.0f;
      eyesight = 5.0f;
      canMove = true;
      afraidOf = new organismType[0];
      foodSources = new organismType[0];
      hydrationSources = new fluidType[0];
      oType = organismType.None;
      sex = (Program.random.Next(2) == 0) ? sex.Male : sex.Female;
    }
  }

  public struct statsStruct
  {
    public float food;
    public float hydration;
    public float health;
    public float maxHealth;
    public bool locateMate;
    public int matingTimer;

    public statsStruct()
    {
      food = 10.0f;
      hydration = 10.0f;
      health = 10.0f;
      locateMate = false;
      matingTimer = 300;
      maxHealth = health;
    }
  }

  public Vector2 organismPosition = Vector2.Zero;
  public Vector2 target = Vector2.Zero;
  public bool moving = false;
  public traitsStruct traits = new traitsStruct();
  public statsStruct stats = new statsStruct();
  private Texture debugTex;

  private Texture generateDebugTex()
  {
    Image* img;
    img = (Image*)Raylib.MemAlloc((uint)sizeof(Image));
    *img = Raylib.GenImageColor(256, 512, Raylib.BLANK);
    Raylib.ImageDrawText(img, $"sex: {traits.sex}", 10, 10, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"speed: {traits.speed}", 10, 48*1, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"eyesight: {traits.eyesight}", 10, 48*2, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"health: {stats.health}", 10, 48*3, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"nutrition: {stats.food}", 10, 48*4, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"hydration: {stats.hydration}", 10, 48*5, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"mating_timer: {stats.matingTimer}", 10, 48*6, 24, Raylib.BLACK);
    Raylib.ImageDrawText(img, $"find_mate: {stats.locateMate}", 10, 48*7, 24, Raylib.BLACK);
    Texture tex = Raylib.LoadTextureFromImage(*img);
    Raylib.UnloadImage(*img);
    Raylib.MemFree(img);
    return tex;
  }

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

  private float animationBounceSpeed = 0.1f;
  private float animationBounceHeight = 0.25f;
  private float animationTimeOffset = 0.0f;

  public organism()
  {
    traits.speed = Program.random.Next(50, 200) / 100.0f;
    traits.eyesight = Program.random.Next(10, 500) / 100.0f;
    animationTimeOffset = Program.random.Next(0, 300) / 100.0f;
    debugTex = generateDebugTex();
  }

  public void Update()
  {
    stats.matingTimer -= Program.random.Next(0, 2);
    if (traits.canMove == true)
    {
      if (stats.food >= 3.0f && stats.hydration >= 3.0f)
      {
        if (stats.matingTimer <= 0)
        {
          stats.locateMate = true;
        }
        else
        {
          stats.locateMate = false;
        }
        if (stats.locateMate && traits.sex == sex.Female)
        {
          float nearestMateDist = float.PositiveInfinity;
          organism nearestMate = null;
          foreach (basicRenderable renderable in Program.renderables)
          {
            if (renderable is organism)
            {
              organism renderableOrganism = (organism)renderable;
              if (
                renderableOrganism.traits.sex == sex.Male
                && renderableOrganism.traits.oType == traits.oType
                && renderableOrganism.stats.locateMate
                )
              {
                float dist = new Vector2(
                  renderableOrganism.organismPosition.X - organismPosition.X,
                  renderableOrganism.organismPosition.Y - organismPosition.Y).Length();
                if (dist < nearestMateDist && dist <= traits.eyesight * 3)
                {
                  nearestMate = renderableOrganism;
                  nearestMateDist = dist;
                }
              }
            }
          }
          if (nearestMate != null)
          {
            target = nearestMate.organismPosition;
            nearestMate.target = organismPosition;
            if (traits.sex == sex.Female && nearestMateDist <= 1.0f)
            {
              stats.matingTimer = 300;
              nearestMate.stats.matingTimer = 300;
              for (int i = 0; i < Program.random.Next(1, 2); i++)
              {
                organism child = this.Clone();
                child.traits.eyesight = (traits.eyesight + nearestMate.traits.eyesight) / 2;
                child.traits.speed = (traits.speed + nearestMate.traits.speed) / 2;
                Program.renderablesToAdd.Add(child);
              }
            }
          }
          else
          {
            wander();
          }
        }
        else
        {
          wander();
        }

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
            nearestFoodSource.stats.health -= 10.0f;
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
      stats.food -= 0.0025f;
      stats.hydration -= 0.0015f;

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
    else if (stats.health < stats.maxHealth)
    {
      stats.health += 0.05f;
    }

    if (stats.health > stats.maxHealth)
    {
      stats.health = stats.maxHealth;
    }

    if (new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y).Length() < 0.5f)
    {
      moving = false;
    }

    float anim = (MathF.Sin(Program.time * animationBounceSpeed + animationTimeOffset) + 1) * animationBounceHeight;


    if (moving == true)
    {
      Vector2 moveDirection = new Vector2(target.X - organismPosition.X, target.Y - organismPosition.Y);
      moveDirection = new Vector2(moveDirection.X / moveDirection.Length(), moveDirection.Y / moveDirection.Length()); // Normalize
      moveDirection *= traits.speed / 20.0f;
      organismPosition += moveDirection;
    }

    if (!traits.canMove)
    {
      anim = 0.0f;
    }

    position = new Vector3(organismPosition.X, anim, organismPosition.Y);

    if (((JsonElement)Program.config["debug"]).GetBoolean())
    {
      float cameraDistance = (position - Program.camera.position).Length();
      if (cameraDistance < 20)
      {
        if (cameraDistance < 5)
        {
          debugTex = generateDebugTex();
        }
        Raylib.DrawBillboard(Program.camera, debugTex, position + new Vector3(0.0f, 2.5f - anim, 0.0f), 2.5f, Raylib.RED);
        //Raylib.DrawCircle3D(position, traits.eyesight, new Vector3(1.0f,0,0), 90.0f, Raylib.RED);
      }
    }
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

unsafe class Program()
{

  public static List<basicRenderable> renderables = new List<basicRenderable>();
  public static List<basicRenderable> renderablesToRemove = new List<basicRenderable>();
  public static List<basicRenderable> renderablesToAdd = new List<basicRenderable>();

  public static Random random = new Random();
  public static Dictionary<string, object> config;
  public static Camera3D camera;

  static int countOrganisms(organismType type)
  {
    int num = 0;
    foreach (basicRenderable renderable in renderables)
    {
      if (renderable is organism)
      {
        organism renderableOrganism = (organism)renderable;
        if (renderableOrganism.traits.oType == type)
        {
          num++;
        }
      }
    }
    return num;
  }

  public static float time = 0.0f;

  static void Main()
  {
    Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
    Raylib.InitWindow(640, 480, "Ecosystem Simulation");
    Raylib.SetTargetFPS(60);
    Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_WARNING);

    // Initalize camera
    camera = new Camera3D()
    {
      position = Vector3.Zero,
      target = new Vector3(0.0f, 0.0f, 1.0f),
      up = new Vector3(0.0f, 1.0f, 0.0f),
      fovy = 90.0f,
      projection = (int)CameraProjection.CAMERA_PERSPECTIVE
    };

    // Initialize shaders
    Shader lit = Raylib.LoadShader("./shader/lit.vs", "./shader/lit.fs");

    // Initialize renderables

    // Rabbit organism
    Image brownImage = Raylib.GenImageColor(10, 10, Raylib.BROWN);
    Texture browntexture = Raylib.LoadTextureFromImage(brownImage);

    Model rabbitModel = primShapes.sphere(0.5f, 12);
    unsafe
    {
      rabbitModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = browntexture;
      rabbitModel.materials[0].shader = lit;
    }


    // Fox organism
    Image orangeImage = Raylib.GenImageColor(10, 10, Raylib.ORANGE);
    Texture orangetexture = Raylib.LoadTextureFromImage(orangeImage);

    Model foxModel = primShapes.sphere(0.5f, 12);
    unsafe
    {
      foxModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = orangetexture;
      foxModel.materials[0].shader = lit;
    }

    // Bush organism
    Image greenImage = Raylib.GenImageColor(10, 10, Raylib.GREEN);
    Texture greentexture = Raylib.LoadTextureFromImage(greenImage);

    Model bushModel = primShapes.sphere(1, 12);
    unsafe
    {
      bushModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = greentexture;
      bushModel.materials[0].shader = lit;
    }

    organism bush = new organism()
    {
      model = bushModel,
      position = new Vector3(0.0f, 0.0f, 0.0f),
    };

    bush.stats.health = 30.0f;
    bush.stats.maxHealth = 30.0f;
    bush.traits.oType = organismType.Bush;
    bush.traits.canMove = false;

    // Pond
    Image blueImage = Raylib.GenImageColor(10, 10, Raylib.BLUE);
    Texture bluetexture = Raylib.LoadTextureFromImage(blueImage);

    Model pondModel = primShapes.plane(2, 2, 1, 1);
    unsafe
    {
      pondModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = bluetexture;
      pondModel.materials[0].shader = lit;
    }

    fluidSource pond = new fluidSource()
    {
      model = pondModel,
      sourceType = fluidType.Water
    };

    // Add rabbits
    for (int i = 0; i < 60; i++)
    {
      organism rabbit = new organism()
      {
        model = rabbitModel,
        position = new Vector3(0.0f, 0.0f, 5.0f)
      };

      rabbit.traits.oType = organismType.Rabbit;
      rabbit.traits.foodSources = new organismType[] { organismType.Bush };
      rabbit.traits.hydrationSources = new fluidType[] { fluidType.Water };
      rabbit.organismPosition = new Vector2(random.Next(-50, 50), random.Next(-50, 50));
      rabbit.traits.eyesight = (float)random.Next(3, 15);
      rabbit.traits.speed = (float)random.Next(1, 5) / 10;
      renderables.Add(rabbit);
    }

    // Add foxes
    for (int i = 0; i < 40; i++)
    {
      organism fox = new organism()
      {
        model = foxModel,
        position = new Vector3(0.0f, 0.0f, 5.0f)
      };

      fox.traits.oType = organismType.Fox;
      fox.traits.foodSources = new organismType[] { organismType.Rabbit };
      fox.traits.hydrationSources = new fluidType[] { fluidType.Water };
      fox.organismPosition = new Vector2(random.Next(-50, 50), random.Next(-50, 50));
      fox.traits.eyesight = (float)random.Next(3, 15);
      fox.traits.speed = (float)random.Next(3, 10) / 10;
      renderables.Add(fox);
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
      pondClone.position = new Vector3(random.Next(-50, 50), -0.5f, random.Next(-50, 50));
      renderables.Add(pondClone);
    }

    // Ground
    Image* groundHeightmap = (Image*)Raylib.MemAlloc((uint)sizeof(Image));
    *groundHeightmap = Raylib.GenImageColor(1024, 1024, Raylib.BLACK);
    Raylib.ImageDrawCircle(groundHeightmap, 512, 512, 512, Raylib.WHITE);
    Texture groundTexture = Raylib.LoadTextureFromImage(Raylib.GenImageColor(10, 10, Raylib.DARKGREEN));
    Model groundModel = Raylib.LoadModelFromMesh(Raylib.GenMeshHeightmap(*groundHeightmap, new Vector3(100.0f, 6.0f, 100.0f)));
    groundModel.materials[0].shader = lit;
    groundModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = groundTexture;

    Raylib.UnloadImage(*groundHeightmap);
    Raylib.MemFree(groundHeightmap);

    basicRenderable ground = new basicRenderable();
    ground.model = groundModel;
    ground.position = new Vector3(-50.0f, -6.5f, -50.0f);
    renderables.Add(ground);

    Texture waterTexture = Raylib.LoadTextureFromImage(Raylib.GenImageColor(1, 1, Raylib.BLUE));
    Model waterModel = primShapes.plane(400, 400, 1, 1);
    waterModel.materials[0].shader = lit;
    waterModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = waterTexture;

    basicRenderable water = new basicRenderable();
    water.model = waterModel;
    water.position = new Vector3(0.0f, -1.0f, 0.0f);
    renderables.Add(water);

    

    bool menuOpen = false;

    Raylib.DisableCursor();
    Raylib.SetExitKey(0);


    bool running = true;

    // Load config
    config = null;

    if (Directory.Exists("./data"))
    {
      if (File.Exists("./data/conf.json"))
      {
        string configText = File.ReadAllText("./data/conf.json");
        config = JsonSerializer.Deserialize<Dictionary<string, object>>(configText);
      }
    }
    else
    {
      Console.WriteLine("Data folder does not exist, generating...");

      // Create the data dir
      Directory.CreateDirectory("data");
      Directory.CreateDirectory("./data/out");

      // Create the config file
      Dictionary<string, object> baseConfig = new Dictionary<string, object>
      {
        { "statistics", false },
        { "debug", false },
        { "show_fps", false }
      };
      string jsonText = JsonSerializer.Serialize(baseConfig);
      File.WriteAllText("./data/conf.json", jsonText);
    }

    if (config == null)
    {
      string configText = File.ReadAllText("./data/conf.json");
      config = JsonSerializer.Deserialize<Dictionary<string, object>>(configText);
    }

    int statLogTimer = 0;
    List<Dictionary<organismType, int>> organismCounts = new List<Dictionary<organismType, int>>();

    bool loggingEnabled = ((JsonElement)config["statistics"]).GetBoolean();

    while (!Raylib.WindowShouldClose() && running)
    {
      time++; // Increment time
      // Statistics logging
      if (loggingEnabled)
      {
        statLogTimer++;
        if (statLogTimer >= (60 * 5))
        {
          statLogTimer = 0;
          organismCounts.Add(new Dictionary<organismType, int> {
            {organismType.Rabbit, countOrganisms(organismType.Rabbit)},
            {organismType.Fox, countOrganisms(organismType.Fox)}
          });
        }
      }

      // Clear frame and clear for drawing
      Raylib.BeginDrawing();
      Raylib.ClearBackground(Raylib.RAYWHITE);

      if (!menuOpen)
      {
        Raylib.UpdateCamera(ref camera, CameraMode.CAMERA_FREE); // Update camera 
      }

      // 3D
      Raylib.BeginMode3D(camera);

      if (((JsonElement)config["debug"]).GetBoolean())
      {
        Raylib.DrawGrid(100, 1); 
      }

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
        renderablesToRemove.Clear();
      }

      if (renderablesToAdd.Count > 0)
      {
        foreach (basicRenderable renderable in renderablesToAdd)
        {
          renderables.Add(renderable);
        }
        renderablesToAdd.Clear();
      }

      Raylib.EndMode3D();

      // Client
      if (Raylib.IsKeyPressed(KeyboardKey.KEY_TAB))
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
        Raylib.DrawRectangle(0, 0, 128, 480, Raylib.BLACK);

        int idx = 0;
        unsafe
        {
          foreach (string propertyName in config.Keys)
          {
            string str = propertyName;
            sbyte* txt = stackalloc sbyte[str.Length + 1];
            for (int i = 0; i < str.Length; i++)
            {
              txt[i] = (sbyte)str[i];
            }
            txt[str.Length] = 0;
            Bool active = (Bool)((JsonElement)config[propertyName]).GetBoolean();
            RayGui.GuiCheckBox(new Rectangle(16, idx * 28 + 16, 16, 16), txt, &active);
            config[propertyName] = JsonDocument.Parse(JsonSerializer.Serialize((bool)active)).RootElement;
            idx++;
          }
        }

        if (RayGui.GuiButton(new Rectangle(10, 450, 96, 16), "Exit") == 1)
        {
          running = false;
        }
      }

      if (((JsonElement)config["show_fps"]).GetBoolean())
      {
        Raylib.DrawFPS(548, 16);
      }

      if (loggingEnabled != ((JsonElement)config["statistics"]).GetBoolean())
      {
        Raylib.DrawText("Restart Required", 500, 460, 16, Raylib.RED);
      }

      Raylib.EndDrawing(); // End frame
    }

    string json = JsonSerializer.Serialize(config);
    File.WriteAllText("./data/conf.json", json);

    // Write logging data
    if (loggingEnabled)
    {
      using (StreamWriter writer = new StreamWriter("./data/out/log.csv"))
      {
        writer.WriteLine("Time,Foxes,Rabbits");
        int i = 0;
        foreach (Dictionary<organismType, int> ct in organismCounts)
        {
          writer.WriteLine($"{i},{ct[organismType.Fox]},{ct[organismType.Rabbit]}");
          i++;
        }
      }
    }

    Raylib.CloseWindow();
  }
}