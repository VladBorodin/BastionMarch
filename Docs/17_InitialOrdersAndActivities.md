# TDD-17. Первые Orders и Activities

## 1. Назначение

Документ фиксирует минимальный gameplay-срез Stage 15.

Цель:

```text
Planning
→ MoveBrigadeOrder
→ Action Phase move
→ следующая свободная фаза
→ Activity tick
→ TurnEnd
```

Это первый случай, где новая turn architecture реально меняет Bastion.

---

## 2. Атомарное перемещение Brigade

Текущий Bastion предоставляет:

- `TryStopBrigadeWork`;
- `TryUndeployBrigade`;
- `TryDeployBrigadeToModule`.

Не использовать их внешнюю последовательность как Move:

```text
StopWork
→ Undeploy
→ Deploy
```

Причины:

- возможна partial mutation;
- можно потерять исходное placement;
- traversal не является частью операции;
- StopWork не обязан быть успешным для idle Brigade;
- появляется риск teleport semantics.

Нужна атомарная доменная операция.

---

## 3. BrigadeMoveAssessment

Перед mutation read-only assessment проверяет:

- Brigade существует;
- Brigade не расформирована;
- Brigade deployed;
- source module существует;
- target module существует;
- target является соседним;
- существует подходящий Passage;
- направление traversal разрешено;
- текущий passage state допускает переход;
- дополнительные ограничения M1 выполнены.

Assessment не изменяет Bastion.

---

## 4. Atomic move operation

Mutating operation должна либо выполнить переход полностью, либо не изменить состояние.

Успех включает:

1. повторную проверку preconditions;
2. прекращение прежней work/activity-семантики;
3. удаление occupancy source;
4. установку occupancy target;
5. обновление brigade location;
6. согласованное состояние `WorkingBrigadeIds`.

Не оставлять Brigade в промежуточном undeployed state.

---

## 5. MoveBrigadeOrder

Первая версия:

- Brigade-scoped;
- один соседний переход;
- `RequiredPhases = 1`.

Order хранит identity, необходимые для revalidation.

Минимально:

- `OrderId`;
- `BrigadeId`;
- source expectation;
- target module;
- Passage identity или достаточный набор данных для однозначной проверки.

Точный набор source/passage fields определяется при реализации, исходя из существующего traversal API.

Длинный маршрут M1 разбивается на последовательность соседних Move Orders.

---

## 6. Movement duration

Для M1:

```text
one adjacent module transition
= one Action Phase
```

Это прототипное правило, а не вечное ограничение engine.

Общий Order contract уже допускает `RequiredPhases > 1`.

---

## 7. Activity после Move

Move прекращает прежнюю Activity.

Не вшивать:

```text
Move completed
→ TryStartBrigadeWork()
```

как универсальное правило.

После перемещения:

1. прежняя Activity прекращена;
2. система определяет доступную Activity нового контекста;
3. однозначная default Activity может быть выбрана;
4. иначе Brigade остаётся без специальной Activity/в standby semantics.

Location и Activity остаются разными понятиями.

---

## 8. ModuleWorkActivity

Первая Activity M1 использует уже существующую work model.

Existing source of truth:

- `Bastion._workingBrigadeIds`;
- `ModuleInstance.WorkingBrigadeIds`.

Existing operations:

- `TryStartBrigadeWork`;
- `TryStopBrigadeWork`.

`ModuleWorkActivity` не создаёт третий mutable registry.

Она является игровой интерпретацией существующего operational work state.

---

## 9. Working не равно Activity framework

`IsWorking` означает текущую работу в модуле и уже влияет на staffing/work efficiency.

Будущие Activity могут отличаться:

- CommandWork;
- WeaponCrew;
- DefensivePreparation;
- Firefighting;
- Repair task;
- Logistics.

Поэтому:

```text
ModuleWorkActivity uses Working
```

не означает:

```text
all Activities == IsWorking
```

---

## 10. Пустая Brigade phase

Отдельный `WaitOrder` для M1 не требуется.

Если blocking Order отсутствует:

```text
Activity tick
```

Если Activity отсутствует:

```text
нет специального эффекта
```

Это не ошибка планирования.

---

## 11. Минимальный интеграционный сценарий

Исходно:

```text
Brigade #1
Location = Module A
Activity = ModuleWorkActivity
```

Planning:

```text
Phase 1:
Move A → B

Phase 2:
no blocking Order
```

Execution:

```text
Phase 1
→ revalidate Move
→ atomic Move A → B
→ previous work/activity stops

Phase 2
→ no blocking Order
→ valid/default Activity of new context receives tick
```

TurnEnd:

- location = B;
- operational state согласован;
- staffing/work state соответствует фактической Activity.

---

## 12. Presentation boundary

Stage 15 остаётся Simulation-first.

Presentation позднее получает:

- confirmed plan;
- movement result;
- route/path data, если требуется визуализации;
- activity/result state.

Анимация движения не определяет момент Simulation mutation.

---

## 13. Non-goals Stage 15

Не реализовывать:

- свободное перемещение внутри модуля;
- pathfinding отдельных человечков;
- combat initiative;
- boarding;
- weapon fire;
- defensive preparation;
- StatusEffect;
- final animation system.

---

## 14. Критерий завершения Stage 15

Stage 15 завершён, когда:

1. существует read-only BrigadeMoveAssessment;
2. Bastion умеет атомарно переместить Brigade через допустимый соседний Passage;
3. MoveBrigadeOrder исполняется через turn resolver;
4. Move занимает одну Action Phase M1;
5. прежняя Activity корректно прекращается;
6. ModuleWorkActivity использует существующее work state без нового duplicate registry;
7. свободная следующая фаза вызывает Activity tick;
8. интеграционный сценарий Move → Activity → TurnEnd детерминирован и покрыт EditMode-тестами.

### Прогресс этапа 15

- [x] 15.1. BrigadeMoveAssessment и атомарный move.
- [x] 15.2. MoveBrigadeOrder.
- [x] 15.3. Activity transition после move.
- [x] 15.4. ModuleWorkActivity.
- [x] 15.5. Activity tick в свободной фазе.
- [x] 15.6. Интеграционный ход Move → default Activity → TurnEnd.

### Реализация Stage 15.2

Добавлен immutable `MoveBrigadeOrder : BrigadeTurnOrder`.

Поля:

- OrderId;
- BrigadeId;
- SourceModuleId;
- TargetModuleId;
- PassageId;
- RequiredPhases = 1.

Order не хранит ссылки на Bastion, ModuleInstance
или ModulePassage.

Создание Order проверяет только корректность identity.

Существование и доступность перехода, текущее расположение
бригады и возможность перемещения проверяются
при исполнении через Bastion.AssessBrigadeMove().

MoveBrigadeOrder не выполняет world mutation.

Подключение к TurnPhaseResolver относится к Stage 15.3.

### Stage 15.3b.1 — Brigade movement batch

Добавлен Bastion.CommitAssessedBrigadeMoves.

Пакет принимает только allowed BrigadeMoveAssessment,
не допускает повторного перемещения одной бригады
и повторно проверяет все переходы до первой mutation.

При невалидном пакете Bastion не изменяется.

В M1 несколько бригад могут занимать один отсек,
поэтому перемещения разных бригад пока не конфликтуют
по вместимости.

Одиночное и пакетное перемещение используют
общую доменную реализацию.

Подключение пакетного перемещения к TurnPhaseResolver,
обработка Failed execution outcomes и журналирование
относятся к Stage 15.3b.2.

### Stage 15.3b.2 — Move execution integration

MoveBrigadeOrder подключён к TurnPhaseResolver.

Перед world mutation выполняются:

- universal execution assessment;
- BrigadeMoveAssessment;
- conflict resolution;
- подготовка execution outcomes на копии registry;
- формирование immutable PhaseResolutionResult.

Разрешённые Move Orders передаются в
Bastion.CommitAssessedBrigadeMoves.

Успешное перемещение получает Completed.
Доменный отказ получает Failed без progress tick.

Отказы записываются в журнал с BrigadeMoveFailureReason
и ModulePassageTraversalFailureReason.

Blocking reservation подавляет Activity в занятой фазе
даже при Failed Move.

После успешной пакетной операции продвигается TurnCycle
и публикуется подготовленный OrderExecutionRegistry.

Гарантии относятся к текущим доменным операциям M1
и однопоточному исполнению Simulation. Универсального
rollback для любых world mutations пока нет.

### Stage 15.4 — ModuleWorkActivity

Добавлены ModuleWorkActivity и ModuleWorkActivityResolver.

ModuleWorkActivity — immutable представление текущей работы
бригады в конкретном модуле.

ModuleWorkActivityResolver читает состояние Bastion через
TryGetBrigadeOperationalState и TryGetBrigadeLocation.

Activity существует только для живой, размещённой
и фактически работающей бригады.

Источником истины остаются Bastion._workingBrigadeIds
и ModuleInstance.WorkingBrigadeIds.

Новый Activity registry не создаётся.

После перемещения прежняя работа прекращается.
Работа в целевом отсеке автоматически не начинается.

Фактическое исполнение Activity tick и интеграционный
сценарий Move → Activity → TurnEnd относятся к Stage 15.5.

### Stage 15.5.1 — ModuleWorkActivity tick

ModuleWorkActivityTickProcessor подтверждает продолжение
фактической работы бригады в свободной Action Phase.

Рабочий tick фиксирует BrigadeId, ModuleId, ActionPhase
и WorkingPersonnel.

PhaseResolutionResult различает SelectedActivityCount
и ModuleWorkTickCount.

Tick не создаёт дополнительный mutable Activity registry,
не запускает работу автоматически и пока не начисляет
ресурсы, ремонт или опыт.

После Move прежняя работа прекращается.

Если Activity не выбрана, следующая свободная фаза
оставляет Brigade в standby.

Политика выбора новой default Activity после перемещения
остаётся отдельным решением Stage 15.5.

### Stage 15.5.2 — Default ModuleWork eligibility

Добавлена read-only политика ModuleWorkDefaultPolicy.

Она использует существующие BrigadeWorkProfileCatalog,
WorkType и CrewRequirement.

Для M1 default ModuleWork предлагается только idle
бригаде, если модуль содержит ровно один подходящий
вид профильной работы с affinity 100%.

При отсутствии профильной работы или неоднозначном
выборе автоматическое назначение не выполняется.

Assessment не запускает работу и не меняет Bastion.

Следующий шаг — подключение default Activity к свободной
Action Phase с повторной проверкой условий и согласованной
публикацией результатов.

### Stage 15.5.3 — Default ModuleWork execution

TurnPhaseResolver поддерживает опциональную default work policy
через BrigadeWorkProfileCatalog.

Если у свободной бригады уже есть Activity, она имеет
приоритет. Иначе default policy может выбрать
единственную профильную ModuleWorkActivity.

Выбор default Activity осуществляется после проверки
blocking reservations и не создаёт самостоятельный Order.

Начало работы и ModuleWorkActivity tick подготавливаются
для текущей Action Phase.

Bastion.CommitBrigadePhaseActions проверяет весь пакет
перемещений и начал работы до первой mutation.

После успешного commit:
- рабочее состояние хранится только в Bastion;
- ModuleWorkActivityTick фиксируется в PhaseResolutionResult;
- TurnCycle продвигается на следующую фазу.

MoveBrigadeOrder сам не запускает работу.
Default work возможна только в свободной фазе.

MaximumUsefulPersonnel не является жёсткой вместимостью
для M1. Распределение ограниченных рабочих мест
потребует отдельного правила конфликтов.