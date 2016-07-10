using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

namespace DiscordSharpTest
{
    public class WarframeDataContext : DbContext
    {
        public DbSet<WarframeItemCategory> Categories { get; set; }
        public DbSet<WarframeItem> WarframeItems { get; set; }
        public DbSet<ItemCategoryAssociation> ItemCategoryAssociations { get; set; }
        public DbSet<SolarNode> SolarNodes { get; set; }
        public DbSet<WFMiscIgnoreSettings> WFMiscIgnoreOptions { get; set; }
        public DbSet<WFVoidFissure> WFVoidFissures { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "WarframeData.db" };
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
}
