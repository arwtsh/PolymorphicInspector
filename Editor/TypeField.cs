using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PolymorphicInspector.Editor
{
    //A stripped down version of PopupField
    internal class TypeField : VisualElement
    {
        internal System.Action<System.Type> OnTypeSelected; //For some reason, RegisterValueChangedCallback doens't execute when value is set to null.

        private VisualElement labelElement;
        private VisualElement buttonElement;
        private Label buttonLabelElement;

        private AdvancedSelector selectorMenu;

        internal TypeField(string label, System.Type parentType)
        {                        
            AddToClassList("unity-base-popup-field");
            AddToClassList("unity-base-field");
            pickingMode = PickingMode.Ignore;

            labelElement = new Label(label);
            labelElement.AddToClassList("unity-base-field__label");
            labelElement.pickingMode = PickingMode.Ignore;
            Add(labelElement);

            buttonElement = new();
            buttonElement.AddToClassList("unity-base-popup-field__input");
            buttonElement.AddToClassList("unity-base-field__input");
            buttonElement.pickingMode = PickingMode.Position;
            Add(buttonElement);

            buttonLabelElement = new Label("Type Name");
            buttonLabelElement.AddToClassList("unity-base-popup-field__text");
            labelElement.pickingMode = PickingMode.Ignore;
            buttonElement.Add(buttonLabelElement);

            VisualElement arrowElement = new();
            arrowElement.AddToClassList("unity-base-popup-field__arrow");
            arrowElement.pickingMode = PickingMode.Ignore;
            buttonElement.Add(arrowElement);

            //Tells the BaseField logic to align the ButtonElement with the rest of the Unity fields.
            AddToClassList("unity-base-field__aligned");
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            selectorMenu = new AdvancedSelector(parentType, "Select Type");
            selectorMenu.OnItemSelected = TypeSelected;

            //Register click callbacks so the drawer can open the dropdown
            buttonElement.RegisterCallback<ClickEvent>(OnClickEvent);
            RegisterCallback<NavigationSubmitEvent>(OnNavSubmitEvent);
        }

        internal void SetText(string text)
        {
            Assert.IsNotNull(buttonLabelElement);
            buttonLabelElement.text = text;
        }

        private void OnClickEvent(ClickEvent evt)
        {
            ShowSelectionMenu();
        }

        private void OnNavSubmitEvent(NavigationSubmitEvent evt)
        {
            ShowSelectionMenu();
        }

        private void ShowSelectionMenu()
        {
            Assert.IsNotNull(buttonElement);
            Assert.IsNotNull(selectorMenu);

            Rect rect = buttonElement.worldBound;
            rect.width = Mathf.Max(200, rect.width); 

            selectorMenu.Show(rect);
        }

        private void TypeSelected(System.Type type)
        {
            OnTypeSelected?.Invoke(type);
        }

        /*
         *  Logic for aligning the button is copied from BaseField.
         */

        private static readonly string inspectorFieldUssClassName = "unity-base-field__inspector-field";

        private VisualElement m_CachedContextWidthElement;
        private VisualElement m_CachedInspectorElement;
        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;
        private float m_LabelBaseMinWidth;
        private static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        private static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");
        private static CustomStyleProperty<float> s_LabelBaseMinWidthProperty = new CustomStyleProperty<float>("--unity-property-field-label-base-min-width");


        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (e.destinationPanel == null || e.destinationPanel.contextType == ContextType.Player)
            {
                return;
            }

            m_CachedInspectorElement = null;
            m_CachedContextWidthElement = null;
            for (VisualElement visualElement = base.parent; visualElement != null; visualElement = visualElement.parent)
            {
                if (visualElement.ClassListContains("unity-inspector-element"))
                {
                    m_CachedInspectorElement = visualElement;
                }

                if (visualElement.ClassListContains("unity-inspector-main-container"))
                {
                    m_CachedContextWidthElement = visualElement;
                    break;
                }
            }

            if (m_CachedInspectorElement == null)
            {
                RemoveFromClassList(inspectorFieldUssClassName);
                return;
            }

            m_LabelWidthRatio = 0.45f;
            m_LabelExtraPadding = 37f;
            m_LabelBaseMinWidth = 123f;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            AddToClassList(inspectorFieldUssClassName);
            RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
        }
        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio))
            {
                m_LabelWidthRatio = labelWidthRatio;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding))
            {
                m_LabelExtraPadding = labelExtraPadding;
            }

            if (evt.customStyle.TryGetValue(s_LabelBaseMinWidthProperty, out var labelBaseMinWidth))
            {
                m_LabelBaseMinWidth = labelBaseMinWidth;
            }

            AlignLabel();
        }

        private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            AlignLabel();
        }

        private void AlignLabel()
        {
            if (ClassListContains("unity-base-field__aligned") && m_CachedInspectorElement != null)
            {
                float labelExtraPadding = m_LabelExtraPadding;
                float num = base.worldBound.x - m_CachedInspectorElement.worldBound.x - m_CachedInspectorElement.resolvedStyle.paddingLeft;
                labelExtraPadding += num;
                labelExtraPadding += base.resolvedStyle.paddingLeft;
                float a = m_LabelBaseMinWidth - num - base.resolvedStyle.paddingLeft;
                VisualElement visualElement = m_CachedContextWidthElement ?? m_CachedInspectorElement;
                labelElement.style.minWidth = Mathf.Max(a, 0f);
                float num2 = Mathf.Ceil(visualElement.resolvedStyle.width * m_LabelWidthRatio) - labelExtraPadding;
                if (Mathf.Abs(labelElement.resolvedStyle.width - num2) > 1E-30f)
                {
                    labelElement.style.width = Mathf.Max(0f, num2);
                }
            }
        }
    }
}
