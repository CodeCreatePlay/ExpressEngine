## 👾 Library of graphics, rendering and AI programming tools designed to be used as backend for games and other related projects, feel free to include in your projects!
 
<h3 align="center">Community</h3>

<p align="center">
<a href='https://discord.gg/WZ3GZCvVtg' target="_blank"><img alt='Discord' src='https://img.shields.io/badge/Discord-5865F2?style=plastic&logo=discord&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Patreon' src='https://img.shields.io/badge/Patreon-F96854?style=plastic&logo=patreon&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/Reddit-FF4500?style=plastic&logo=reddit&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/YouTube-FF0000?style=plastic&logo=youtube&logoColor=white'/></a>
</p>

### Table of Contents
- [StateMachine with Transition Builder](https://github.com/CodeCreatePlay/ExpressEngine)
- [Hierarchical RuleBuilder](https://github.com/CodeCreatePlay/ExpressEngine)

### 🟦 StateMachine with Transition Builder
A State Machine is a computational model used to design systems that can be in one of a finite number of specific states at any given time, transitions between these states is defined by fixed rules which are based on user-input, external or internal events.  

**Features:-**
- Define states as either as separate objects or as callback methods.
- Separate state initialization and disposal logic.
- A global state to account for situations when an entity stops to exists, such as character death.
- [RuleBuilder](https://github.com/CodeCreatePlay/ExpressEngine) or transition builder to define state transitions in a modular-hierarchical way, in one place, rather than embedding them within individual state objects.

```
using ExpressEngine.StateMachine;

public class AI_Character : MonoBehaviour
{
    private void Start()
    {
        // Initialize StateMachine instance
        state_machine = new StateMachine(this);

        // Create instances of various States
        combat_state = new CombatState(this);
        patrol_state = new PatrolState(this);
        death_state  = new DeathState(this);

        // **1. Add States to the StateMachine**

        // Option 1: Directly add State instances with a unique ID
        state_machine.AddState(combat_state, COMBAT_STATE);
        state_machine.AddState(patrol_state, PATROL_STATE);

        // Option 2: Define lightweight States (like Idle) with Enter, Update, and Exit methods
        // Specify a unique ID and optional callbacks for entering and exiting the State
        idle_state = state_machine.AddState(OnIdleUpdate, IDLE_STATE, 
                                            enterMethodCallback: OnIdleEnter, 
                                            exitMethodCallback: OnIdleExit);

        // **2. Add a Global State**

        // Global States handle conditions that should be respected regardless of the current State
        // Example: When character dies, the StateMachine switches to death_state if IsAlive returns false.
        state_machine.AddGlobalState(death_state, triggerMethodCallback: IsAlive);

        // **3. Switch to a specific State**

        // Set the StateMachine to start in the Idle State
        state_machine.SwitchState(IDLE_STATE);

        // **State Transition Order:**
        // - When a transition is requested, the Exit method of the current State is called.
        // - Then, the Enter method of the new State is called.
        // - Complex initialization and disposal logic can be controlled by returning false from Enter and Exit methods.
        // - The Update method of the new State begins after Enter returns true.
    }

    private void Update()
    {
        if (state_machine != null)
            state_machine.Update();
    }

    // Example callbacks for the Idle State
    private bool OnIdleEnter()
    {
        // Continue initialize logic as long as 'some_condition' is true.
        if (some_condition)
        {
            // Continue initializing
            return false;
        }

        // Initialization complete, proceed to Update method
        return true;
    }

    private void OnIdleUpdate() 
    {
        // Logic during Idle
    }

    private bool OnIdleExit()
    {
        // Continue exit logic as long as 'some_condition' is true.
        if (some_condition)
        {
            // Continue cleanup
            return false;
        }

        // Cleanup complete, proceed to the next State's Enter method
        return true;
    }

    private bool IsAlive() => /* return true if character is alive, false otherwise */;
}
```

To create a new State for the State Machine, define a class that inherits from StateBase<T>. This base class requires a template parameter (T) representing the type of the State Machine's owner, which is usually the main object that holds or controls the State Machine (e.g., an AI character or any game entity).

1. Define the State Class: Create a new class that extends StateBase<T>, where T is the owner’s type.
2. Implement State Methods: Override methods such as OnEnter, OnUpdate, and OnExit to define the behavior of this State during its lifecycle.

```
public class PatrolState<T> : StateBase<T>
{
    public PatrolState(T owner) : base(owner) {}

    // Called each frame while the State is active
    private void OnUpdate()
    {
        Debug.Log("Patrol state on update");
    }
	
    // Optionally override OnEnter and OnExit methods
}

```

### Hierarchical RuleBuilder

An intuitive, hierarchical rule-building system designed for creating and evaluating complex conditional logic in a modular way across a variety of decision-making needs, such as AI logic, character behavior design, and any scenario requiring layered rule evaluation. RuleBuilder enables developers to define branching conditions, nested rules, and dynamic decision trees using `If`, `ElseIf`, `Else`, and `Switch` statements—all within an easy-to-read, fluent interface.  

This code sample demonstrates a basic usage of the RuleBuilder to manage a robot’s state transitions based on health, customer service, and task priorities, For a complete reference, see the demo project.

```
var nextAction = RuleBuilder<RobotState>.Begin()
    .If(() => robot.HasFault() || robot.IsHealthLow()) // Priority check: Health and physical evaluation.
        .Then(RuleBuilder<RobotState>.Begin() // Nested RuleBuilder.
            .Switch(() => robot.HealthState)
                .Case(HealthState.HealthLow, RobotState.RECHARGE)
                .Case(HealthState.HasFault,  RobotState.MAINTENANCE)
                .Case(HealthState.Critical,  RobotState.STOPPED)
                .Default(RobotState.UNHANDLED_HEALTH_STATE) // Notify operator if health state is not handled.
            .EndSwitch()
            .Evaluate())
    .ElseIf(robot.HasCustomers) // Next priority: Customer service after health checks.
        .Then(RobotState.CUSTOMER_CARE)
    .ElseIf(robot.HasTask) // If no customers, check for pending tasks.
        .Then(RuleBuilder<RobotState>.Begin()
            .Switch(() => robot.RobotTask)    
                .Case(RobotTask.Packaging, RobotState.PACKAGING)
                .Case(RobotTask.Inventory, RobotState.INVENTORY_MANAGEMENT)
                .Default(RobotState.UNHANDLED_TASK_STATE) // Notify operator if task type is not handled.
            .EndSwitch()
            .Evaluate())
    .Else(RobotState.REST) // Default to rest state if no conditions are met.
    .Evaluate();
```

This tool is sepcially designed for defining transitions within a StateMachine and creating flexible rule sets for Fuzzy Logic decision-making. It provides a streamlined approach for handling complex conditions and transitions in a centralized, easy-to-read format. For detailed examples, refer to the demo projects.

