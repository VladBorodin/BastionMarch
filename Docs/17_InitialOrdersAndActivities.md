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
