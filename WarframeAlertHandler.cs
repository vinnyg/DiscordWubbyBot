using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharp.Objects;

namespace DiscordSharpTest
{
    class WarframeAlertHandler : IComparable
    {
        WarframeAlert _alert;
        DiscordMessage _associatedMessage;

        public WarframeAlertHandler(WarframeAlert warframeAlert, DiscordMessage message)
        {
            _alert = warframeAlert;
            _associatedMessage = message;
        }

        public int CompareTo(object obj)
        {
            WarframeAlertHandler alert = obj as WarframeAlertHandler;
            return this._alert.ExpireTime.CompareTo(obj);
        }
    }
}
