using ZeroElectric.Vinculum;
using System.Numerics;
using System.Text.Json;
using ZeroElectric.Vinculum.Extensions;

unsafe class Program()
{
  public static List<basicRenderable> renderables = new List<basicRenderable>();
  public static List<basicRenderable> renderablesToRemove = new List<basicRenderable>();
  public static List<basicRenderable> renderablesToAdd = new List<basicRenderable>();

  public static Random random = new Random();
  public static Dictionary<string, object> config;
  public static Image heightMap;
  public static int landThreshold;
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

  static void loadingScreen(string part)
  {
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Raylib.BLACK);
    Raylib.DrawText("Loading...", 260, 208, 32, Raylib.RAYWHITE);
    Raylib.DrawText(part, 260, 248, 16, Raylib.RAYWHITE);
    Raylib.EndDrawing();
  }

  private static void placeFeatures(Dictionary<basicRenderable, int> objects, Image heightmap, int generationThreshold)
  {
    foreach (basicRenderable renderable in objects.Keys)
    {
      for (int i = 0; i < objects[renderable]; i++)
      {
        Vector2 p = new Vector2(random.Next(-200, 200), random.Next(-200, 200));
        bool canGenerate = Raylib.GetImageColor(heightmap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > generationThreshold;
        while (!canGenerate) {
          p = new Vector2(random.Next(-200, 200), random.Next(-200, 200));
          canGenerate = Raylib.GetImageColor(heightmap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > generationThreshold;
        }

        if (renderable is organism)
        {
          organism clone = (organism)renderable.Clone();
          clone.organismPosition = p;
          clone.traits.sex = (Program.random.Next(2) == 0) ? sex.Male : sex.Female;
          clone.traits.eyesight = (float)random.Next(3, 15);
          clone.traits.speed = (float)random.Next(1, 5) / 10;
          clone.animationTimeOffset = random.Next(0, 30) / 10.0f;
          renderables.Add(clone);
        }
        else
        {
          basicRenderable clone = renderable.Clone();
          clone.position = new Vector3(p.X, 0, p.Y);
          clone.eulerRotation = new Vector3(0, random.Next(0, 360), 0);
          renderables.Add(clone);
        }
      }
    }
  }

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

    loadingScreen("Shaders");

    // Initialize shaders
    Shader lit = Raylib.LoadShader("./shader/lit_vert.glsl", "./shader/lit_frag.glsl");

    // Initialize textures

    loadingScreen("Organism Textures");

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

    


    // Ground
    Image* groundHeightmap = (Image*)Raylib.MemAlloc((uint)sizeof(Image));
    *groundHeightmap = Raylib.GenImagePerlinNoise(1024, 1024, 0, 0, 5.0f);
    Raylib.ImageDrawCircle(groundHeightmap, 512, 512, 196, Raylib.WHITE);
    for (int i = 0; i < 50; i++)
    {
      loadingScreen($"Terrain | Landmass {i} of 50");
      Raylib.ImageDrawCircle(
        groundHeightmap,
        512 + Program.random.Next(-256, 256),
        512 + Program.random.Next(-256, 256),
        Program.random.Next(32, 128), Raylib.WHITE);
    }

    loadingScreen($"Terrain | Smoothing");
    Raylib.ImageBlurGaussian(groundHeightmap, 10);

    int terrainThreshold = 245;
    landThreshold = terrainThreshold;

    loadingScreen("Geographical Features");

    // Ponds
    fluidSource pond = new fluidSource()
    {
      sourceType = fluidType.Water
    };

    for (int i = 0; i < 150; i++)
    {
      loadingScreen($"Terrain | Pond {i} of 150");
      Vector2 p = new Vector2(random.Next(-200, 200), random.Next(-200, 200));
      bool canGenerate = Raylib.GetImageColor(*groundHeightmap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > terrainThreshold;
      while (!canGenerate)
      {
        p = new Vector2(random.Next(-200, 200), random.Next(-200, 200));
        canGenerate = Raylib.GetImageColor(*groundHeightmap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > terrainThreshold;
      }
      fluidSource pondClone = pond.Clone();
      pondClone.position = new Vector3(p.X, -0.5f, p.Y);
      renderables.Add(pondClone);
      Raylib.ImageDrawCircle(groundHeightmap,
      (int)MathF.Round((p.X + 200) / 400.0f * 1023),
      (int)MathF.Round((p.Y + 200) / 400.0f * 1023),
      4, new Color(185, 185, 185, 255));
    }

    loadingScreen($"Terrain | Smoothing");
    Raylib.ImageBlurGaussian(groundHeightmap, 2);


    loadingScreen($"Textures | Terrain");
    // Create copy of heightmap
    Image* groundImage = (Image*)Raylib.MemAlloc((uint)sizeof(Image));
    *groundImage = Raylib.ImageCopy(*groundHeightmap);
    // Recolor
    Color* pixels = Raylib.LoadImageColors(*groundImage);
    for (int i = 0; i < groundImage->width * groundImage->height; i++)
    {
      if (pixels[i].r <= terrainThreshold && pixels[i].g <= terrainThreshold && pixels[i].b <= terrainThreshold)
      {
        pixels[i] = new Color((int)(120 * (pixels[i].r / (255 / 1.5f))) + 15, (int)(91 * (pixels[i].r / (255 / 1.5f))) + 15, (int)(13 * (pixels[i].r / (255 / 1.5f))) + 15, 255);
      }
      else if (pixels[i].r >= terrainThreshold && pixels[i].g >= terrainThreshold && pixels[i].b >= terrainThreshold)
      {
        pixels[i] = new Color(45, 99, 38, 255);
      }
    }

    *groundImage = new Image
    {
      data = pixels,
      width = 1024,
      height = 1024,
      mipmaps = 1,
      format = (int)PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
    };

    Texture groundTexture = Raylib.LoadTextureFromImage(*groundImage);
    Model groundModel = Raylib.LoadModelFromMesh(Raylib.GenMeshHeightmap(*groundHeightmap, new Vector3(400.0f, 12.0f, 400.0f)));
    groundModel.materials[0].shader = lit;
    groundModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = groundTexture;

    basicRenderable ground = new basicRenderable();
    ground.model = groundModel;
    ground.position = new Vector3(-200.0f, -12.5f, -200.0f);
    renderables.Add(ground);

    Texture waterTexture = Raylib.LoadTextureFromImage(Raylib.GenImageColor(1, 1, new Color(35, 137, 218, 155)));
    Model waterModel = primShapes.plane(400, 400, 1, 1);
    waterModel.materials[0].shader = lit;
    waterModel.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_ALBEDO].texture = waterTexture;

    basicRenderable water = new basicRenderable();
    water.model = waterModel;
    water.position = new Vector3(0.0f, -1.0f, 0.0f);
    renderables.Add(water);


    heightMap = Raylib.ImageCopy(*groundHeightmap);

    // Add details
    loadingScreen($"Details | Load");

    organism fox = new organism()
    {
      model = foxModel,
      position = new Vector3(0.0f, 0.0f, 5.0f)
    };

    fox.traits.oType = organismType.Fox;
    fox.traits.foodSources = new organismType[] { organismType.Rabbit };
    fox.traits.hydrationSources = new fluidType[] { fluidType.Water };

    organism rabbit = new organism()
    {
      model = rabbitModel,
      position = new Vector3(0.0f, 0.0f, 5.0f)
    };

    rabbit.traits.oType = organismType.Rabbit;
    rabbit.traits.foodSources = new organismType[] { organismType.Bush };
    rabbit.traits.hydrationSources = new fluidType[] { fluidType.Water };

    organism bush = new organism()
    {
      model = bushModel,
      position = new Vector3(0.0f, 0.0f, 0.0f),
    };

    bush.stats.health = 30.0f;
    bush.stats.maxHealth = 30.0f;
    bush.traits.oType = organismType.Bush;
    bush.traits.canMove = false;
    
    loadingScreen($"Details | Place");
    placeFeatures(new Dictionary<basicRenderable, int>
    {
      { bush, 150 },
      { fox, 120 },
      { rabbit, 120 }
    }, *groundHeightmap, terrainThreshold);



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
        { "show_fps", false },
        { "noclip", false }
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

    float cameraYaw = 0.0f;
    float cameraPitch = 0.0f;
    float mouseSens = 0.1f;
    float cameraSpeed = 0.2f;

    bool godMenuOpen = false;

    bool lastMenuOpen = false;
    bool lastGodMenuOpen = false;


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

      if (!menuOpen && !godMenuOpen)
      {
        if (menuOpen != lastMenuOpen || godMenuOpen != lastGodMenuOpen)
        {
          Raylib.DisableCursor();
        }
        Vector2 mouseDelta = Raylib.GetMouseDelta();

        cameraYaw += mouseDelta.X * mouseSens;
        cameraPitch -= mouseDelta.Y * mouseSens;

        cameraPitch = Math.Clamp(cameraPitch, -89.9f, 89.9f);

        Vector3 forward = new Vector3(
          MathF.Cos(cameraYaw * (MathF.PI / 180.0f)) * MathF.Cos(cameraPitch * (MathF.PI / 180.0f)),
          MathF.Sin(cameraPitch * (MathF.PI / 180.0f)),
          MathF.Sin(cameraYaw * (MathF.PI / 180.0f)) * MathF.Cos(cameraPitch * (MathF.PI / 180.0f))
        );

        forward = Vector3.Normalize(forward);

        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0.0f, 1.0f, 0.0f)));

        float frameCameraSpeed = cameraSpeed;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
        {
          frameCameraSpeed *= 2;
        }

        Vector3 movement = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) { movement += forward * frameCameraSpeed; }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) { movement -= forward * frameCameraSpeed; }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) { movement -= right * frameCameraSpeed; }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) { movement += right * frameCameraSpeed; }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) { movement -= new Vector3(0.0f, 1.0f, 0.0f) * frameCameraSpeed; }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_SPACE)) { movement += new Vector3(0.0f, 1.0f, 0.0f) * frameCameraSpeed; }

        if (!((JsonElement)config["noclip"]).GetBoolean())
        {
          float terrainHeight = Raylib.GetImageColor(*groundHeightmap,
          (int)MathF.Round((camera.position.X + 200) / 400.0f * 1023),
          (int)MathF.Round((camera.position.Z + 200) / 400.0f * 1023)).r / 255.0f * 12.0f - 12.0f;
          if (camera.position.Y < terrainHeight)
          {
            camera.position.Y = terrainHeight;
          }
        }

        camera.position += movement;
        camera.target = camera.position + forward;
      }
      else
      {
        if (menuOpen != lastMenuOpen || godMenuOpen != lastGodMenuOpen)
        {
          Raylib.EnableCursor();
        }
      }

      lastMenuOpen = menuOpen;
      lastGodMenuOpen = godMenuOpen;

      // 3D
      Raylib.BeginMode3D(camera);

      // Update cycle
      foreach (basicRenderable renderable in renderables)
      {
        renderable.Draw();
        if (renderable is organism)
        {
          organism renderableOrganism = (organism)renderable;
          if (!godMenuOpen)
          {
            renderableOrganism.Update();
          }
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

      List<organism> nearbyOrganisms = new List<organism>();
      foreach (basicRenderable renderable in renderables)
      {
        if (renderable is organism && new Vector3(
          renderable.position.X - camera.position.X,
          renderable.position.Y - camera.position.Y,
          renderable.position.Z - camera.position.Z).Length() < 5)
        {
          nearbyOrganisms.Add((organism)renderable);
        }
      }

      organism targetedOrganism = null;
      float targetedOrganismDist = 0.0f;
      foreach (organism o in nearbyOrganisms)
      {
        Vector3 vec = -Vector3.Normalize(new Vector3(
          camera.position.X - o.position.X,
          camera.position.Y - o.position.Y,
          camera.position.Z - o.position.Z
        ));
        Vector3 camLook = Vector3.Normalize(new Vector3(
          camera.target.X - camera.position.X,
          camera.target.Y - camera.position.Y,
          camera.target.Z - camera.position.Z
        ));
        float dot = Math.Clamp(
          Vector3.Dot(vec, camLook), 0, 1
        );
        if (dot > 0.90 && dot > targetedOrganismDist)
        {
          targetedOrganism = o;
        }
      }

      if (targetedOrganism != null)
      {
        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && !menuOpen)
        {
          if (!godMenuOpen)
          {
            godMenuOpen = true;
          }
        }
        Raylib.DrawSphere(targetedOrganism.position, 1.15f, new Color(15, 155, 200, 155));
      }

      Raylib.EndMode3D();

      // God menu
      if (godMenuOpen)
      {
        int w = 480;
        int h = 64;
        int x = 320 - w / 2;
        int y = 480 - h;
        Raylib.DrawRectangle(x, y, w, h, Raylib.BLACK);
        if (RayGui.GuiButton(new Rectangle(x + 16, y + 16, 64, 32), "Kill") == 1)
        {
          targetedOrganism.stats.health = -100.0f;
          godMenuOpen = false;
        }

        if (RayGui.GuiButton(new Rectangle(x + 16*2 + 64, y + 16, 64, 32), "Feed") == 1)
        {
          targetedOrganism.stats.food = 10.0f;
          godMenuOpen = false;
        }

        if (RayGui.GuiButton(new Rectangle(x + 16*3 + 64*2, y + 16, 64, 32), "Hydrate") == 1)
        {
          targetedOrganism.stats.hydration = 10.0f;
          godMenuOpen = false;
        }
      }

      // Water overlay
      if (camera.position.Y < -1.0f)
      {
        float depth = (10 + camera.position.Y) / 10.0f;
        if (depth < 0.15)
        {
          depth = 0.15f;
        }
        Raylib.DrawRectangle(0, 0, 640, 640, new Color((int)(35 * depth), (int)(137 * depth), (int)(218 * depth), 255 - (int)(120 * depth)));
      }

      // Client
      if (Raylib.IsKeyPressed(KeyboardKey.KEY_TAB))
      {
        menuOpen = !menuOpen;
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