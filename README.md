# Unity AI Vehicle System 🚗

A sophisticated AI-powered vehicle control system for Unity, featuring intelligent pathfinding, obstacle avoidance, and advanced vehicle maneuvering capabilities.
## 🎥 Demo
![image](https://github.com/user-attachments/assets/e59e3e6f-cb12-4b81-8c89-bf282691a9d8)
![image](https://github.com/user-attachments/assets/b72239cf-837e-494a-be7d-b0d46c7c4a45)
## Video:
https://github-production-user-asset-6210df.s3.amazonaws.com/103859433/412818215-40b54e94-deee-4711-bdd6-9294ad92379f.webm

## 🌟 Features

### AI Vehicle Controller
- **Dynamic Path Following**: Intelligent navigation to target destinations
- **Speed Control**: Adaptive speed based on road conditions and situations
- **Wheel Physics**: Realistic wheel physics and vehicle movement
- **Obstacle Detection**: Multi-directional obstacle detection and avoidance

### Advanced Maneuvers
- **Smart Overtaking**: Intelligent obstacle avoidance and overtaking system
- **Dynamic U-Turns**: Automated U-turn execution when target is behind
- **Reverse System**: Advanced reverse maneuvering in tight spaces
- **Stuck Detection**: Automatic detection and recovery from stuck situations

### Vehicle Physics
- **Realistic Wheel System**: Complete wheel physics with suspension
- **Anti-Stuck System**: Prevents vehicles from getting stuck on obstacles
- **Terrain Adaptation**: Adjusts to different terrain conditions
- **Smooth Controls**: Natural and realistic vehicle movement

## 🛠️ Installation

1. Clone this repository to your Unity project:
```bash
git clone https://github.com/AminHasanloo/Unity-AI-Vehicle-System.git
```

2. Import the following scripts into your Unity project:
   - `AIVehicleController.cs`
   - `VehicleController.cs`
   - `VehicleObstacleAvoidance.cs`
   - `VehicleReverseSystem.cs`
   - `VehiclesAntiStuckSystem.cs`

## 🔧 Setup

1. Create a vehicle GameObject in your scene
2. Add the required components:
   ```
   - Rigidbody
   - Wheel Colliders
   - AIVehicleController
   - VehicleObstacleAvoidance
   - VehicleReverseSystem
   ```
3. Configure the wheel settings in the inspector:
   - Set up wheel transforms
   - Configure wheel colliders
   - Adjust physics parameters

## ⚙️ Configuration

### AIVehicleController
```csharp
[Header("AI Navigation")]
public float detectionRange = 50f;
public float obstacleDetectionRange = 20f;
public float sideDetectionRange = 8f;

[Header("Speed Settings")]
public float normalSpeed = 100f;
public float boostSpeed = 150f;
public float corneringSpeed = 70f;
```

### VehicleReverseSystem
```csharp
[Header("Reverse Detection")]
public float stuckCheckDuration = 2f;
public float minimumSpeedThreshold = 2f;
public float obstacleCheckDistance = 1.5f;

[Header("Turn Around Settings")]
public float quickTurnThreshold = 5f;
public float turnInPlaceForce = 800f;
```

## 🎮 Usage

1. Add a target object to your scene
2. Assign the target to the AI Vehicle Controller
3. Configure the layers for obstacle detection
4. Adjust the parameters to match your vehicle's characteristics

```csharp
// Example: Setting up the AI destination
aiVehicleController.destination = targetTransform;
```

## 🔍 Advanced Features

### Intelligent Path Finding
The system automatically calculates the optimal path to the target while avoiding obstacles:
- Checks for clearance before making turns
- Determines the best overtaking direction
- Adjusts speed based on path complexity

### Smart Reverse System
Handles complex situations where the vehicle needs to reverse:
- Detects when reverse is necessary
- Calculates safe reverse distance
- Manages multi-point turns

### Dynamic Speed Control
Automatically adjusts vehicle speed based on:
- Distance to target
- Upcoming turns
- Obstacle proximity
- Terrain conditions

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

- Unity Technologies for the amazing physics system
- The Unity community for valuable feedback and suggestions
- All contributors who helped improve this system

## 📞 Contact

- Amin Hasanloo - [hsoamin76@gmail.com]
- Linkedin -[https://www.linkedin.com/in/amin-hasanloo-b0974716b/]
- TELEGRAM - [https://t.me/tilitpro]





---
Made with ❤️ for the Unity Community
