using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using WarframeDatabaseNet.Core.Domain;

namespace WarframeDatabaseNet
{
    public class WarframeDataContext : DbContext
    {
        const string DEFAULT_DATASOURCE = "WarframeData.db";
        //const string DEFAULT_DATASOURCE = @"S:\Repos\DiscordSharpTest\DiscordSharpTest\bin\Release\WarframeData.db";
        public WarframeDataContext(string dataSource = DEFAULT_DATASOURCE)
        {
            DataSource = dataSource;
        }

        public DbSet<WarframeItemCategory> Categories { get; set; }
        public DbSet<WarframeItem> WarframeItems { get; set; }
        public DbSet<ItemCategoryAssociation> ItemCategoryAssociations { get; set; }
        public DbSet<WFSolarNode> SolarNodes { get; set; }
        public DbSet<WFMiscIgnoreSettings> WFMiscIgnoreOptions { get; set; }
        public DbSet<WFVoidFissure> WFVoidFissures { get; set; }
        public DbSet<WFBoss> WFBossInfo { get; set; }
        public DbSet<WFRegion> WFRegionNames { get; set; }
        public DbSet<WFSortieMission> WFSortieMissions { get; set; }
        public DbSet<WFSortieCondition> WFSortieConditions { get; set; }
        public DbSet<WFPlanetRegionMission> WFPlanetRegionMissions { get; set; }
        public DbSet<SolarMapMission> SolarMapMissions { get; set; }
        public string DataSource { get; private set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = DataSource };
            var Connection = new SqliteConnection(connectionStringBuilder.ToString());

            optionsBuilder.UseSqlite(Connection);
        }
    }
}