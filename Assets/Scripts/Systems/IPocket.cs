using System.Collections.Generic;

namespace OurGame.Systems
{
    public interface IPocket
    {
        string Name { get; }
        int Capacity { get; }
        IReadOnlyList<InventorySlotData> Slots { get; }
    }
}
