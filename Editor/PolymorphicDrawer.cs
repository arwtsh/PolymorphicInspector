using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace PolymorphicInspector.Editor
{
    [CustomPropertyDrawer(typeof(PolymorphicAttribute))]
    internal class PolymorphicDrawer : PropertyDrawer
    {
        private const string NULL_SELECTED = "<null>";
        private const string MULTIPLE_SELECTED = "—";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(property.serializedObject.targetObject))
            {
                SerializationUtility.ClearAllManagedReferencesWithMissingTypes(property.serializedObject.targetObject);
            }

            //Checks if the current reference stored by SerializedProperty is compatable with the variable this property drawer represents.
            //If the Type of the variable changes, the SerializedProperty doesn't clear the old value.
            if (!IsValidReference(property))
            {
                property.managedReferenceValue = null;
            }

            VisualElement container = new();

            //If the property is invalid for a polymorphic inspector, just draw the standard property.
            if (property.propertyType != SerializedPropertyType.ManagedReference) // TypeSelectors only work on non-monobehavior types
            {
                container.Add(new Label($"WARNING: Invalid use of {nameof(PolymorphicAttribute)}")
                {
                    style = { color = Color.red }
                });
                container.Add(new PropertyField(property));
                
                return container;
            }

            TypeField selector = new TypeField(property.displayName, GetParentType());
            container.Add(selector);

            // Create the container to hold the property's fields
            VisualElement propertyContentContainer = new VisualElement();

            //Style the content so it is indented property and looks spererated from everything else.
            propertyContentContainer.AddToClassList("unity-foldout__content");
            propertyContentContainer.AddToClassList("unity-collection-view--with-border");
            propertyContentContainer.style.borderTopWidth = 0;
            propertyContentContainer.style.borderRightWidth = 0;
            propertyContentContainer.style.borderBottomRightRadius = 0;
            propertyContentContainer.style.borderTopLeftRadius = 0;
            propertyContentContainer.style.borderTopRightRadius = 0;
            propertyContentContainer.style.paddingBottom = 2;
            propertyContentContainer.style.paddingLeft = 15;

            container.Add(propertyContentContainer);

            //Using PropertyField adds support for subclasses having their own CustomPropertyDrawers.
            PropertyField subclassProperty = new PropertyField(property, GetTypeName(GetSelectedType(property)));
            propertyContentContainer.Add(subclassProperty);

            //Set the initial state of everything
            UpdateVisuals(property, selector, propertyContentContainer, subclassProperty);

            //Whenever the user changes the type, update everything.
            //We can't rely on Unity to redraw the inspector, since it sometimes decides not to even after ApplyModifiedProperties gets called.
            selector.OnTypeSelected = value =>
            {
                UpdateValue(value, property);
                UpdateVisuals(property, selector, propertyContentContainer, subclassProperty);
            };

            return container;
        }

        private System.Type GetSelectedType(SerializedProperty p)
        {
            var managedReferenceValue = p.managedReferenceValue;
            if (managedReferenceValue != null)
            {
          		return managedReferenceValue.GetType();
            }

            return null;
        }

        private string GetTypeName(System.Type type)
        {
            if (type == null)
		    {
			    return NULL_SELECTED;
		    }
            else if (type.GetCustomAttribute(typeof(PolymorphicOverrideAttribute), false) is PolymorphicOverrideAttribute nameAttribute)
            {
                if (!string.IsNullOrEmpty(nameAttribute.Name))
                    return nameAttribute.Name;
                else
                    return type.Name;
            }
            else
            {
                return type.Name;
            }
        }

        //For whatever reason, even though the property drawer applies to elements of a list instead of the list itself, fieldInfo.FieldType would be of type List<>
        private System.Type GetParentType()
        {
            System.Type fieldType = fieldInfo.FieldType;
            
            // Check if it's a generic collection type
            if (fieldType.IsConstructedGenericType)
            {
                System.Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();

                // Handle common collection types
                if (genericTypeDefinition == typeof(List<>) ||
                    genericTypeDefinition == typeof(IList<>) ||
                    genericTypeDefinition == typeof(ICollection<>) ||
                    genericTypeDefinition == typeof(IEnumerable<>))
                {
                    return fieldType.GetGenericArguments()[0];
                }
            }

            // Check if it's an array
            if (fieldType.IsArray)
            {
                return fieldType.GetElementType();
            }

            return fieldType;
        }

        private void UpdateVisuals(SerializedProperty property, TypeField button, VisualElement propertyContainer, PropertyField propertyField)
        {
            System.Type type = GetSelectedType(property);
            bool hasMultipleDifferentValues = HasMultipleDifferentValues(property);

            if(type != null && !hasMultipleDifferentValues)
            {
                propertyContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                propertyContainer.style.display = DisplayStyle.None;
            }

            if (hasMultipleDifferentValues)
            {
                button.SetText(MULTIPLE_SELECTED);
            }
            else
            {
                string typeName = GetTypeName(type);
                button.SetText(typeName);
                propertyField.label = typeName;
            }
        }

        //Can't use default property.hasMultipleDifferentValues since the type of the object determines if its the same, not if its the same object.
        private bool HasMultipleDifferentValues(SerializedProperty property)
        {
            bool hasMultipleDifferentValues = false;

            UnityEngine.Object[] objects = property.serializedObject.targetObjects;
            if (objects.Length > 1) //It is not editing multiple objects otherwise
            {
                bool firstTypeSet = false;
                System.Type firstType = null; //All of the types will be checked agains the first type, if any are different we immediately know there are different values.

                //Loop through each object that's being edited.
                for (int i = 0; i < objects.Length && !hasMultipleDifferentValues; i++) //Exits if hasMultipleDifferentValues is true;
                {
                    UnityEngine.Object targetObject = objects[i];

                    SerializedObject individualObject = new SerializedObject(targetObject);
                    SerializedProperty individualProperty = individualObject.FindProperty(property.propertyPath);

                    System.Type thisType = null;
                    if(individualProperty.managedReferenceValue != null)
                    {
                        thisType = individualProperty.managedReferenceValue.GetType();
                    }

                    if (!firstTypeSet) //Set the first type
                    {
                        firstTypeSet = true;
                        firstType = thisType;
                    }
                    else
                    {
                        if (!firstType.Equals(thisType))
                        {
                            Debug.Log("FirstType: " + firstType + "\nThisType: " + thisType);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void UpdateValue(System.Type type, SerializedProperty property)
        {
            foreach (UnityEngine.Object targetObject in property.serializedObject.targetObjects) //Suport for multi-object editing
            {
                SerializedObject individualObject = new SerializedObject(targetObject);
                SerializedProperty individualProperty = individualObject.FindProperty(property.propertyPath);

                if (type != null)
                {
                    //Create an object using reflection, must have default constructor.
                    Assert.IsTrue(type.IsValueType || type.GetConstructor(System.Type.EmptyTypes) != null, "Type " + type.Name + " was selected for a TypeSelector in the inspector but it doesn't have a default constructor.");
                    
                    object result = null;

                    //Try to create the object from json
                    //This allows some persistance when changing types. If both types have a field named "Description" then description shouldn't change.
                    if (property.managedReferenceValue != null)
                    {
                        string json = JsonUtility.ToJson(property.managedReferenceValue);
                        result = JsonUtility.FromJson(json, type);
                    }

                    //If json failed, create from scratch.
                    if (result == null)
                    {
                        result = System.Activator.CreateInstance(type);
                    }

                    individualProperty.managedReferenceValue = result;
                }
                else
                {
                    individualProperty.managedReferenceValue = null;
                }

                //This will redraw the entire drawer again.
                individualObject.ApplyModifiedProperties();
            }
        }

        private bool IsValidReference(SerializedProperty property)
        {
            System.Type parentType = GetParentType();
            Assert.IsNotNull(parentType, "The parent type can't be null."); //It shouldn't be null because GetParentType should retrieve the type of the field, not value.

            System.Type childType; 
            if(property.managedReferenceValue != null)
            {
                childType = property.managedReferenceValue.GetType();
            }
            else
            {
                return true; //No selection is a valid choice for everything.
            }

            return parentType.IsAssignableFrom(childType);
        }
    }
}
