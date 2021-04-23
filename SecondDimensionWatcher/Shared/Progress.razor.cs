using Microsoft.AspNetCore.Components;

namespace SecondDimensionWatcher.Shared
{
    public partial class Progress
    {
        [Parameter] public string Class { get; set; }

        [Parameter] public double Value { get; set; }

        public string Style => $"width: {Value:P}";
    }
}