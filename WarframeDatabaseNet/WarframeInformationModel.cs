using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

namespace WarframeDatabase
{
    public class WarframeDataContext : DbContext
    {
        const string DEFAULT_DATASOURCE = "WarframeData.db";
        public WarframeDataContext(string dataSource = DEFAULT_DATASOURCE)
        {
            DataSource = dataSource;
        }

        public DbSet<WarframeItemCategory> Categories { get; set; }
        public DbSet<WarframeItem> WarframeItems { get; set; }
        public DbSet<ItemCategoryAssociation> ItemCategoryAssociations { get; set; }
        public DbSet<SolarNode> SolarNodes { get; set; }
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

    public class WarframeItemCategory
    {
        public int ID { get; set; }
        public int TypeID { get; set; }
        public string Name { get; set; }
        public int Ignore { get; set; }
        virtual public ICollection<ItemCategoryAssociation> Items { get; set; }
    }

    public class ItemCategoryAssociation
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        virtual public WarframeItem Item { get; set; }
        virtual public WarframeItemCategory Category { get; set; }
    }

    public class WarframeItem
    {
        public int ID { get; set; }
        public string ItemURI { get; set; }
        public string Name { get; set; }
        public int Ignore { get; set; }
        virtual public ICollection<ItemCategoryAssociation> Categories { get; set; }
    }

    public class SolarNode
    {
        public int ID { get; set; }
        public string NodeURI { get; set; }
        public string NodeName { get; set; }
    }

    public class WFMiscIgnoreSettings
    {
        [Key]
        public int ItemID { get; set; }
        public int MinQuantity { get; set; }
    }

    public class WFVoidFissure
    {
        [Key]
        public string FissureURI { get; set; }
        public string FissureName { get; set; }
    }

    public class WFBoss
    {
        [Key]
        public int ID { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string FactionIndex { get; set; }
    }

    public class WFRegion
    {
        [Key]
        public int ID { get; set; }
        public string RegionName { get; set; }
    }

    public class WFSortieCondition
    {
        [Key]
        public int ID { get; set; }
        public int ConditionIndex { get; set; }
        public string ConditionName { get; set; }
    }
    
    public class WFSortieMission
    {
        [Key]
        public int ID { get; set; }
        public string MissionType { get; set; }
    }

    public class WFPlanetRegionMission
    {
        [Key]
        public int ID { get; set; }
        public int RegionID { get; set; }
        virtual public WFRegion Region { get; set; }

        //The order that mission indices appear in the Warframe source per region.
        //e.g. Each region has a different set of available missions, therefore the index of each mission will be different depending on the region.
        public int JSONIndexOrder { get; set; }

        virtual public WFSortieMission Mission { get; set; }

        public int MissionID { get; set; }
    }

    public class SolarMapMission
    {
        [Key]
        public int NodeID { get; set; }
        public string MissionType { get; set; }
        public string Faction { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int RequiresArchwing { get; set; }
        public string NodeType { get; set; }
    }
}
