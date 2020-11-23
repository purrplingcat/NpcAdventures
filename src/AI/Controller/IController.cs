using PurrplingCore.Internal;
using StardewModdingAPI.Events;
using StardewValley.Monsters;

namespace NpcAdventure.AI.Controller
{
    internal interface IController : IUpdateable
    {
        bool IsIdle { get; }
        void Activate();
        void Deactivate();
        void SideUpdate(UpdateTickedEventArgs e);
    }
}
