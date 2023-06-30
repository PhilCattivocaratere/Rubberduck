﻿using System.Collections.Generic;
using Rubberduck.Parsing.UIContext;

namespace Rubberduck.UI.Command.MenuItems.ParentMenus
{
    public class WindowParentMenu : ParentMenuItemBase
    {
        public WindowParentMenu(IEnumerable<IMenuItem> items, IUiDispatcher dispatcher)
            : base(dispatcher, "WindowMenu", items)
        {
        }

        public override int DisplayOrder => (int)RubberduckMenuItemDisplayOrder.Windows;
    }

    public enum WindowMenuItemDisplayOrder
    {
        CodeExplorer,
        CodeMetrics,
        TestExplorer,
        ToDoExplorer
    }
}
