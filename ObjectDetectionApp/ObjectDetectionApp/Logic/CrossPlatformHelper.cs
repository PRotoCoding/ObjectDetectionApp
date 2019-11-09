using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDetectionApp.Logic
{
    public interface ICrossPlatform { }

    public class MultipleImplementationException : Exception
    {
        public MultipleImplementationException() : base() { }
        public MultipleImplementationException(string message) : base(message) { }
    }

    /// <summary>
    /// Provides a generic class to make platform specific implementations accessible to PCL
    /// </summary>
    /// <typeparam name="T">interface type that realizes ICrossPlatform</typeparam>
    public static class CrossPlatformHelper<T> where T : ICrossPlatform
    {
        private static T instance;
        /// <summary>
        /// Gets a reference to the platform specific interface implementation
        /// </summary>
        public static T Instance {
            get {
                if (instance == null)
                    throw new NotImplementedException("An instance of " + typeof(T).ToString() + " was not implemented yet.");
                else
                    return instance;
            }
            private set { instance = value; }
        }
        public static bool IsInstanciated { get => instance == null ? false : true; }

        public static event EventHandler<T> ImplementationAdded;

        /// <summary>
        /// Adds an implementation of the cross platform interface type. This function has to be called from platform specific project.
        /// Only one implementation of the given type can be added, otherwise a MultipleImplementationException is thrown.
        /// </summary>
        /// <param name="implementation">Instance that implements the given interface type</param>
        public static void AddImplementation(T implementation)
        {
            if(IsInstanciated)
                throw new MultipleImplementationException("An Implementation of interface " + typeof(T).ToString() + " was already added.");
            else
            {
                Instance = implementation;
                ImplementationAdded?.Invoke(typeof(CrossPlatformHelper<T>), Instance);
            }
        }
    }
}
