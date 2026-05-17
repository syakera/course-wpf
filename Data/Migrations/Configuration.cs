using System.Data.Entity.Migrations;
using MedicalCenter.Data;

namespace MedicalCenter.Data.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<MedicalCenterDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
            ContextKey = "MedicalCenter.Data.MedicalCenterDbContext";
        }
    }
}
