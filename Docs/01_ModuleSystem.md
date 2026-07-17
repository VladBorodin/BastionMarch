# TDD-01. Система модулей

## Назначение

Бастион строится из функционально законченных модулей-отсеков. Игрок не размещает отдельные станки, двигатели, кровати и стойки: они считаются частью конструкции выбранного модуля.

## ModuleDefinition

Неизменяемое описание из каталога:

- Id;
- NameLocalizationKey;
- Category;
- Type;
- GridSize;
- MassKg;
- Cost;
- MaxDurability;
- пороги повреждения;
- IdlePowerConsumption;
- ActivePowerConsumption;
- HeatGeneration;
- CrewRequirement;
- Features.

Пользовательский текст не хранится в Simulation.

## ModuleInstance

Конкретный установленный отсек хранит:

- Guid;
- определение;
- позицию;
- текущую прочность;
- техническое состояние;
- контроль;
- RequestedPowerMode;
- EffectivePowerMode;
- приоритет питания;
- OccupyingBrigadeIds;
- WorkingBrigadeIds.

## Персонал

CrewRequirement агрегирует требования по WorkType:

- MinimumPersonnel;
- OptimalPersonnel;
- MaximumUsefulPersonnel.

MaximumUsefulPersonnel — полезные рабочие места, а не вместимость бастиона. Переполнение считается отдельно по всем людям в отсеке.

## Функциональные особенности

Реализованы:

- PropulsionFeatureDefinition;
- PowerGenerationFeatureDefinition;
- RepairSupportFeatureDefinition;
- AmmoStorageFeatureDefinition;
- CrewAccommodationFeatureDefinition;
- VentilationFeatureDefinition.

## Техническое и операционное состояние

TechnicalState:

- Operational;
- Damaged;
- Critical;
- Destroyed.

Исправный модуль может не работать из-за персонала, энергии, контроля, ресурсов или будущих опасных состояний.

ControlState:

- Friendly;
- Contested;
- Occupied.

## Текущий каталог

Реализованы:

1. малый машинный отсек;
2. большой машинный отсек;
3. генераторный отсек;
4. ремонтный отсек;
5. склад боеприпасов;
6. жилой отсек;
7. вентиляционный отсек.

Числа прототипные.

## Модернизации и автоматизация

Модернизация является конкретным инженерным изменением, а не уровнем качества.

Она должна описывать совместимость, объём, массу, энергопотребление, тепло, ремонтопригодность, надёжность, изменение работ, функции и ограничения.

Ручное орудие не становится автоматическим универсальным флагом. Для автоматизации нужны реальные механизмы, питание, датчики, совместимые боеприпасы и система управления.
