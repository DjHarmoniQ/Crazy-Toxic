# Crazy-Toxic 🎮

A **3D Side-Scroller Pixel Art Roguelike** built with Unity 3D.

---

## 🚀 Getting Started

### Prerequisites

- **Unity** 2022.3 LTS or newer (download from [unity.com](https://unity.com/download))
- **Git** (to clone this repository)

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone https://github.com/DjHarmoniQ/Crazy-Toxic.git
   ```

2. **Open in Unity**
   - Launch **Unity Hub**
   - Click **"Add"** → **"Add project from disk"**
   - Navigate to the cloned `Crazy-Toxic` folder and select it
   - Open the project (Unity will import assets automatically)

3. **Open the Main Scene**
   - In the **Project** window, navigate to `Assets/Scenes/`
   - Double-click **MainScene** to open it

4. **Press Play**
   - Hit the **▶ Play** button in Unity
   - Use the controls below to test the player movement!

---

## 🎮 Controls

| Action         | Key(s)                   |
|----------------|--------------------------|
| Move Left/Right| `A` / `D` or Arrow Keys  |
| Jump           | `Space`                  |
| Double Jump    | `Space` (while airborne) |
| Dash           | `Left Shift`             |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── PlayerController.cs   # Player movement, jumping, dashing
│   ├── CameraController.cs   # Smooth side-scroller camera follow
│   └── GameManager.cs        # Core game state management
├── Scenes/
│   └── MainScene.unity       # Main test/game scene
├── Prefabs/                  # Reusable GameObjects (player, platforms, enemies)
├── Art/
│   ├── Sprites/              # 2D pixel art sprites
│   └── Tilesets/             # Tilemap tilesets
├── Audio/
│   ├── Music/                # Background music tracks
│   └── SFX/                  # Sound effects
└── Animations/               # Animation controllers and clips
```

---

## 🛠️ Core Systems

### PlayerController.cs
Handles all player movement and abilities:
- **Horizontal movement** with smooth acceleration/deceleration
- **Jumping** with configurable gravity and jump force
- **Double Jump** – press Space a second time while airborne
- **Dash** – press Shift to dash in the current movement direction
- **Ground Detection** – overlap circle-based for accurate grounded checks

### CameraController.cs
Smooth side-scroller camera that:
- Follows the player with configurable smoothing
- Maintains a fixed Z position for true side-scroller view
- Supports horizontal and vertical offsets

### GameManager.cs
Singleton pattern for core game state:
- Scene management helpers
- Game pause/resume functionality
- Ready to expand for score, health, and loot systems

---

## 🗺️ Roadmap

- [ ] Enemy AI & Behavior
- [ ] Combat System (melee + projectile)
- [ ] Procedural Level Generation
- [ ] Loot & Item System
- [ ] UI / HUD (health bar, score, abilities)
- [ ] Pixel Art Assets
- [ ] Audio & SFX
- [ ] Boss Encounters

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/enemy-ai`)
3. Commit your changes (`git commit -m 'Add enemy AI system'`)
4. Push to your branch (`git push origin feature/enemy-ai`)
5. Open a Pull Request

---

## 📄 License

This project is open source. See [LICENSE](LICENSE) for details.

---

*Built with ❤️ and Unity 3D*
