## 👾 Library of graphics, rendering and AI-programming tools.
 
<h3 align="center">Community</h3>

<p align="center">
<a href='https://discord.gg/WZ3GZCvVtg' target="_blank"><img alt='Discord' src='https://img.shields.io/badge/Discord-5865F2?style=plastic&logo=discord&logoColor=white'/></a>
<a href='https://www.patreon.com/expressengine' target="_blank"><img alt='Patreon' src='https://img.shields.io/badge/Patreon-F96854?style=plastic&logo=patreon&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/Reddit-FF4500?style=plastic&logo=reddit&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/YouTube-FF0000?style=plastic&logo=youtube&logoColor=white'/></a>
</p>

### Table of Contents
- [StateMachine](https://github.com/CodeCreatePlay/ExpressEngine)
- [Character Controller](https://github.com/CodeCreatePlay/ExpressEngine)

### 🟦 StateMachine
A State Machine is a computational model used to design systems that can be in one of a finite number of specific states at any given time, transitions between these states based on inputs or events, with well-defined rules that dictate how and when transitions occur. StateMachines are particularly useful for modeling behaviors in systems where the current state influences future behavior, such as control systems, parsers and game logic.

**Features:-**
- Define States as either as separate objects or as callback methods.
- Separate State initialization and disposal logic.
- A Global State to account for situations when an entity stops to exists, such as character death.
- State transition builder to easily define State transition conditions and rules in one place, rather than hard-coding them directly within the State objects themselves.

```
public class AI_Character : MonoBehaviour
{
    private void Start()
    {
        // StateMachine instance
        state_machine = new(this);

        // Create State instances
        combat_state = new(this);
        patrol_state = new(this);
        death_state  = new(this);

        // There are 2 ways to add a new State to StateMachine.

        // 1. Add State instances directly to StateMachine together a unique int ID for that State.
        state_machine.AddState(combat_state, COMBAT_STATE);
        state_machine.AddState(patrol_state, PATROL_STATE);

        // 2. For lightweight StateS such as an idle State, you can directly specify
        // Enter, Update and Exit methods, together with a unique int ID for the new State. 
        // Enter and Exit methods are optional.
        // The returned value of AddState is a new State object created internally.
        idle_state = state_machine.AddState(OnIdleUpdate, IDLE_STATE, enterMethodCallback:OnIdleEnter, exitMethodCallback:OnIdleExit);

        // To account for situations such as a character's death which must be honored regardless the current State, can be
        // done using a Global-State, together with a Global-State-Trigger which is just a callback function and
        // returns a bool value, the Global-State will trigger as soon as its trigger will return true.
        state_machine.AddGlobalState(death_state, triggerMethodCallback: IsAlive);

        // To switch to a different State 
        state_machine.SwitchState(IDLE_STATE);
		
        // The default order of execution is that, when a State transition is requested, the Exit-Method of current State is invoked,
        // followed by the Start-Method of new State, for complex initialization and disposal logic both Enter and Exit methods can
        // continue to execute as long as their return value is 'false', OnUpdate method starts after Start method return 'true'.
    }

    private void Update()
    {
        if(state_machine != null)
            state_machine.Update();
    }
}
```

### 🟦 Character Controller

Versatile Rigidbody CharacterController designed for realistic humanoid character movements in mind, however the the system is flexible enough to be modified for other character types as well.

**Features:-**
- Ground detection - Ground contact information is provided each physics frame. Performs ground detection with configurable parameters, using 'SphereCast' to support ledge perching.  
- Slope traversal with ground snapping - Snap to ground surface while moving. Correctly handles velocity on angled surfaces.  
- Velocity physics - Optional built-in acceleration and deceleration.  
- Supports moving surfaces - Correctly handles velocity on moving platforms.  
- Intuitive collider adjustment - Configure collider height or step height while keeping collider bottom or top fixed.

**Usage Instructions:-**

See the included CharacterController demo project for an example usage, the included CharacterController script, built on top of CharacterMotor, should be good enough for most humanoid character movement use cases.

**CharacterMotor:-**

CharacterMotor handles the actual character movements. You can use it to build your own CharacterControllers on top, or simply modify the included CharacterController script to suit your project's requirements. CharacterMotor provides several methods for controlling movements.

- **1. Move:** This is the easiest way to get moving. Call `Move` every fixed update to set the intended movement velocity.

	```csharp
	Move(Vector3 velocity);
	Move(float speed, Vector3 direction);
	```

- **2. Active Velocity:**
`activeVelocity` is an internal velocity field that CharacterMotor uses to set Rigidbody velocity at the end of each fixed update. When the 'Velocity Mode' inspector field is set to a mode that uses velocity physics (IE. acceleration, deceleration, friction), `activeVelocity` will persist across fixed updates and gradually change based on the configured velocity physics logic.
To ignore any applicable velocity physics and directly set CharacterMotor velocity, you can set `activeVelocity` by:

	```csharp
	SetActiveVelocity(Vector3 velocity);
	```

  You can clear the `activeVelocity` by:

	```csharp
	ClearActiveVelocity();
	```

- **3. Delta Position:** Directly sets the intended position change for the current fixed update. This can be used to drive CharacerMotor by animation root motion.

	```csharp
	MoveDeltaPosition(Vector3 deltaPosition, bool alignToGround, bool restrictToGround)
	```

**Velocity Physics Modes:-**
Use the Velocity Mode inspector field on a CharacerMotor to configure velocity physics behavior.
- Raw: Instant acceleration and deceleration
- Simple: Fixed acceleration and deceleration based on the Speed Change Rate inspector field.
