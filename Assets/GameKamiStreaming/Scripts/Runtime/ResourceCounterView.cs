using UnityEngine;

namespace GameKamiStreaming
{
    public sealed class ResourceCounterView : MonoBehaviour
    {
        [SerializeField] int resourceId;
        [SerializeField] PixelNumberLabel numberLabel;

        public int ResourceId => resourceId;
        public PixelNumberLabel NumberLabel => numberLabel;

        public void Configure(int id, PixelNumberLabel label)
        {
            resourceId = id;
            numberLabel = label;
        }
    }
}
