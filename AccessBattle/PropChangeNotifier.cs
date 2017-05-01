using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AccessBattle
{
    /// <summary>
    /// Base class for classes that require the INotifyPropertyChanged interface.
    /// </summary>
    public class PropChangeNotifier : INotifyPropertyChanged
    {
        /// <summary>
        /// Property change event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="prop">Property to set.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>True if property was changed.</returns>
        protected bool SetProp<T>(
            ref T prop,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(prop, value))
                return false;
            prop = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Used to perform an action that sets a property.
        /// The action is only performed when the property needs to be changed.
        /// Automatically fires the PropertyChanged event after the action was performed.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="prop">Property to check.</param>
        /// <param name="value">Value to checl.</param>
        /// <param name="setAction">
        /// Action to perform before the PropertyChanged event is fired. 
        /// Not executed if property equals value.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>True if property was not equal to value.</returns>
        /// <example>
        /// SetProp(_phase, value,()=> { _phase = value; OnPhaseChanged(); });
        /// </example>
        protected bool SetProp<T>(
            T prop,
            T value,
            Action setAction,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(prop, value))
            {
                return false;
            }
            if (setAction != null) setAction();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
