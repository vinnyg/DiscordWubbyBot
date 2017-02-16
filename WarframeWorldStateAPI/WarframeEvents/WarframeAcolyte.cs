using System;

namespace WarframeWorldStateAPI.WarframeEvents
{
    public class WarframeAcolyte : WarframeEvent
    {
        public string Name { get; private set; }
        public float Health { get; private set; }
        public int RegionIndex { get; private set; }
        
        public bool IsDiscovered
        {
            get
            {
                return _isDiscovered;
            }

            set
            {
                if (!_isDiscovered)
                    _hasBeenLocatedFlag = value;

                _isDiscovered = value;
            }
        }

        //Used to identify if the acolyte has been located again
        private bool _isDiscovered = false;
        private bool _hasBeenLocatedFlag { get; set; }

        public WarframeAcolyte(string guid, string name, string destination, float health, int regionIndex, bool isDiscovered) : base(guid, destination, DateTime.Now)
        {
            Name = name;
            Health = health;
            RegionIndex = regionIndex;
            IsDiscovered = isDiscovered;
        }

        public void UpdateLocation(string newDestinationName)
        {
            SetDestinationNode(newDestinationName);
        }
        public void UpdateHealth(float newHealth)
        {
            Health = newHealth;
        }

        public override bool IsExpired()
        {
            return (Health <= 0);
        }

        public bool IsLocated()
        {
            var result = _hasBeenLocatedFlag;
            _hasBeenLocatedFlag = false;
            return result;
        }
    }
}
