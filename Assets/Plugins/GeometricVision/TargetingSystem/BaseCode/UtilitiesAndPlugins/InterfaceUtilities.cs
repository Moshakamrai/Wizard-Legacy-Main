using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public static class InterfaceUtilities
    {
        public static bool ListContainsInterfaceImplementationOfType<T>(Type typeToCheck, SortedSet<T> interfaces)
        {
            bool found = false;

            foreach (var processor in interfaces)
            {
                if (processor.GetType() == typeToCheck && processor != null)
                {
                    found = true;
                }
            }

            return found;
        }


        public static void AddImplementation<TInterface, TValue>(TValue implementationToAdd, SortedSet<TInterface> implementations) where TValue : TInterface
        {

            if (ListContainsInterfaceImplementationOfType(implementationToAdd.GetType(), implementations) == false)
            {
                var dT = (TInterface) default(TValue);
                if (Equals(implementationToAdd, dT) == false)
                {
                    implementations.Add(implementationToAdd);
                }
            }
        }

        public static void AddImplementation<TInterface, TValue>(Func<ITargetVisibilityProcessor, ITargetVisibilityProcessor> functionDelegate, TValue implementationToAdd, SortedSet<TInterface> implementations) where TValue : TInterface
        {
            if (ListContainsInterfaceImplementationOfType(implementationToAdd.GetType(), implementations) == false)
            {
                var dT = (TInterface) default(TValue);
                if (Equals(implementationToAdd, dT) == false)
                {
                    implementations.Add(implementationToAdd);
                }
            }

            functionDelegate.Invoke((ITargetVisibilityProcessor) implementationToAdd);
        }

        public static void RemoveInterfaceImplementationsOfTypeFromList<T>(ref SortedSet<ITargetCreator> targetCreators, TargetingSystemComparers.TargetCreatorComparer comparer)
        {
            SortedSet<ITargetCreator> tempList = new SortedSet<ITargetCreator>(comparer);
            
            foreach (var implementation in targetCreators)
            {
                if (implementation.GetType() != typeof(T))
                {
                    tempList.Add(implementation);
                }
            }

            targetCreators.Clear();
            targetCreators = tempList;
        }

        public static void RemoveInterfaceImplementationsOfTypeFromList<T>(
            ref SortedSet<ITargetCreator> implementations, IComparer<T> comparer)
        {
            SortedSet<ITargetCreator> tempList = new SortedSet<ITargetCreator>((IComparer<ITargetCreator>) comparer);
            foreach (var implementation in implementations)
            {
                if (implementation.GetType() != typeof(T))
                {
                    tempList.Add(implementation);
                }
            }

            implementations.Clear();
            implementations = tempList;
        }

        public static void RemoveInterfaceImplementationsOfTypeFromList<T>(
            ref SortedSet<ITargetVisibilityProcessor> targetVisibilityProcessors, TargetingSystemComparers.VisibilityProcessorComparer comparer)
        {
            SortedSet<ITargetVisibilityProcessor> tempList =
                new SortedSet<ITargetVisibilityProcessor>(
                    (TargetingSystemComparers.VisibilityProcessorComparer) comparer);
            foreach (var implementation in targetVisibilityProcessors)
            {
                if (implementation.GetType() != typeof(T))
                {
                    tempList.Add(implementation);
                }
            }

            targetVisibilityProcessors.Clear();
            targetVisibilityProcessors = tempList;
        }

        public static T GetInterfaceImplementationOfTypeFromList<T>(SortedSet<T> implementations)
        {
            T interfaceToReturn = default(T);
            foreach (var implementation in implementations)
            {
                if (implementation.GetType() == typeof(T))
                {
                    interfaceToReturn = implementation;
                    break;
                }
            }

            return interfaceToReturn;
        }

        public static T GetInterfaceImplementationOfTypeFromList<T>(SortedSet<ITargetCreator> targetCreators)
            where T : ITargetCreator
        {
            T interfaceToReturn = default(T);
            foreach (var implementation in targetCreators)
            {
                if (implementation.GetType() == typeof(T))
                {
                    interfaceToReturn = (T) implementation;
                    break;
                }
            }

            return interfaceToReturn;
        }

        /// <summary>
        /// Gets targetProcessor that matches the type from given targeting instruction and returns it.
        /// </summary>
        /// <param name="targetingInstructions">From these the targeting processor is searched, by geometry type</param>
        /// <param name="targetingInstructionIn">Instruction to get geometry type targeted from</param>
        /// <returns></returns>
        public static ITargetProcessor GetGameObjectTargetProcessorFromTargetingInstructionsList(List<TargetingInstruction> targetingInstructions, TargetingInstruction targetingInstructionIn)
        {
            
            ITargetProcessor interfaceToReturn = null;
            foreach (var targetingInstruction in targetingInstructions)
            {
                if ((int)targetingInstruction.GeometryType != (int)targetingInstructionIn.GeometryType ||
                     targetingInstruction.TargetProcessorForGameObjects == null)
                {
                    continue;
                }
                
                interfaceToReturn =  targetingInstruction.TargetProcessorForGameObjects;
                break;
            }

            return interfaceToReturn;
        }

        public static T GetInterfaceImplementationOfTypeFromList<T>(
            SortedSet<ITargetVisibilityProcessor> targetVisibilityProcessors)
        {
            T interfaceToReturn = default(T);
            foreach (var implementation in targetVisibilityProcessors)
            {
                if (implementation.GetType() == typeof(T))
                {
                    interfaceToReturn = (T) implementation;
                    break;
                }
            }

            return interfaceToReturn;
        }


    }
}