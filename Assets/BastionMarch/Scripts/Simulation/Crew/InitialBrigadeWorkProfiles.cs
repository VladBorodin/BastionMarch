using System.Collections.Generic;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Прототипная матрица пригодности бригад.
    ///
    /// Числа используются для проверки архитектуры
    /// и пока не являются окончательным балансом.
    /// </summary>
    public static class InitialBrigadeWorkProfiles
    {
        public static BrigadeWorkProfileCatalog CreateCatalog()
        {
            return new BrigadeWorkProfileCatalog(
                CreateProfiles());
        }

        public static IReadOnlyList<BrigadeWorkProfile>
            CreateProfiles()
        {
            return new[]
            {
                CreateRecruitProfile(),
                CreateDriverProfile(),
                CreateOfficerProfile(),
                CreateGunnerProfile(),
                CreateMechanicProfile(),
                CreateEngineerProfile(),
                CreateSignalProfile(),
                CreateMedicalProfile(),
                CreateLogisticsProfile(),
                CreateAssaultProfile()
            };
        }

        private static BrigadeWorkProfile CreateRecruitProfile()
        {
            return CreateProfile(
                BrigadeType.Recruit,
                defaultEfficiencyPercent: 30,
                new WorkAffinityDefinition(
                    WorkType.General, 75),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 60),
                new WorkAffinityDefinition(
                    WorkType.Assault, 50),
                new WorkAffinityDefinition(
                    WorkType.Mechanical, 40),
                new WorkAffinityDefinition(
                    WorkType.Gunnery, 40));
        }

        private static BrigadeWorkProfile CreateDriverProfile()
        {
            return CreateProfile(
                BrigadeType.Driver,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Driving, 100),
                new WorkAffinityDefinition(
                    WorkType.Mechanical, 65),
                new WorkAffinityDefinition(
                    WorkType.General, 60),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 50));
        }

        private static BrigadeWorkProfile CreateOfficerProfile()
        {
            return CreateProfile(
                BrigadeType.Officer,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Command, 100),
                new WorkAffinityDefinition(
                    WorkType.Communications, 75),
                new WorkAffinityDefinition(
                    WorkType.General, 60),
                new WorkAffinityDefinition(
                    WorkType.Gunnery, 55));
        }

        private static BrigadeWorkProfile CreateGunnerProfile()
        {
            return CreateProfile(
                BrigadeType.Gunner,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Gunnery, 100),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 70),
                new WorkAffinityDefinition(
                    WorkType.General, 55),
                new WorkAffinityDefinition(
                    WorkType.Assault, 50));
        }

        private static BrigadeWorkProfile CreateMechanicProfile()
        {
            return CreateProfile(
                BrigadeType.Mechanic,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Mechanical, 100),
                new WorkAffinityDefinition(
                    WorkType.Engineering, 80),
                new WorkAffinityDefinition(
                    WorkType.General, 70),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 60));
        }

        private static BrigadeWorkProfile CreateEngineerProfile()
        {
            return CreateProfile(
                BrigadeType.Engineer,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Engineering, 100),
                new WorkAffinityDefinition(
                    WorkType.Mechanical, 85),
                new WorkAffinityDefinition(
                    WorkType.Communications, 65),
                new WorkAffinityDefinition(
                    WorkType.General, 60));
        }

        private static BrigadeWorkProfile CreateSignalProfile()
        {
            return CreateProfile(
                BrigadeType.Signal,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Communications, 100),
                new WorkAffinityDefinition(
                    WorkType.Engineering, 70),
                new WorkAffinityDefinition(
                    WorkType.Command, 60),
                new WorkAffinityDefinition(
                    WorkType.General, 55));
        }

        private static BrigadeWorkProfile CreateMedicalProfile()
        {
            return CreateProfile(
                BrigadeType.Medical,
                defaultEfficiencyPercent: 20,
                new WorkAffinityDefinition(
                    WorkType.Medical, 100),
                new WorkAffinityDefinition(
                    WorkType.General, 60),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 50));
        }

        private static BrigadeWorkProfile CreateLogisticsProfile()
        {
            return CreateProfile(
                BrigadeType.Logistics,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Logistics, 100),
                new WorkAffinityDefinition(
                    WorkType.General, 80),
                new WorkAffinityDefinition(
                    WorkType.Gunnery, 60),
                new WorkAffinityDefinition(
                    WorkType.Mechanical, 50));
        }

        private static BrigadeWorkProfile CreateAssaultProfile()
        {
            return CreateProfile(
                BrigadeType.Assault,
                defaultEfficiencyPercent: 25,
                new WorkAffinityDefinition(
                    WorkType.Assault, 100),
                new WorkAffinityDefinition(
                    WorkType.General, 80),
                new WorkAffinityDefinition(
                    WorkType.Logistics, 60),
                new WorkAffinityDefinition(
                    WorkType.Gunnery, 55));
        }

        private static BrigadeWorkProfile CreateProfile(
            BrigadeType brigadeType,
            int defaultEfficiencyPercent,
            params WorkAffinityDefinition[] affinities)
        {
            return new BrigadeWorkProfile(
                brigadeType,
                defaultEfficiencyPercent,
                affinities);
        }
    }
}