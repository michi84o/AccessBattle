using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

// WPF ist just stupid for not letting me use the same snimation clock for all controls
// This is a workaround to synchronize the blinking of all fields.

namespace AccessBattleWpf
{
#pragma warning disable CC0022 // Should dispose object
    public class StoryboardAsyncWrapper
    {
        Storyboard _storyboard;
        FrameworkElement _element;

        public StoryboardAsyncWrapper(Storyboard storyboard, FrameworkElement element)
        {
            _storyboard = storyboard;
            _element = element;

            // Dummy task for continuation
            _managerTask = new Task(() => { });
            _managerTask.Start();
        }

        Task _managerTask;
        enum ManageAction { Add, Remove }
        List<ManageItem> _manageList = new List<ManageItem>();
        class ManageItem
        {
            public Timeline Timeline;
            public ManageAction Action;
            public ManageItem(Timeline timeline, ManageAction action)
            {
                Timeline = timeline;
                Action = action;
            }
        }

        void Manage()
        {
            int cnt = _manageList.Count;
            if (cnt == 0) return;            
            bool repeat = true;
            // Wait to collect all changes,
            // only continue if there was no change for 50ms
            while (repeat)
            {
                Thread.Sleep(10);
                lock (_manageList)
                {
                    if (cnt == _manageList.Count)
                        repeat = false;
                    else cnt = _manageList.Count;
                }
            }
            List<ManageItem> manageList;
            lock (_manageList)
            {
                // Copy all current items to a new list, then remove them from the list
                manageList = _manageList.ToList();
                _manageList.Clear();
            }
            Trace.WriteLine("Manage Storyboard ("+ manageList.Count + ")");
            // Disable the storyboard
            ExecuteUi(() => { _storyboard.Stop(_element); });
            Thread.Sleep(20); // Let the ui do its stuff
            // Execute all commands, then continue storyboard
            ExecuteUi(() => 
            {
                foreach (var item in manageList)
                {
                    if (item.Timeline == null) continue;
                    if (item.Action == ManageAction.Add)
                    {
                        try
                        {
                            if (!_storyboard.Children.Contains(item.Timeline))
                                _storyboard.Children.Add(item.Timeline);
                        }
                        catch { }
                    }
                    else
                    {
                        try { _storyboard.Children.Remove(item.Timeline); }
                        catch { }
                    }
                }
                if (_storyboard.Children.Count > 0)
                    _storyboard.Begin(_element, true);
            });
        }

        void ExecuteUi(Action action)
        {
            Application.Current.Dispatcher.Invoke(action, null);
        }

        public void AddAnimation(Timeline timeline)
        {
            lock (_manageList)
            {
                _manageList.Add(new ManageItem(timeline, ManageAction.Add));
            }
            _managerTask = _managerTask.ContinueWith((o) => Manage());
        }

        public void RemoveAnimation(Timeline timeline)
        {
            lock (_manageList)
            {
                _manageList.Add(new ManageItem(timeline, ManageAction.Remove));
            }
            _managerTask = _managerTask.ContinueWith((o) => Manage());
        }

    }
#pragma warning restore CC0022
}
