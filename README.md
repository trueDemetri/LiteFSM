<p align="center">
  <a href="./README.md"><b>|English|</b></a>
  <a href="./README_RU.md">Русский</a>
</p>

## LiteFSM - A Simple Lightweight Finite State Machine (FSM) Tool

### General Description
The system allows defining states and transitions between them, as well as processing parameters during transitions.

### Components

#### State<TState>
**Base abstract class** for creating states.

Main properties:
* **StateMachine** - reference to the controlling StateMachine
* **Active** - state activity flag

Methods:
* **Enter(prevState)** - called when entering the state. The prevState parameter contains the previous state from which the transition occurred. This can be important in some cases.
* **Exit(nextState)** - called when exiting the state. The nextState parameter will contain the state to which the transition will be made.
* **Dispose()** - releases resources

It is also possible to pass parameters to a state during transition.
To do this, the corresponding state must implement the **IParamState<TValue>** interface, i.e., define the method
```void OnEnter(TState prevState, TValue value)```
After that, it becomes possible to pass an additional parameter of type TValue when calling the StateMachine.SetState method. In principle, a state can implement multiple such interfaces (with different TValue types), and each of them will provide the corresponding capability.
However, it should be noted that only one such parameter can be passed to StateMachine.SetState at a time. Also, for any state, a parameterless transition method is always available (calling StateMachine.SetState<TState>() without additional parameters).
You can change the logic of this transition by overriding ```protected void OnEnter(TState prevState)```

Method:
* **Enter(prevState, parameter)** - entering the state with a parameter

#### StateMachine<TStateBase>
**Main class** for state management.

Properties:
* **CurrentState** - currently active state

Methods:
* **SetStates(states)** - setting a set of states
* **SetState<TState>()** - transition to a state without parameters
* **SetState<TState, TValue>(parameter)** - transition to a state with a parameter
* **Dispose()** - cleaning up all states

### Implementation Features

* **Typing** through generics ensures strict typing of states
* **Transition management** is handled via SetState methods
* **Logging** is activated through the debug parameter
* **Error handling** includes checking the correctness of transitions
* **Resource management** is implemented through IDisposable

### Usage

1. Create state classes by inheriting from State<TState>. If needed, add specific common behavior (for example, a Tick() method for each frame).
2. Implement logic in OnEnter/OnExit methods
3. Create a StateMachine instance
4. Add states using SetStates
5. Manage transitions using SetState

A simplified example below is written for Unity, but the presence of a game engine is not required to use the tool.

```csharp
public abstract class WeaponState : State<WeaponState>
{
    protected readonly Weapon Weapon;

    protected WeaponState(Weapon weapon)
    {
        Weapon = weapon;
    }

    // Example of common behavior methods with the ability to make them unique for each state
    public virtual void Tick()
    { }
}

public class ReloadingState : WeaponState, IParamState<bool>
{
    private const float FullReloadingDuration = 4f;
    private const float QuickReloadingDuration = 1f;

    private float _stopTime;
    private bool _fullReloading;

    public ReloadingState(Weapon Weapon) : base(weapon)
    {}

    public override void Tick()
    {
        if (Time.time < _stopTime)
        {
            return;
        }

        weapon.SetAmmo(_fullReloading ? Weapon.MagazineSize : Weapon.Ammo + 1);
        StateMachine.SetState<ArmedState>();   // The StateMachine property with the current FSM is available in any state
    }

    // Transition with a bool parameter
    public void OnEnter(BaseState prevState, bool fullReloading)
    {
        _fullReloading = fullReloading;
        var duration = fullReloading ? FullReloadingDuration : QuickReloadingDuration;
        _stopTime = Time.time + duration;
    }

    protected override void OnEnter(BaseState prevState)
    {
        OnEnter(prevState, true);
    }
}

public class ArmedState : WeaponState
{
    public ArmedState(Weapon weapon) : base(weapon)
    {}

    public override void Tick()
    {
        if (Weapon.Ammo < Weapon.MagazineSize)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StateMachine.SetState<ReloadingState>();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                StateMachine.SetState<ReloadingState>(false);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Weapon.Fire();
            if (Weapon.Ammo == 0)
            {
                StateMachine.SetState<ReloadingState>();
            }
        }
    }
}

...

public class WeaponUser : MonoBehaviour
{
    [SerializeField] private Weapon _weapon;

    private void Start()
    {
        StateMachine<BaseState> _stateMachine = new();
        _stateMachine.SetStates(
            new BaseState[]
            {
                new ReloadingState(_weapon),
                new ArmedState(_weapon)
            });

        // Don't forget to set the initial FSM state!
        _stateMachine.SetState<ArmedState>(); // Simplistically assuming the weapon is always loaded initially
    }

    private void Update()
    {
        _stateMachine.CurrentState.Tick();
    }
}
```

The example above implements the simplest weapon behavior. In the armed state, the weapon can fire (using the Space key).
Once the ammunition runs out (or the player presses one of the reload keys: Q for quick reload, R for full reload), the weapon transitions to the reloading state. The weapon will not fire until the reload timer expires (until it transitions back to the Armed state).
