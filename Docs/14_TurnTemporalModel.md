# TDD-14. Временная модель хода

## 1. Назначение

Документ фиксирует временное ядро BastionMarch после завершения Stage 12.

Временная модель отвечает только за:

- номер хода;
- крупную стадию хода;
- количество Action Phase;
- текущую Action Phase;
- неизменяемый snapshot участников текущего хода;
- детерминированные переходы между стадиями.

Она не исполняет Orders, Activities и боевые эффекты.

---

## 2. Структура хода

Один ход состоит из:

```text
Planning
→ Action Resolution
    → Action Phase 1
    → Action Phase 2
    → ...
    → Action Phase N
→ Turn End
```

Planning и Turn End не являются Action Phase.

Стандартное число Action Phase первой версии:

```text
2
```

Архитектура допускает любое положительное `ActionPhaseCount`.

Action Phase является минимальным дискретным моментом игрового времени.

---

## 3. Planning

Во время Planning игрок или AI формирует намерения текущего хода.

Базовая Simulation не ограничивает Planning реальным временем.

Ограничение времени может принадлежать:

- режиму игры;
- сложности;
- Application/Presentation слою.

После подтверждения Planning временной цикл переходит к первой Action Phase.

Начиная со Stage 13 само наличие и валидность плана контролируются отдельным planning-слоем. `TurnCycle` не владеет TurnPlan.

---

## 4. Action Phase

Нумерация Action Phase начинается с 1.

Когда `Stage == ActionResolution`:

```text
CurrentActionPhase ∈ [1..ActionPhaseCount]
```

Во всех остальных стадиях:

```text
CurrentActionPhase = null
```

Малое действие может занимать одну Action Phase.

Крупное действие может занимать несколько Action Phase и переживать границу хода.

---

## 5. Участники текущего хода

Для текущего хода создаётся snapshot активных бригад.

Активной считается бригада, которая на момент создания snapshot:

- зарегистрирована в Bastion;
- не расформирована;
- оперативно размещена.

`IsWorking` не влияет на участие в ходу.

`TurnBrigadeParticipant` содержит только стабильную идентичность:

- `BrigadeId`;
- `BrigadeNumber`.

Изменяемые данные не копируются в participant snapshot.

Snapshot сортируется:

1. `BrigadeNumber`;
2. `BrigadeId`.

Snapshot не меняется в течение текущего хода.

Актуальное состояние Brigade проверяется позднее непосредственно перед исполнением Order.

Для нового хода формируется новый snapshot.

---

## 6. Детерминированный порядок не является инициативой

Сортировка:

```text
BrigadeNumber
→ BrigadeId
```

нужна для:

- воспроизводимости;
- тестов;
- стабильного Presentation;
- replay;
- diagnostics.

Она НЕ означает:

- игровую инициативу;
- более раннее действие меньшего BrigadeNumber;
- преимущество BrigadeId;
- право первой мутации мира.

Если порядок действий должен влиять на результат, он должен быть отдельным явным игровым правилом.

Будущий resolver Action Phase строится вокруг:

```text
Assess
→ Resolve conflicts
→ Commit
```

а не вокруг случайного порядка `foreach`.

---

## 7. TurnCycle

`TurnCycle` является чистым временным ядром.

Он не хранит:

- `Bastion`;
- `TurnPlanDraft`;
- `ConfirmedTurnPlan`;
- Orders;
- Activities;
- боевую логику.

Публичное состояние включает:

- `TurnNumber`;
- `Stage`;
- `ActionPhaseCount`;
- `CurrentActionPhase`;
- `HasActiveActionPhase`;
- `IsPlanningConfirmed`;
- `ActiveBrigades`;
- `ActiveBrigadeCount`.

Поддерживаются переходы:

- `TryConfirmPlanning()`;
- `TryAdvanceActionPhase()`;
- `TryBeginNextTurn(activeBrigades)`.

Ожидаемые ошибки переходов возвращаются через `TurnTransitionResult`.

---

## 8. TurnTransitionResult

`TurnTransitionResult` различает состояние до и после операции.

Он содержит:

- `PreviousTurnNumber`;
- `CurrentTurnNumber`;
- `TurnNumber` как shorthand текущего номера;
- `PreviousStage`;
- `CurrentStage`;
- `PreviousActionPhase`;
- `CurrentActionPhase`;
- `FailureReason`;
- `IsSuccess`.

Failure result не должен мутировать TurnCycle.

---

## 9. Жизненный цикл

### 9.1. Новый цикл

Новый `TurnCycle` начинается:

```text
TurnNumber = 1
Stage = Planning
CurrentActionPhase = null
```

Допускается создание с известного положительного номера хода.

### 9.2. Confirm Planning

Успешный:

```text
Planning
→ ActionResolution
CurrentActionPhase = 1
```

Повторное подтверждение возвращает:

```text
PlanningAlreadyConfirmed
```

### 9.3. Advance Action Phase

Если текущая фаза не последняя:

```text
Phase N
→ Phase N+1
```

Если фаза последняя:

```text
ActionResolution / Phase N
→ TurnEnd
CurrentActionPhase = null
```

Попытка advance вне ActionResolution возвращает:

```text
ActionResolutionNotActive
```

### 9.4. Begin Next Turn

Операция разрешена только из `TurnEnd`.

Успешный переход:

- увеличивает `TurnNumber`;
- переводит Stage в `Planning`;
- оставляет `CurrentActionPhase = null`;
- сохраняет `ActionPhaseCount`;
- заменяет participant snapshot.

Snapshot следующего хода передаётся извне.

`TurnCycle` не читает Bastion самостоятельно.

---

## 10. Orders и Activities — граница Stage 12

Order и Activity не являются частью временного ядра.

Order — конечное или многоэтапное намеренное действие.

Activity — постоянная деятельность бригады, выполняемая в свободные от blocking Order фазы.

Базовое правило будущего resolver:

```text
есть blocking Order
→ Order tick
→ Activity не получает tick

blocking Order отсутствует
→ Activity tick
```

Подробная модель планирования описана в `15_TurnPlanning.md`.

Разрешение Action Phase описано в `16_OrderResolution.md`.

---

## 11. Длительные воздействия

Если воздействие полностью разрешается внутри одной Action Phase, его визуальное прохождение может оставаться Presentation-анимацией.

Если объект или воздействие:

- переживает границу Action Phase;
- может быть обнаружено;
- способно изменить последующие решения игрока;

оно должно существовать в Simulation.

Пример:

```text
артиллерийский снаряд выпущен
→ остаётся несколько фаз до попадания
→ игрок получает возможность отреагировать
→ снаряд достигает цели
```

Persistent world processes вводятся после появления реального игрового применения.

---

## 12. Инварианты Stage 12

После Stage 12 публичный API должен сохранять только следующие комбинации:

```text
Planning
CurrentActionPhase = null
```

или:

```text
ActionResolution
CurrentActionPhase ∈ [1..ActionPhaseCount]
```

или:

```text
TurnEnd
CurrentActionPhase = null
```

Пустой ход допустим.

`ActionPhaseCount >= 1`.

Несколько последовательных пустых ходов детерминированы.

Одинаковое исходное состояние создаёт одинаковую последовательность наблюдаемых состояний.

---

## 13. Завершение Stage 12

Stage 12 завершён.

Подтверждено:

- полный жизненный цикл хода;
- произвольное положительное число Action Phase;
- неизменяемый participant snapshot;
- свежий snapshot следующего хода;
- typed transition results;
- отсутствие Unity-зависимостей;
- отсутствие зависимости TurnCycle от Bastion;
- воспроизводимость пустых ходов.

Текущая регрессионная точка:

```text
214 EditMode tests passed
```

Следующий этап:

```text
Stage 13.1
TurnPlanDraft foundation
```
