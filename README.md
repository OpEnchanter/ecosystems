# Ecosystem Simulation

This project is a simple ecosystem simulation written in C# using [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) for 3D rendering. The simulation features organisms (rabbits, foxes, bushes) and fluid sources (ponds) interacting in a 3D environment.

## Features

- **Organisms:**  
  - Rabbits, foxes, and bushes, each with their own traits and stats.
  - Organisms move, seek food and water, and lose health if they can't find resources.
  - Foxes hunt rabbits, rabbits eat bushes, and all can seek water from ponds.

- **Environment:**  
  - Randomly placed organisms and ponds on a grid.
  - 3D camera controls using Raylib's free camera mode.
  - Simple 3D models for each entity.

## Controls

- **Mouse:** Move the camera (free camera mode).
- **ESC:** Close the simulation window.

## Getting Started

### Prerequisites

- [.NET 6.0+ SDK](https://dotnet.microsoft.com/download)
- [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) NuGet package

### Running the Simulation

1. Clone this repository:
   ```sh
   git clone https://github.com/OpEnchanter/ecosystems.git
   cd ecosystems
   ```
2. Restore dependencies:
   ```sh
   dotnet restore
   ```
3. Build and run:
   ```sh
   dotnet run
   ```

## Project Structure

- `Program.cs` — Main simulation logic, rendering, and entity definitions.
- `.gitignore` — Standard C# and VS Code ignores.

## How It Works

- **Organisms** have traits (speed, eyesight, food/hydration sources) and stats (food, hydration, health).
- Each update, organisms:
  - Wander randomly if healthy.
  - Seek food or water if low on resources.
  - Lose health if starving or dehydrated.
  - Are removed from the simulation if health reaches zero.
- **Rendering** uses Raylib-cs to draw simple spheres and planes for entities.

## Customization

- Add new organism types or behaviors by extending the `organism` class.
- Adjust population sizes or traits in `Program.cs` for different simulation dynamics.

## License

This project is open source and available