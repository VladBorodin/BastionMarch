# TDD-14. Временная модель хода

## 1. Ход

Ход состоит из:

1. Planning;
2. последовательности Action Phase;
3. Turn End.

Planning и Turn End не являются Action Phase.

Стандартное число Action Phase в первой версии — 2.
Архитектура не должна предполагать, что их всегда ровно две.

## 2. Planning

Simulation не ограничивает Planning реальным временем.

Ограничение времени является правилом режима игры,
сложности или Presentation/Application слоя.

После подтверждения план текущего хода изменять нельзя.

## 3. Action Phase

Action Phase является минимальным дискретным
моментом игрового времени.

Обычное небольшое действие занимает одну фазу.
Крупное действие может занимать несколько фаз
и продолжаться через границу хода.

## 4. Orders и Activities

Order — конечное или многоэтапное действие,
занимающее Action Phase.

Activity — постоянная деятельность бригады,
выполняемая в свободные от Order фазы.

Если в текущей фазе бригада выполняет Order,
её обычная Activity в эту фазу не получает tick.

## 5. Длительные воздействия

Если воздействие полностью разрешается внутри одной
Action Phase, его визуальное прохождение может быть
только Presentation-анимацией.

Если объект или воздействие переживает границу фаз
и может изменить последующие решения игрока,
оно должно существовать в Simulation.

Пример:
долго летящий артиллерийский снаряд.

Луч, полностью разрешаемый в одной фазе,
не требует persistent projectile entity.

## Реализация 12.1

Добавлены:

- TurnStage;
- TurnCycle;
- TurnNumber.

TurnStage содержит:

- Planning;
- ActionResolution;
- TurnEnd.

Новый TurnCycle начинается:

- с TurnNumber = 1;
- со стадии Planning.

Допускается создание TurnCycle с известного
положительного номера хода.

На этапе 12.1 TurnCycle ещё не содержит:

- ActionPhaseCount;
- CurrentActionPhase;
- активные бригады;
- переходы между стадиями;
- планы;
- приказы.

TurnCycle не принадлежит Bastion и не изменяет его.

## Реализация 12.2

TurnCycle хранит:

- ActionPhaseCount;
- CurrentActionPhase.

Стандартное число фаз:

2.

Минимально допустимое число:

1.

Временное ядро не устанавливает верхний предел
количества Action Phase.

CurrentActionPhase использует nullable int.

Значение null означает, что TurnCycle находится
вне Action Phase.

Во время Planning:

- Stage = Planning;
- CurrentActionPhase = null;
- HasActiveActionPhase = false.

Нумерация Action Phase начинается с 1.

На этапе 12.2 переход в Action Resolution
ещё не реализован.

## Реализация 12.3

Для текущего хода создаётся неизменяемый snapshot
активных бригад.

Активной считается бригада, которая на момент
создания snapshot:

- зарегистрирована в Bastion;
- не расформирована;
- оперативно размещена.

Состояние IsWorking не влияет на участие в ходе.

TurnBrigadeParticipant содержит только:

- BrigadeId;
- BrigadeNumber.

Изменяемые данные Brigade намеренно не копируются
в участника хода.

Детерминированный порядок:

1. BrigadeNumber;
2. BrigadeId.

TurnCycle копирует переданную коллекцию участников
и не хранит ссылку на Bastion.

Изменение, удаление или расформирование Brigade после
создания snapshot не меняет список участников уже
начатого хода.

Актуальное состояние Brigade должно повторно
проверяться непосредственно перед исполнением Order.

Для следующего хода создаётся новый snapshot.

## Реализация 12.4

TurnCycle поддерживает подтверждение Planning через:

TryConfirmPlanning()

До подтверждения:

- Stage = Planning;
- CurrentActionPhase = null;
- IsPlanningConfirmed = false.

После успешного подтверждения:

- Stage = ActionResolution;
- CurrentActionPhase = 1;
- IsPlanningConfirmed = true.

Planning подтверждается только один раз за ход.

Повторная попытка не изменяет TurnCycle и возвращает:

PlanningAlreadyConfirmed.

Для изменения временного состояния используется
неизменяемый TurnTransitionResult.

Он содержит:

- TurnNumber;
- PreviousStage;
- CurrentStage;
- PreviousActionPhase;
- CurrentActionPhase;
- FailureReason;
- IsSuccess.

Пустой список активных бригад не запрещает
подтверждение Planning.

На этапе 12.4 план приказов ещё не существует.
Подтверждение изменяет только временное состояние.