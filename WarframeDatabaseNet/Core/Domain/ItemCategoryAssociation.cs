namespace WarframeDatabaseNet.Core.Domain
{
    public class ItemCategoryAssociation
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        virtual public WarframeItem Item { get; set; }
        virtual public WarframeItemCategory Category { get; set; }
    }
}
