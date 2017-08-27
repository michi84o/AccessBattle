using AccessBattle.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AccessBattle.Wpf.ViewModel
{
    public class GameOverMenuViewModel : MenuViewModelBase
    {
        string _gameOverMessage;
        public string GameOverMessage
        {
            get { return _gameOverMessage; }
            set { SetProp(ref _gameOverMessage, value); }
        }

        public ICommand RematchCommand => new RelayCommand(o=>
        {
            // TODO: NOT IMPLEMENTED
        }, o=>{ return ParentViewModel.Game.JoinedGame && false; });

        public ICommand LeaveCommand => new RelayCommand(o =>
        {
            ParentViewModel.Game.Client.ExitGame(ParentViewModel.Game.UID);
            // TODO: Must be changed for singleplayer
            // ! UID is set to 0 when exit from server is received
            ParentViewModel.CurrentMenu = MenuType.NetworkGame;
        });

        public GameOverMenuViewModel(IMenuHolder parent) : base(parent) { }

        public override void Activate()
        {

        }

        public override void Suspend()
        {

        }
    }
}
