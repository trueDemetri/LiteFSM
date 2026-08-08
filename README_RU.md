<p align="center">
  <a href="./README.md">English</a>
  <a href="./README_RU.md"><b>|Русский|</b></a>
</p>

##  LiteFSM - простой легковесный инструмент для реализации конечного автомата состояний (FSM - Finite State Machine)

### Общее описание
Система позволяет определять состояния и переходы между ними, а также обрабатывать параметры при переходах.

### Компоненты

#### State<TState>
**Базовый абстрактный класс** для создания состояний.

Основные свойства:
* **StateMachine** - ссылка на управляющий StateMachine
* **Active** - флаг активности состояния

Методы:
* **Enter(prevState)** - вызывается при входе в состояние. В prevState будет передано предыдущее состояние, из которого произошёл переход. Иногда это важно.
* **Exit(nextState)** - вызывается при выходе из состояния. В nextState соответственно придёт состояние, в которое будет выполнен переход.
* **Dispose()** - освобождение ресурсов

Также есть возможность передачи параметров состоянию при переходе в него.
Для этого соответствующее состояние должно реализовывать интерфейс <b>IParamState<TValue></b>, т.е. определить метод
```void OnEnter(TState prevState, TValue value)```
После этого появляется возможность при вызове метода StateMachine.SetState
дополнительно передать параметр типа TValue. В принципе состояние может реализовывать несколько таких интерфейсов (с разным типом TValue) и для каждого из них появляется соответствующая возможность.
Но нужно учитывать, что передать в StateMachine.SetState можно только один из таких параметров за раз. Также для любого состояния всегда доступен метод перехода без параметров (вызов StateMachine.SetState<TState>() без дополнительных параметров).
Логику этого перехода можно поменять, переопределив ```protected void OnEnter(TSTate prevState)```

Метод:
* **Enter(prevState, parameter)** - вход в состояние с параметром

#### StateMachine<TStateBase>
**Основной класс** управления состояниями.

Свойства:
* **CurrentState** - текущее активное состояние

Методы:
* **SetStates(states)** - установка набора состояний
* **SetState<TState>()** - переход в состояние без параметров
* **SetState<TState, TValue>(parameter)** - переход в состояние с параметром
* **Dispose()** - очистка всех состояний

### Особенности реализации

* **Типизация** через дженерики обеспечивает строгую типизацию состояний
* **Управление переходами** осуществляется через методы SetState
* **Логирование** активируется через параметр debug
* **Обработка ошибок** включает проверку корректности переходов
* **Управление ресурсами** реализовано через IDisposable

### Использование

1. Создать классы состояний, наследуя от State<TState>. Если необходимо, добавить в них специфическое общее поведение (например, метод Tick() для каждого кадра).
2. Реализовать логику в методах OnEnter/OnExit
3. Создать экземпляр StateMachine
4. Добавить состояния через SetStates
5. Управлять переходами через SetState

Упрощённый пример далее написан под Unity, но для использования инструмента наличие игрового движка необязательно.

```csharp
public abstract class WeaponState : State<WeaponState>
{
  protected readonly Weapon Weapon;

  protected WeaponState(Weapon weapon)
  {
    Weapon = weapon;
  }

  // Пример методов общего поведения с возможностью сделать его уникальным для каждого состояния

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
    StateMachine.SetState<ArmedState>();   // Свойство StateMachine с текущей FSM есть у любого состояния
  }

  // Переход с параметром bool
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

    if (Input.GetKeyDown(KeyCode.Space)
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

    // Не забываем выставлять стартовое состояние FSM!
    _stateMachine.SetState<ArmedState>(); // Упрощенно предполагаем, что оружие всегда сначала заряжено
  }

  private void Update()
  {
    _stateMachine.CurrentState.Tick();
  }
}
```

Пример выше реализует простейшее поведение оружия. В заряженном состоянии оно может стрелять (клавиша Space).
Как только патроны заканчиваются (либо игрок нажимает одну из клавиш перезарядки: Q - быстрая, R - полная) оружие переходит в состояние перезарядки. Оружие не будет стрелять, пока не закончится таймер перезарядки
(пока снова не перейдет в состояние Armed).
