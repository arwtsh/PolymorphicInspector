# Documentation

## Overview

A Unity package that adds a unique inspector field that allows a variable to be populated with a subclass.

## Package contents

- ScriptableAction ScriptableObject & custom inspector
- IActionReciever interface

## Installation Instructions

Installing this package via the Unity package manager is the best way to install this package. There are multiple methods to install a third-party package using the package manager, the recommended one is `Install package from Git URL`. The URL for this package is `https://github.com/arwtsh/ScriptableActions.git`. The Unity docs contains a walkthrough on how to install a package. It also contains information on [specifying a specific branch or release](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html#revision).

Alternatively, you can download directly from this GitHub repo and extract the .zip file into the folder `ProjectRoot/Packages/com.jjasundry.scriptable-actions`. This will also allow you to edit the contents of the package.

## Requirements

Tested on Unity version 6000.0; will most likely work on older versions, but you will need to manually port it.

## Description of Assets

This package centers around ScriptableObjects called ScriptableAction. New ScriptableActions can be created from the context menu Create->Scriptable Objects->ScriptableAction. ScriptableActions represent a type of action. However, unlike other event systems, ScriptableActions map each listener to an IActionReciever. When a ScriptableAction is invoked, an IActionReciever instance is passed in and only listers associated with that reciever are invoked. For example, there may be multiple enemies listening to the Attack ScriptableAction asset. However, each AI only invokes the enemy it controls.

ScriptableActions have three different events inside them: started, performed, and cancled. Performed is the default, and ScriptableActions use this option when adding listeners or invoking if no other ActionType was specified. Started and Cancled are for advanced action systems. A started event could tell an enemy to "start moving in this direction" and then the cancled event would tell the enemy stop moving.

ScriptableActions are able to use the [Player Input system](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/manual/index.html). Each ScriptableAction can be assigned an InputActionReference. When the function RegisterToInput is called, the ScriptableAction will automatically invoke itself whenever the input system triggers the InputActionReference. An IActionReciever still needs to be provided to RegisterToInput. A good use for this is a PlayerController has a list of ScriptableActions and calls RegisterToInput and UnregisterToInput on each of them whenever the player possesses a new pawn.

By default, only the performed action is connected with the input system. You can opt-in to have the ScriptableAction listen to the InputActionReference's started and cancled events, but started and cancled are called differently depending on the Interactions and Action Type of the InputActionRefernece. When connecting a ScriptableAction to the input system, the invoking logic is handled entirely by Unity's input system.

*TIP: [Lambdas](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions) look nice for listeners, but they are considered an anonomous function. It is difficult to unregister a lambda from an event.* 