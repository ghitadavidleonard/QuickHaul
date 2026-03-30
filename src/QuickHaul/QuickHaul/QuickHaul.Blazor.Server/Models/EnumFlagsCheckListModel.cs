using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using QuickHaul.Blazor.Server.Components;

namespace QuickHaul.Blazor.Server.Models
{
    public class FlagsEnumCheckListModel : ComponentModelBase
    {
        public int Value
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public EventCallback<int> ValueChanged
        {
            get => GetPropertyValue<EventCallback<int>>();
            set => SetPropertyValue(value);
        }

        public List<CheckListItem> Items
        {
            get => GetPropertyValue<List<CheckListItem>>();
            set => SetPropertyValue(value);
        }

        public bool IsReadOnly
        {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }

        public override Type ComponentType => typeof(FlagsEnumCheckList);
    }
}
