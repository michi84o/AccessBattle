using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class PropChangeNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

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
