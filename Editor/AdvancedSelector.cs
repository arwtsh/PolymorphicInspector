using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.WSA;

namespace PolymorphicInspector.Editor
{
    internal class AdvancedSelector : AdvancedDropdown
    {
        private readonly float headerHeight = EditorGUIUtility.singleLineHeight * 2f;
        const int maxLines = 13;
        const int maxNodes = 10000; //This is a kill switch for a loop.

        public System.Action<System.Type> OnItemSelected;

        //A temp class used to generate the tree before it is compiled into the AdvancedDropdown
        private class AdvancedSelectorTreeNode
        {
            public string Name;
            public Dictionary<string, AdvancedSelectorTreeNode> Children;
            public System.Type Type;

            public AdvancedSelectorTreeNode(string name)
            {
                Name = name;
                Children = new();
            }

            /// <summary>
            /// Returns the child of this folder matching the name.
            /// If the child doesn't exist, it creates it.
            /// </summary>
            public AdvancedSelectorTreeNode GetOrAddChild(string name)
            {
                if(Children.TryGetValue(name, out AdvancedSelectorTreeNode item))
                {
                    return item;
                }
                else
                {
                    item = new AdvancedSelectorTreeNode(name);

                    Children.Add(name, item);

                    return item;
                }
            }

            public IEnumerable<AdvancedSelectorTreeNode> GetItems()
            {
                return Children.OrderBy(node => node.Key) //Sort alphabetically
                    .Where(node => node.Value.Children.Count == 0) //If it has no children, then it is an item
                    .Select(node => node.Value);
            }

            public IEnumerable<AdvancedSelectorTreeNode> GetFolders()
            {
                return Children.OrderBy(node => node.Key) //Sort alphabetically
                    .Where(node => node.Value.Children.Count > 0) //If it has any children, then it is a folder
                    .Select(node => node.Value);
            }
        }

        private class AdvancedSelectorItem : AdvancedDropdownItem
        {
            public System.Type Type;

            public AdvancedSelectorItem(string name) : base(name) { }
            public AdvancedSelectorItem(string name, System.Type type) : base(name) { Type = type; }
        }

        //Cache the root for each AdvancedSelector so it won't be re-created every time it opens.
        private AdvancedSelectorItem root;

        internal AdvancedSelector(System.Type baseType, string title) : base(new AdvancedDropdownState())
        {
            AdvancedSelectorTreeNode tree = CreateTypeTree(GetTypes(baseType));

            root = CompileAdvancedSelector(tree, new AdvancedSelectorItem(title));

            minimumSize = new Vector2(minimumSize.x, EditorGUIUtility.singleLineHeight * maxLines + headerHeight);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            return root;
        }

        private IEnumerable<System.Type> GetTypes(System.Type baseType)
        {
            return  from type in TypeCache.GetTypesDerivedFrom(baseType) //TypeCache is a Unity utility function, it works faster than System.Reflection.
                    .Concat(new[] { baseType }) // Sometimes the base type is also a valid choice
                    where !type.IsAbstract //Don't include abstract classes
                    && !type.IsGenericTypeDefinition //Don't allow undefined templates or generics. Foo<T> is not accepted, Foo<string> is.
                    && !typeof(UnityEngine.Object).IsAssignableFrom(type) //Classes inheriting from Unity types, like GameObjects and ScriptableObjects should be handled by Object references.
                    select type;
        }

        private AdvancedSelectorTreeNode CreateTypeTree(IEnumerable<System.Type> types)
        {
            AdvancedSelectorTreeNode root = new AdvancedSelectorTreeNode("");

            foreach(System.Type type in types)
            {
                string[] folders;
                string name;

                //Get the name of the type and the folder structure.
                //By default, the folder structure will be each part of a namespace (System.Collections.Generic becomes System->Collections->Generic)
                //If the user added an attribute PolymorphicOverrideAttribute with an empty name, it will use the type's name by default.
                //If the user specified an empty folder, it will place the item at the very top of the tree.
                if (type.GetCustomAttribute(typeof(PolymorphicOverrideAttribute), false) is PolymorphicOverrideAttribute nameAttribute)
                {
                    //Set name
                    if (string.IsNullOrEmpty(nameAttribute.Name))
                    {
                        name = type.Name;
                    }
                    else
                    {
                        name = nameAttribute.Name;
                    }

                    //Set folder location
                    if (nameAttribute.IsFolderSet)
                    {
                        if (!string.IsNullOrEmpty(nameAttribute.Folder))
                        {
                            folders = nameAttribute.Folder.Split('.', '/');
                        }
                        else
                        {
                            folders = System.Array.Empty<string>();
                        }
                    }
                    else
                    {
                        folders = GetTypeSeperatedNamespace(type);
                    }
                }
                else
                {
                    name = type.Name;

                    folders = GetTypeSeperatedNamespace(type);
                }

                //Create the folder structure
                AdvancedSelectorTreeNode iterator = root;
                foreach (string folder in folders)
                {
                    iterator = iterator.GetOrAddChild(folder);
                }

                //After the folder structure has been created, iterator will be the final location to place the item.
                AdvancedSelectorTreeNode child = iterator.GetOrAddChild(name);
                child.Type = type;
            }

            return root;
        }

        private AdvancedSelectorItem CompileAdvancedSelector(AdvancedSelectorTreeNode node, AdvancedSelectorItem root)
        {
            //Add the default items to the root.
            root.AddChild(new AdvancedSelectorItem("<null>", null));
            root.AddSeparator();

            //Keep track of which node to process by a stack
            Stack<(AdvancedSelectorTreeNode, AdvancedSelectorItem)> stack = new();
            stack.Push((node, root));

            for (int i = 0; i < maxNodes && stack.Count > 0; i++) //Goes until there is nothing left in the stack, or the kill switch is triggered.
            {
                var (currentNode, parentItem) = stack.Pop();

                var items = currentNode.GetItems();
                var folders = currentNode.GetFolders();
                
                foreach (AdvancedSelectorTreeNode childFolder in folders)
                {
                    AdvancedSelectorItem item = new AdvancedSelectorItem(childFolder.Name);
                    parentItem.AddChild(item);
                    stack.Push((childFolder, item));
                }

                //If there were both folders and items, add a seperator between them.
                if (items.Count() > 0 && folders.Count() > 0)
                {
                    parentItem.AddSeparator();
                }

                foreach (AdvancedSelectorTreeNode childItem in items)
                {
                    AdvancedSelectorItem item = new AdvancedSelectorItem(childItem.Name, childItem.Type);
                    parentItem.AddChild(item);

                    //If the type does not have a default constructor, don't allow the user to choose it.
                    //Otherwise it will through an error when creating the object.
                    if (!(childItem.Type.IsValueType || childItem.Type.GetConstructor(System.Type.EmptyTypes) != null))
                    {
                        item.enabled = false;
                    }
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (!item.enabled) //If its disabled, it isn't creatable by the system.
                return;

            if (item is AdvancedSelectorItem selectorItem)
            {
                OnItemSelected?.Invoke(selectorItem.Type);
            }
        }

        private string[] GetTypeSeperatedNamespace(System.Type type)
        {
            string[] result = System.Array.Empty<string>();

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                result = type.Namespace.Split('.');
            }

            return result;
        }
    }
}
