using AccessBattle.Networking.Packets;
using AccessBattle.Wpf.ViewModel;
using System;
using System.ComponentModel;

namespace AccessBattle.Wpf.Interfaces
{
    public interface IMenuViewModel : INotifyPropertyChanged
    {
        IMenuHolder ParentViewModel { get; }
    }

    public interface IMenuHolder
    {
        MenuType CurrentMenu { get; set; }
        GameViewModel Game { get; }
        GameInfo JoiningGame { get; set; }
        bool IsBusy { get; set; }
        Action<string> ShowError { get; }
    }
}
