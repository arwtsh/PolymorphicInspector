# Documentation

## Overview

A Unity package that adds a unique inspector field that allows a variable to be populated with a subclass.

## Package contents

- 2 Attributes
- 1 Property Drawer

## Installation Instructions

Installing this package via the Unity package manager is the best way to install this package. There are multiple methods to install a third-party package using the package manager, the recommended one is `Install package from Git URL`. The URL for this package is `https://github.com/arwtsh/PolymorphicInspector.git`. The Unity docs contains a walkthrough on how to install a package. It also contains information on [specifying a specific branch or release](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html#revision).

Alternatively, you can download directly from this GitHub repo and extract the .zip file into the folder `ProjectRoot/Packages/com.jjasundry.polymorphic-inspector`. This will also allow you to edit the contents of the package.

## Requirements

Tested on Unity version 6000.0; will most likely work on older versions, but you will need to manually port it.

## Description of Assets

The attribute [Polymorphic] tells the Unity inspector to show the variable in the inspector with a custom property drawer. The variable must also have the [SerializedReference] attribute. The variable cannot be a value type (int, float, string, Vector3, etc.) or a Unity reference (GameObject, ScriptableObject, AudioClip, etc.), it should be a class or interface you or a 3rd party created. 

```csharp
public class InspectorExample : MonoBehaviour
{
    [SerializeReference, Polymorphic]
    public BaseClass foo;
}
```

The Polymorphic system automatically finds public inheriting types, both classes and structs implemented interfaces. The attribute [PolymorphicOverride] can be added to a class to specify a UI friendly name and folder path.

The Polymorphic UI encases the default UI of the selected subclass. This allows the subclasses to display their custom UI unempeded.