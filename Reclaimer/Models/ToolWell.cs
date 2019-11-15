﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Reclaimer.Models
{
    public class ToolWell : TabWellBase
    {
        private bool isWindow;
        public bool IsWindow
        {
            get { return isWindow; }
            internal set { SetProperty(ref isWindow, value); }
        }

        private Dock dock;
        public Dock Dock
        {
            get { return dock; }
            internal set { SetProperty(ref dock, value); }
        }

        protected override void TogglePinStatusExecuted(TabItem _)
        {
            var container = ParentContainer; //keep an instance because it will get nulled at the end
            foreach (var item in Children.ToList())
            {
                Children.Remove(item);
                item.IsActive = true;

                if (Dock == Dock.Left)
                    container.LeftDockItems.Add(item);
                else if (Dock == Dock.Top)
                    container.TopDockItems.Add(item);
                else if (Dock == Dock.Right)
                    container.RightDockItems.Add(item);
                else if (Dock == Dock.Bottom)
                    container.BottomDockItems.Add(item);
            }
        }

        protected override void OnChildrenChanged()
        {
            if (Children.Count == 0)
                Remove();
        }

        internal void Remove()
        {
            if (ParentBrach != null)
                ParentBrach.Remove(this);
            else if (ParentContainer != null)
                ParentContainer.Content = null;
        }
    }
}
