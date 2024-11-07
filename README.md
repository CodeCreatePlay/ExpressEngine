## 👾 This is a repository of AI-programming and graphics-rendering tools for an upcoming game by 'girlsdevgames', these tools are not meant to be genre-specific, feel free to include them in your projects!
 
<h3 align="center">Community</h3>

<p align="center">
<a href='https://discord.gg/WZ3GZCvVtg' target="_blank"><img alt='Discord' src='https://img.shields.io/badge/Discord-5865F2?style=plastic&logo=discord&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Patreon' src='https://img.shields.io/badge/Patreon-F96854?style=plastic&logo=patreon&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/Reddit-FF4500?style=plastic&logo=reddit&logoColor=white'/></a>
<a href='https://github.com/CodeCreatePlay/ExpressEngine' target="_blank"><img alt='Unity' src='https://img.shields.io/badge/YouTube-FF0000?style=plastic&logo=youtube&logoColor=white'/></a>
</p>

### Table of Contents
- [StateMachine with Transition Builder](https://github.com/CodeCreatePlay/ExpressEngine)

### 🟦 StateMachine with Transition Builder
A State Machine is a computational model used to design systems that can be in one of a finite number of specific states at any given time, transitions between these states is defined by fixed rules which are based on user-input, external or internal events.  
In context of 

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

