using ZeroElectric.Vinculum;
using System.Numerics;
using System.Text.Json;
using ZeroElectric.Vinculum.Extensions;

enum organismType
{
    None,
    Bush,
    Rabbit,
    Fox
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
      Vector2 p = organismPosition + localTarget;
      bool validPosition = Raylib.GetImageColor(Program.heightMap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > Program.landThreshold;
      while (!validPosition)
      {
        angle = (float)(Program.random.NextDouble() * MathF.PI * 2); // 0 to 2π
        localTarget = new Vector2(MathF.Cos(angle) * traits.eyesight, MathF.Sin(angle) * traits.eyesight);
        p = organismPosition + localTarget;
        validPosition = Raylib.GetImageColor(Program.heightMap, (int)MathF.Round((p.X + 200) / 400.0f * 1023), (int)MathF.Round((p.Y + 200) / 400.0f * 1023)).r > Program.landThreshold;
      }
      target = p;
      moving = true;
    }
  }

  private float animationBounceSpeed = 0.1f;
  public float animationBounceHeight = 0.25f;
  public float animationTimeOffset = 0.0f;

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
      if (stats.food >= 3.0f && stats.hydration >= 5.0f)
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

          if (nearestFoodSource != null && nearestFoodSourceDist < 1)
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
        if (stats.hydration < 5.0f)
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
                float distance = new Vector2(fluid.position.X - position.X, fluid.position.Z - position.Z).Length();
                if (distance <= traits.eyesight && distance < nearestFluidSourceDist)
                {
                  nearestFluidSourceDist = distance;
                  nearestFluidSource = fluid;
                }
              }
            }
          }

          if (nearestFluidSource != null && nearestFluidSourceDist < 0.5)
          {
            stats.hydration = 10.0f;
          }

          if (nearestFluidSource != null)
          {
            target = new Vector2(nearestFluidSource.position.X, nearestFluidSource.position.Z);
            moving = true;
          }
          else
          {
            wander();
          }
        }
      }

      if (((JsonElement)Program.config["debug"]).GetBoolean())
      {
        Vector3 p = new Vector3(target.X, 1.5f, target.Y);
        Raylib.DrawSphere(p, 0.25f, Raylib.GREEN);
        Raylib.DrawLine3D(position, p, Raylib.GREEN);
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