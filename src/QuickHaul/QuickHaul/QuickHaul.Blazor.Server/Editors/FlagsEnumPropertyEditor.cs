using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using QuickHaul.Blazor.Server.Models;
using System.ComponentModel;

namespace QuickHaul.Blazor.Server.Editors
{
    [PropertyEditor(typeof(Enum), false)]
    public class FlagsEnumPropertyEditor : BlazorPropertyEditorBase
    {
        private Type _enumType;

        public FlagsEnumPropertyEditor(Type objectType, IModelMemberViewItem model)
            : base(objectType, model)
        {
            _enumType = model.ModelMember.MemberInfo.MemberType;

            var flagsAttribute = Attribute.GetCustomAttribute(_enumType, typeof(FlagsAttribute));
            if (flagsAttribute == null)
            {
                throw new InvalidOperationException(
                    $"Enum type {_enumType.Name} must have the [Flags] attribute to use FlagsEnumPropertyEditor");
            }
        }

        public override FlagsEnumCheckListModel ComponentModel
            => (FlagsEnumCheckListModel)base.ComponentModel;

        protected override IComponentModel CreateComponentModel()
        {
            var model = new FlagsEnumCheckListModel();

            // Build the list of checkbox items from enum values
            var items = new List<CheckListItem>();
            foreach (Enum enumValue in Enum.GetValues(_enumType))
            {
                var fieldInfo = _enumType.GetField(enumValue.ToString());
                var descriptionAttr = (DescriptionAttribute)Attribute.GetCustomAttribute(
                    fieldInfo, typeof(DescriptionAttribute));
                var displayName = descriptionAttr?.Description ?? enumValue.ToString();

                items.Add(new CheckListItem
                {
                    Value = Convert.ToInt32(enumValue),
                    Text = displayName,
                    IsChecked = false
                });
            }
            model.Items = items;

            model.IsReadOnly = !AllowEdit;

            // Handle value changes
            if (AllowEdit)
            {
                model.ValueChanged = EventCallback.Factory.Create<int>(this, value =>
                {
                    model.Value = value;
                    OnControlValueChanged();
                    WriteValue();
                });
            }
            return model;
        }

        protected override void ReadValueCore()
        {
            base.ReadValueCore();

            if (PropertyValue != null)
            {
                int intValue = Convert.ToInt32(PropertyValue);
                ComponentModel.Value = intValue;

                // Update checkbox states
                foreach (var item in ComponentModel.Items)
                {
                    item.IsChecked = (intValue & item.Value) == item.Value;
                }
            }

            ComponentModel.IsReadOnly = !AllowEdit;
        }

        protected override object GetControlValueCore()
            => ComponentModel.Value;

        protected override void WriteValueCore()
        {
            if (ComponentModel.Value > 0)
            {
                PropertyValue = Enum.ToObject(_enumType, ComponentModel.Value);
            }
        }

        protected override void ApplyReadOnly()
        {
            base.ApplyReadOnly();

            if (ComponentModel != null)
            {
                ComponentModel.IsReadOnly = !AllowEdit;
            }

            ComponentModel?.SetAttribute("disabled", !AllowEdit);
        }
    }
}
