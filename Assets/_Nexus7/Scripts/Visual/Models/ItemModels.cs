using Nexus7.Devices;
using Nexus7.Inventory;

namespace Nexus7.Visual.Models
{
    /// <summary>Plano del modelo de un objeto del inventario (los dispositivos usan su tipo y su color).</summary>
    public static class ItemModels
    {
        public static ModelBlueprint For(ItemData item)
        {
            if (item == null) return ModelLibrary.Item(null, ItemCategory.Part, ModelBlueprint.MetalColor);
            var device = item as DeviceItemData;
            if (device != null) return ModelLibrary.Device(device.Type, ModelFactory.ToModel(device.DeviceColor));
            return ModelLibrary.Item(item.Id, item.Category, ModelFactory.ToModel(item.UiColor));
        }
    }
}
