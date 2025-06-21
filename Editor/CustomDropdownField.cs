using System;
using System.ComponentModel;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace PolymorphicInspector.Editor
{
    //A stripped down version of PopupField
    internal class CustomDropdownField : VisualElement
    {
        private Label labelElement;
        private Label textElement;
        private VisualElement button;

        //Used for aligning the button with other Unity fields
        private VisualElement m_CachedContextWidthElement;
        private VisualElement m_CachedInspectorElement;

        internal Action OnClicked;

        internal CustomDropdownField(string label)
        {
            AddToClassList("unity-base-field");
            AddToClassList("unity-base-field__inspector-field");
            focusable = true;

            labelElement = new Label(label);
            labelElement.AddToClassList("unity-base-field__label");
            labelElement.AddToClassList("unity-label");
            Add(labelElement);

            button = new VisualElement();
            button.AddToClassList("unity-base-field__input");
            button.AddToClassList("unity-base-popup-field__input");
            Add(button);

            textElement = new Label();
            textElement.pickingMode = PickingMode.Ignore;
            textElement.AddToClassList("unity-base-popup-field__text");
            button.Add(textElement);

            VisualElement arrowElement = new VisualElement();
            arrowElement.pickingMode = PickingMode.Ignore;
            arrowElement.AddToClassList("unity-base-popup-field__arrow");
            button.Add(arrowElement);

            //Register click callbacks so the drawer can open the dropdown
            button.RegisterCallback<ClickEvent>(OnClickEvent);
            RegisterCallback<NavigationSubmitEvent>(OnNavSubmitEvent);

            //Allow the button to align with other Unity fields
            RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        internal void SetText(string text)
        {
            Assert.IsNotNull(textElement);
            textElement.text = text;
        }

        private void OnClickEvent(ClickEvent evt)
        {
            OnClicked.Invoke();
        }
        private void OnNavSubmitEvent(NavigationSubmitEvent evt)
        {
            OnClicked.Invoke();
        }

        internal Rect GetButtonRect()
        {
            Assert.IsNotNull(button);
            return button.worldBound;
        }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
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

            RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
        }

        private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            AlignLabel();
        }

        //Copied from BaseField
        private void AlignLabel()
        {
            float labelExtraPadding = 37f;
            float num = base.worldBound.x - m_CachedInspectorElement.worldBound.x - m_CachedInspectorElement.resolvedStyle.paddingLeft;
            labelExtraPadding += num;
            labelExtraPadding += base.resolvedStyle.paddingLeft;
            float a = 123f - num - base.resolvedStyle.paddingLeft;
            VisualElement visualElement = m_CachedContextWidthElement ?? m_CachedInspectorElement;
            labelElement.style.minWidth = Mathf.Max(a, 0f);
            float num2 = Mathf.Ceil(visualElement.resolvedStyle.width * 0.45f) - labelExtraPadding;
            if (Mathf.Abs(labelElement.resolvedStyle.width - num2) > 1E-30f)
            {
                labelElement.style.width = Mathf.Max(0f, num2);
            }
        }
    }
}
